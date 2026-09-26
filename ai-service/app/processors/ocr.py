"""Document and Image OCR Processor Module (Scheduled for Phase 5)"""

class OcrProcessor:
    """
    OCR Processor skeleton for Tesseract / LayoutLM.
    Marked UNIMPLEMENTED: Will be integrated in Phase 5 for document intelligence.
    """
    def __init__(self):
        self.is_ready = False

    async def extract_text(self, file_path: str) -> str:
        raise NotImplementedError("OCR text extraction is scheduled for Phase 5. No fake extraction performed.")
