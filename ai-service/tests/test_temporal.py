import unittest
import sys
import os
from datetime import datetime, timezone

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from app.nlp.temporal_normalization import TemporalNormalizer

class TestTemporalNormalization(unittest.TestCase):
    def setUp(self):
        self.normalizer = TemporalNormalizer()

    def test_1_exact_iso_timestamp(self):
        text = "Meeting recorded at 2026-03-12T14:30:15Z at checkpoint."
        res = self.normalizer.normalize(text)
        self.assertEqual(res["time_precision"], "EXACT")
        self.assertIn("2026-03-12T14:30:15", res["start_time_utc"])
        self.assertEqual(res["start_time_utc"], res["end_time_utc"])
        self.assertFalse(res["is_approximate"])
        self.assertGreaterEqual(res["confidence"], 0.95)

    def test_2_minute_precision_timestamp(self):
        text = "Observed arrival at 12/03/2026 10:15 near warehouse."
        res = self.normalizer.normalize(text)
        self.assertEqual(res["time_precision"], "MINUTE")
        self.assertIn("2026-03-12T10:15:00", res["start_time_utc"])
        self.assertFalse(res["is_approximate"])

    def test_3_standard_date_only_text(self):
        text = "The transaction occurred on 12 March 2026."
        res = self.normalizer.normalize(text)
        self.assertEqual(res["time_precision"], "DATE_ONLY")
        self.assertIn("2026-03-12T00:00:00", res["start_time_utc"])
        self.assertIn("2026-03-12T23:59:59", res["end_time_utc"])

    def test_4_standard_date_only_numeric(self):
        text = "Filing dated 15/04/2026 was received."
        res = self.normalizer.normalize(text)
        self.assertEqual(res["time_precision"], "DATE_ONLY")
        self.assertIn("2026-04-15T00:00:00", res["start_time_utc"])

    def test_5_date_range(self):
        text = "Activity observed between 10 and 12 March 2026."
        res = self.normalizer.normalize(text)
        self.assertEqual(res["time_precision"], "DAY")
        self.assertIn("2026-03-10T00:00:00", res["start_time_utc"])
        self.assertIn("2026-03-12T23:59:59", res["end_time_utc"])

    def test_6_approximate_period_evening(self):
        text = "Surveillance noted arrival on the evening of 12 March 2026."
        res = self.normalizer.normalize(text)
        self.assertEqual(res["time_precision"], "HOUR")
        self.assertTrue(res["is_approximate"])
        self.assertIn("2026-03-12T18:00:00", res["start_time_utc"])
        self.assertIn("2026-03-12T22:00:00", res["end_time_utc"])

    def test_7_month_year(self):
        text = "Account was opened in March 2026."
        res = self.normalizer.normalize(text)
        self.assertEqual(res["time_precision"], "MONTH")
        self.assertIn("2026-03-01T00:00:00", res["start_time_utc"])

    def test_8_time_window_range(self):
        text = "Suspect present between 10:00 and 11:30."
        ref_date = datetime(2026, 3, 10, tzinfo=timezone.utc)
        res = self.normalizer.normalize(text, reference_date=ref_date)
        self.assertEqual(res["time_precision"], "MINUTE")
        self.assertIn("2026-03-10T10:00:00", res["start_time_utc"])
        self.assertIn("2026-03-10T11:30:00", res["end_time_utc"])

    def test_9_relative_date_with_reference(self):
        text = "Second visit occurred three days later."
        ref_date = datetime(2026, 3, 10, tzinfo=timezone.utc)
        res = self.normalizer.normalize(text, reference_date=ref_date)
        self.assertEqual(res["time_precision"], "DAY")
        self.assertTrue(res["is_approximate"])
        self.assertIn("2026-03-13T00:00:00", res["start_time_utc"])

    def test_10_relative_date_unresolved(self):
        text = "Second visit occurred three days later."
        res = self.normalizer.normalize(text, reference_date=None)
        self.assertEqual(res["time_precision"], "APPROXIMATE")
        self.assertTrue(res["is_approximate"])
        self.assertIsNone(res["start_time_utc"])

    def test_11_empty_and_whitespace(self):
        res1 = self.normalizer.normalize("")
        self.assertEqual(res1["time_precision"], "UNKNOWN")
        self.assertIsNone(res1["start_time_utc"])

        res2 = self.normalizer.normalize("   ")
        self.assertEqual(res2["time_precision"], "UNKNOWN")

    def test_12_non_temporal_text(self):
        text = "Blue sedan registered in Maharashtra."
        res = self.normalizer.normalize(text)
        self.assertEqual(res["time_precision"], "UNKNOWN")
        self.assertIsNone(res["start_time_utc"])

if __name__ == "__main__":
    unittest.main()
