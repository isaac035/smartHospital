from datetime import datetime, timedelta, timezone

import pytest

from app.schemas.optimization import OptimizeRequest, RealSlot, RecommendedDoctor
from app.services.explanation import explain
from app.services.ranking import deterministic_reason, rank_available_slots
from app.utils.settings import Settings


def make_request(priority="Normal", starts=None, status="Available"):
    starts = starts or [datetime.now(timezone.utc) + timedelta(days=3)]
    return OptimizeRequest(
        patientId=41,
        category="Cardiology",
        priority=priority,
        recommendedDoctors=[
            RecommendedDoctor(doctorId=12, doctorProfileId=4, name="Dr Test", department="Cardiology")
        ],
        availableSlots=[
            RealSlot(
                doctorId=12,
                doctorProfileId=4,
                slotStart=start,
                slotEnd=start + timedelta(minutes=30),
                durationMinutes=30,
                status=status,
            )
            for start in starts
        ],
    )


def test_multiple_real_slots_are_sorted_earliest_first():
    later = datetime.now(timezone.utc) + timedelta(days=4)
    earlier = datetime.now(timezone.utc) + timedelta(days=1)
    ranked = rank_available_slots(make_request(starts=[later, earlier]))
    assert [slot.slot_start for slot in ranked] == [earlier, later]


def test_only_backend_marked_available_slots_are_returned():
    request = make_request(status="Booked")
    assert rank_available_slots(request) == []


def test_doctor_with_zero_available_slots_returns_no_candidates():
    request = make_request()
    request.available_slots = []
    assert rank_available_slots(request) == []


def test_urgent_priority_favors_near_term_slots_without_inventing_any():
    now = datetime.now(timezone.utc)
    starts = [now + timedelta(days=3), now + timedelta(days=10), now + timedelta(days=20)]
    urgent = rank_available_slots(make_request(priority="Urgent", starts=starts))
    normal = rank_available_slots(make_request(priority="Normal", starts=starts))
    assert [slot.slot_start for slot in urgent] == starts[:2]
    assert [slot.slot_start for slot in normal] == starts
    backend_starts = {slot.slot_start for slot in make_request(starts=starts).available_slots}
    assert {slot.slot_start for slot in urgent}.issubset(backend_starts)


def test_slots_for_unrecommended_doctors_are_discarded():
    request = make_request()
    request.available_slots[0].doctor_id = 999
    assert rank_available_slots(request) == []


def test_ranked_slot_fields_are_taken_from_real_backend_slot():
    from app.services.ranking import choice

    request = make_request()
    slot = request.available_slots[0]
    ranked = rank_available_slots(request)
    output = choice(ranked[0])
    assert (output.doctor_id, output.doctor_profile_id, output.slot_start, output.slot_end) == (
        slot.doctor_id,
        slot.doctor_profile_id,
        slot.slot_start,
        slot.slot_end,
    )


@pytest.mark.parametrize("priority, expected", [
    ("Normal", "earliest available"),
    ("Urgent", "for your urgent priority"),
    ("Emergency", "for your emergency priority"),
])
def test_priority_changes_explanation_without_changing_real_slots(priority, expected):
    request = make_request(priority=priority)
    ranked = rank_available_slots(request)
    assert len(ranked) == 1
    assert expected in deterministic_reason(priority, ranked[0])


@pytest.mark.asyncio
async def test_gemini_failure_falls_back_to_deterministic_reason(monkeypatch):
    import app.services.explanation as explanation

    class BrokenModels:
        async def generate_content(self, **kwargs):
            raise RuntimeError("offline")

    class BrokenClient:
        class aio:
            models = BrokenModels()

    monkeypatch.setattr(explanation.genai, "Client", lambda **kwargs: BrokenClient())
    slot = make_request().available_slots[0]
    fallback = "Earliest available slot matching your priority."
    settings = Settings(gemini_api_key="test-key", gemini_model="test-model")
    assert await explain(settings, "Urgent", slot, fallback) == fallback
