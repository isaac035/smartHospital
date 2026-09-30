from datetime import datetime, timezone
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, field_validator
from pydantic.alias_generators import to_camel

Priority = Literal["Normal", "Urgent", "Emergency"]


class CamelModel(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)


class RecommendedDoctor(CamelModel):
    doctor_id: int = Field(gt=0)
    doctor_profile_id: int = Field(gt=0)
    name: str = Field(min_length=1, max_length=200)
    specialization: str = Field(default="", max_length=150)
    department: str = Field(min_length=1, max_length=100)


class RealSlot(CamelModel):
    """A slot supplied by the API from AvailabilityService; never synthesized here."""

    doctor_id: int = Field(gt=0)
    doctor_profile_id: int = Field(gt=0)
    slot_start: datetime
    slot_end: datetime
    duration_minutes: int = Field(gt=0, le=480)
    status: str = Field(min_length=1, max_length=32)

    @field_validator("slot_start", "slot_end")
    @classmethod
    def require_utc_instant(cls, value: datetime) -> datetime:
        if value.tzinfo is None or value.utcoffset() is None:
            raise ValueError("Slot times must include a UTC offset.")
        return value.astimezone(timezone.utc)


class OptimizeRequest(CamelModel):
    patient_id: int = Field(gt=0)
    category: str = Field(min_length=1, max_length=100)
    priority: Priority
    recommended_doctors: list[RecommendedDoctor] = Field(min_length=1, max_length=5)
    available_slots: list[RealSlot] = Field(max_length=500)


class SlotChoice(CamelModel):
    doctor_id: int
    doctor_profile_id: int
    slot_start: datetime
    slot_end: datetime
    duration_minutes: int
    reason: str | None = None


class OptimizeResponse(CamelModel):
    patient_id: int
    doctor_id: int
    recommended_slot: SlotChoice | None = None
    alternative_slots: list[SlotChoice]
    message: str | None = None
