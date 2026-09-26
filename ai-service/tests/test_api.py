import unittest
from fastapi.testclient import TestClient
import sys
import os

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from app.main import app

class TestAiServiceApi(unittest.TestCase):
    def setUp(self):
        self.client = TestClient(app)

    def test_health_endpoint(self):
        response = self.client.get("/health")
        self.assertEqual(response.status_code, 200)
        data = response.json()
        self.assertEqual(data["status"], "healthy")
        self.assertIn("version", data)
        self.assertIn("governance_notice", data)

    def test_ready_endpoint(self):
        response = self.client.get("/ready")
        self.assertEqual(response.status_code, 200)
        data = response.json()
        self.assertEqual(data["status"], "ready")
        self.assertTrue(data["ready"])
        self.assertIn("checked_at_utc", data)

    def test_process_evidence_real_extraction(self):
        payload = {
            "evidence_id": "EVD-2026-TEST-001",
            "case_id": "CASE-001",
            "text": "Subject Rahul Mehta used vehicle MH12AB4321 to visit Pune Central. Contacted on +919000001001.",
            "mime_type": "text/plain"
        }
        response = self.client.post("/api/v1/process/evidence", json=payload)
        self.assertEqual(response.status_code, 200)
        data = response.json()
        self.assertEqual(data["evidence_id"], "EVD-2026-TEST-001")
        self.assertEqual(data["status"], "REVIEW_REQUIRED")
        
        entities = data["entities"]
        self.assertTrue(len(entities) >= 3)
        types = [e["type"] for e in entities]
        self.assertIn("PERSON", types)
        self.assertIn("VEHICLE", types)
        self.assertIn("PHONE", types)

        relationships = data["relationships"]
        self.assertTrue(len(relationships) >= 1)
        self.assertEqual(relationships[0]["relationship"], "USED")

if __name__ == "__main__":
    unittest.main()
