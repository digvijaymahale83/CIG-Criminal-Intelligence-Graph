"""
JSON Evidence Processor.
Parses structured JSON evidence (e.g. telecom exports, digital forensics artifacts, banking transactions).
Flattens objects with item keys for provenance tracking.
"""
import os
import json
from typing import List, Tuple, Any

class JsonProcessor:
    def process_file(self, file_path: str) -> List[Tuple[int, str]]:
        """
        Parses a JSON file and returns a list of (item_index, text) tuples.
        """
        if not os.path.exists(file_path):
            raise FileNotFoundError(f"JSON evidence file not found: {file_path}")

        records: List[Tuple[int, str]] = []
        try:
            with open(file_path, mode="r", encoding="utf-8", errors="replace") as f:
                data = json.load(f)

            if isinstance(data, list):
                for idx, item in enumerate(data):
                    text = self._flatten_item(item)
                    if text:
                        records.append((idx + 1, text))
            elif isinstance(data, dict):
                # Check if data contains a records/items list
                for key, val in data.items():
                    if isinstance(val, list):
                        for sub_idx, sub_item in enumerate(val):
                            text = f"[{key}] " + self._flatten_item(sub_item)
                            records.append((sub_idx + 1, text))
                    else:
                        records.append((1, f"{key}: {str(val)}"))
            else:
                records.append((1, str(data)))
        except Exception as ex:
            raise RuntimeError(f"JSON processor failed to parse file: {str(ex)}")

        return records

    def _flatten_item(self, item: Any) -> str:
        if isinstance(item, dict):
            return "; ".join(f"{k}: {v}" for k, v in item.items() if v is not None and str(v).strip())
        return str(item)
