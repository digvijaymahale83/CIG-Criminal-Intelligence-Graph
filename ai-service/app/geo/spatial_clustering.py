import math
from typing import List, Dict, Any, Tuple

EARTH_RADIUS_KM = 6371.0

def haversine_distance_km(lat1: float, lon1: float, lat2: float, lon2: float) -> float:
    """
    Computes exact great-circle distance between two coordinates in kilometers.
    """
    if abs(lat1 - lat2) < 1e-9 and abs(lon1 - lon2) < 1e-9:
        return 0.0

    d_lat = math.radians(lat2 - lat1)
    d_lon = math.radians(lon2 - lon1)

    r_lat1 = math.radians(lat1)
    r_lat2 = math.radians(lat2)

    a = (math.sin(d_lat / 2.0) ** 2 +
         math.cos(r_lat1) * math.cos(r_lat2) * (math.sin(d_lon / 2.0) ** 2))

    c = 2.0 * math.atan2(math.sqrt(a), math.sqrt(1.0 - a))
    return EARTH_RADIUS_KM * c

def is_valid_coordinate(latitude: float, longitude: float) -> bool:
    """
    Validates latitude in [-90, 90] and longitude in [-180, 180].
    """
    if math.isnan(latitude) or math.isinf(latitude) or math.isnan(longitude) or math.isinf(longitude):
        return False
    return -90.0 <= latitude <= 90.0 and -180.0 <= longitude <= 180.0

def calculate_speed_kmh(distance_km: float, duration_hours: float) -> float:
    """
    Computes average velocity in km/h.
    """
    if duration_hours <= 0.0001:
        return 0.0
    return distance_km / duration_hours

def is_implausible_velocity(distance_km: float, duration_hours: float, threshold_kmh: float = 900.0) -> Tuple[bool, float]:
    """
    Checks if a transition between two events implies physically implausible travel velocity.
    """
    speed = calculate_speed_kmh(distance_km, duration_hours)
    is_implausible = distance_km > 100.0 and duration_hours > 0.0 and speed > threshold_kmh
    return is_implausible, speed

def cluster_locations_by_proximity(
    locations: List[Dict[str, Any]],
    radius_km: float = 25.0
) -> List[Dict[str, Any]]:
    """
    Groups nearby geographic points into regional clusters using a deterministic proximity threshold.
    """
    clusters = []
    visited = set()
    cluster_counter = 1

    for loc in locations:
        loc_id = loc.get("id", str(loc))
        if loc_id in visited:
            continue

        cluster_members = [loc]
        visited.add(loc_id)

        lat1, lon1 = float(loc["latitude"]), float(loc["longitude"])

        for other in locations:
            other_id = other.get("id", str(other))
            if other_id in visited:
                continue

            lat2, lon2 = float(other["latitude"]), float(other["longitude"])
            dist = haversine_distance_km(lat1, lon1, lat2, lon2)

            if dist <= radius_km:
                cluster_members.append(other)
                visited.add(other_id)

        avg_lat = sum(float(m["latitude"]) for m in cluster_members) / len(cluster_members)
        avg_lon = sum(float(m["longitude"]) for m in cluster_members) / len(cluster_members)

        clusters.append({
            "cluster_id": f"cluster-geo-{cluster_counter}",
            "cluster_label": f"{cluster_members[0].get('name', 'Cluster')} Region ({len(cluster_members)} Locations)",
            "centroid_latitude": round(avg_lat, 4),
            "centroid_longitude": round(avg_lon, 4),
            "location_count": len(cluster_members),
            "members": cluster_members
        })
        cluster_counter += 1

    return clusters
