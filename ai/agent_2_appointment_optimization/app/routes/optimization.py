from typing import Annotated

from fastapi import APIRouter, Depends

from app.schemas.optimization import OptimizeRequest, OptimizeResponse
from app.services.explanation import explain
from app.services.ranking import choice, deterministic_reason, rank_available_slots
from app.utils.security import require_internal_key
from app.utils.settings import Settings, get_settings

router = APIRouter(prefix="/v1", dependencies=[Depends(require_internal_key)])


@router.post("/optimize-appointment", response_model=OptimizeResponse, response_model_by_alias=True)
async def optimize_appointment(
    request: OptimizeRequest,
    settings: Annotated[Settings, Depends(get_settings)],
) -> OptimizeResponse:
    ranked = rank_available_slots(request)
    if not ranked:
        doctor_names = ", ".join(doctor.name for doctor in request.recommended_doctors)
        return OptimizeResponse(
            patient_id=request.patient_id,
            doctor_id=request.recommended_doctors[0].doctor_id,
            alternative_slots=[],
            message=(f"No available appointment slots were found for {doctor_names} in the next 30 days. "
                     "Try another recommended doctor or browse doctors and slots manually."),
        )

    first = ranked[0]
    fallback = deterministic_reason(request.priority, first)
    reason = await explain(settings, request.priority, first, fallback)
    return OptimizeResponse(
        patient_id=request.patient_id,
        doctor_id=first.doctor_id,
        recommended_slot=choice(first, reason),
        alternative_slots=[choice(slot) for slot in ranked[1:settings.max_alternatives + 1]],
    )
