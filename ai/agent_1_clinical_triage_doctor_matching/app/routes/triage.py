import logging
from typing import Annotated

from fastapi import APIRouter, Depends
from fastapi.responses import JSONResponse

from app.schemas.triage import (
    EMERGENCY_NOTICE,
    ErrorResponse,
    TriageMatchRequest,
    TriageMatchResponse,
)
from app.services.doctor_matching import match_doctors, no_doctors_message
from app.services.llm_client import AIServiceError, TriageLLM, create_triage_llm
from app.services.triage_service import triage
from app.utils.red_flags import has_emergency_red_flag
from app.utils.security import require_internal_key
from app.utils.settings import Settings, get_settings

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/v1", dependencies=[Depends(require_internal_key)])


def get_llm(settings: Annotated[Settings, Depends(get_settings)]) -> TriageLLM | None:
    try:
        return create_triage_llm(settings)
    except AIServiceError as exc:
        logger.error("AI provider is not configured: %s", exc)
        return None


@router.post(
    "/triage-doctor-match",
    response_model=TriageMatchResponse,
    response_model_by_alias=True,
    responses={503: {"model": ErrorResponse}},
)
def triage_doctor_match(
    request: TriageMatchRequest,
    settings: Annotated[Settings, Depends(get_settings)],
    llm: Annotated[TriageLLM | None, Depends(get_llm)],
):
    try:
        if llm is None:
            raise AIServiceError("AI provider is not configured.")
        result = triage(
            llm,
            request.text,
            request.categories,
            request.default_category,
            settings.min_confidence,
        )
    except AIServiceError as exc:
        # The red-flag screen still applies when the AI is unavailable.
        emergency = has_emergency_red_flag(request.text)
        body = ErrorResponse(
            detail=str(exc),
            possible_emergency=emergency,
            emergency_notice=EMERGENCY_NOTICE if emergency else None,
        )
        return JSONResponse(status_code=503, content=body.model_dump(by_alias=True))

    doctors = match_doctors(result.category, request.candidate_doctors, settings.max_recommendations)
    return TriageMatchResponse(
        patient_id=request.patient_id,
        category=result.category,
        priority=result.priority,
        reason=result.reason,
        confidence=result.confidence,
        possible_emergency=result.possible_emergency,
        emergency_notice=EMERGENCY_NOTICE if result.possible_emergency else None,
        used_default_category=result.used_default_category,
        recommended_doctors=doctors,
        message=None if doctors else no_doctors_message(result.category),
    )
