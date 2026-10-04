from app.schemas.allocation import Candidate, RankedCandidate, RecommendRequest

GROUPS = {
    "cardiology": ("cardiology", "cardiac", "heart", "cardiovascular"),
    "maternity": ("maternity", "postnatal", "obstetric", "gynecolog", "gynaecolog", "birthing", "fetal", "newborn", "delivery"),
    "pediatric": ("pediatric", "paediatric", "child", "infant", "neonatal", "nicu"),
    "emergency": ("emergency", "accident", "trauma", "er ward", "emergency room", "resuscitation", "defibrillator"),
    "surgical": ("surgical", "surgery", "operating theater", "operating theatre", "orthopedic", "orthopaedic"),
    "icu": ("icu", "critical care", "intensive care", "ventilator", "high dependency"),
    "general": ("general", "internal medicine", "primary care"),
}

def _group(text: str) -> str | None:
    value = text.casefold()
    for group, terms in GROUPS.items():
        if any(term in value for term in terms):
            return group
    return None

def _candidate_group(candidate: Candidate) -> str | None:
    return _group(" ".join((candidate.name, candidate.type, candidate.specialty_text)))

def rank(request: RecommendRequest) -> list[RankedCandidate]:
    specialty = request.clinical_specialty or request.department_name or request.doctor_specialty
    wanted = _group(specialty)
    rows: list[RankedCandidate] = []
    for candidate in request.candidates:
        group = _candidate_group(candidate)
        # Strong match dominates all priority adjustments (max delta 4).
        if wanted and group == wanted:
            score, reason = 100, f"Matches the {specialty} specialty."
            # Priority is a small acuity tie-breaker within an already matching specialty.
            acuity_text = f"{candidate.name} {candidate.type} {candidate.specialty_text}".casefold()
            if request.priority == "Emergency" and any(term in acuity_text for term in ("icu", "critical", "intensive")):
                score += 4
            elif request.priority == "Urgent" and any(term in acuity_text for term in ("high dependency", "step-down", "monitor")):
                score += 2
        elif group == "general":
            score, reason = 35, f"General option; no {specialty}-specific match is available."
        elif group is None:
            score, reason = 20, "Available general-purpose option; specialty fit is not explicit in its record."
        else:
            score, reason = -30, f"Specialized for {group}; weaker fit for {specialty}."
        if request.priority != "Normal":
            reason += f" {request.priority} priority is a secondary tie-breaker only."
        rows.append(RankedCandidate(**candidate.model_dump(), score=score, reason=reason))
    rows.sort(key=lambda item: (-item.score, item.kind, item.name.casefold(), item.resource_id))
    return rows
