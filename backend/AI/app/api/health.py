"""
Health and readiness diagnostics for the AI Extraction Engine.
"""
from fastapi import APIRouter
from pydantic import BaseModel, Field
import datetime

router = APIRouter(tags=["Health"])

class HealthResponse(BaseModel):
    status: str = Field(default="healthy")
    service: str = Field(default="Criminal Network AI Service")
    version: str = Field(default="2.0.0")
    checked_at_utc: str = Field(default_factory=lambda: datetime.datetime.now(datetime.timezone.utc).isoformat())
    governance_notice: str = Field(
        default="AI/analytics outputs are investigative findings requiring human officer validation. Not an automated guilt determination system."
    )

class ReadinessResponse(BaseModel):
    ready: bool = Field(default=True)
    status: str = Field(default="ready")
    checked_at_utc: str = Field(default_factory=lambda: datetime.datetime.now(datetime.timezone.utc).isoformat())
    processors: dict = Field(
        default={
            "pdf": "available",
            "csv": "available",
            "xlsx": "available",
            "json": "available",
            "text": "available",
            "image": "available"
        }
    )

@router.get("/health", response_model=HealthResponse)
def get_health():
    return HealthResponse()

@router.get("/ready", response_model=ReadinessResponse)
def get_ready():
    return ReadinessResponse()
