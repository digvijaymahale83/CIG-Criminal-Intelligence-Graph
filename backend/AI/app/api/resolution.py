"""
FastAPI Router for Entity Resolution Comparison and Scoring.
"""
from fastapi import APIRouter
from pydantic import BaseModel, Field
from typing import Dict, Any, List, Optional
from ..resolution.entity_matcher import EntityResolutionEngine

router = APIRouter(prefix="/api/v1/resolution", tags=["Entity Resolution"])
engine = EntityResolutionEngine()

class EntityComparisonPayload(BaseModel):
    entity_a: Dict[str, Any] = Field(..., description="First entity attributes (name, type, phone, vehicle, account, aliases)")
    entity_b: Dict[str, Any] = Field(..., description="Second entity attributes")

@router.post("/compare")
async def compare_entities(payload: EntityComparisonPayload):
    """
    Computes deterministic multi-signal match score between two entity dictionaries.
    Exposes individual factors, weights, and model metadata.
    """
    result = engine.compare_entities(payload.entity_a, payload.entity_b)
    return result
