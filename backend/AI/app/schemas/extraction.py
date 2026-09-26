from pydantic import BaseModel, Field
from typing import List, Optional, Dict, Any
import datetime

class EntitySource(BaseModel):
    evidence_id: Optional[str] = None
    page: Optional[int] = None
    row: Optional[int] = None
    line: Optional[int] = None
    field: Optional[str] = None
    location_label: Optional[str] = None

class ExtractedEntitySchema(BaseModel):
    id: str = Field(..., description="Temporary extraction identifier (e.g. ENT-001)")
    type: str = Field(..., description="Canonical entity type: PERSON, PHONE, VEHICLE, LOCATION, ORGANIZATION, ACCOUNT, DEVICE, DOCUMENT, CASE, EVENT")
    raw_value: str = Field(..., description="Exact raw string extracted from source")
    normalized_value: str = Field(..., description="Standardized representation for matching")
    confidence: float = Field(default=1.0, ge=0.0, le=1.0)
    source: EntitySource = Field(default_factory=EntitySource)

class ExtractedRelationshipSchema(BaseModel):
    source: str = Field(..., description="Temporary extraction ID or normalized value of source entity")
    relationship: str = Field(..., description="Supported type: CALLED, USED, OWNED, VISITED, LOCATED_AT, WORKED_FOR, ASSOCIATED_WITH, INVOLVED_IN, MENTIONED_IN, TRANSFERRED_TO, MET, COMMUNICATED_WITH")
    target: str = Field(..., description="Temporary extraction ID or normalized value of target entity")
    confidence: float = Field(default=1.0, ge=0.0, le=1.0)
    evidence_id: Optional[str] = None
    page: Optional[int] = None
    row: Optional[int] = None
    source_location: Optional[str] = None

class ExtractedEventSchema(BaseModel):
    event_type: str = Field(default="INCIDENT")
    timestamp: Optional[str] = None
    location: Optional[str] = None
    related_entities: List[str] = Field(default_factory=list)
    confidence: float = Field(default=1.0, ge=0.0, le=1.0)
    source_location: Optional[str] = None

class EvidenceProcessRequest(BaseModel):
    evidence_id: str
    case_id: Optional[str] = None
    file_path: Optional[str] = None
    text: Optional[str] = None
    mime_type: Optional[str] = None
    original_filename: Optional[str] = None
    options: Optional[Dict[str, Any]] = Field(default_factory=dict)

class EvidenceProcessResponse(BaseModel):
    evidence_id: str
    case_id: Optional[str] = None
    status: str = Field(default="REVIEW_REQUIRED")
    text_snippet: Optional[str] = None
    entities: List[ExtractedEntitySchema] = Field(default_factory=list)
    relationships: List[ExtractedRelationshipSchema] = Field(default_factory=list)
    events: List[ExtractedEventSchema] = Field(default_factory=list)
    metadata: Dict[str, Any] = Field(default_factory=dict)
    processed_at_utc: str = Field(default_factory=lambda: datetime.datetime.now(datetime.timezone.utc).isoformat())
