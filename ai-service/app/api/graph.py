"""
Graph ML & GAT Inference API Endpoint (ai-service mirror).
"""
import sys
import os

# Mirror backend/AI/app/api/graph.py
root_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
backend_ai_path = os.path.join(root_dir, "backend", "AI")
if backend_ai_path not in sys.path:
    sys.path.insert(0, backend_ai_path)

from backend.AI.app.api.graph import router, GraphInferenceRequest, GraphInferenceResponse

__all__ = ["router", "GraphInferenceRequest", "GraphInferenceResponse"]
