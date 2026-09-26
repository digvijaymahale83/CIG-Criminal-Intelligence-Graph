"""
Central Extraction Pipeline Service.
Coordinates file parsing, OCR, text segmentation, hybrid entity extraction,
deterministic normalization, and relationship/event extraction.
"""
import os
from typing import Dict, Any, List
from ..schemas.extraction import (
    EvidenceProcessRequest,
    EvidenceProcessResponse,
    ExtractedEntitySchema,
    ExtractedRelationshipSchema,
    ExtractedEventSchema
)
from ..processors import (
    PdfProcessor,
    CsvProcessor,
    XlsxProcessor,
    JsonProcessor,
    ImageProcessor,
    TextProcessor
)
from ..nlp.entity_extraction import EntityExtractor
from ..nlp.relationship_extraction import RelationshipExtractor

SUPPORTED_EXTENSIONS = {
    ".pdf": "PDF",
    ".txt": "TEXT",
    ".csv": "CSV",
    ".xlsx": "XLSX",
    ".xls": "XLSX",
    ".json": "JSON",
    ".jpg": "IMAGE",
    ".jpeg": "IMAGE",
    ".png": "IMAGE"
}

class ExtractionService:
    def __init__(self):
        self.pdf_proc = PdfProcessor()
        self.csv_proc = CsvProcessor()
        self.xlsx_proc = XlsxProcessor()
        self.json_proc = JsonProcessor()
        self.image_proc = ImageProcessor()
        self.text_proc = TextProcessor()
        self.entity_extractor = EntityExtractor()
        self.rel_extractor = RelationshipExtractor()

    def process(self, request: EvidenceProcessRequest) -> EvidenceProcessResponse:
        """
        Executes the end-to-end extraction pipeline for an evidence item.
        """
        file_path = request.file_path
        raw_text = request.text

        segments: List[tuple] = []
        file_type = "TEXT"

        # 1. Determine input source and file type
        if file_path:
            if not os.path.exists(file_path):
                return EvidenceProcessResponse(
                    evidence_id=request.evidence_id,
                    case_id=request.case_id,
                    status="FAILED",
                    text_snippet=f"File not found: {file_path}",
                    metadata={"error": f"Evidence file not found on disk: {file_path}"}
                )

            ext = os.path.splitext(file_path)[1].lower()
            if ext not in SUPPORTED_EXTENSIONS:
                return EvidenceProcessResponse(
                    evidence_id=request.evidence_id,
                    case_id=request.case_id,
                    status="FAILED",
                    text_snippet=f"Unsupported file extension: {ext}",
                    metadata={"error": f"Unsupported evidence file format '{ext}'. Supported types: PDF, TXT, CSV, XLSX, JSON, JPG, PNG"}
                )

            file_type = SUPPORTED_EXTENSIONS[ext]

            # 2. Parse file into segments: list of (index, text_content)
            try:
                if file_type == "PDF":
                    segments = self.pdf_proc.process_file(file_path)
                elif file_type == "CSV":
                    segments = self.csv_proc.process_file(file_path)
                elif file_type == "XLSX":
                    segments = self.xlsx_proc.process_file(file_path)
                elif file_type == "JSON":
                    segments = self.json_proc.process_file(file_path)
                elif file_type == "IMAGE":
                    segments = self.image_proc.process_file(file_path)
                elif file_type == "TEXT":
                    segments = self.text_proc.process_file(file_path)
            except Exception as parse_err:
                return EvidenceProcessResponse(
                    evidence_id=request.evidence_id,
                    case_id=request.case_id,
                    status="FAILED",
                    text_snippet=f"Parsing error: {str(parse_err)}",
                    metadata={"error": f"Processor error for {file_type}: {str(parse_err)}"}
                )

        elif raw_text:
            segments = self.text_proc.process_raw_text(raw_text)
        else:
            return EvidenceProcessResponse(
                evidence_id=request.evidence_id,
                case_id=request.case_id,
                status="FAILED",
                text_snippet="Neither file_path nor text provided in extraction request",
                metadata={"error": "No input content provided"}
            )

        # 3. Extract entities across segments
        all_entities: List[ExtractedEntitySchema] = []
        all_relationships: List[ExtractedRelationshipSchema] = []
        all_events: List[ExtractedEventSchema] = []
        full_text_parts: List[str] = []

        seen_entities = {}
        entity_counter = 1

        for idx, seg_text in segments:
            full_text_parts.append(seg_text)
            source_label = f"Page {idx}" if file_type == "PDF" else (f"Row {idx}" if file_type in ("CSV", "XLSX", "JSON") else f"Line {idx}")

            seg_entities = self.entity_extractor.extract_from_text(
                text=seg_text,
                evidence_id=request.evidence_id,
                page_num=idx if file_type == "PDF" else None,
                row_num=idx if file_type in ("CSV", "XLSX", "JSON") else None,
                source_label=source_label
            )

            # Map to unique IDs across document
            page_entities_for_rel: List[ExtractedEntitySchema] = []
            for e in seg_entities:
                dedup_key = (e.type, e.normalized_value)
                if dedup_key in seen_entities:
                    existing = seen_entities[dedup_key]
                    page_entities_for_rel.append(existing)
                else:
                    e.id = f"ENT-{entity_counter:03d}"
                    entity_counter += 1
                    seen_entities[dedup_key] = e
                    all_entities.append(e)
                    page_entities_for_rel.append(e)

            # 4. Extract relationships within this segment
            seg_rels, seg_evs = self.rel_extractor.extract_from_sentences(
                text=seg_text,
                entities=page_entities_for_rel,
                evidence_id=request.evidence_id,
                page_num=idx if file_type == "PDF" else 1,
                source_label=source_label
            )
            all_relationships.extend(seg_rels)
            all_events.extend(seg_evs)

        combined_text = "\n".join(full_text_parts)
        text_snippet = combined_text[:1000] + ("..." if len(combined_text) > 1000 else "")

        return EvidenceProcessResponse(
            evidence_id=request.evidence_id,
            case_id=request.case_id,
            status="REVIEW_REQUIRED",
            text_snippet=text_snippet,
            entities=all_entities,
            relationships=all_relationships,
            events=all_events,
            metadata={
                "file_type": file_type,
                "segments_processed": len(segments),
                "total_entities": len(all_entities),
                "total_relationships": len(all_relationships),
                "total_events": len(all_events)
            }
        )
