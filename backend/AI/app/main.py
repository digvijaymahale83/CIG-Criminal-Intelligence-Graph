"""
Criminal Network Analysis System — AI Extraction Engine Microservice (Phase 2).
FastAPI application providing deterministic document processing, hybrid entity extraction,
normalization, and relationship extraction for investigative intelligence.
"""
from fastapi import FastAPI
import os
from .api.health import router as health_router
from .api.extraction import router as extraction_router
from .api.resolution import router as resolution_router
from .api.graph import router as graph_router
from .api.temporal import router as temporal_router

app = FastAPI(
    title="Criminal Network Intelligence AI Extraction Engine",
    description="Investigator decision support microservice for multi-modal evidence processing, "
                "hybrid entity extraction, and relationship parsing. "
                "Notice: AI outputs are investigative candidates requiring human investigator review.",
    version="2.0.0"
)

# Include sub-routers
app.include_router(health_router)
app.include_router(extraction_router)
app.include_router(resolution_router)
app.include_router(graph_router)
app.include_router(temporal_router)

if __name__ == "__main__":
    import uvicorn
    port = int(os.environ.get("AI_SERVICE_PORT", "8000"))
    uvicorn.run("backend.AI.app.main:app", host="0.0.0.0", port=port, reload=False)
