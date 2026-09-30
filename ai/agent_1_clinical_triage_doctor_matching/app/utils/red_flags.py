"""Deterministic emergency red-flag screen.

This runs on every request, before and independently of the LLM, so the
emergency notice appears even if the model misses it or the AI call fails.
It only ever raises the flag - it never lowers one the model set.
"""

import re

_RED_FLAG_PATTERNS = [
    # Chest pain / pressure
    r"\bchest\b.{0,25}\b(pain|pressure|tight(ness)?|crushing|squeez\w*)\b",
    r"\b(crushing|severe|sharp)\b.{0,25}\bchest\b",
    # Breathing
    r"\b(can'?t|cannot|can not|unable to|struggling to|hard to)\s+breath\w*",
    r"\b(difficulty|trouble|problems?)\s+breathing\b",
    r"\bshort(ness)?\s+of\s+breath\b",
    r"\bnot\s+breathing\b",
    r"\bchoking\b",
    # Stroke signs
    r"\bstroke\b",
    r"\bface\b.{0,15}\bdroop\w*",
    r"\bslurred\s+speech\b",
    r"\b(numb(ness)?|weak(ness)?|paralys\w*)\b.{0,25}\b(one|left|right)\s+side\b",
    r"\bsudden(ly)?\b.{0,25}\b(can'?t|cannot|unable to)\s+(speak|talk|move|see)\b",
    # Severe bleeding
    r"\b(severe|heavy|heavily|uncontrolled|a lot of)\b.{0,15}\bbleed\w*",
    r"\bbleed\w*\b.{0,20}\b(won'?t|will not|doesn'?t|does not|can'?t|cannot)\s+stop\b",
    r"\b(coughing|vomiting|throwing)\s+(up\s+)?blood\b",
    # Loss of consciousness
    r"\b(unconscious|unresponsive|passed\s+out|fainted|collapsed|blacked\s+out)\b",
    r"\bloss\s+of\s+consciousness\b",
    # Seizure
    r"\b(seizures?|convuls\w*)\b",
    # Severe allergic reaction
    r"\banaphyla\w*",
    r"\bthroat\b.{0,15}\b(closing|swelling|swollen)\b",
    # Self-harm / overdose / poisoning
    r"\bsuicid\w*",
    r"\b(kill|hurt|harm)\s+myself\b",
    r"\bend\s+my\s+life\b",
    r"\boverdos\w*",
    r"\bpoison\w*",
]

_COMPILED = [re.compile(p, re.IGNORECASE) for p in _RED_FLAG_PATTERNS]


def _normalize(text: str) -> str:
    # Curly apostrophes -> straight, collapse whitespace.
    return " ".join(text.replace("’", "'").replace("‘", "'").split())


def has_emergency_red_flag(text: str) -> bool:
    normalized = _normalize(text)
    return any(pattern.search(normalized) for pattern in _COMPILED)
