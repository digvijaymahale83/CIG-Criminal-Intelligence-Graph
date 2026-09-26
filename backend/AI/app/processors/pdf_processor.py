"""
PDF Evidence Processor using PyMuPDF (fitz).
Extracts text page-by-page preserving page numbers and table text for precise provenance citation.
"""
import os
from typing import List, Tuple

class PdfProcessor:
    def process_file(self, file_path: str) -> List[Tuple[int, str]]:
        """
        Parses a PDF file and returns a list of (page_number, text) tuples.
        Page numbers are 1-indexed.
        """
        if not os.path.exists(file_path):
            raise FileNotFoundError(f"PDF evidence file not found: {file_path}")

        try:
            import pymupdf as fitz
        except ImportError:
            import fitz

        pages_text: List[Tuple[int, str]] = []
        try:
            with fitz.open(file_path) as doc:
                for page_idx in range(len(doc)):
                    page = doc[page_idx]
                    page_num = page_idx + 1
                    text = page.get_text("text")
                    if text and text.strip():
                        pages_text.append((page_num, text))
        except Exception as ex:
            raise RuntimeError(f"PyMuPDF failed to parse PDF document: {str(ex)}")

        return pages_text
