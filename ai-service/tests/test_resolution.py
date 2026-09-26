import unittest
import os
import sys

root_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
if root_dir not in sys.path:
    sys.path.insert(0, root_dir)

from backend.AI.app.resolution.entity_matcher import EntityResolutionEngine

class TestEntityResolutionEngine(unittest.TestCase):
    def setUp(self):
        self.engine = EntityResolutionEngine()

    def test_exact_phone_match(self):
        ent_a = {"name": "Rahul Mehta", "type": "PERSON", "phone": "+919000001001"}
        ent_b = {"name": "R. Sharma", "type": "PERSON", "phone": "+919000001001"}
        result = self.engine.compare_entities(ent_a, ent_b)
        self.assertGreaterEqual(result["match_score"], 0.85)
        self.assertIn("SHARED_PHONE", [f["type"] for f in result["factors"]])
        self.assertEqual(result["model_version"], "1.0.0-deterministic-resolution")

    def test_incompatible_types_no_match(self):
        ent_a = {"name": "Rahul Mehta", "type": "PERSON"}
        ent_b = {"name": "MH12AB4321", "type": "VEHICLE"}
        result = self.engine.compare_entities(ent_a, ent_b)
        self.assertEqual(result["match_score"], 0.0)
        self.assertEqual(result["method"], "TYPE_MISMATCH")

    def test_name_alone_capped_false_positive_protection(self):
        ent_a = {"name": "Rahul Sharma", "type": "PERSON"}
        ent_b = {"name": "Rahul Sharma", "type": "PERSON"}
        result = self.engine.compare_entities(ent_a, ent_b)
        # False-positive protection caps score at <= 0.55 when only name matches
        self.assertLessEqual(result["match_score"], 0.55)

    def test_alias_match(self):
        ent_a = {"name": "Vikram Singhania", "type": "PERSON", "aliases": ["Vicky Cargo"]}
        ent_b = {"name": "Vicky Cargo", "type": "PERSON"}
        result = self.engine.compare_entities(ent_a, ent_b)
        self.assertIn("ALIAS_MATCH", [f["type"] for f in result["factors"]])

if __name__ == "__main__":
    unittest.main()
