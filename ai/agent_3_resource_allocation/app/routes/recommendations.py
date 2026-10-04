from fastapi import APIRouter, Depends
from app.schemas.allocation import RecommendRequest, RecommendResponse
from app.services.recommender import rank
from app.utils.security import require_internal_key
from app.utils.settings import Settings, get_settings

router = APIRouter(prefix="/v1", dependencies=[Depends(require_internal_key)])

@router.post("/recommendations", response_model=RecommendResponse)
def recommendations(request: RecommendRequest, settings: Settings = Depends(get_settings)) -> RecommendResponse:
    ranked = rank(request)
    ranked = ranked[:max(1, min(settings.AGENT3_MAX_RECOMMENDATIONS, 100))]
    return RecommendResponse(
        appointment_id=request.appointment_id,
        clinical_specialty=request.clinical_specialty,
        recommendations=ranked,
        message=(
            "No suitable resources are currently available."
            if not ranked else
            "No specialty-specific or general-purpose resource is available; the remaining options are specialized for other care types."
            if max(item.score for item in ranked) < 20 else None
        ),
    )
