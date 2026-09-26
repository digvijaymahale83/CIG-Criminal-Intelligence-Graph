"""
Criminal Network Analysis AI Service Entrypoint (mirrors backend/AI/app/main.py).
"""
import sys
import os

# Ensure project root is in python path
root_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
if root_dir not in sys.path:
    sys.path.insert(0, root_dir)

from backend.AI.app.main import app

if __name__ == "__main__":
    import uvicorn
    port = int(os.environ.get("AI_SERVICE_PORT", "8000"))
    uvicorn.run("ai-service.app.main:app", host="0.0.0.0", port=port, reload=False)
