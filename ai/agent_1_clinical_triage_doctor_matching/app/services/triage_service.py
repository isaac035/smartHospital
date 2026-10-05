"""Clinical triage: turns the LLM's untrusted output into a validated
recommendation constrained to the real specialty list and priority scale."""

import math

from app.schemas.triage import PRIORITIES, TriageResult
from app.services.llm_client import TriageLLM
from app.utils.language import fallback_reason, safe_reason
from app.utils.red_flags import has_emergency_red_flag


def triage(
    llm: TriageLLM,
    text: str,
    categories: list[str],
    default_category: str,
    min_confidence: float,
) -> TriageResult:
    """Raises AIServiceError if the model is unavailable; callers must still
    honour the red-flag screen in that case (see red_flags)."""
    red_flag = has_emergency_red_flag(text)
    raw = llm.classify(text, categories)

    canonical = {c.casefold(): c for c in categories}
    default = canonical[default_category.casefold()]

    confidence = raw.confidence if math.isfinite(raw.confidence) else 0.0
    confidence = round(min(max(confidence, 0.0), 1.0), 2)

    category = canonical.get(" ".join(raw.category.split()).casefold())
    used_default = category is None or confidence < min_confidence
    if used_default:
        # Don't force a low-confidence or out-of-list answer into a specialty.
        category = default
        reason = fallback_reason(category)
    else:
        reason = safe_reason(raw.reason, category)

    priority = raw.priority if raw.priority in PRIORITIES else "Normal"
    possible_emergency = raw.possible_emergency or red_flag
    if possible_emergency:
        priority = "Emergency"

    return TriageResult(
        category=category,
        priority=priority,
        reason=reason,
        confidence=confidence,
        possible_emergency=possible_emergency,
        used_default_category=used_default,
    )
