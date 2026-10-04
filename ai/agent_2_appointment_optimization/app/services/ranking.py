from datetime import datetime, timedelta, timezone

from app.schemas.optimization import OptimizeRequest, RealSlot, SlotChoice


def rank_available_slots(request: OptimizeRequest) -> list[RealSlot]:
    doctor_ids = {doctor.doctor_id for doctor in request.recommended_doctors}
    # The backend's availability service can return generated schedule entries marked
    # Booked. Only currently free entries from recommended doctors are candidates.
    slots = [
        slot for slot in request.available_slots
        if slot.status.casefold() == "available" and slot.doctor_id in doctor_ids
    ]
    # Urgency narrows the alternatives to a nearer window when that window has real slots.
    # If it has none, retain the farther real slots so the patient still gets a useful option.
    horizon_days = {"Normal": 30, "Urgent": 14, "Emergency": 7}[request.priority]
    now = datetime.now(timezone.utc)
    near_term = [slot for slot in slots if slot.slot_start <= now + timedelta(days=horizon_days)]
    if near_term:
        slots = near_term
    # Within the applicable window, earlier real slots always rank first.
    return sorted(slots, key=lambda slot: (slot.slot_start, slot.doctor_id, slot.doctor_profile_id))


def choice(slot: RealSlot, reason: str | None = None) -> SlotChoice:
    return SlotChoice(
        doctor_id=slot.doctor_id,
        doctor_profile_id=slot.doctor_profile_id,
        slot_start=slot.slot_start.astimezone(timezone.utc),
        slot_end=slot.slot_end.astimezone(timezone.utc),
        duration_minutes=slot.duration_minutes,
        reason=reason,
    )


def deterministic_reason(priority: str, slot: RealSlot, now: datetime | None = None) -> str:
    now = now or datetime.now(timezone.utc)
    days = max(0, (slot.slot_start.date() - now.date()).days)
    urgency = "for your emergency priority" if priority == "Emergency" else (
        "for your urgent priority" if priority == "Urgent" else ""
    )
    if days == 0:
        timing = "today"
    elif days == 1:
        timing = "tomorrow"
    else:
        timing = f"in {days} days"
    if urgency:
        return f"This is an early available appointment {urgency}, {timing}."
    return f"This is the earliest available appointment, {timing}."
