"""Doctor matching over the candidate list supplied by the ASP.NET Core API.

Candidates are real doctors the API has already confirmed are bookable. Matching
compares the validated category with each doctor's department using the same
normalisation on both sides - never the model's raw text against free-text
specialization strings."""

import re

from app.schemas.triage import DoctorCandidate, RecommendedDoctor

_NON_ALNUM = re.compile(r"[^0-9a-z]+")


def normalize(value: str) -> str:
    return _NON_ALNUM.sub("", value.casefold())


def match_doctors(
    category: str, candidates: list[DoctorCandidate], limit: int
) -> list[RecommendedDoctor]:
    key = normalize(category)
    seen: set[int] = set()
    matches: list[RecommendedDoctor] = []
    for doctor in sorted(candidates, key=lambda d: d.name.casefold()):
        if normalize(doctor.department) != key or doctor.doctor_id in seen:
            continue
        seen.add(doctor.doctor_id)
        matches.append(
            RecommendedDoctor(
                doctor_id=doctor.doctor_id,
                doctor_profile_id=doctor.doctor_profile_id,
                name=doctor.name,
                specialization=doctor.specialization,
                department=doctor.department,
            )
        )
        if len(matches) >= limit:
            break
    return matches


def no_doctors_message(category: str) -> str:
    return (
        f"No {category} doctors are available for online booking right now. "
        "You can browse all doctors instead."
    )
