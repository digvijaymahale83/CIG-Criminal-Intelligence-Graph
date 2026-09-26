"""
Deterministic entity normalization module for criminal intelligence investigation.
Preserves original raw values while producing consistent, canonical representations.
"""
import re
from typing import Optional

def normalize_phone(raw: str) -> str:
    """
    Normalizes Indian and international phone numbers into standard E.164 format.
    Examples:
      '98765 43210' -> '+919876543210'
      '+91 9876543210' -> '+919876543210'
      '091-9876543210' -> '+919876543210'
      '09876543210' -> '+919876543210'
    """
    if not raw:
        return ""

    # Remove all formatting non-digit characters except leading +
    cleaned = re.sub(r"[^\d+]", "", raw.strip())
    
    if cleaned.startswith("+"):
        digits_only = cleaned[1:]
    else:
        digits_only = cleaned

    # Strip leading zeros
    while digits_only.startswith("0") and len(digits_only) > 10:
        digits_only = digits_only[1:]

    # If it starts with 91 and has 12 digits total
    if digits_only.startswith("91") and len(digits_only) == 12:
        return f"+{digits_only}"
    
    # If 10 digits, default to Indian country code +91 in Maharashtra Police context
    if len(digits_only) == 10:
        return f"+91{digits_only}"

    if cleaned.startswith("+"):
        return cleaned
        
    return f"+{digits_only}" if digits_only else raw.strip()

def normalize_vehicle(raw: str) -> str:
    """
    Normalizes Indian vehicle registration plates into contiguous uppercase string.
    Examples:
      'MH 12 AB 1234' -> 'MH12AB1234'
      'MH-12-AB-1234' -> 'MH12AB1234'
      'mh12ab1234' -> 'MH12AB1234'
    """
    if not raw:
        return ""
    # Strip spaces, hyphens, periods and convert to uppercase
    normalized = re.sub(r"[\s\-\.]", "", raw).upper()
    return normalized

def normalize_person_name(raw: str) -> str:
    """
    Normalizes personal names: collapses duplicate spaces, trims, and applies title casing.
    Examples:
      'RAHUL KUMAR' -> 'Rahul Kumar'
      'Rahul  Kumar' -> 'Rahul Kumar'
      'Shri. Sameer Patil' -> 'Sameer Patil'
    """
    if not raw:
        return ""
    # Collapse multiple whitespace
    cleaned = re.sub(r"\s+", " ", raw.strip())
    # Remove common prefixes in investigation records if isolated
    cleaned = re.sub(r"^(Shri|Smt|Mr|Mrs|Ms|Insp|Inspector|Sub-Insp|PSI|API|ACP|DCP|Dr)\.?\s+", "", cleaned, flags=re.IGNORECASE)
    # Title casing
    parts = cleaned.split()
    return " ".join(p.capitalize() for p in parts)

def normalize_email(raw: str) -> str:
    """
    Normalizes email addresses to lowercase and trimmed.
    """
    if not raw:
        return ""
    return raw.strip().lower()

def normalize_account(raw: str) -> str:
    """
    Normalizes bank accounts and transaction identifiers:
    removes hyphens, spaces, and retains uppercase alphanumeric.
    Examples:
      'SBIN0001234 - 98765432' -> 'SBIN000123498765432'
    """
    if not raw:
        return ""
    return re.sub(r"[\s\-\/]", "", raw).upper()

def normalize_entity(entity_type: str, raw_value: str) -> str:
    """
    Dispatches to appropriate normalizer based on entity type.
    """
    etype = entity_type.upper()
    if etype == "PHONE":
        return normalize_phone(raw_value)
    elif etype == "VEHICLE":
        return normalize_vehicle(raw_value)
    elif etype == "PERSON":
        return normalize_person_name(raw_value)
    elif etype in ("ACCOUNT", "DEVICE"):
        return normalize_account(raw_value)
    elif etype == "LOCATION":
        # Title case and collapse whitespace for locations
        return " ".join(p.capitalize() for p in re.sub(r"\s+", " ", raw_value.strip()).split())
    elif etype == "ORGANIZATION":
        return re.sub(r"\s+", " ", raw_value.strip()).upper()
    else:
        return re.sub(r"\s+", " ", raw_value.strip())
