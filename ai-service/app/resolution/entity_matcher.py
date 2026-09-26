"""
Entity Resolution & Disambiguation Engine.
Provides deterministic, explainable multi-signal entity comparison and similarity scoring.
Notice: AI outputs are investigative candidates requiring explicit human review.
"""
from typing import Dict, Any, List
import re
from datetime import datetime

class EntityResolutionEngine:
    """
    Deterministic entity resolution engine comparing entity records across cases.
    Evaluates exact identifiers, aliases, normalized strings, and token-aware name similarity.
    """
    def __init__(self):
        self.version = "1.0.0-deterministic-resolution"
        self.is_ready = True

    def _levenshtein(self, s1: str, s2: str) -> int:
        if len(s1) < len(s2):
            return self._levenshtein(s2, s1)
        if len(s2) == 0:
            return len(s1)

        previous_row = range(len(s2) + 1)
        for i, c1 in enumerate(s1):
            current_row = [i + 1]
            for j, c2 in enumerate(s2):
                insertions = previous_row[j + 1] + 1
                deletions = current_row[j] + 1
                substitutions = previous_row[j] + (c1.lower() != c2.lower())
                current_row.append(min(insertions, deletions, substitutions))
            previous_row = current_row

        return previous_row[-1]

    def _levenshtein_ratio(self, s1: str, s2: str) -> float:
        if not s1 and not s2:
            return 1.0
        if not s1 or not s2:
            return 0.0
        max_len = max(len(s1), len(s2))
        if max_len == 0:
            return 1.0
        dist = self._levenshtein(s1, s2)
        return max(0.0, 1.0 - (dist / max_len))

    def _jaro_distance(self, s1: str, s2: str) -> float:
        if s1 == s2:
            return 1.0
        if not s1 or not s2:
            return 0.0

        len1, len2 = len(s1), len(s2)
        max_dist = (max(len1, len2) // 2) - 1
        if max_dist < 0:
            max_dist = 0

        s1_matches = [False] * len1
        s2_matches = [False] * len2
        matches = 0

        for i in range(len1):
            start = max(0, i - max_dist)
            end = min(i + max_dist + 1, len2)
            for j in range(start, end):
                if s2_matches[j]:
                    continue
                if s1[i].lower() != s2[j].lower():
                    continue
                s1_matches[i] = True
                s2_matches[j] = True
                matches += 1
                break

        if matches == 0:
            return 0.0

        k = 0
        transpositions = 0
        for i in range(len1):
            if not s1_matches[i]:
                continue
            while not s2_matches[k]:
                k += 1
            if s1[i].lower() != s2[k].lower():
                transpositions += 1
            k += 1

        return (
            (matches / len1)
            + (matches / len2)
            + ((matches - (transpositions / 2.0)) / matches)
        ) / 3.0

    def _jaro_winkler(self, s1: str, s2: str, p: float = 0.1) -> float:
        jaro = self._jaro_distance(s1, s2)
        if jaro < 0.7:
            return jaro

        prefix = 0
        max_prefix = min(4, min(len(s1), len(s2)))
        for i in range(max_prefix):
            if s1[i].lower() == s2[i].lower():
                prefix += 1
            else:
                break

        return jaro + (prefix * p * (1.0 - jaro))

    def compare_entities(self, entity_a: Dict[str, Any], entity_b: Dict[str, Any]) -> Dict[str, Any]:
        """
        Computes explainable match score and factor decomposition between two entity dictionaries.
        """
        type_a = str(entity_a.get("type", "PERSON")).upper()
        type_b = str(entity_b.get("type", "PERSON")).upper()

        if type_a != type_b:
            return {
                "match_score": 0.0,
                "factors": [],
                "method": "TYPE_MISMATCH",
                "model_version": self.version,
                "evaluated_at_utc": datetime.utcnow().isoformat()
            }

        factors: List[Dict[str, Any]] = []
        has_exact = False

        # 1. Phone match
        phone_a = str(entity_a.get("phone", "")).strip()
        phone_b = str(entity_b.get("phone", "")).strip()
        if phone_a and phone_b and phone_a == phone_b:
            factors.append({
                "type": "SHARED_PHONE",
                "weight": 0.45,
                "score": 1.0,
                "description": f"Identical normalized phone number: {phone_a}"
            })
            has_exact = True

        # 2. Vehicle match
        veh_a = str(entity_a.get("vehicle", "")).strip().upper()
        veh_b = str(entity_b.get("vehicle", "")).strip().upper()
        if veh_a and veh_b and veh_a == veh_b:
            factors.append({
                "type": "SHARED_VEHICLE",
                "weight": 0.35,
                "score": 1.0,
                "description": f"Identical vehicle registration: {veh_a}"
            })
            has_exact = True

        # 3. Account match
        acc_a = str(entity_a.get("account", "")).strip().upper()
        acc_b = str(entity_b.get("account", "")).strip().upper()
        if acc_a and acc_b and acc_a == acc_b:
            factors.append({
                "type": "SHARED_ACCOUNT",
                "weight": 0.35,
                "score": 1.0,
                "description": f"Identical account identifier: {acc_a}"
            })
            has_exact = True

        # 4. Name similarity
        name_a = str(entity_a.get("name", "")).strip()
        name_b = str(entity_b.get("name", "")).strip()
        if name_a and name_b:
            jw = self._jaro_winkler(name_a, name_b)
            lev = self._levenshtein_ratio(name_a, name_b)
            name_score = max(jw, lev)

            # Token overlap check (e.g. initials "R. Sharma" vs "Rahul Sharma")
            tokens_a = re.findall(r"\w+", name_a.lower())
            tokens_b = re.findall(r"\w+", name_b.lower())
            if tokens_a and tokens_b and tokens_a[-1] == tokens_b[-1]:  # same surname
                if tokens_a[0][0] == tokens_b[0][0]:  # same initial
                    name_score = max(name_score, 0.88)

            if name_score >= 0.70:
                factors.append({
                    "type": "NAME_SIMILARITY",
                    "weight": 0.30,
                    "score": round(name_score, 2),
                    "description": f"High name similarity ({int(name_score * 100)}%): '{name_a}' vs '{name_b}'"
                })

        # 5. Alias match
        aliases_a = [str(a).lower() for a in entity_a.get("aliases", []) if a]
        aliases_b = [str(b).lower() for b in entity_b.get("aliases", []) if b]
        alias_matched = False
        if name_a.lower() in aliases_b or name_b.lower() in aliases_a or (set(aliases_a) & set(aliases_b)):
            alias_matched = True
            factors.append({
                "type": "ALIAS_MATCH",
                "weight": 0.25,
                "score": 1.0,
                "description": "Documented alias match between entity records."
            })

        if not factors:
            return {
                "match_score": 0.0,
                "factors": [],
                "method": "NO_MATCH",
                "model_version": self.version,
                "evaluated_at_utc": datetime.utcnow().isoformat()
            }

        total_weight = sum(f["weight"] for f in factors)
        weighted_score = sum(f["score"] * f["weight"] for f in factors)
        score = weighted_score / total_weight if total_weight > 0 else 0.0

        # False-positive protection for PERSON: name alone capped at 0.55
        if type_a == "PERSON" and not has_exact and not alias_matched and all(f["type"] == "NAME_SIMILARITY" for f in factors):
            score = min(score, 0.55)

        method = "MULTI_SIGNAL" if (has_exact and len(factors) > 1) else (
            "EXACT_IDENTIFIER" if has_exact else ("ALIAS_MATCH" if alias_matched else "FUZZY_NAME")
        )

        return {
            "match_score": round(score, 2),
            "factors": factors,
            "method": method,
            "model_version": self.version,
            "evaluated_at_utc": datetime.utcnow().isoformat()
        }
