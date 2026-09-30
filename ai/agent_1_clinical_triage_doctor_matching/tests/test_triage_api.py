import pytest
from fastapi.testclient import TestClient

from app.main import app
from app.routes.triage import get_llm
from app.schemas.triage import EMERGENCY_NOTICE
from app.services.llm_client import AIServiceError, RawTriage
from app.utils.settings import Settings, get_settings

INTERNAL_KEY = "test-internal-key"
HEADERS = {"X-Internal-Api-Key": INTERNAL_KEY}

CATEGORIES = ["General Medicine", "Cardiology", "Pediatrics", "Dermatology", "Neurology", "Orthopedics"]

DOCTORS = [
    {"doctorId": 2, "doctorProfileId": 1, "name": "Nadia Perera", "specialization": "General Medicine", "department": "General Medicine"},
    {"doctorId": 11, "doctorProfileId": 2, "name": "Ashan Wijesekara", "specialization": "Interventional Cardiology", "department": "Cardiology"},
    {"doctorId": 12, "doctorProfileId": 3, "name": "Priya Gunawardena", "specialization": "Cardiac Electrophysiology", "department": "Cardiology"},
    {"doctorId": 13, "doctorProfileId": 6, "name": "Nimali Karunaratne", "specialization": "Clinical Dermatology", "department": "Dermatology"},
]


class FakeLLM:
    def __init__(self, result: RawTriage | None = None, error: Exception | None = None):
        self.result = result
        self.error = error
        self.calls: list[tuple[str, list[str]]] = []

    def classify(self, text, categories):
        self.calls.append((text, categories))
        if self.error:
            raise self.error
        return self.result


def raw(category="Cardiology", priority="Urgent", reason="Chest discomfort on exertion could suggest seeing a heart specialist.", confidence=0.85, emergency=False):
    return RawTriage(category=category, priority=priority, reason=reason, confidence=confidence, possible_emergency=emergency)


@pytest.fixture
def client_with():
    def _make(llm):
        app.dependency_overrides[get_settings] = lambda: Settings(_env_file=None, AGENT1_INTERNAL_API_KEY=INTERNAL_KEY)
        app.dependency_overrides[get_llm] = lambda: llm
        return TestClient(app)

    yield _make
    app.dependency_overrides.clear()


def body(text="I get chest discomfort when I climb stairs", categories=CATEGORIES, doctors=DOCTORS):
    return {
        "patientId": 4,
        "text": text,
        "categories": categories,
        "defaultCategory": "General Medicine",
        "candidateDoctors": doctors,
    }


def test_normal_triage_returns_matching_real_doctors(client_with):
    client = client_with(FakeLLM(raw()))
    res = client.post("/v1/triage-doctor-match", json=body(), headers=HEADERS)
    assert res.status_code == 200
    data = res.json()
    assert data["patientId"] == 4
    assert data["category"] == "Cardiology"
    assert data["priority"] == "Urgent"
    assert data["possibleEmergency"] is False
    assert data["emergencyNotice"] is None
    assert data["usedDefaultCategory"] is False
    assert [d["doctorId"] for d in data["recommendedDoctors"]] == [11, 12]
    assert data["message"] is None


def test_different_specialization(client_with):
    client = client_with(FakeLLM(raw(category="Dermatology", priority="Normal", reason="An itchy rash may indicate a skin issue worth checking.", confidence=0.9)))
    data = client.post("/v1/triage-doctor-match", json=body("itchy rash on my arms for a week"), headers=HEADERS).json()
    assert data["category"] == "Dermatology"
    assert data["priority"] == "Normal"
    assert [d["name"] for d in data["recommendedDoctors"]] == ["Nimali Karunaratne"]


def test_red_flag_sets_emergency_even_if_model_does_not(client_with):
    client = client_with(FakeLLM(raw(priority="Normal", emergency=False)))
    data = client.post("/v1/triage-doctor-match", json=body("severe crushing chest pain and I can't breathe"), headers=HEADERS).json()
    assert data["possibleEmergency"] is True
    assert data["priority"] == "Emergency"
    assert data["emergencyNotice"] == EMERGENCY_NOTICE


def test_model_emergency_flag_is_respected(client_with):
    client = client_with(FakeLLM(raw(emergency=True, priority="Urgent")))
    data = client.post("/v1/triage-doctor-match", json=body("something feels very wrong"), headers=HEADERS).json()
    assert data["possibleEmergency"] is True
    assert data["priority"] == "Emergency"


def test_vague_request_falls_back_to_default_with_low_confidence(client_with):
    client = client_with(FakeLLM(raw(category="UNCLEAR", priority="Normal", confidence=0.2)))
    data = client.post("/v1/triage-doctor-match", json=body("I just don't feel right"), headers=HEADERS).json()
    assert data["category"] == "General Medicine"
    assert data["usedDefaultCategory"] is True
    assert data["confidence"] == 0.2
    assert "doesn't clearly point to one specialty" in data["reason"]
    assert [d["doctorId"] for d in data["recommendedDoctors"]] == [2]


def test_low_confidence_in_listed_category_still_falls_back(client_with):
    client = client_with(FakeLLM(raw(category="Neurology", confidence=0.3)))
    data = client.post("/v1/triage-doctor-match", json=body("occasional headaches"), headers=HEADERS).json()
    assert data["category"] == "General Medicine"
    assert data["usedDefaultCategory"] is True


def test_category_outside_real_list_is_rejected(client_with):
    client = client_with(FakeLLM(raw(category="Oncology", confidence=0.95)))
    data = client.post("/v1/triage-doctor-match", json=body(), headers=HEADERS).json()
    assert data["category"] == "General Medicine"
    assert data["usedDefaultCategory"] is True


def test_invalid_priority_is_normalised(client_with):
    client = client_with(FakeLLM(raw(priority="Critical")))
    data = client.post("/v1/triage-doctor-match", json=body(), headers=HEADERS).json()
    assert data["priority"] == "Normal"


def test_category_with_zero_bookable_doctors_explains_and_offers_fallback(client_with):
    client = client_with(FakeLLM(raw(category="Neurology", priority="Normal", reason="Recurring numb fingers could suggest seeing a nerve specialist.", confidence=0.8)))
    data = client.post("/v1/triage-doctor-match", json=body("my fingers keep going numb"), headers=HEADERS).json()
    assert data["category"] == "Neurology"
    assert data["recommendedDoctors"] == []
    assert "No Neurology doctors are available" in data["message"]
    assert "browse all doctors" in data["message"]


def test_diagnostic_or_medication_language_is_replaced(client_with):
    client = client_with(FakeLLM(raw(reason="You have angina. Take 300 mg aspirin now.")))
    data = client.post("/v1/triage-doctor-match", json=body(), headers=HEADERS).json()
    assert "angina" not in data["reason"]
    assert "mg" not in data["reason"]
    assert data["reason"].startswith("Based on what you described, a Cardiology doctor")


@pytest.mark.parametrize("text", ["", "  ", "hi", "x" * 1001])
def test_invalid_input_rejected(client_with, text):
    llm = FakeLLM(raw())
    client = client_with(llm)
    res = client.post("/v1/triage-doctor-match", json=body(text), headers=HEADERS)
    assert res.status_code == 422
    assert llm.calls == []


def test_default_category_must_be_real(client_with):
    client = client_with(FakeLLM(raw()))
    payload = body() | {"defaultCategory": "Made Up"}
    assert client.post("/v1/triage-doctor-match", json=payload, headers=HEADERS).status_code == 422


def test_ai_failure_returns_503_and_keeps_emergency_screen(client_with):
    client = client_with(FakeLLM(error=AIServiceError("The AI service timed out.")))
    res = client.post("/v1/triage-doctor-match", json=body("my father collapsed and is unresponsive"), headers=HEADERS)
    assert res.status_code == 503
    data = res.json()
    assert data["possibleEmergency"] is True
    assert data["emergencyNotice"] == EMERGENCY_NOTICE


def test_ai_not_configured_returns_503(client_with):
    client = client_with(None)
    res = client.post("/v1/triage-doctor-match", json=body(), headers=HEADERS)
    assert res.status_code == 503
    assert res.json()["possibleEmergency"] is False


def test_missing_or_wrong_internal_key_is_rejected(client_with):
    llm = FakeLLM(raw())
    client = client_with(llm)
    assert client.post("/v1/triage-doctor-match", json=body()).status_code == 401
    assert client.post("/v1/triage-doctor-match", json=body(), headers={"X-Internal-Api-Key": "wrong"}).status_code == 401
    assert llm.calls == []


def test_unconfigured_internal_key_fails_closed():
    app.dependency_overrides[get_settings] = lambda: Settings(_env_file=None, AGENT1_INTERNAL_API_KEY="")
    app.dependency_overrides[get_llm] = lambda: FakeLLM(raw())
    try:
        res = TestClient(app).post("/v1/triage-doctor-match", json=body(), headers=HEADERS)
        assert res.status_code == 503
    finally:
        app.dependency_overrides.clear()


def test_only_candidate_doctors_are_ever_returned(client_with):
    # The service has no doctor data of its own: an empty candidate list means no doctors.
    client = client_with(FakeLLM(raw()))
    data = client.post("/v1/triage-doctor-match", json=body(doctors=[]), headers=HEADERS).json()
    assert data["recommendedDoctors"] == []
