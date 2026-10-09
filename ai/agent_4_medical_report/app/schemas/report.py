from pydantic import BaseModel, Field


class ReportReasonRequest(BaseModel):
    recorded_data: dict = Field(alias="recordedData")


class DietAdvice(BaseModel):
    prefer: list[str] = Field(default_factory=list)
    avoid: list[str] = Field(default_factory=list)


class ExerciseAdvice(BaseModel):
    suitable: list[str] = Field(default_factory=list)
    avoid: list[str] = Field(default_factory=list)


class FollowUpAdvice(BaseModel):
    timeframe: str = ""
    monitoring: list[str] = Field(default_factory=list)


class Recommendations(BaseModel):
    """Patient-friendly lifestyle guidance. Every field is optional; empty lists are omitted by the viewers."""

    basis: str = "general"
    diet: DietAdvice = Field(default_factory=DietAdvice)
    exercise: ExerciseAdvice = Field(default_factory=ExerciseAdvice)
    lifestyle: list[str] = Field(default_factory=list)
    warning_signs: list[str] = Field(default_factory=list, alias="warningSigns")
    follow_up: FollowUpAdvice = Field(default_factory=FollowUpAdvice, alias="followUp")
    note: str = ""

    model_config = {"populate_by_name": True}


class LlmReportOutput(BaseModel):
    """Shape requested from the model. Parsed leniently so a bad recommendations block never loses the summary."""

    ai_summary: str = Field(alias="aiSummary")
    recommendations: Recommendations | None = None

    model_config = {"populate_by_name": True}


class ReportReasonResponse(BaseModel):
    ai_summary: str = Field(alias="aiSummary")
    ai_recommendations: list[str] = Field(alias="aiRecommendations")
    mode: str = "gemini"
    # Optional addition: older API versions ignore it, and it is None when unavailable or invalid.
    recommendations: Recommendations | None = None

    model_config = {"populate_by_name": True}
