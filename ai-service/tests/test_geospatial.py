import unittest
from app.geo.spatial_clustering import (
    haversine_distance_km,
    is_valid_coordinate,
    calculate_speed_kmh,
    is_implausible_velocity,
    cluster_locations_by_proximity,
    EARTH_RADIUS_KM
)

class TestGeospatialIntelligence(unittest.TestCase):
    def test_coordinate_validation_valid(self):
        self.assertTrue(is_valid_coordinate(18.5204, 73.8567)) # Pune
        self.assertTrue(is_valid_coordinate(18.9438, 72.8387)) # Mumbai
        self.assertTrue(is_valid_coordinate(0.0, 0.0))         # Equator / Prime Meridian
        self.assertTrue(is_valid_coordinate(-90.0, -180.0))    # Min bounds
        self.assertTrue(is_valid_coordinate(90.0, 180.0))      # Max bounds

    def test_coordinate_validation_invalid(self):
        self.assertFalse(is_valid_coordinate(91.0, 73.8567))    # Lat > 90
        self.assertFalse(is_valid_coordinate(-90.1, 73.8567))   # Lat < -90
        self.assertFalse(is_valid_coordinate(18.5204, 180.5))   # Lon > 180
        self.assertFalse(is_valid_coordinate(18.5204, -181.0))  # Lon < -180
        self.assertFalse(is_valid_coordinate(float('nan'), 0.0))# NaN
        self.assertFalse(is_valid_coordinate(0.0, float('inf')))# Inf

    def test_haversine_distance_identical_points(self):
        dist = haversine_distance_km(18.5204, 73.8567, 18.5204, 73.8567)
        self.assertEqual(dist, 0.0)

    def test_haversine_distance_pune_to_mumbai(self):
        # Pune (18.5204, 73.8567) to Mumbai Port Trust (18.9438, 72.8387)
        dist = haversine_distance_km(18.5204, 73.8567, 18.9438, 72.8387)
        # Expected great circle distance is between 110 km and 130 km
        self.assertGreater(dist, 115.0)
        self.assertLess(dist, 125.0)

    def test_speed_calculation(self):
        # 120 km in 2 hours -> 60 km/h
        speed = calculate_speed_kmh(120.0, 2.0)
        self.assertAlmostEqual(speed, 60.0, places=2)

        # Zero or negative hours handling
        self.assertEqual(calculate_speed_kmh(100.0, 0.0), 0.0)

    def test_implausible_velocity_detection(self):
        # 120 km in 2 hours -> normal (not implausible)
        is_implausible, speed = is_implausible_velocity(120.0, 2.0)
        self.assertFalse(is_implausible)
        self.assertAlmostEqual(speed, 60.0, places=1)

        # 1500 km in 0.5 hours -> 3000 km/h (> 900 km/h threshold)
        is_implausible, speed = is_implausible_velocity(1500.0, 0.5)
        self.assertTrue(is_implausible)
        self.assertAlmostEqual(speed, 3000.0, places=1)

    def test_proximity_clustering(self):
        locations = [
            {"id": "loc-1", "name": "Pune Shivajinagar", "latitude": 18.5314, "longitude": 73.8446},
            {"id": "loc-2", "name": "Pune Camp", "latitude": 18.5167, "longitude": 73.8750}, # ~4 km away from loc-1
            {"id": "loc-3", "name": "Mumbai Port", "latitude": 18.9438, "longitude": 72.8387} # ~120 km away
        ]

        clusters = cluster_locations_by_proximity(locations, radius_km=25.0)
        # Expect 2 clusters: one containing Pune points (2 locations), one containing Mumbai (1 location)
        self.assertEqual(len(clusters), 2)
        pune_cluster = next(c for c in clusters if c["location_count"] == 2)
        mumbai_cluster = next(c for c in clusters if c["location_count"] == 1)

        self.assertIn("Shivajinagar", pune_cluster["cluster_label"])
        self.assertIn("Mumbai Port", mumbai_cluster["cluster_label"])

if __name__ == "__main__":
    unittest.main()
