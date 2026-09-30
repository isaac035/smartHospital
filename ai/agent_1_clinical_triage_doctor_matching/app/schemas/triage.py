from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator
from pydantic.alias_generators import to_camel

# Exactly the AppointmentPriority enum used by Appointment & Queue Management
# (Normal = 1, Urgent = 2, Emergency = 3). No other scale is ever produced.
Priority = Literal["Normal", "Urgent", "Emergency"]
PRIORITIES: tuple[str, ...] = ("Normal", "Urgent", "Emergency")

EMERGENCY_NOTICE = (
    "If this is a medical emergency, call emergency services or go to the "
    "nearest emergency department now."
)


class CamelModel(BaseModel):
    """JSON uses camelCase to match the ASP.NET Core API's contract."""

    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)


class DoctorCandidate(CamelModel):
    """A doctor the ASP.NET Core API has already confirmed is bookable. The
    service only ever recommends from this list - it never invents doctors."""

    doctor_id: int = Field(gt=0, description="Doctor's User id - the id the appointment system books against.")
    doctor_profile_id: int = Field(gt=0, description="Doctor profile id (Doctors table).")
    name: str = Field(min_length=1, max_length=200)
    specialization: str = Field(default="", max_length=150)
    department: str = Field(min_length=1, max_length=100)


class TriageMatchRequest(CamelModel):
    # Derived from the JWT by the ASP.NET Core API - never from patient input.
    patient_id: int = Field(gt=0)
    text: str = Field(min_length=3, max_length=1000)
    # The real, currently active specialty vocabulary (department names).
    categories: list[str] = Field(min_length=1, max_length=100)
    default_category: str = Field(min_length=1, max_length=100)
    candidate_doctors: list[DoctorCandidate] = Field(default_factory=list, max_length=1000)

    @field_validator("text")
    @classmethod
    def _strip_text(cls, value: str) -> str:
        value = value.strip()
        if len(value) < 3:
            raise ValueError("Please describe your symptoms or what you need help with.")
        return value

    @field_validator("categories")
    @classmethod
    def _clean_categories(cls, value: list[str]) -> list[str]:
        seen: dict[str, str] = {}
        for item in value:
            name = " ".join(item.split())
            if name and name.casefold() not in seen:
                seen[name.casefold()] = name
        if not seen:
            raise ValueError("At least one category is required.")
        return list(seen.values())

    @model_validator(mode="after")
    def _default_in_categories(self) -> "TriageMatchRequest":
        if self.default_category.casefold() not in {c.casefold() for c in self.categories}:
            raise ValueError("defaultCategory must be one of categories.")
        return self


class RecommendedDoctor(CamelModel):
    doctor_id: int
    doctor_profile_id: int
    name: str
    specialization: str
    department: str


class TriageResult(CamelModel):
    """Validated clinical triage (Section 4 contract)."""

    category: str
    priority: Priority
    reason: str
    confidence: float = Field(ge=0, le=1)
    possible_emergency: bool
    used_default_category: bool


class TriageMatchResponse(CamelModel):
    """Final contract returned to the ASP.NET Core API (Sections 4 and 6)."""

    patient_id: int
    category: str
    priority: Priority
    reason: str
    confidence: float
    possible_emergency: bool
    emergency_notice: str | None = None
    used_default_category: bool
    recommended_doctors: list[RecommendedDoctor]
    message: str | None = None


class ErrorResponse(CamelModel):
    detail: str
    possible_emergency: bool = False
    emergency_notice: str | None = None
