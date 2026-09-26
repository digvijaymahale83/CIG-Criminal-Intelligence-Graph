import unittest
import sys
import os
import torch
import numpy as np

# Ensure ai-service is in sys.path
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from app.graph_ml.features import extract_node_features, FEATURE_DIM
from app.graph_ml.dataset import build_graph_tensors
from app.graph_ml.gat_model import GATNet, PurePyTorchGATConv
from app.graph_ml.inference import run_gat_inference, get_gat_model, are_entity_types_compatible
from app.graph_ml.training import sample_negative_edges, train_gat_link_prediction
from app.graph_ml.evaluation import evaluate_link_prediction
from fastapi.testclient import TestClient
from app.main import app


class TestGraphAttentionNetwork(unittest.TestCase):
    def setUp(self):
        # Deterministic sample investigation graph:
        # Community A: Person A (Rahul) -> Phone P1, Vehicle V1
        # Community B: Person B (Sameer) -> Phone P2
        # Bridge: Person C (Vikram) -> Person A, Person B
        self.sample_nodes = [
            {"id": "ent-1", "canonicalName": "Rahul Mehta", "type": "PERSON", "evidenceCount": 2},
            {"id": "ent-2", "canonicalName": "+919000001001", "type": "PHONE", "evidenceCount": 1},
            {"id": "ent-3", "canonicalName": "MH12AB4321", "type": "VEHICLE", "evidenceCount": 1},
            {"id": "ent-4", "canonicalName": "Vikram Gaikwad", "type": "PERSON", "evidenceCount": 1},
            {"id": "ent-5", "canonicalName": "Sameer Patil", "type": "PERSON", "evidenceCount": 3},
            {"id": "ent-6", "canonicalName": "+919111112222", "type": "PHONE", "evidenceCount": 1},
        ]
        self.sample_edges = [
            {"source": "ent-1", "target": "ent-2", "type": "COMMUNICATED_WITH"},
            {"source": "ent-1", "target": "ent-3", "type": "OPERATES"},
            {"source": "ent-1", "target": "ent-4", "type": "COMMUNICATED_WITH"}, # Rahul <-> Vikram
            {"source": "ent-4", "target": "ent-5", "type": "ASSOCIATE_OF"},        # Vikram <-> Sameer
            {"source": "ent-5", "target": "ent-6", "type": "COMMUNICATED_WITH"},
        ]
        self.client = TestClient(app)

    def test_1_feature_construction_dimensions(self):
        """1. Verify graph feature tensor construction has correct dimensions."""
        features, id_to_idx, idx_to_id = extract_node_features(self.sample_nodes, self.sample_edges)
        self.assertEqual(features.shape[0], len(self.sample_nodes))
        self.assertEqual(features.shape[1], FEATURE_DIM)
        self.assertEqual(len(id_to_idx), len(self.sample_nodes))
        self.assertEqual(len(idx_to_id), len(self.sample_nodes))

    def test_2_features_deterministic(self):
        """2. Verify feature values are deterministic given fixed graph metadata."""
        feat1, _, _ = extract_node_features(self.sample_nodes, self.sample_edges)
        feat2, _, _ = extract_node_features(self.sample_nodes, self.sample_edges)
        np.testing.assert_array_almost_equal(feat1, feat2)

    def test_3_gat_forward_pass(self):
        """3. Verify GAT model forward pass succeeds with PyTorch tensor inputs."""
        x, edge_index, _, _ = build_graph_tensors(self.sample_nodes, self.sample_edges)
        model = GATNet(in_features=FEATURE_DIM, hidden_dim=16, out_features=32, heads=2)
        model.eval()
        with torch.no_grad():
            embeddings, att = model(x, edge_index)
        self.assertIsNotNone(embeddings)
        self.assertEqual(embeddings.size(0), len(self.sample_nodes))

    def test_4_output_embedding_dimensions(self):
        """4. Verify GAT output embedding dimensions match configured size."""
        x, edge_index, _, _ = build_graph_tensors(self.sample_nodes, self.sample_edges)
        out_dim = 48
        model = GATNet(in_features=FEATURE_DIM, hidden_dim=16, out_features=out_dim, heads=2)
        with torch.no_grad():
            embeddings, _ = model(x, edge_index)
        self.assertEqual(embeddings.size(1), out_dim)

    def test_5_attention_coefficients_valid(self):
        """5. Verify attention coefficients are normalized and non-negative."""
        x, edge_index, _, _ = build_graph_tensors(self.sample_nodes, self.sample_edges)
        conv = PurePyTorchGATConv(in_features=FEATURE_DIM, out_features=16, heads=2)
        with torch.no_grad():
            out, (full_edge_index, alpha) = conv(x, edge_index)
        self.assertTrue(torch.all(alpha >= 0.0))
        self.assertTrue(torch.all(alpha <= 1.0001))

    def test_6_candidate_generation_two_hop(self):
        """6. Verify link prediction generates candidate pairs for 2-hop neighbors (e.g. ent-1 and ent-5 via ent-4)."""
        result = run_gat_inference(self.sample_nodes, self.sample_edges, candidate_threshold=0.30)
        leads = result["leads"]
        self.assertTrue(len(leads) > 0)
        # Check if 2-hop pair (ent-1, ent-5) connected through ent-4 was surfaced
        pair_found = any(
            (l["sourceEntityId"] == "ent-1" and l["targetEntityId"] == "ent-5") or
            (l["sourceEntityId"] == "ent-5" and l["targetEntityId"] == "ent-1")
            for l in leads
        )
        self.assertTrue(pair_found, "2-hop bridge candidates must be generated")

    def test_7_self_loops_prevented(self):
        """7. Verify self-loops (u == v) are strictly prevented in link candidate generation."""
        result = run_gat_inference(self.sample_nodes, self.sample_edges, candidate_threshold=0.10)
        for lead in result["leads"]:
            self.assertNotEqual(lead["sourceEntityId"], lead["targetEntityId"])

    def test_8_existing_edges_excluded(self):
        """8. Verify existing verified edges are excluded from candidate link predictions."""
        result = run_gat_inference(self.sample_nodes, self.sample_edges, candidate_threshold=0.10)
        existing_pairs = {("ent-1", "ent-2"), ("ent-1", "ent-3"), ("ent-1", "ent-4"), ("ent-4", "ent-5"), ("ent-5", "ent-6")}
        for lead in result["leads"]:
            pair1 = (lead["sourceEntityId"], lead["targetEntityId"])
            pair2 = (lead["targetEntityId"], lead["sourceEntityId"])
            self.assertNotIn(pair1, existing_pairs)
            self.assertNotIn(pair2, existing_pairs)

    def test_9_entity_type_compatibility(self):
        """9. Verify entity-type compatibility rules are respected."""
        self.assertTrue(are_entity_types_compatible("PERSON", "PERSON"))
        self.assertTrue(are_entity_types_compatible("PERSON", "PHONE"))
        self.assertTrue(are_entity_types_compatible("PERSON", "VEHICLE"))
        self.assertFalse(are_entity_types_compatible("DOCUMENT", "EVENT"))

    def test_10_negative_sampling(self):
        """10. Verify negative sampling generates valid non-edges."""
        x, edge_index, _, _ = build_graph_tensors(self.sample_nodes, self.sample_edges)
        neg_edges = sample_negative_edges(num_nodes=len(self.sample_nodes), pos_edge_index=edge_index, num_neg_samples=4)
        self.assertEqual(neg_edges.size(0), 2)
        self.assertTrue(neg_edges.size(1) > 0)
        # Ensure none of negative edges are in positive edge index
        pos_set = set(zip(edge_index[0].tolist(), edge_index[1].tolist()))
        for i in range(neg_edges.size(1)):
            neg_pair = (int(neg_edges[0, i]), int(neg_edges[1, i]))
            self.assertNotIn(neg_pair, pos_set)

    def test_11_link_scoring_deterministic(self):
        """11. Verify link scoring is deterministic for identical graph inputs."""
        res1 = run_gat_inference(self.sample_nodes, self.sample_edges)
        res2 = run_gat_inference(self.sample_nodes, self.sample_edges)
        self.assertEqual(len(res1["leads"]), len(res2["leads"]))
        if len(res1["leads"]) > 0:
            self.assertEqual(res1["leads"][0]["score"], res2["leads"][0]["score"])

    def test_12_empty_graph_handling(self):
        """12. Verify model handles empty graph gracefully without crashing."""
        result = run_gat_inference([], [])
        self.assertEqual(result["nodeCount"], 0)
        self.assertEqual(result["edgeCount"], 0)
        self.assertEqual(len(result["leads"]), 0)

    def test_13_single_node_graph_handling(self):
        """13. Verify model handles single-node graph gracefully."""
        single_node = [{"id": "ent-single", "canonicalName": "Solo", "type": "PERSON"}]
        result = run_gat_inference(single_node, [])
        self.assertEqual(result["nodeCount"], 1)
        self.assertEqual(len(result["leads"]), 0)

    def test_14_disconnected_subgraphs(self):
        """14. Verify model handles disconnected subgraphs correctly."""
        nodes = [
            {"id": "a1", "type": "PERSON"},
            {"id": "a2", "type": "PHONE"},
            {"id": "b1", "type": "PERSON"},
            {"id": "b2", "type": "VEHICLE"}
        ]
        edges = [
            {"source": "a1", "target": "a2", "type": "COMMUNICATED_WITH"},
            {"source": "b1", "target": "b2", "type": "OPERATES"}
        ]
        result = run_gat_inference(nodes, edges)
        self.assertEqual(result["nodeCount"], 4)
        self.assertEqual(result["edgeCount"], 2)

    def test_15_model_version_metadata(self):
        """15. Verify model version metadata is returned in response."""
        result = run_gat_inference(self.sample_nodes, self.sample_edges)
        self.assertEqual(result["modelVersion"], "GAT-v1.0.0")

    def test_16_fastapi_endpoint(self):
        """16. Verify FastAPI endpoint /api/v1/graph/gat-inference returns structured payload."""
        payload = {
            "caseId": "CASE-TEST-001",
            "nodes": self.sample_nodes,
            "edges": self.sample_edges,
            "candidateThreshold": 0.30,
            "maxCandidates": 10
        }
        response = self.client.post("/api/v1/graph/gat-inference", json=payload)
        self.assertEqual(response.status_code, 200)
        data = response.json()
        self.assertEqual(data["modelVersion"], "GAT-v1.0.0")
        self.assertEqual(data["nodeCount"], len(self.sample_nodes))
        self.assertEqual(data["edgeCount"], len(self.sample_edges))
        self.assertIn("embeddings", data)
        self.assertIn("leads", data)
        if len(data["leads"]) > 0:
            lead = data["leads"][0]
            self.assertIn("sourceEntityId", lead)
            self.assertIn("targetEntityId", lead)
            self.assertIn("score", lead)
            self.assertIn("signals", lead)
            self.assertIn("cosineSimilarity", lead["signals"])
            self.assertIn("contributingSignals", lead["signals"])


if __name__ == "__main__":
    unittest.main()
