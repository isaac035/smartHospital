import asyncio
import logging

from google import genai

from app.schemas.optimization import RealSlot
from app.utils.settings import Settings

logger = logging.getLogger(__name__)


async def explain(settings: Settings, priority: str, slot: RealSlot, fallback: str) -> str:
    """Gemini may phrase the reason; it cannot return or modify any slot fields."""
    if not settings.gemini_api_key:
        return fallback
    try:
        client = genai.Client(api_key=settings.gemini_api_key)
        prompt = (
            "Write one short patient-friendly sentence explaining this appointment suggestion. "
            "Do not diagnose, promise urgency handling, or change any date/time. "
            f"Priority request: {priority}. Slot date: {slot.slot_start.date().isoformat()}."
        )
        response = await asyncio.wait_for(
            client.aio.models.generate_content(model=settings.gemini_model, contents=prompt),
            timeout=settings.gemini_timeout_seconds,
        )
        text = (response.text or "").strip()
        if not text or len(text) > 240:
            return fallback
        return text
    except Exception as exc:  # Gemini is explanatory only; deterministic ranking stays available.
        logger.info("Gemini explanation unavailable; using deterministic reason (%s).", type(exc).__name__)
        return fallback
