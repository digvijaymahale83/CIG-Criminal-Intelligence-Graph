"""
Plain Text Evidence Processor.
Processes UTF-8 investigation statements, FIR transcripts, witness testimonies, and officer field notes.
Preserves line numbers for exact evidence provenance citation.
"""
import os
from typing import List, Tuple

class TextProcessor:
    def process_file(self, file_path: str) -> List[Tuple[int, str]]:
        """
        Parses a plain text file and returns a list of (line_number, line_text) tuples.
        """
        if not os.path.exists(file_path):
            raise FileNotFoundError(f"Text evidence file not found: {file_path}")

        records: List[Tuple[int, str]] = []
        try:
            with open(file_path, mode="r", encoding="utf-8", errors="replace") as f:
                for line_idx, line in enumerate(f):
                    cleaned = line.strip()
                    if cleaned:
                        records.append((line_idx + 1, cleaned))
        except Exception as ex:
            raise RuntimeError(f"Text processor failed to read file: {str(ex)}")

        return records

    def process_raw_text(self, text: str) -> List[Tuple[int, str]]:
        """
        Parses in-memory raw string text into line-numbered records.
        """
        if not text:
            return []
        lines = text.splitlines()
        return [(idx + 1, line.strip()) for idx, line in enumerate(lines) if line.strip()]
