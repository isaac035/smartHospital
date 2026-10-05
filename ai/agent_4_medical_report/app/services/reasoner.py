import json
import logging
import os

from app.schemas.report import ReportReasonResponse

logger = logging.getLogger(__name__)

SYSTEM_INSTRUCTIONS = """You organize a patient's supplied medical record into a concise summary for clinical review.
Use only the supplied recordedData. Never invent facts, symptoms, test results, medications, or history.
Do not diagnose with certainty and do not predict future disease. Distinguish historical records from the current encounter.
Keep recorded data separate from your synthesis and from advisory recommendations. Recommendations are suggestions for clinical review, never directives or diagnoses.
Never claim a test was completed if it is only recommended. If data is empty or unavailable, say that it is not recorded.
Return JSON with aiSummary (plain text) and aiRecommendations. Do not repeat or alter source records. Do not invent a diagnosis, medical fact, or follow-up date/interval. Follow-up timing must defer to the treating doctor. Recommendations are replaced with deterministic safe wellness and priority guidance after generation."""


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
                response_schema=ReportReasonResponse,
                temperature=0.1,
            ),
        )
        parsed = ReportReasonResponse.model_validate_json(response.text or "")
        return ReportReasonResponse(
            aiSummary=parsed.ai_summary,
            aiRecommendations=_safe_recommendations(data),
            mode="gemini",
        )
    except Exception:
        logger.exception("Gemini reasoning failed; returning structured non-AI fallback")
        return _fallback(data)
