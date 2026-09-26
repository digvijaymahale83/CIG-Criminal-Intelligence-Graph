"""
Text & Document Processing API Contract
Designed for Phase 5-9 pipelines: OCR, NER, Relationship Extraction, Entity Resolution, GAT.
"""
from fastapi import APIRouter, HTTPException, status
from pydantic import BaseModel, Field
from typing import List, Optional, Dict, Any
import datetime

router = APIRouter(prefix="/api/v1/process", tags=["Processing"])

class TextProcessRequest(BaseModel):
    investigation_id: str = Field(..., description="Active investigation workspace ID")
    evidence_id: Optional[str] = Field(None, description="Source evidence identifier for provenance")
    text: str = Field(..., min_length=1, description="Raw textual evidence or extracted document text")
    language: Optional[str] = Field(default="en", description="ISO language code (e.g. en, mr, hi)")
    options: Optional[Dict[str, Any]] = Field(default_factory=dict, description="Pipeline configuration options")

class EntityPlaceholder(BaseModel):
    id: Optional[str] = None
    type: str
    canonical_name: str
    confidence: float
    span_start: Optional[int] = None
    span_end: Optional[int] = None

class RelationshipPlaceholder(BaseModel):
    source_entity: str
    target_entity: str
    type: str
    confidence: float

class TextProcessResponse(BaseModel):
    status: str = Field(default="UNIMPLEMENTED_SKELETON", description="Pipeline execution status")
    investigation_id: str
    evidence_id: Optional[str] = None
    processed_at_utc: str = Field(default_factory=lambda: datetime.datetime.now(datetime.timezone.utc).isoformat())
    entities: List[EntityPlaceholder] = Field(default_factory=list)
    relationships: List[RelationshipPlaceholder] = Field(default_factory=list)
    modules: Dict[str, str] = Field(
        default={
            "ocr": "UNIMPLEMENTED (Phase 5)",
            "ner": "UNIMPLEMENTED (Phase 6)",
            "entity_resolution": "UNIMPLEMENTED (Phase 7)",
            "gat": "UNIMPLEMENTED (Phase 9)"
        }
    )
    notice: str = Field(
        default="AI extraction modules are structured architectural contracts for Phase 2+ implementation. No fabricated findings returned."
    )

@router.post("/text", response_model=TextProcessResponse, status_code=status.HTTP_200_OK)
async def process_text(request: TextProcessRequest):
    """
    Structured endpoint for text processing.
    Validates input parameters and returns contract-compliant response clearly indicating
    subsequent phase readiness.
    """
    return TextProcessResponse(
        status="UNIMPLEMENTED_SKELETON",
        investigation_id=request.investigation_id,
        evidence_id=request.evidence_id,
        entities=[],
        relationships=[],
    )
