import json
import logging
import os
import re

from app.schemas.report import (
    DietAdvice,
    ExerciseAdvice,
    FollowUpAdvice,
    LlmReportOutput,
    Recommendations,
    ReportReasonResponse,
)

logger = logging.getLogger(__name__)

SYSTEM_INSTRUCTIONS = """You organize a patient's supplied medical record into a concise summary for clinical review.
Use only the supplied recordedData. Never invent facts, symptoms, test results, medications, or history.
Do not diagnose with certainty and do not predict future disease. Distinguish historical records from the current encounter.
Keep recorded data separate from your synthesis and from advisory recommendations. Recommendations are suggestions for clinical review, never directives or diagnoses.
Never claim a test was completed if it is only recommended. If data is empty or unavailable, say that it is not recorded.
Do not repeat or alter source records.

Return JSON with:
- aiSummary: plain text, 2-5 short sentences in simple English a patient can read.
- recommendations: patient-friendly lifestyle guidance based ONLY on the recorded diagnoses, symptoms, vital signs,
  prescriptions, lab results, age, doctor specialty and visit type. Fields:
  - basis: "condition_specific" when the recorded data supports specific advice, otherwise "general".
  - diet.prefer / diet.avoid: foods or eating habits to prefer / to limit.
  - exercise.suitable / exercise.avoid: suitable activity and activity to avoid (e.g. heavy lifting) when relevant.
  - lifestyle: sleep, stress, hydration, smoking or alcohol advice when relevant.
  - warningSigns: symptoms that mean the patient should contact a doctor or go to the emergency department.
  - followUp.timeframe: a short suggested timeframe such as "about 1 month"; use a recorded follow-up date if one exists.
  - followUp.monitoring: measurements or tests worth repeating (e.g. blood pressure, blood sugar, lipid profile).
  - note: when data is limited, say briefly that the advice is general.
Rules for recommendations: at most 5 items per list, each item one plain-text sentence under 200 characters.
No markdown, no HTML, no bullet symbols. Never name a medicine that is not already prescribed, never give doses or tell
the patient to start, stop or change a medicine, never promise a cure, never add a new diagnosis. Be specific to the
condition when the data supports it (e.g. heart-healthy advice for a cardiology case, pregnancy-appropriate advice for a
pregnancy case); otherwise give short general healthy-living advice and set basis to "general"."""

MAX_ITEMS = 5
MAX_ITEM_LENGTH = 200
MAX_TIMEFRAME_LENGTH = 80
MAX_NOTE_LENGTH = 300

_TEXT_LIST = {"type": "ARRAY", "items": {"type": "STRING"}}
# Explicit Gemini response schema (no defaults/aliases, which the schema converter may not accept).
RESPONSE_SCHEMA = {
    "type": "OBJECT",
    "properties": {
        "aiSummary": {"type": "STRING"},
        "recommendations": {
            "type": "OBJECT",
            "nullable": True,
            "properties": {
                "basis": {"type": "STRING", "enum": ["condition_specific", "general"]},
                "diet": {"type": "OBJECT", "properties": {"prefer": _TEXT_LIST, "avoid": _TEXT_LIST}},
                "exercise": {"type": "OBJECT", "properties": {"suitable": _TEXT_LIST, "avoid": _TEXT_LIST}},
                "lifestyle": _TEXT_LIST,
                "warningSigns": _TEXT_LIST,
                "followUp": {"type": "OBJECT", "properties": {"timeframe": {"type": "STRING"}, "monitoring": _TEXT_LIST}},
                "note": {"type": "STRING"},
            },
        },
    },
    "required": ["aiSummary"],
}

_LEADING_MARKERS = re.compile(r"^\s*(?:[-*•#>]+|\d+[.)])\s*")
_MARKDOWN_CHARS = re.compile(r"[*_`~]")
_WHITESPACE = re.compile(r"\s+")
_UNSAFE_PATTERNS = [
    re.compile(r"<[^>]*>|[<>]"),  # HTML / markup
    re.compile(r"javascript\s*:|https?://|www\.", re.IGNORECASE),  # links and script URLs
    re.compile(r"\b\d+(?:\.\d+)?\s?(?:mg|mcg|µg|g|ml|iu|units?)\b", re.IGNORECASE),  # doses
    re.compile(
        r"\b(?:start|stop|increase|decrease|double|reduce|change|skip|discontinue)\b.{0,40}\b(?:dose|dosage|medication|medicine|tablets?|pills?|insulin)\b",
        re.IGNORECASE,
    ),
    re.compile(r"\bcure[sd]?\b|\bguarantee", re.IGNORECASE),
]


def _clean_text(value, max_length: int = MAX_ITEM_LENGTH) -> str | None:
    """Plain-text, single-line, safe advice text; None when the value is unusable."""
    if not isinstance(value, str):
        return None
    text = _LEADING_MARKERS.sub("", value)
    text = _MARKDOWN_CHARS.sub("", text)
    text = _WHITESPACE.sub(" ", text).strip()
    if not text or len(text) > max_length:
        return None
    if any(pattern.search(text) for pattern in _UNSAFE_PATTERNS):
        return None
    return text


def _clean_list(value) -> list[str]:
    if not isinstance(value, list):
        return []
    cleaned: list[str] = []
    for item in value:
        text = _clean_text(item)
        if text and text not in cleaned:
            cleaned.append(text)
        if len(cleaned) == MAX_ITEMS:
            break
    return cleaned


def sanitize_recommendations(raw) -> Recommendations | None:
    """Validate model output field by field; drops unsafe items and returns None if nothing usable remains."""
    if not isinstance(raw, dict):
        return None
    def section(*keys):
        for key in keys:
            if isinstance(raw.get(key), dict):
                return raw[key]
        return {}

    diet = section("diet")
    exercise = section("exercise")
    follow_up = section("followUp", "follow_up")
    result = Recommendations(
        basis="condition_specific" if raw.get("basis") == "condition_specific" else "general",
        diet=DietAdvice(prefer=_clean_list(diet.get("prefer")), avoid=_clean_list(diet.get("avoid"))),
        exercise=ExerciseAdvice(suitable=_clean_list(exercise.get("suitable")), avoid=_clean_list(exercise.get("avoid"))),
        lifestyle=_clean_list(raw.get("lifestyle")),
        warningSigns=_clean_list(raw.get("warningSigns", raw.get("warning_signs"))),
        followUp=FollowUpAdvice(
            timeframe=_clean_text(follow_up.get("timeframe"), MAX_TIMEFRAME_LENGTH) or "",
            monitoring=_clean_list(follow_up.get("monitoring")),
        ),
        note=_clean_text(raw.get("note"), MAX_NOTE_LENGTH) or "",
    )
    has_content = any([
        result.diet.prefer, result.diet.avoid, result.exercise.suitable, result.exercise.avoid,
        result.lifestyle, result.warning_signs, result.follow_up.timeframe, result.follow_up.monitoring,
    ])
    return result if has_content else None


def _safe_recommendations(data: dict) -> list[str]:
    appointment = data.get("appointmentSummary") or {}
    triage = data.get("clinicalTriageSummary") or {}
    priority = str(triage.get("priority") or appointment.get("priority") or "").lower()
    if priority == "emergency":
        follow_up = "If symptoms worsen or new severe symptoms develop, seek immediate medical attention. Follow up with your doctor as they advise."
    elif priority == "urgent":
        follow_up = "A follow-up visit within the timeframe recommended by your doctor is advisable."
    elif priority == "normal":
        follow_up = "Routine follow-up as advised by your treating clinician is recommended."
    else:
        follow_up = "Follow up with your doctor as they advise."

    return [
        "Staying adequately hydrated may help.",
        "Light regular activity as tolerated by your current condition may help.",
        "Getting adequate rest is generally advisable.",
        "Avoid known triggers if any are documented on file.",
        follow_up,
    ]


def _fallback(data: dict) -> ReportReasonResponse:
    sections = data.get("sectionsPresent", [])
    summary = "Non-AI structured summary: this report organizes the recorded patient and encounter information. "
    summary += "Recorded sections: " + (", ".join(sections) if sections else "no clinical sections with recorded data") + "."
    return ReportReasonResponse(
        aiSummary=summary,
        aiRecommendations=_safe_recommendations(data),
        mode="non_ai_fallback",
    )


def _parse_model_output(text: str) -> tuple[str, Recommendations | None]:
    """Summary must be valid; recommendations are optional and validated separately."""
    payload = json.loads(text or "")
    summary = LlmReportOutput.model_validate({"aiSummary": payload.get("aiSummary")}).ai_summary
    try:
        recommendations = sanitize_recommendations(payload.get("recommendations"))
    except Exception:
        logger.warning("Agent 4 recommendations were invalid; omitting them from the report")
        recommendations = None
    return summary, recommendations


def reason(data: dict) -> ReportReasonResponse:
    api_key = os.getenv("GEMINI_API_KEY", "").strip()
    if not api_key:
        return _fallback(data)
    try:
        from google import genai
        from google.genai import types

        client = genai.Client(api_key=api_key)
        prompt = SYSTEM_INSTRUCTIONS + "\n\nRecorded data (JSON):\n" + json.dumps(data, ensure_ascii=False, default=str)
        response = client.models.generate_content(
            model=os.getenv("GEMINI_MODEL", "gemini-2.5-flash"),
            contents=prompt,
            config=types.GenerateContentConfig(
                response_mime_type="application/json",
                response_schema=RESPONSE_SCHEMA,
                temperature=0.1,
            ),
        )
        summary, recommendations = _parse_model_output(response.text or "")
        return ReportReasonResponse(
            aiSummary=summary,
            aiRecommendations=_safe_recommendations(data),
            mode="gemini",
            recommendations=recommendations,
        )
    except Exception:
        logger.exception("Gemini reasoning failed; returning structured non-AI fallback")
        return _fallback(data)
