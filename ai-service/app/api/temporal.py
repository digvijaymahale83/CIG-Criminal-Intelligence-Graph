from fastapi import APIRouter
from pydantic import BaseModel, Field
from typing import Optional
from datetime import datetime
from ..nlp.temporal_normalization import TemporalNormalizer

router = APIRouter(prefix="/api/v1/temporal", tags=["Temporal Intelligence"])
normalizer = TemporalNormalizer()

class TemporalNormalizeRequest(BaseModel):
    text: str = Field(..., description="Text segment containing date/time expression")
    reference_date: Optional[str] = Field(None, description="Optional ISO reference date for relative expressions")

class TemporalNormalizeResponse(BaseModel):
    raw_text: str
    start_time_utc: Optional[str] = None
    end_time_utc: Optional[str] = None
    time_precision: str
    is_approximate: bool
    confidence: float
    description: str

@router.post("/normalize", response_model=TemporalNormalizeResponse)
async def normalize_temporal_expression(request: TemporalNormalizeRequest):
    ref_dt = None
    if request.reference_date:
        try:
            ref_dt = datetime.fromisoformat(request.reference_date)
        except ValueError:
            pass
    
    result = normalizer.normalize(request.text, reference_date=ref_dt)
    return TemporalNormalizeResponse(**result)
