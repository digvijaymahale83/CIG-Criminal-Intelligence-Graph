"""
Deterministic feature engineering for graph neural network (GAT) models.
Extracts fixed-dimension numerical tensors from verified graph entities and relationships.
"""
from typing import List, Dict, Any, Tuple
import numpy as np

CANONICAL_TYPES = [
    "PERSON", "PHONE", "VEHICLE", "LOCATION", "ORGANIZATION",
    "ACCOUNT", "DEVICE", "EVENT", "DOCUMENT"
]
TYPE_TO_IDX = {t: i for i, t in enumerate(CANONICAL_TYPES)}
NUM_TYPE_FEATURES = len(CANONICAL_TYPES) # 9

# Feature dimension: 9 (type one-hot) + 1 (norm degree) + 2 (in/out ratio) + 4 (neighborhood type ratios) + 1 (evidence) + 1 (temporal/events) + 1 (centrality proxy) = 19
FEATURE_DIM = NUM_TYPE_FEATURES + 10


def extract_node_features(
    nodes: List[Dict[str, Any]], 
    edges: List[Dict[str, Any]]
) -> Tuple[np.ndarray, Dict[str, int], Dict[int, str]]:
    """
    Extracts a deterministic float32 feature matrix X in R^(N x FEATURE_DIM).
    
    Args:
        nodes: List of node dicts with 'id', 'type', and optional 'properties'.
        edges: List of edge dicts with 'source', 'target', 'type'.
        
    Returns:
        (feature_matrix, id_to_idx, idx_to_id)
    """
    n = len(nodes)
    if n == 0:
        return np.zeros((0, FEATURE_DIM), dtype=np.float32), {}, {}

    id_to_idx: Dict[str, int] = {node["id"]: i for i, node in enumerate(nodes)}
    idx_to_id: Dict[int, str] = {i: node["id"] for i, node in enumerate(nodes)}

    # Graph degree and neighbor analysis
    in_degrees = np.zeros(n, dtype=np.float32)
    out_degrees = np.zeros(n, dtype=np.float32)
    neighbor_types: Dict[int, List[str]] = {i: [] for i in range(n)}

    for edge in edges:
        s_id = edge.get("source") or edge.get("sourceEntityId") or edge.get("Source")
        t_id = edge.get("target") or edge.get("targetEntityId") or edge.get("Target")
        if s_id in id_to_idx and t_id in id_to_idx:
            s_idx = id_to_idx[s_id]
            t_idx = id_to_idx[t_id]
            out_degrees[s_idx] += 1.0
            in_degrees[t_idx] += 1.0

            s_type = (nodes[s_idx].get("type") or "UNKNOWN").upper()
            t_type = (nodes[t_idx].get("type") or "UNKNOWN").upper()
            neighbor_types[s_idx].append(t_type)
            neighbor_types[t_idx].append(s_type)

    total_degrees = in_degrees + out_degrees
    max_deg = max(1.0, float(n - 1))

    features = np.zeros((n, FEATURE_DIM), dtype=np.float32)

    for i, node in enumerate(nodes):
        # 1. Entity type one-hot (9 dims)
        ent_type = (node.get("type") or "UNKNOWN").upper()
        if ent_type in TYPE_TO_IDX:
            features[i, TYPE_TO_IDX[ent_type]] = 1.0

        offset = NUM_TYPE_FEATURES

        # 2. Normalized total degree (1 dim)
        features[i, offset] = float(total_degrees[i]) / max_deg
        offset += 1

        # 3. In-degree and out-degree proportions (2 dims)
        deg = total_degrees[i]
        if deg > 0:
            features[i, offset] = in_degrees[i] / deg
            features[i, offset + 1] = out_degrees[i] / deg
        offset += 2

        # 4. Neighborhood entity type ratios: PHONE, VEHICLE, LOCATION, ACCOUNT (4 dims)
        nbrs = neighbor_types[i]
        nbr_count = max(1, len(nbrs))
        features[i, offset] = sum(1 for t in nbrs if "PHONE" in t) / float(nbr_count)
        features[i, offset + 1] = sum(1 for t in nbrs if "VEHICLE" in t) / float(nbr_count)
        features[i, offset + 2] = sum(1 for t in nbrs if "LOCATION" in t) / float(nbr_count)
        features[i, offset + 3] = sum(1 for t in nbrs if "ACCOUNT" in t) / float(nbr_count)
        offset += 4

        # 5. Evidence count (1 dim)
        ev_count = float(node.get("evidenceCount") or node.get("EvidenceCount") or 1.0)
        features[i, offset] = min(1.0, ev_count / 10.0)
        offset += 1

        # 6. Temporal / event activity count (1 dim)
        props = node.get("properties") or {}
        event_count = float(props.get("eventCount", 0.0) or props.get("temporalActivityCount", 0.0))
        features[i, offset] = min(1.0, event_count / 10.0)
        offset += 1

        # 7. Centrality proxy (1 dim)
        centrality = float(props.get("betweenness", 0.0) or props.get("centrality", 0.0))
        features[i, offset] = min(1.0, max(0.0, centrality))

    return features, id_to_idx, idx_to_id
