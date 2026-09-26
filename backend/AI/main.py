"""
Criminal Network Analysis System - AI Analytics Service
Evidence-backed investigative decision-support platform.
Note: AI/analytics outputs are potential findings that require human investigator review.
Guilt, criminality, or arrest decisions are strictly determined by human authorities.
"""

from fastapi import FastAPI, HTTPException, status
from pydantic import BaseModel, Field
from typing import List, Optional
import datetime
import os

app = FastAPI(
    title="Criminal Network Analysis AI Service",
    description="Investigator decision support service for entity resolution, NER extraction, and graph heuristics.",
    version="1.0.0"
)

class HealthStatusResponse(BaseModel):
    status: str = Field(default="healthy", description="Operational health status")
    service: str = Field(default="Criminal Network AI Service")
    version: str = Field(default="1.0.0")
    ready: bool = Field(default=True)
    checked_at_utc: str = Field(default_factory=lambda: datetime.datetime.utcnow().isoformat())
    governance_notice: str = Field(
        default="AI/analytics outputs are potential findings requiring human investigator validation. Not an automated guilt determination system."
    )

@app.get("/health", response_model=HealthStatusResponse, tags=["Health"])
def get_health():
    """Liveness and readiness probe for the AI analytics service."""
    return HealthStatusResponse()

@app.get("/api/v1/health", response_model=HealthStatusResponse, tags=["Health"])
def get_api_health():
    """API versioned health probe."""
    return HealthStatusResponse()

if __name__ == "__main__":
    import uvicorn
    port = int(os.environ.get("AI_SERVICE_PORT", "5050"))
    uvicorn.run("main:app", host="0.0.0.0", port=port, reload=False)
