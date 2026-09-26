"""
Anomaly Detection & Signal Generation Module
Phase 8: Criminal Intelligence & Network Investigation Platform

Provides algorithmic anomaly detection algorithms across:
- Graph topology (degree disparity)
- Temporal event distributions (bursts and rolling windows)
- Geospatial transitions (implausible velocity)
- Activity aggregation (z-score spikes)
- Explainable priority scoring and SHA-256 fingerprinting

NOTE: All surfaced signals represent investigative indicators requiring review,
NOT proof of criminal activity. Neutral investigative terminology is strictly enforced.
"""

from typing import List, Dict, Any, Optional, Tuple
from datetime import datetime, timezone
import math
import hashlib
import json


def haversine_distance_km(lat1: float, lon1: float, lat2: float, lon2: float) -> float:
    """Calculates great-circle distance between two geodetic coordinates in km."""
    r = 6371.0
    d_lat = math.radians(lat2 - lat1)
    d_lon = math.radians(lon2 - lon1)
    a = (math.sin(d_lat / 2.0) ** 2 +
         math.cos(math.radians(lat1)) * math.cos(math.radians(lat2)) *
         math.sin(d_lon / 2.0) ** 2)
    c = 2.0 * math.atan2(math.sqrt(a), math.sqrt(1.0 - a))
    return r * c


def detect_network_degree_anomalies(
    nodes: List[Dict[str, Any]],
    edges: List[Dict[str, Any]],
    multiplier_threshold: float = 2.5,
    min_absolute_degree: int = 5
) -> List[Dict[str, Any]]:
    """
    Identifies graph entities exhibiting disproportionate degree connectivity.
    Investigative terminology: High Connectivity Nexus Entity.
    """
    degree_counts: Dict[str, int] = {n.get("id", ""): 0 for n in nodes}
    for edge in edges:
        s = edge.get("source_id") or edge.get("source") or ""
        t = edge.get("target_id") or edge.get("target") or ""
        if s in degree_counts:
            degree_counts[s] += 1
        if t in degree_counts:
            degree_counts[t] += 1

    degrees = list(degree_counts.values())
    if not degrees or sum(degrees) == 0:
        return []

    mean_degree = sum(degrees) / len(degrees)
    anomalies: List[Dict[str, Any]] = []

    for node in nodes:
        node_id = node.get("id", "")
        deg = degree_counts.get(node_id, 0)
        canonical_name = node.get("name") or node.get("canonical_name") or node_id

        if deg >= min_absolute_degree and (deg >= mean_degree * multiplier_threshold or deg >= 10):
            score = min(0.95, round(0.60 + (deg / max(1.0, mean_degree) * 0.08), 2))
            severity = "HIGH" if deg >= 10 or score >= 0.85 else "MEDIUM"
            anomalies.append({
                "alert_type": "NETWORK_ANOMALY",
                "entity_id": node_id,
                "entity_name": canonical_name,
                "degree": deg,
                "mean_degree": round(mean_degree, 2),
                "score": score,
                "severity": severity,
                "detection_method": "DegreeCentralityDisparity",
                "title": f"High Connectivity Nexus Entity: {canonical_name}",
                "description": f"Entity has {deg} verified links exceeding the graph average ({mean_degree:.1f}).",
                "explanation": f"WHAT: Elevated network degree observed.\nWHO: {canonical_name}\nDEGREE: {deg} (Avg: {mean_degree:.1f})\nNOTE: Investigative signal requiring review, not proof of criminal activity."
            })

    return anomalies


def detect_temporal_burst_anomalies(
    events: List[Dict[str, Any]],
    window_hours: float = 48.0,
    burst_threshold: int = 4
) -> List[Dict[str, Any]]:
    """
    Identifies temporal activity bursts for entities within a rolling time window.
    """
    # Group events by entity
    entity_events: Dict[str, List[Dict[str, Any]]] = {}
    for ev in events:
        entities = ev.get("related_entities", [])
        if isinstance(entities, str):
            try:
                entities = json.loads(entities)
            except Exception:
                entities = [entities]
        
        # Parse timestamp
        ts_raw = ev.get("start_time_utc") or ev.get("timestamp")
        if not ts_raw:
            continue
        if isinstance(ts_raw, str):
            try:
                dt = datetime.fromisoformat(ts_raw.replace("Z", "+00:00"))
            except Exception:
                continue
        elif isinstance(ts_raw, datetime):
            dt = ts_raw
        else:
            continue

        ev_copy = dict(ev)
        ev_copy["_dt"] = dt

        for ent in entities:
            if not ent:
                continue
            entity_events.setdefault(ent, []).append(ev_copy)

    anomalies: List[Dict[str, Any]] = []

    for ent, ent_evs in entity_events.items():
        if len(ent_evs) < burst_threshold:
            continue

        ent_evs.sort(key=lambda x: x["_dt"])
        i = 0
        while i < len(ent_evs):
            win_start = ent_evs[i]["_dt"]
            in_window = [
                e for e in ent_evs[i:]
                if (e["_dt"] - win_start).total_seconds() <= window_hours * 3600
            ]

            if len(in_window) >= burst_threshold:
                count = len(in_window)
                score = min(0.92, round(0.60 + (count * 0.05), 2))
                severity = "HIGH" if count >= 8 else "MEDIUM"
                anomalies.append({
                    "alert_type": "TEMPORAL_ANOMALY",
                    "entity_name": ent,
                    "event_count": count,
                    "window_start": win_start.isoformat(),
                    "score": score,
                    "severity": severity,
                    "detection_method": "RollingWindowBurstAnalysis",
                    "title": f"Temporal Activity Burst: {ent}",
                    "description": f"Recorded {count} distinct events within {window_hours} hours.",
                    "explanation": f"WHAT: High event density in rolling window.\nWHO: {ent}\nCOUNT: {count} events in {window_hours}h.\nNOTE: Investigative lead requiring timeline examination."
                })
                i += count
            else:
                i += 1

    return anomalies


def detect_velocity_anomalies(
    movements: List[Dict[str, Any]],
    speed_threshold_kmh: float = 800.0,
    min_distance_km: float = 80.0
) -> List[Dict[str, Any]]:
    """
    Identifies physically implausible travel velocities between sequential observation points.
    """
    # Group by entity
    entity_moves: Dict[str, List[Dict[str, Any]]] = {}
    for m in movements:
        ent = m.get("entity_name") or m.get("entity_id") or "UNKNOWN"
        entity_moves.setdefault(ent, []).append(m)

    anomalies: List[Dict[str, Any]] = []

    for ent, seq in entity_moves.items():
        if len(seq) < 2:
            continue

        for i in range(len(seq) - 1):
            p1 = seq[i]
            p2 = seq[i + 1]

            t1 = p1.get("timestamp") or p1.get("start_time_utc")
            t2 = p2.get("timestamp") or p2.get("start_time_utc")
            if not t1 or not t2:
                continue

            dt1 = datetime.fromisoformat(str(t1).replace("Z", "+00:00"))
            dt2 = datetime.fromisoformat(str(t2).replace("Z", "+00:00"))

            elapsed_hours = (dt2 - dt1).total_seconds() / 3600.0
            if elapsed_hours <= 0.0:
                elapsed_hours = 0.001

            dist = haversine_distance_km(
                float(p1.get("latitude", 0.0)),
                float(p1.get("longitude", 0.0)),
                float(p2.get("latitude", 0.0)),
                float(p2.get("longitude", 0.0))
            )

            if dist >= min_distance_km:
                implied_speed = dist / elapsed_hours
                if implied_speed > speed_threshold_kmh:
                    score = min(0.96, round(0.70 + min(0.25, (implied_speed / 2000.0) * 0.25), 2))
                    severity = "HIGH" if implied_speed > 1000.0 else "MEDIUM"
                    anomalies.append({
                        "alert_type": "UNUSUAL_TRAVEL",
                        "entity_name": ent,
                        "distance_km": round(dist, 1),
                        "elapsed_hours": round(elapsed_hours, 2),
                        "implied_speed_kmh": round(implied_speed, 1),
                        "score": score,
                        "severity": severity,
                        "detection_method": "GeodesicVelocityCalculation",
                        "title": f"Implausible Travel Velocity: {ent}",
                        "description": f"Transition of {dist:.1f} km in {elapsed_hours:.2f}h implies {implied_speed:.0f} km/h.",
                        "explanation": f"WHAT: Implausible velocity observed.\nWHO: {ent}\nSPEED: {implied_speed:.0f} km/h over {dist:.1f} km in {elapsed_hours:.2f}h.\nNOTE: May indicate observation timestamp inaccuracy or proxy usage."
                    })

    return anomalies


def detect_activity_spike_anomalies(
    daily_counts: Dict[str, int],
    z_threshold: float = 2.0,
    min_count: int = 5
) -> List[Dict[str, Any]]:
    """
    Computes Gaussian z-score statistics over daily frequency counts to surface activity spikes.
    """
    if len(daily_counts) < 2:
        return []

    values = [float(c) for c in daily_counts.values()]
    mean = sum(values) / len(values)
    variance = sum((x - mean) ** 2 for x in values) / len(values)
    std_dev = math.sqrt(variance)

    anomalies: List[Dict[str, Any]] = []

    for date_str, count in daily_counts.items():
        if count < min_count:
            continue

        z_score = (count - mean) / std_dev if std_dev > 0.0001 else 0.0
        ratio = count / mean if mean > 0 else float(count)

        if z_score >= z_threshold or ratio >= 2.5:
            score = min(0.96, round(0.65 + (max(z_score, 2.0) * 0.08), 2))
            severity = "CRITICAL" if z_score >= 3.0 or count >= 15 else "HIGH"
            anomalies.append({
                "alert_type": "ACTIVITY_SPIKE",
                "date": date_str,
                "count": count,
                "mean": round(mean, 2),
                "std_dev": round(std_dev, 2),
                "z_score": round(z_score, 2),
                "spike_ratio": round(ratio, 2),
                "score": score,
                "severity": severity,
                "detection_method": "ZScoreDailyAggregateSpike",
                "title": f"Case Activity Spike: {date_str}",
                "description": f"Recorded {count} events on {date_str}, exceeding baseline mean ({mean:.1f}).",
                "explanation": f"WHAT: Significant activity spike.\nDATE: {date_str}\nCOUNT: {count} (Mean: {mean:.1f}, StdDev: {std_dev:.2f}, Z-Score: {z_score:.2f})\nNOTE: Requires investigative review of daily log inputs."
            })

    return anomalies


def calculate_anomaly_priority(
    score: float,
    has_evidence: bool,
    is_cross_case: bool,
    weights: Tuple[float, float, float, float] = (0.40, 0.25, 0.20, 0.15)
) -> float:
    """
    Standardized, explainable priority formula:
    P = (0.40 * Score) + (0.25 * HasEvidence) + (0.20 * IsCrossCase) + (0.15 * Base)
    Guaranteed bounded in [0.15, 1.0].
    """
    w_score, w_ev, w_cross, w_base = weights
    evidence_val = 1.0 if has_evidence else 0.0
    cross_case_val = 1.0 if is_cross_case else 0.0
    base_val = 1.0

    raw = (w_score * max(0.0, min(1.0, score)) +
           w_ev * evidence_val +
           w_cross * cross_case_val +
           w_base * base_val)

    return round(min(1.0, max(0.0, raw)), 4)


def generate_deduplication_fingerprint(
    alert_type: str,
    source_entity_id: Optional[str] = None,
    target_entity_id: Optional[str] = None,
    time_window_key: Optional[str] = None,
    location_id: Optional[str] = None
) -> str:
    """
    Generates a deterministic SHA-256 fingerprint string for deduplication across batch runs.
    """
    payload = f"{alert_type.upper().strip()}|{source_entity_id or ''}|{target_entity_id or ''}|{time_window_key or ''}|{location_id or ''}"
    digest = hashlib.sha256(payload.encode("utf-8")).hexdigest()
    return f"SHA256:{digest}"
