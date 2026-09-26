"""
Graph ML & GAT Inference API Endpoint.
Exposes real Graph Attention Network representation learning and link prediction for investigative analysis.
"""
from fastapi import APIRouter, HTTPException, status
from pydantic import BaseModel, Field
from typing import List, Dict, Any, Optional

import sys
import os

# Ensure ai-service is in sys.path
root_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
ai_service_path = os.path.join(root_dir, "ai-service")
if ai_service_path not in sys.path:
    sys.path.insert(0, ai_service_path)

try:
    from app.graph_ml.inference import run_gat_inference
except ImportError:
    # Try relative import if executed from within ai-service
    from ..graph_ml.inference import run_gat_inference

router = APIRouter(prefix="/api/v1/graph", tags=["Graph Machine Learning"])


class NodeInput(BaseModel):
    id: str
    canonicalName: Optional[str] = None
    name: Optional[str] = None
    type: str = "PERSON"
    properties: Optional[Dict[str, Any]] = None
    evidenceCount: Optional[int] = 1


class EdgeInput(BaseModel):
    id: Optional[str] = None
    source: str
    target: str
    type: str = "ASSOCIATE_OF"
    confidence: Optional[float] = 1.0


class GraphInferenceRequest(BaseModel):
    caseId: Optional[str] = None
    nodes: List[NodeInput] = Field(default_factory=list)
    edges: List[EdgeInput] = Field(default_factory=list)
    candidateThreshold: Optional[float] = 0.50
    maxCandidates: Optional[int] = 25


class LeadSignalBreakdown(BaseModel):
    cosineSimilarity: float
    sharedNeighborsCount: int
    sharedNeighborNames: List[str]
    attentionWeight: float
    contributingSignals: List[Dict[str, Any]]


class GraphLeadOutput(BaseModel):
    sourceEntityId: str
    sourceEntityName: str
    sourceEntityType: str
    targetEntityId: str
    targetEntityName: str
    targetEntityType: str
    leadType: str
    suggestedRelationshipType: str
    score: float
    status: str
    modelVersion: str
    signals: LeadSignalBreakdown


class GraphInferenceResponse(BaseModel):
    modelVersion: str
    nodeCount: int
    edgeCount: int
    embeddings: Dict[str, List[float]]
    leads: List[GraphLeadOutput]


@router.post("/gat-inference", response_model=GraphInferenceResponse)
async def perform_gat_inference(request: GraphInferenceRequest):
    """
    Computes deterministic Graph Attention Network (GAT) node representations
    and surfaces explainable candidate link predictions for human investigator review.
    """
    try:
        nodes_dict = [n.dict() for n in request.nodes]
        edges_dict = [e.dict() for e in request.edges]

        result = run_gat_inference(
            nodes=nodes_dict,
            edges=edges_dict,
            candidate_threshold=request.candidateThreshold or 0.50,
            max_candidates=request.maxCandidates or 25
        )

        return result
    except Exception as ex:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"GAT graph inference error: {str(ex)}"
        )
