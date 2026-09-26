import unittest
import os
import sys
import tempfile
from fastapi.testclient import TestClient

root_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
if root_dir not in sys.path:
    sys.path.insert(0, root_dir)

from backend.AI.app.main import app
from backend.AI.app.nlp.normalization import (
    normalize_phone,
    normalize_vehicle,
    normalize_person_name,
    normalize_email,
    normalize_account
)
from backend.AI.app.processors import (
    TextProcessor,
    CsvProcessor,
    JsonProcessor,
    PdfProcessor
)
from backend.AI.app.nlp.entity_extraction import EntityExtractor
from backend.AI.app.nlp.relationship_extraction import RelationshipExtractor

class TestAiExtractionPipeline(unittest.TestCase):
    def setUp(self):
        self.client = TestClient(app)
        self.entity_extractor = EntityExtractor()
        self.rel_extractor = RelationshipExtractor()

    def test_normalization(self):
        # 1. Phone normalization
        self.assertEqual(normalize_phone("98765 43210"), "+919876543210")
        self.assertEqual(normalize_phone("+91 9876543210"), "+919876543210")
        self.assertEqual(normalize_phone("091-9876543210"), "+919876543210")
        self.assertEqual(normalize_phone("09876543210"), "+919876543210")

        # 2. Vehicle normalization
        self.assertEqual(normalize_vehicle("MH 12 AB 1234"), "MH12AB1234")
        self.assertEqual(normalize_vehicle("mh-14-cd-7788"), "MH14CD7788")

        # 3. Person name normalization
        self.assertEqual(normalize_person_name("RAHUL KUMAR"), "Rahul Kumar")
        self.assertEqual(normalize_person_name("Rahul  Kumar"), "Rahul Kumar")
        self.assertEqual(normalize_person_name("Shri. Sameer Patil"), "Sameer Patil")

        # 4. Email and Account
        self.assertEqual(normalize_email("SUSPECT.MULE@DOMAIN.COM"), "suspect.mule@domain.com")
        self.assertEqual(normalize_account("SBIN0001234 - 98765432"), "SBIN000123498765432")

    def test_text_processor(self):
        proc = TextProcessor()
        sample = "Line 1: Statement of witness\nLine 2: Vehicle spotted near docks"
        records = proc.process_raw_text(sample)
        self.assertEqual(len(records), 2)
        self.assertEqual(records[0][0], 1)
        self.assertIn("Statement of witness", records[0][1])

    def test_csv_processor(self):
        with tempfile.NamedTemporaryFile("w", delete=False, suffix=".csv", encoding="utf-8") as f:
            f.write("caller,callee,duration,location\n")
            f.write("+919000001001,+919000001002,120,Pune Central\n")
            f.write("+919000001001,+919820011223,45,Mumbai\n")
            temp_path = f.name

        try:
            proc = CsvProcessor()
            records = proc.process_file(temp_path)
            self.assertEqual(len(records), 2)
            self.assertIn("caller: +919000001001", records[0][1])
            self.assertIn("location: Pune Central", records[0][1])
        finally:
            if os.path.exists(temp_path):
                os.remove(temp_path)

    def test_json_processor(self):
        with tempfile.NamedTemporaryFile("w", delete=False, suffix=".json", encoding="utf-8") as f:
            f.write('[{"transaction_id":"TXN-101","account":"SBIN000112233","amount":500000}]')
            temp_path = f.name

        try:
            proc = JsonProcessor()
            records = proc.process_file(temp_path)
            self.assertEqual(len(records), 1)
            self.assertIn("account: SBIN000112233", records[0][1])
        finally:
            if os.path.exists(temp_path):
                os.remove(temp_path)

    def test_pdf_processor(self):
        try:
            import pymupdf as fitz
        except ImportError:
            import fitz

        with tempfile.NamedTemporaryFile("wb", delete=False, suffix=".pdf") as f:
            temp_path = f.name
            f.write(b"") # ensure file exists

        try:
            doc = fitz.open()
            page = doc.new_page()
            page.insert_text((50, 72), "Investigation Report: Case MH-CYB-2026-0001.\nSubject Rahul Mehta was seen in vehicle MH12AB4321.")
            doc.save(temp_path)
            doc.close()

            proc = PdfProcessor()
            pages = proc.process_file(temp_path)
            self.assertEqual(len(pages), 1)
            self.assertEqual(pages[0][0], 1)
            self.assertIn("Rahul Mehta", pages[0][1])
            self.assertIn("MH12AB4321", pages[0][1])
        finally:
            if os.path.exists(temp_path):
                try:
                    os.remove(temp_path)
                except Exception:
                    pass

    def test_entity_extraction(self):
        text = "Subject Rahul Mehta with phone +91 9000001001 drove vehicle MH12AB4321 to Pune Central. Reference case FIR-2026-8812."
        entities = self.entity_extractor.extract_from_text(text, evidence_id="EVD-001", page_num=1)
        
        types = [e.type for e in entities]
        self.assertIn("PERSON", types)
        self.assertIn("PHONE", types)
        self.assertIn("VEHICLE", types)
        self.assertIn("LOCATION", types)
        self.assertIn("CASE", types)

        phone_ent = next(e for e in entities if e.type == "PHONE")
        self.assertEqual(phone_ent.normalized_value, "+919000001001")

        veh_ent = next(e for e in entities if e.type == "VEHICLE")
        self.assertEqual(veh_ent.normalized_value, "MH12AB4321")

    def test_relationship_extraction(self):
        text = "Rahul Mehta used vehicle MH12AB4321 during the cargo transit."
        entities = self.entity_extractor.extract_from_text(text, evidence_id="EVD-001", page_num=1)
        rels, events = self.rel_extractor.extract_from_sentences(text, entities, evidence_id="EVD-001", page_num=1)

        self.assertTrue(len(rels) >= 1)
        used_rel = rels[0]
        self.assertEqual(used_rel.relationship, "USED")
        self.assertIn("Page 1", used_rel.source_location)

    def test_api_endpoints(self):
        # 1. Health
        h_resp = self.client.get("/health")
        self.assertEqual(h_resp.status_code, 200)
        self.assertEqual(h_resp.json()["status"], "healthy")

        # 2. Ready
        r_resp = self.client.get("/ready")
        self.assertEqual(r_resp.status_code, 200)
        self.assertTrue(r_resp.json()["ready"])

        # 3. Process Evidence text
        payload = {
            "evidence_id": "EVD-TEST-99",
            "case_id": "CASE-2026-001",
            "text": "Subject Sameer Patil contacted Rahul Mehta on +919000001002 in Pune Central.",
            "mime_type": "text/plain"
        }
        p_resp = self.client.post("/api/v1/process/evidence", json=payload)
        self.assertEqual(p_resp.status_code, 200)
        data = p_resp.json()
        self.assertEqual(data["evidence_id"], "EVD-TEST-99")
        self.assertEqual(data["status"], "REVIEW_REQUIRED")
        self.assertTrue(len(data["entities"]) >= 2)

if __name__ == "__main__":
    unittest.main()
