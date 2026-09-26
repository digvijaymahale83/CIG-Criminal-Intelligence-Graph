"""
XLSX / Excel Evidence Processor using openpyxl.
Extracts spreadsheet rows with sheet names and row coordinates for provenance.
"""
import os
from typing import List, Tuple

class XlsxProcessor:
    def process_file(self, file_path: str) -> List[Tuple[int, str]]:
        """
        Parses an XLSX file and returns a list of (row_number, text) tuples.
        """
        if not os.path.exists(file_path):
            raise FileNotFoundError(f"Excel evidence file not found: {file_path}")

        import openpyxl

        records: List[Tuple[int, str]] = []
        try:
            wb = openpyxl.load_workbook(file_path, data_only=True, read_only=True)
            for sheet_name in wb.sheetnames:
                ws = wb[sheet_name]
                header = []
                row_idx = 0
                for row_cells in ws.iter_rows(values_only=True):
                    row_idx += 1
                    cell_values = [str(c).strip() for c in row_cells if c is not None and str(c).strip()]
                    if not cell_values:
                        continue

                    if not header:
                        header = [str(c).strip() if c is not None else f"Col{i+1}" for i, c in enumerate(row_cells)]
                        continue

                    # Construct key-value format for accurate entity & relation identification
                    parts = []
                    for i, val in enumerate(cell_values):
                        col_label = header[i] if i < len(header) else f"Col{i+1}"
                        parts.append(f"{col_label}: {val}")

                    line_text = f"[{sheet_name}] " + "; ".join(parts)
                    records.append((row_idx, line_text))
            wb.close()
        except Exception as ex:
            raise RuntimeError(f"XLSX processor failed to parse Excel workbook: {str(ex)}")

        return records
