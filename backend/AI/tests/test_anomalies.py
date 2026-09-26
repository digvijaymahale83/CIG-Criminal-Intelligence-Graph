import unittest
import sys
import os
from datetime import datetime, timezone, timedelta

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from app.analytics.anomaly_detector import (
    haversine_distance_km,
    detect_network_degree_anomalies,
    detect_temporal_burst_anomalies,
    detect_velocity_anomalies,
    detect_activity_spike_anomalies,
    calculate_anomaly_priority,
    generate_deduplication_fingerprint
)


class TestAnomalyDetection(unittest.TestCase):

    def test_haversine_distance_calculation(self):
        # Pune (18.5204, 73.8567) to Mumbai (18.9220, 72.8347) is ~120 km
        dist = haversine_distance_km(18.5204, 73.8567, 18.9220, 72.8347)
        self.assertGreater(dist, 110.0)
        self.assertLess(dist, 140.0)

    def test_network_degree_anomalies(self):
        nodes = [
            {"id": "n1", "name": "Rahul Mehta"},
            {"id": "n2", "name": "Associate A"},
            {"id": "n3", "name": "Associate B"},
            {"id": "n4", "name": "Associate C"},
            {"id": "n5", "name": "Associate D"},
            {"id": "n6", "name": "Associate E"},
        ]
        # n1 has 5 connections, others have 1 -> mean is (5 + 1 + 1 + 1 + 1 + 1)/6 = 1.67
        edges = [
            {"source_id": "n1", "target_id": "n2"},
            {"source_id": "n1", "target_id": "n3"},
            {"source_id": "n1", "target_id": "n4"},
            {"source_id": "n1", "target_id": "n5"},
            {"source_id": "n1", "target_id": "n6"},
        ]
        anomalies = detect_network_degree_anomalies(nodes, edges, multiplier_threshold=2.0, min_absolute_degree=5)
        self.assertEqual(len(anomalies), 1)
        self.assertEqual(anomalies[0]["entity_id"], "n1")
        self.assertEqual(anomalies[0]["alert_type"], "NETWORK_ANOMALY")
        self.assertGreaterEqual(anomalies[0]["score"], 0.70)

    def test_temporal_burst_anomalies(self):
        base_time = datetime(2026, 3, 10, 8, 0, 0, tzinfo=timezone.utc)
        events = [
            {"id": "e1", "related_entities": ["Rahul Mehta"], "start_time_utc": (base_time + timedelta(hours=1)).isoformat()},
            {"id": "e2", "related_entities": ["Rahul Mehta"], "start_time_utc": (base_time + timedelta(hours=4)).isoformat()},
            {"id": "e3", "related_entities": ["Rahul Mehta"], "start_time_utc": (base_time + timedelta(hours=12)).isoformat()},
            {"id": "e4", "related_entities": ["Rahul Mehta"], "start_time_utc": (base_time + timedelta(hours=20)).isoformat()},
            {"id": "e5", "related_entities": ["Rahul Mehta"], "start_time_utc": (base_time + timedelta(hours=100)).isoformat()},
        ]
        anomalies = detect_temporal_burst_anomalies(events, window_hours=48, burst_threshold=4)
        self.assertEqual(len(anomalies), 1)
        self.assertEqual(anomalies[0]["entity_name"], "Rahul Mehta")
        self.assertEqual(anomalies[0]["event_count"], 4)
        self.assertEqual(anomalies[0]["alert_type"], "TEMPORAL_ANOMALY")

    def test_velocity_anomalies(self):
        # 120 km in 5 minutes (0.0833 hours) -> ~1440 km/h (implausible supersonic velocity)
        t1 = datetime(2026, 3, 10, 10, 0, 0, tzinfo=timezone.utc)
        t2 = t1 + timedelta(minutes=5)
        movements = [
            {"entity_name": "Target Alpha", "latitude": 18.5204, "longitude": 73.8567, "timestamp": t1.isoformat()},
            {"entity_name": "Target Alpha", "latitude": 18.9220, "longitude": 72.8347, "timestamp": t2.isoformat()}
        ]
        anomalies = detect_velocity_anomalies(movements, speed_threshold_kmh=800.0)
        self.assertEqual(len(anomalies), 1)
        self.assertEqual(anomalies[0]["alert_type"], "UNUSUAL_TRAVEL")
        self.assertGreater(anomalies[0]["implied_speed_kmh"], 1000.0)
        self.assertEqual(anomalies[0]["severity"], "HIGH")

    def test_activity_spike_anomalies(self):
        daily_counts = {
            "2026-03-01": 2,
            "2026-03-02": 3,
            "2026-03-03": 2,
            "2026-03-04": 2,
            "2026-03-05": 18,  # Spike
            "2026-03-06": 3
        }
        anomalies = detect_activity_spike_anomalies(daily_counts, z_threshold=2.0, min_count=5)
        self.assertEqual(len(anomalies), 1)
        self.assertEqual(anomalies[0]["date"], "2026-03-05")
        self.assertEqual(anomalies[0]["count"], 18)
        self.assertGreater(anomalies[0]["z_score"], 2.0)
        self.assertEqual(anomalies[0]["alert_type"], "ACTIVITY_SPIKE")

    def test_priority_calculation_bounded_and_weighted(self):
        # Minimal score, no evidence, no cross-case: 0.4*0 + 0.25*0 + 0.20*0 + 0.15 = 0.15
        p_min = calculate_anomaly_priority(0.0, has_evidence=False, is_cross_case=False)
        self.assertAlmostEqual(p_min, 0.15, places=3)

        # Maximal score, with evidence, with cross-case: 0.4*1.0 + 0.25*1.0 + 0.20*1.0 + 0.15 = 1.0
        p_max = calculate_anomaly_priority(1.0, has_evidence=True, is_cross_case=True)
        self.assertAlmostEqual(p_max, 1.0, places=3)

        # Typical case: score=0.8, evidence=True, cross=False -> 0.32 + 0.25 + 0 + 0.15 = 0.72
        p_typical = calculate_anomaly_priority(0.8, has_evidence=True, is_cross_case=False)
        self.assertAlmostEqual(p_typical, 0.72, places=3)

    def test_deduplication_fingerprint(self):
        fp1 = generate_deduplication_fingerprint("NETWORK_ANOMALY", "e1", "e2", "w1", "loc1")
        fp2 = generate_deduplication_fingerprint("NETWORK_ANOMALY", "e1", "e2", "w1", "loc1")
        fp3 = generate_deduplication_fingerprint("NETWORK_ANOMALY", "e1", "e3", "w1", "loc1")

        self.assertTrue(fp1.startswith("SHA256:"))
        self.assertEqual(fp1, fp2)
        self.assertNotEqual(fp1, fp3)

    def test_strict_neutral_terminology(self):
        # Verify no biased or prejudicial terms are outputted
        nodes = [{"id": "n1", "name": "Rahul Mehta"}]
        edges = [{"source_id": "n1", "target_id": "n1"}]
        anomalies = detect_network_degree_anomalies(nodes, edges, min_absolute_degree=1)
        for a in anomalies:
            text = f"{a['title']} {a['description']} {a['explanation']}".lower()
            self.assertNotIn("criminal", text)
            self.assertNotIn("guilty", text)
            self.assertNotIn("kingpin", text)
            self.assertNotIn("perpetrator", text)


if __name__ == "__main__":
    unittest.main()
