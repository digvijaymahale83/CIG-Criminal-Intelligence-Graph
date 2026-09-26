"""
FastAPI extraction router exposing evidence processing endpoints.
"""
from fastapi import APIRouter, HTTPException, status
from ..schemas.extraction import EvidenceProcessRequest, EvidenceProcessResponse
from ..services.extraction_service import ExtractionService

router = APIRouter(prefix="/api/v1/process", tags=["Extraction"])
service = ExtractionService()

@router.post("/evidence", response_model=EvidenceProcessResponse, status_code=status.HTTP_200_OK)
def process_evidence(request: EvidenceProcessRequest):
    """
    Processes uploaded evidence file or text payload, extracting canonical entities,
    normalized representations, and supported relationships.
    """
    try:
        response = service.process(request)
        return response
    except Exception as ex:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Extraction pipeline execution error: {str(ex)}"
        )

@router.post("/text", response_model=EvidenceProcessResponse, status_code=status.HTTP_200_OK)
def process_text_direct(request: EvidenceProcessRequest):
    """
    Shortcut endpoint for processing raw text payloads.
    """
    try:
        response = service.process(request)
        return response
    except Exception as ex:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Text processing error: {str(ex)}"
        )
