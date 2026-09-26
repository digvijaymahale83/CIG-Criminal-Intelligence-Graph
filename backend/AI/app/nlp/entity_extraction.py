"""
Hybrid Entity Extraction Engine for Criminal Intelligence & Network Investigation.
Uses deterministic regex for structured identifiers, gazetteers for known regional locations/agencies,
and syntactic/contextual patterns for people, organizations, devices, and cases.
Does NOT fabricate entities if none exist in the source evidence.
"""
import re
from typing import List, Dict, Any, Tuple
from .normalization import normalize_entity
from ..schemas.extraction import ExtractedEntitySchema, EntitySource

# Pre-compiled regex patterns
PHONE_PATTERN = re.compile(r"(?:\+91[\s\-]?)?(?:[6-9]\d{4}[\s\-]?\d{5}|[6-9]\d{9})\b")
VEHICLE_PATTERN = re.compile(r"\b(MH|DL|GJ|KA|TN|HR|UP|MP|AP|TS|WB|KL|RJ|PB|BR|OD|GA)[-\s]?(\d{1,2})[-\s]?([A-Z]{1,3})[-\s]?(\d{3,4})\b", re.IGNORECASE)
EMAIL_PATTERN = re.compile(r"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b")
ACCOUNT_PATTERN = re.compile(r"\b(?:A\/C|Account|Acc|AC|IBAN|Mule\s+Account)[\s:#\-]+([A-Z0-9]{8,20})\b", re.IGNORECASE)
UPI_PATTERN = re.compile(r"\b([a-zA-Z0-9.\-_]{3,30}@(okhdfcbank|okaxis|oksbi|okicici|paytm|apl|ybl|axl|ibl))\b", re.IGNORECASE)
CASE_PATTERN = re.compile(r"\b(?:FIR|CASE|CRIME|MHP|MH)-[A-Z0-9\-\/]{4,25}\b", re.IGNORECASE)
DEVICE_PATTERN = re.compile(r"\b(?:IMEI|MAC|Device|SIM)[\s:#\-]+([A-F0-9:]{12,17}|\d{14,16})\b", re.IGNORECASE)

# Maharashtra & Indian Police contextual locations
KNOWN_LOCATIONS = {
    "pune", "pune central", "mumbai", "mumbai port", "mumbai dock area", "nashik", "nashik road",
    "thane", "navi mumbai", "nagpur", "solapur", "aurangabad", "chhatrapati sambhajinagar",
    "bandra", "kurla", "bandra kurla complex", "andheri", "dadar", "shivajinagar", "colaba",
    "byculla", "worli", "dharavi", "panvel", "vashi", "kalyan", "dombivli", "borivali", "docks"
}

# Common organization indicators
ORG_KEYWORDS = ["syndicate", "pvt ltd", "ltd", "limited", "bank", "enterprises", "logistics", "traders", "trust", "hospital", "customs", "port trust", "finance", "agency", "corporation"]

# Contextual person cues
PERSON_PREFIXES = r"\b(?:Shri|Smt|Mr|Mrs|Ms|Inspector|Insp|Sub-Insp|PSI|API|ACP|DCP|Subject|Suspect|Accused|Witness|Driver|Operator|Associate)\.?\s+([A-Z][a-z]+(?:\s+[A-Z][a-z]+){1,3})\b"
PERSON_PREFIX_PATTERN = re.compile(PERSON_PREFIXES)

class EntityExtractor:
    def __init__(self):
        pass

    def extract_from_text(
        self,
        text: str,
        evidence_id: str,
        page_num: int = 1,
        row_num: int = None,
        source_label: str = None
    ) -> List[ExtractedEntitySchema]:
        """
        Extracts all canonical entities from a single text segment.
        """
        if not text or not text.strip():
            return []

        results: List[ExtractedEntitySchema] = []
        seen_keys = set()
        counter = 1

        def add_entity(etype: str, raw: str, conf: float, line_no: int = None):
            nonlocal counter
            raw_val = raw.strip()
            if not raw_val or len(raw_val) < 2:
                return

            norm_val = normalize_entity(etype, raw_val)
            dedup_key = (etype, norm_val)
            if dedup_key in seen_keys:
                return
            seen_keys.add(dedup_key)

            loc_label = source_label or (f"Page {page_num}" if page_num else (f"Row {row_num}" if row_num else "Document"))
            if line_no:
                loc_label += f", Line {line_no}"

            entity = ExtractedEntitySchema(
                id=f"ENT-{counter:03d}",
                type=etype,
                raw_value=raw_val,
                normalized_value=norm_val,
                confidence=round(conf, 2),
                source=EntitySource(
                    evidence_id=evidence_id,
                    page=page_num,
                    row=row_num,
                    line=line_no,
                    location_label=loc_label
                )
            )
            counter += 1
            results.append(entity)

        # 1. Phone numbers
        for m in PHONE_PATTERN.finditer(text):
            add_entity("PHONE", m.group(0), 0.98)

        # 2. Vehicle registrations
        for m in VEHICLE_PATTERN.finditer(text):
            add_entity("VEHICLE", m.group(0), 0.95)

        # 3. Emails
        for m in EMAIL_PATTERN.finditer(text):
            add_entity("ACCOUNT", m.group(0), 0.96)

        # 4. Bank accounts & UPI
        for m in ACCOUNT_PATTERN.finditer(text):
            add_entity("ACCOUNT", m.group(1), 0.92)
        for m in UPI_PATTERN.finditer(text):
            add_entity("ACCOUNT", m.group(0), 0.97)

        # 5. Case references
        for m in CASE_PATTERN.finditer(text):
            add_entity("CASE", m.group(0), 0.95)

        # 6. Devices (IMEI, MAC)
        for m in DEVICE_PATTERN.finditer(text):
            add_entity("DEVICE", m.group(1), 0.94)

        # 7. Contextual Persons (with title/role prefixes)
        for m in PERSON_PREFIX_PATTERN.finditer(text):
            person_candidate = m.group(1).strip()
            if person_candidate.lower() not in KNOWN_LOCATIONS:
                add_entity("PERSON", person_candidate, 0.95)

        # 7b. Capitalized Proper Names (2-3 title-cased words not matching locations or standard document terms)
        name_stopwords = {
            "investigation report", "crime branch", "police station", "charge sheet",
            "forensic lab", "call detail", "pune central", "nashik road", "dock area",
            "mumbai port", "bandra kurla", "pune city", "first information", "cdr records",
            "central bank", "state bank", "cargo transit", "air cargo", "customs clearance"
        }
        for m in re.finditer(r"\b([A-Z][a-z]{2,15}\s+[A-Z][a-z]{2,15}(?:\s+[A-Z][a-z]{2,15})?)\b", text):
            cand = m.group(1).strip()
            cand_lower = cand.lower()
            if (cand_lower not in KNOWN_LOCATIONS and 
                cand_lower not in name_stopwords and
                not any(kw in cand_lower for kw in ORG_KEYWORDS)):
                add_entity("PERSON", cand, 0.88)

        # 8. Locations (Gazetteer lookup against text)
        lower_text = text.lower()
        for loc in KNOWN_LOCATIONS:
            # Word boundary search
            pattern = rf"\b{re.escape(loc)}\b"
            if re.search(pattern, lower_text):
                # Find original casing in text
                match = re.search(pattern, text, re.IGNORECASE)
                raw_loc = match.group(0) if match else loc.title()
                add_entity("LOCATION", raw_loc, 0.92)

        # 9. Organizations
        for line in text.split("\n"):
            for keyword in ORG_KEYWORDS:
                if keyword in line.lower():
                    # Extract phrase around keyword
                    match = re.search(rf"\b([A-Z0-9][A-Za-z0-9\s&]{{2,30}}\b{re.escape(keyword)}\b[A-Za-z0-9\s]{{0,15}})", line, re.IGNORECASE)
                    if match:
                        add_entity("ORGANIZATION", match.group(0).strip(), 0.88)

        return results
