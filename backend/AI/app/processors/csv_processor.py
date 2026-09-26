"""
CSV Evidence Processor using pandas / csv standard library.
Extracts tabular records row-by-row with row indices for precise evidence citation.
Recognizes common investigation tables (CDR records, bank statements, toll plazas).
"""
import os
import csv
from typing import List, Tuple, Dict, Any

class CsvProcessor:
    def process_file(self, file_path: str) -> List[Tuple[int, str]]:
        """
        Parses a CSV file and returns a list of (row_number, text) tuples.
        Row numbers are 1-indexed (excluding header).
        """
        if not os.path.exists(file_path):
            raise FileNotFoundError(f"CSV evidence file not found: {file_path}")

        records: List[Tuple[int, str]] = []

        try:
            with open(file_path, mode="r", encoding="utf-8-sig", errors="replace") as f:
                reader = csv.reader(f)
                header = next(reader, None)
                header_str = ", ".join(header) if header else ""

                row_idx = 1
                for row in reader:
                    if not row or not any(field.strip() for field in row):
                        continue

                    # Construct meaningful line with headers if available
                    if header and len(header) == len(row):
                        line_parts = [f"{header[i]}: {row[i]}" for i in range(len(row)) if row[i].strip()]
                        line_text = "; ".join(line_parts)
                    else:
                        line_text = ", ".join(row)

                    records.append((row_idx, line_text))
                    row_idx += 1
        except Exception as ex:
            raise RuntimeError(f"CSV processor failed to parse file: {str(ex)}")

        return records
