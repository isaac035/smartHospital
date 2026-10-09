import json

import google.genai
from fastapi.testclient import TestClient

from app.main import app
from app.services import reasoner
from app.services.reasoner import _parse_model_output, reason, sanitize_recommendations

CARDIO_DATA = {
    "appointmentSummary": {"priority": "Normal", "type": "Specialist"},
    "clinicalDiagnoses": {"records": [{"description": "Hypertension"}]},
    "sectionsPresent": ["Patient Information", "Clinical Diagnoses"],
}

VALID_RECOMMENDATIONS = {
    "basis": "condition_specific",
    "diet": {"prefer": ["Vegetables, fruit and whole grains."], "avoid": ["Salty processed foods."]},
    "exercise": {"suitable": ["Brisk walking most days, as tolerated."], "avoid": ["Heavy lifting without advice."]},
    "lifestyle": ["Aim for 7 to 8 hours of sleep.", "Avoid smoking."],
    "warningSigns": ["Chest pain or severe headache: go to the emergency department."],
    "followUp": {"timeframe": "about 1 month", "monitoring": ["Blood pressure at home."]},
    "note": "",
}


class _FakeResponse:
    def __init__(self, text):
        self.text = text


def _fake_client(text=None, error=None):
    class FakeModels:
        def generate_content(self, **_kwargs):
            if error:
                raise error
            return _FakeResponse(text)

    class FakeClient:
        def __init__(self, **_kwargs):
            self.models = FakeModels()

    return FakeClient


def test_valid_recommendations_are_kept():
    result = sanitize_recommendations(VALID_RECOMMENDATIONS)
    assert result is not None
    assert result.basis == "condition_specific"
    assert result.diet.avoid == ["Salty processed foods."]
    assert result.follow_up.timeframe == "about 1 month"
    dumped = result.model_dump(by_alias=True)
    assert "warningSigns" in dumped and "followUp" in dumped


def test_unsafe_markup_doses_and_medication_changes_are_dropped():
    result = sanitize_recommendations({
        "lifestyle": [
            "<script>alert(1)</script>",
            "Take 500 mg of paracetamol twice a day.",
            "Stop taking your blood pressure medication.",
            "This diet will cure your condition.",
            "See https://example.com for more.",
            "- **Drink water** regularly.",
        ],
    })
    assert result is not None
    assert result.lifestyle == ["Drink water regularly."]


def test_lists_are_capped_deduplicated_and_length_limited():
    result = sanitize_recommendations({
        "lifestyle": ["Rest well."] * 3 + [f"Tip number {i}." for i in range(10)] + ["x" * 500],
        "basis": "something-else",
    })
    assert result is not None
    assert len(result.lifestyle) == 5
    assert result.lifestyle[0] == "Rest well."
    assert all(len(item) <= 200 for item in result.lifestyle)
    assert result.basis == "general"


def test_wrong_types_or_empty_content_give_no_recommendations():
    assert sanitize_recommendations(None) is None
    assert sanitize_recommendations("eat well") is None
    assert sanitize_recommendations({"diet": "eat well", "lifestyle": [1, None, ""]}) is None


def test_invalid_recommendations_never_lose_the_summary():
    summary, recommendations = _parse_model_output(json.dumps({"aiSummary": "Summary text.", "recommendations": ["not", "an", "object"]}))
    assert summary == "Summary text."
    assert recommendations is None


def test_model_output_returns_structured_recommendations(monkeypatch):
    monkeypatch.setenv("GEMINI_API_KEY", "test-key")
    monkeypatch.setattr(google.genai, "Client", _fake_client(json.dumps({"aiSummary": "Short summary.", "recommendations": VALID_RECOMMENDATIONS})))
    result = reason(CARDIO_DATA)
    assert result.mode == "gemini"
    assert result.ai_summary == "Short summary."
    assert result.recommendations is not None
    assert result.recommendations.warning_signs
    # The deterministic general guidance is still returned for backward compatibility.
    assert result.ai_recommendations == reasoner._safe_recommendations(CARDIO_DATA)


def test_model_failure_falls_back_without_recommendations(monkeypatch):
    monkeypatch.setenv("GEMINI_API_KEY", "test-key")
    monkeypatch.setattr(google.genai, "Client", _fake_client(error=RuntimeError("model down")))
    result = reason(CARDIO_DATA)
    assert result.mode == "non_ai_fallback"
    assert result.recommendations is None
    assert result.ai_summary.startswith("Non-AI structured summary")


def test_malformed_model_json_falls_back(monkeypatch):
    monkeypatch.setenv("GEMINI_API_KEY", "test-key")
    monkeypatch.setattr(google.genai, "Client", _fake_client("not json at all"))
    result = reason(CARDIO_DATA)
    assert result.mode == "non_ai_fallback"
    assert result.recommendations is None


def test_endpoint_contract_is_backward_compatible(monkeypatch):
    monkeypatch.setenv("AGENT4_INTERNAL_API_KEY", "internal")
    monkeypatch.delenv("GEMINI_API_KEY", raising=False)
    client = TestClient(app)
    assert client.post("/v1/reports/reason", json={"recordedData": {}}).status_code == 401
    response = client.post("/v1/reports/reason", json={"recordedData": CARDIO_DATA}, headers={"X-Internal-Api-Key": "internal"})
    assert response.status_code == 200
    body = response.json()
    assert set(body) == {"aiSummary", "aiRecommendations", "mode", "recommendations"}
    assert body["recommendations"] is None
    assert body["mode"] == "non_ai_fallback"
