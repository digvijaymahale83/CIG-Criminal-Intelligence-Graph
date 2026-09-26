"""
Image Evidence OCR Processor using PIL and pytesseract.
Extracts text from scanned documents, screenshots, identity cards, and vehicle plates.
"""
import os
from typing import List, Tuple
from PIL import Image

class ImageProcessor:
    def process_file(self, file_path: str) -> List[Tuple[int, str]]:
        """
        Runs OCR on an image file and returns a list containing (1, ocr_text).
        """
        if not os.path.exists(file_path):
            raise FileNotFoundError(f"Image evidence file not found: {file_path}")

        try:
            with Image.open(file_path) as img:
                # Basic validation that image is readable
                img.verify()

            # Re-open for OCR because verify() changes internal state
            with Image.open(file_path) as img:
                import pytesseract
                try:
                    ocr_text = pytesseract.image_to_string(img)
                except Exception as tesseract_err:
                    # If Tesseract binary is not on host PATH, return informative error
                    raise RuntimeError(f"Tesseract OCR engine failed: {str(tesseract_err)}")

                if ocr_text and ocr_text.strip():
                    return [(1, ocr_text.strip())]
                return []
        except RuntimeError:
            raise
        except Exception as ex:
            raise RuntimeError(f"Image processor failed to parse image: {str(ex)}")
