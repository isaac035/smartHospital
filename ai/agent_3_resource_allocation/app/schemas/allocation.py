from typing import Literal
from pydantic import BaseModel, ConfigDict, Field, field_validator
from pydantic.alias_generators import to_camel

Priority = Literal["Normal", "Urgent", "Emergency"]

class CamelModel(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)

class Candidate(CamelModel):
    resource_id: int = Field(gt=0)
    kind: Literal["bed", "equipment"]
    name: str
    type: str
    location: str = ""
    status: str
    specialty_text: str = ""

    @field_validator("status")
    @classmethod
    def candidates_must_be_currently_available(cls, value: str) -> str:
        if value.casefold() != "available":
            raise ValueError("Only currently available database records may be recommended.")
        return value

class RecommendRequest(CamelModel):
    appointment_id: int = Field(gt=0)
    clinical_specialty: str = Field(min_length=1, max_length=150)
    doctor_specialty: str = ""
    department_name: str = ""
    priority: Priority = "Normal"
    candidates: list[Candidate] = Field(max_length=500)

class RankedCandidate(Candidate):
    score: int
    reason: str

class RecommendResponse(CamelModel):
    appointment_id: int
    clinical_specialty: str
    recommendations: list[RankedCandidate]
    message: str | None = None
