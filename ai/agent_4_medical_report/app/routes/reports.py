from fastapi import APIRouter, Depends

from app.schemas.report import ReportReasonRequest, ReportReasonResponse
from app.services.reasoner import reason
from app.utils.security import require_internal_key

router = APIRouter(dependencies=[Depends(require_internal_key)])


@router.post("/reason", response_model=ReportReasonResponse, response_model_by_alias=True)
def reason_about_report(request: ReportReasonRequest) -> ReportReasonResponse:
    return reason(request.recorded_data)
