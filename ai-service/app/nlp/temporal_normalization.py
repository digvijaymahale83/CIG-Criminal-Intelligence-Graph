"""
Temporal Expression Extraction and Normalization Module.
Normalizes explicit timestamps, dates, date ranges, month/year expressions, and approximate period cues
into standardized UTC boundaries and precision classifications.
Does NOT fabricate exact times for approximate dates.
"""
import re
from datetime import datetime, timezone, timedelta
from typing import Optional, Dict, Any, Tuple

# Month names mapping
MONTHS = {
    "january": 1, "jan": 1,
    "february": 2, "feb": 2,
    "march": 3, "mar": 3,
    "april": 4, "apr": 4,
    "may": 5,
    "june": 6, "jun": 6,
    "july": 7, "jul": 7,
    "august": 8, "aug": 8,
    "september": 9, "sep": 9, "sept": 9,
    "october": 10, "oct": 10,
    "november": 11, "nov": 11,
    "december": 12, "dec": 12
}

class TemporalNormalizer:
    def __init__(self):
        # 1. Exact ISO timestamp: 2026-03-12T14:30:00Z or 2026-03-12 14:30:00
        self.iso_pattern = re.compile(
            r"\b(\d{4})-(\d{2})-(\d{2})[T\s](\d{2}):(\d{2})(?::(\d{2}))?(?:\.\d+)?(?:Z|[+-]\d{2}:?\d{2})?\b",
            re.IGNORECASE
        )

        # 2. Time Range: e.g. "10:00–11:00", "10:00 to 11:30", "between 10:00 and 11:30"
        self.time_range_pattern = re.compile(
            r"\b(?:between\s+)?(\d{1,2}):(\d{2})(?::(\d{2}))?\s*(?:to|–|-|and)\s*(\d{1,2}):(\d{2})(?::(\d{2}))?\b",
            re.IGNORECASE
        )

        # 3. Date with Time: e.g. "12 March 2026 at 14:30", "12/03/2026 10:15"
        self.date_time_pattern = re.compile(
            r"\b(\d{1,2})[\/\-\s]([A-Za-z]+|\d{1,2})[\/\-\s](\d{4})\s+(?:at\s+)?(\d{1,2}):(\d{2})(?::(\d{2}))?\b",
            re.IGNORECASE
        )

        # 4. Standard Date: e.g. "12 March 2026", "12/03/2026", "2026-03-12"
        self.dmy_text_pattern = re.compile(
            r"\b(\d{1,2})\s+([A-Za-z]+)\s+(\d{4})\b",
            re.IGNORECASE
        )
        self.dmy_numeric_pattern = re.compile(
            r"\b(\d{1,2})[\/\-](\d{1,2})[\/\-](\d{4})\b"
        )
        self.ymd_numeric_pattern = re.compile(
            r"\b(\d{4})-(\d{2})-(\d{2})\b"
        )

        # 5. Date Range: e.g. "between 10 and 12 March 2026", "10-12 March 2026"
        self.date_range_pattern = re.compile(
            r"\b(?:between\s+)?(\d{1,2})\s*(?:and|to|–|-)\s*(\d{1,2})\s+([A-Za-z]+)\s+(\d{4})\b",
            re.IGNORECASE
        )

        # 6. Month-Year: e.g. "March 2026", "03/2026"
        self.month_year_pattern = re.compile(
            r"\b([A-Za-z]+|\d{1,2})[\/\s](\d{4})\b"
        )

        # 7. Approximate periods: e.g. "evening of 12 March 2026", "morning of 12/03/2026"
        self.approx_period_pattern = re.compile(
            r"\b(morning|afternoon|evening|night|dawn|dusk)\s+of\s+(\d{1,2})\s+([A-Za-z]+)\s+(\d{4})\b",
            re.IGNORECASE
        )

        # 8. Relative dates: e.g. "three days later", "2 days after", "next day"
        self.relative_pattern = re.compile(
            r"\b(?:(\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+days?\s+(?:later|after|following))\b",
            re.IGNORECASE
        )

    def normalize(self, text: Optional[str], reference_date: Optional[datetime] = None) -> Dict[str, Any]:
        """
        Extracts and standardizes temporal expressions from text.
        Returns start_time_utc, end_time_utc, time_precision, and confidence.
        """
        result = {
            "raw_text": text or "",
            "start_time_utc": None,
            "end_time_utc": None,
            "time_precision": "UNKNOWN",
            "is_approximate": False,
            "confidence": 0.0,
            "description": ""
        }

        if not text or not text.strip():
            return result

        clean_text = text.strip()

        # 1. Check Exact ISO timestamp
        iso_match = self.iso_pattern.search(clean_text)
        if iso_match:
            try:
                y, m, d = int(iso_match.group(1)), int(iso_match.group(2)), int(iso_match.group(3))
                hh, mm = int(iso_match.group(4)), int(iso_match.group(5))
                ss = int(iso_match.group(6)) if iso_match.group(6) else 0
                dt = datetime(y, m, d, hh, mm, ss, tzinfo=timezone.utc)
                result["start_time_utc"] = dt.isoformat()
                result["end_time_utc"] = dt.isoformat()
                result["time_precision"] = "EXACT" if iso_match.group(6) else "MINUTE"
                result["confidence"] = 0.99
                result["description"] = f"Exact timestamp: {dt.strftime('%Y-%m-%d %H:%M:%S UTC')}"
                return result
            except ValueError:
                pass

        # 2. Check Approximate Period (e.g. "evening of 12 March 2026")
        approx_match = self.approx_period_pattern.search(clean_text)
        if approx_match:
            period_word = approx_match.group(1).lower()
            day = int(approx_match.group(2))
            month_str = approx_match.group(3).lower()
            year = int(approx_match.group(4))
            month = MONTHS.get(month_str)

            if month:
                try:
                    # Map period to hours window
                    period_hours = {
                        "morning": (6, 12),
                        "afternoon": (12, 18),
                        "evening": (18, 22),
                        "night": (22, 23),
                        "dawn": (4, 7),
                        "dusk": (17, 20)
                    }
                    start_h, end_h = period_hours.get(period_word, (0, 23))
                    start_dt = datetime(year, month, day, start_h, 0, 0, tzinfo=timezone.utc)
                    end_dt = datetime(year, month, day, end_h, 0, 0, tzinfo=timezone.utc)

                    result["start_time_utc"] = start_dt.isoformat()
                    result["end_time_utc"] = end_dt.isoformat()
                    result["time_precision"] = "HOUR"
                    result["is_approximate"] = True
                    result["confidence"] = 0.85
                    result["description"] = f"Approximate {period_word} on {year}-{month:02d}-{day:02d}"
                    return result
                except ValueError:
                    pass

        # 3. Check Date Range (e.g. "between 10 and 12 March 2026")
        range_match = self.date_range_pattern.search(clean_text)
        if range_match:
            start_day = int(range_match.group(1))
            end_day = int(range_match.group(2))
            month_str = range_match.group(3).lower()
            year = int(range_match.group(4))
            month = MONTHS.get(month_str)

            if month:
                try:
                    start_dt = datetime(year, month, start_day, 0, 0, 0, tzinfo=timezone.utc)
                    end_dt = datetime(year, month, end_day, 23, 59, 59, tzinfo=timezone.utc)
                    result["start_time_utc"] = start_dt.isoformat()
                    result["end_time_utc"] = end_dt.isoformat()
                    result["time_precision"] = "DAY"
                    result["confidence"] = 0.90
                    result["description"] = f"Date range: {year}-{month:02d}-{start_day:02d} to {year}-{month:02d}-{end_day:02d}"
                    return result
                except ValueError:
                    pass

        # 4. Check Date with Time (e.g. "12 March 2026 at 14:30")
        dt_match = self.date_time_pattern.search(clean_text)
        if dt_match:
            d = int(dt_match.group(1))
            m_raw = dt_match.group(2).lower()
            y = int(dt_match.group(3))
            m = MONTHS.get(m_raw) if m_raw.isalpha() else (int(m_raw) if m_raw.isdigit() else None)
            hh = int(dt_match.group(4))
            mm = int(dt_match.group(5))
            ss = int(dt_match.group(6)) if dt_match.group(6) else 0

            if m and 1 <= m <= 12:
                try:
                    dt = datetime(y, m, d, hh, mm, ss, tzinfo=timezone.utc)
                    result["start_time_utc"] = dt.isoformat()
                    result["end_time_utc"] = dt.isoformat()
                    result["time_precision"] = "EXACT" if dt_match.group(6) else "MINUTE"
                    result["confidence"] = 0.95
                    result["description"] = f"Timestamp: {dt.strftime('%Y-%m-%d %H:%M:%S UTC')}"
                    return result
                except ValueError:
                    pass

        # 5. Check Time Range on an associated date (e.g. "10:00–11:00")
        tr_match = self.time_range_pattern.search(clean_text)
        if tr_match:
            shh, smm = int(tr_match.group(1)), int(tr_match.group(2))
            ehh, emm = int(tr_match.group(4)), int(tr_match.group(5))
            base_date = reference_date or datetime(2026, 3, 10, tzinfo=timezone.utc)
            try:
                start_dt = datetime(base_date.year, base_date.month, base_date.day, shh, smm, 0, tzinfo=timezone.utc)
                end_dt = datetime(base_date.year, base_date.month, base_date.day, ehh, emm, 0, tzinfo=timezone.utc)
                result["start_time_utc"] = start_dt.isoformat()
                result["end_time_utc"] = end_dt.isoformat()
                result["time_precision"] = "MINUTE"
                result["confidence"] = 0.92
                result["description"] = f"Time window: {shh:02d}:{smm:02d} to {ehh:02d}:{emm:02d} UTC"
                return result
            except ValueError:
                pass

        # 6. Check Standard Dates
        # 6a. "12 March 2026"
        dmy_text = self.dmy_text_pattern.search(clean_text)
        if dmy_text:
            d = int(dmy_text.group(1))
            m_str = dmy_text.group(2).lower()
            y = int(dmy_text.group(3))
            m = MONTHS.get(m_str)
            if m:
                try:
                    start_dt = datetime(y, m, d, 0, 0, 0, tzinfo=timezone.utc)
                    end_dt = datetime(y, m, d, 23, 59, 59, tzinfo=timezone.utc)
                    result["start_time_utc"] = start_dt.isoformat()
                    result["end_time_utc"] = end_dt.isoformat()
                    result["time_precision"] = "DATE_ONLY"
                    result["confidence"] = 0.95
                    result["description"] = f"Date: {y}-{m:02d}-{d:02d}"
                    return result
                except ValueError:
                    pass

        # 6b. "2026-03-12"
        ymd = self.ymd_numeric_pattern.search(clean_text)
        if ymd:
            y, m, d = int(ymd.group(1)), int(ymd.group(2)), int(ymd.group(3))
            try:
                start_dt = datetime(y, m, d, 0, 0, 0, tzinfo=timezone.utc)
                end_dt = datetime(y, m, d, 23, 59, 59, tzinfo=timezone.utc)
                result["start_time_utc"] = start_dt.isoformat()
                result["end_time_utc"] = end_dt.isoformat()
                result["time_precision"] = "DATE_ONLY"
                result["confidence"] = 0.95
                result["description"] = f"Date: {y}-{m:02d}-{d:02d}"
                return result
            except ValueError:
                pass

        # 6c. "12/03/2026" (DD/MM/YYYY Indian convention)
        dmy_num = self.dmy_numeric_pattern.search(clean_text)
        if dmy_num:
            d, m, y = int(dmy_num.group(1)), int(dmy_num.group(2)), int(dmy_num.group(3))
            if 1 <= m <= 12 and 1 <= d <= 31:
                try:
                    start_dt = datetime(y, m, d, 0, 0, 0, tzinfo=timezone.utc)
                    end_dt = datetime(y, m, d, 23, 59, 59, tzinfo=timezone.utc)
                    result["start_time_utc"] = start_dt.isoformat()
                    result["end_time_utc"] = end_dt.isoformat()
                    result["time_precision"] = "DATE_ONLY"
                    result["confidence"] = 0.92
                    result["description"] = f"Date: {y}-{m:02d}-{d:02d}"
                    return result
                except ValueError:
                    pass

        # 7. Check Month-Year (e.g. "March 2026")
        my = self.month_year_pattern.search(clean_text)
        if my:
            m_raw = my.group(1).lower()
            y = int(my.group(2))
            m = MONTHS.get(m_raw) if m_raw.isalpha() else (int(m_raw) if m_raw.isdigit() else None)
            if m and 1 <= m <= 12:
                try:
                    start_dt = datetime(y, m, 1, 0, 0, 0, tzinfo=timezone.utc)
                    # Next month start minus 1 second
                    next_y = y if m < 12 else y + 1
                    next_m = m + 1 if m < 12 else 1
                    end_dt = datetime(next_y, next_m, 1, 0, 0, 0, tzinfo=timezone.utc) - timedelta(seconds=1)
                    result["start_time_utc"] = start_dt.isoformat()
                    result["end_time_utc"] = end_dt.isoformat()
                    result["time_precision"] = "MONTH"
                    result["confidence"] = 0.88
                    result["description"] = f"Month: {y}-{m:02d}"
                    return result
                except ValueError:
                    pass

        # 8. Check Relative Date (e.g. "three days later")
        rel = self.relative_pattern.search(clean_text)
        if rel:
            word_map = {"one": 1, "two": 2, "three": 3, "four": 4, "five": 5, "six": 6, "seven": 7, "eight": 8, "nine": 9, "ten": 10}
            val_str = rel.group(1).lower()
            delta_days = word_map.get(val_str) if val_str in word_map else (int(val_str) if val_str.isdigit() else None)

            if delta_days and reference_date:
                target_date = reference_date + timedelta(days=delta_days)
                start_dt = datetime(target_date.year, target_date.month, target_date.day, 0, 0, 0, tzinfo=timezone.utc)
                end_dt = datetime(target_date.year, target_date.month, target_date.day, 23, 59, 59, tzinfo=timezone.utc)
                result["start_time_utc"] = start_dt.isoformat()
                result["end_time_utc"] = end_dt.isoformat()
                result["time_precision"] = "DAY"
                result["is_approximate"] = True
                result["confidence"] = 0.80
                result["description"] = f"Relative: +{delta_days} days from {reference_date.strftime('%Y-%m-%d')}"
                return result
            else:
                result["time_precision"] = "APPROXIMATE"
                result["is_approximate"] = True
                result["confidence"] = 0.50
                result["description"] = f"Unresolved relative temporal expression: {clean_text}"
                return result

        # Fallback for unrecognizable strings
        return result
