"""Guards the patient-facing 'reason' text: recommendation language only,
never a diagnosis, a named disease stated as fact, or medication advice."""

import re

MAX_REASON_LENGTH = 300

_UNSAFE_PATTERNS = [
    r"\byou\s+(have|definitely have|are suffering from|suffer from)\b",
    r"\bthis\s+(means|confirms)\s+(that\s+)?you\b",
    r"\bdiagnos\w*",
    r"\bprescri\w*",
    r"\b\d+(\.\d+)?\s?(mg|mcg|ml|g|iu|units?)\b",
    r"\b(dose|dosage|tablets?|pills?|capsules?)\b",
    r"\btake\s+(some\s+)?(paracetamol|ibuprofen|aspirin|antibiotics?|medicine|medication)\b",
]
_COMPILED = [re.compile(p, re.IGNORECASE) for p in _UNSAFE_PATTERNS]


def default_reason(category: str) -> str:
    return (
        f"Based on what you described, a {category} doctor may be a good place to start. "
        "They can assess you and refer you on if needed."
    )


def fallback_reason(category: str) -> str:
    return (
        "Your description doesn't clearly point to one specialty, so we recommend "
        f"starting with {category}. A doctor there can assess you and refer you on if needed."
    )


def safe_reason(raw_reason: str | None, category: str) -> str:
    """Returns the model's reason if it uses recommendation language, otherwise a
    neutral templated reason. Never passes diagnostic or dosing text through."""
    reason = " ".join((raw_reason or "").split())
    if not reason or any(p.search(reason) for p in _COMPILED):
        return default_reason(category)
    if len(reason) > MAX_REASON_LENGTH:
        cut = reason[:MAX_REASON_LENGTH].rsplit(" ", 1)[0]
        reason = cut.rstrip(",;:") + "..."
    return reason
