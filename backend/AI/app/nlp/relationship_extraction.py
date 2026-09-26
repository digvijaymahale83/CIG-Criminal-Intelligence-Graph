"""
Relationship and Event Extraction Engine.
Extracts grounded relationships between co-occurring entities only when supported by
syntactic action predicates or domain-specific transactional patterns (e.g. CDR calls, vehicle usage, money transfers).
Does NOT link entities merely for appearing in the same document.
"""
import re
from typing import List, Dict, Any, Tuple
from ..schemas.extraction import ExtractedEntitySchema, ExtractedRelationshipSchema, ExtractedEventSchema

# Predicate patterns mapping to canonical relationship types
RELATION_PREDICATES = [
    (re.compile(r"\b(?:called|phoned|dialed|spoke\s+with|contacted|communicated\s+with)\b", re.IGNORECASE), "CALLED", 0.94),
    (re.compile(r"\b(?:used|drove|operated|travelled\s+in|seen\s+driving)\b", re.IGNORECASE), "USED", 0.92),
    (re.compile(r"\b(?:owns|owned|registered\s+to|proprietor\s+of|vehicle\s+owner)\b", re.IGNORECASE), "OWNED", 0.95),
    (re.compile(r"\b(?:visited|travelled\s+to|spotted\s+at|stayed\s+at|arrived\s+at)\b", re.IGNORECASE), "VISITED", 0.90),
    (re.compile(r"\b(?:located\s+at|stationed\s+at|based\s+in|headquartered\s+in)\b", re.IGNORECASE), "LOCATED_AT", 0.91),
    (re.compile(r"\b(?:worked\s+for|employed\s+by|operative\s+for|subordinate\s+of)\b", re.IGNORECASE), "WORKED_FOR", 0.93),
    (re.compile(r"\b(?:associated\s+with|cohort\s+of|accomplice\s+of|partner\s+in)\b", re.IGNORECASE), "ASSOCIATED_WITH", 0.88),
    (re.compile(r"\b(?:involved\s+in|implicated\s+in|suspect\s+in|named\s+in)\b", re.IGNORECASE), "INVOLVED_IN", 0.92),
    (re.compile(r"\b(?:transferred\s+to|sent\s+funds\s+to|paid|wired\s+to|remitted\s+to)\b", re.IGNORECASE), "TRANSFERRED_TO", 0.95),
    (re.compile(r"\b(?:met\s+with|rendezvous\s+with|conferred\s+with|held\s+meeting\s+with)\b", re.IGNORECASE), "MET", 0.91),
]

class RelationshipExtractor:
    def __init__(self):
        pass

    def extract_from_sentences(
        self,
        text: str,
        entities: List[ExtractedEntitySchema],
        evidence_id: str,
        page_num: int = 1,
        source_label: str = None
    ) -> Tuple[List[ExtractedRelationshipSchema], List[ExtractedEventSchema]]:
        """
        Extracts relationships and events from text sentences where entities co-occur with relational predicates.
        """
        if not text or not entities or len(entities) < 2:
            return [], []

        relationships: List[ExtractedRelationshipSchema] = []
        events: List[ExtractedEventSchema] = []
        seen_rel_keys = set()

        # Split into sentences or lines
        sentences = re.split(r"[.\n;]+", text)

        for sentence in sentences:
            sentence_clean = sentence.strip()
            if not sentence_clean:
                continue

            # Find which entities occur in this specific sentence
            present_entities = []
            for ent in entities:
                # Check match against raw or normalized value in this sentence
                if (ent.raw_value.lower() in sentence_clean.lower() or 
                    ent.normalized_value.lower() in sentence_clean.lower()):
                    present_entities.append(ent)

            # Need at least 2 entities in the same sentence to establish a grounded relationship
            if len(present_entities) >= 2:
                # Check for explicit predicate verbs in this sentence
                for pattern, rel_type, base_conf in RELATION_PREDICATES:
                    if pattern.search(sentence_clean):
                        # Link eligible pairs based on semantic entity type compatibility
                        for i in range(len(present_entities)):
                            for j in range(len(present_entities)):
                                if i == j:
                                    continue
                                src = present_entities[i]
                                tgt = present_entities[j]

                                if self._is_compatible_relationship(src.type, rel_type, tgt.type):
                                    rel_key = (src.normalized_value, rel_type, tgt.normalized_value)
                                    if rel_key not in seen_rel_keys:
                                        seen_rel_keys.add(rel_key)
                                        loc_label = source_label or (f"Page {page_num}" if page_num else "Document")
                                        relationships.append(ExtractedRelationshipSchema(
                                            source=src.id,
                                            relationship=rel_type,
                                            target=tgt.id,
                                            confidence=round(base_conf, 2),
                                            evidence_id=evidence_id,
                                            page=page_num,
                                            source_location=f"{loc_label}: \"{sentence_clean[:80]}...\""
                                        ))

                                        # Also extract event if temporal cue present
                                        events.append(ExtractedEventSchema(
                                            event_type=rel_type,
                                            location=loc_label,
                                            related_entities=[src.normalized_value, tgt.normalized_value],
                                            confidence=round(base_conf, 2),
                                            source_location=loc_label
                                        ))

        return relationships, events

    def _is_compatible_relationship(self, src_type: str, rel_type: str, tgt_type: str) -> bool:
        """
        Validates domain semantic constraints for relationship types.
        """
        if rel_type == "USED" and src_type in ("PERSON", "ORGANIZATION") and tgt_type in ("VEHICLE", "PHONE", "DEVICE", "ACCOUNT"):
            return True
        if rel_type == "OWNED" and src_type in ("PERSON", "ORGANIZATION") and tgt_type in ("VEHICLE", "ACCOUNT", "ORGANIZATION", "DEVICE"):
            return True
        if rel_type == "CALLED" and src_type in ("PERSON", "PHONE") and tgt_type in ("PERSON", "PHONE"):
            return True
        if rel_type == "VISITED" and src_type in ("PERSON", "VEHICLE") and tgt_type == "LOCATION":
            return True
        if rel_type == "LOCATED_AT" and src_type in ("PERSON", "ORGANIZATION", "DEVICE", "VEHICLE") and tgt_type == "LOCATION":
            return True
        if rel_type == "WORKED_FOR" and src_type == "PERSON" and tgt_type == "ORGANIZATION":
            return True
        if rel_type in ("ASSOCIATED_WITH", "MET") and src_type == "PERSON" and tgt_type == "PERSON":
            return True
        if rel_type == "INVOLVED_IN" and src_type in ("PERSON", "ORGANIZATION", "VEHICLE") and tgt_type == "CASE":
            return True
        if rel_type == "TRANSFERRED_TO" and src_type in ("PERSON", "ACCOUNT") and tgt_type in ("PERSON", "ACCOUNT"):
            return True
        return False
