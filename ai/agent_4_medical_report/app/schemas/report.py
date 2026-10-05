from pydantic import BaseModel, Field


class ReportReasonRequest(BaseModel):
    recorded_data: dict = Field(alias="recordedData")


class ReportReasonResponse(BaseModel):
    ai_summary: str = Field(alias="aiSummary")
    ai_recommendations: list[str] = Field(alias="aiRecommendations")
    mode: str = "gemini"

    model_config = {"populate_by_name": True}
