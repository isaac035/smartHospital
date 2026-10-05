import json
from types import SimpleNamespace

import pytest
from google.genai import errors

from app.services.llm_client import (
    AIServiceError,
    AnthropicTriageLLM,
    GeminiTriageLLM,
    create_triage_llm,
    model_for,
)
from app.utils.settings import Settings

CATEGORIES = ["General Medicine", "Cardiology"]
GOOD_JSON = json.dumps(
    {
        "category": "Cardiology",
        "priority": "Urgent",
        "reason": "Chest discomfort could suggest seeing a heart specialist.",
        "confidence": 0.8,
        "possibleEmergency": False,
    }
)


def settings(**overrides) -> Settings:
    values = {"AI_API_KEY": "test-key", "AI_PROVIDER": "gemini", "AI_MODEL": ""} | overrides
    # _env_file=None: never let a developer's local .env leak into tests.
    return Settings(_env_file=None, **values)


class FakeModels:
    def __init__(self, response=None, error=None):
        self.response = response
        self.error = error
        self.calls = []

    def generate_content(self, **kwargs):
        self.calls.append(kwargs)
        if self.error:
            raise self.error
        return self.response


def fake_client(response=None, error=None):
    return SimpleNamespace(models=FakeModels(response, error))


def response(text=GOOD_JSON, finish="FinishReason.STOP", block_reason=None):
    return SimpleNamespace(
        text=text,
        candidates=[SimpleNamespace(finish_reason=finish)],
        prompt_feedback=SimpleNamespace(block_reason=block_reason) if block_reason else None,
    )


def test_gemini_returns_parsed_triage_and_sends_schema_constrained_request():
    client = fake_client(response())
    llm = GeminiTriageLLM(settings(), client=client)

    result = llm.classify("chest discomfort on stairs", CATEGORIES)

    assert result.category == "Cardiology"
    assert result.priority == "Urgent"
    assert result.possible_emergency is False
    call = client.models.calls[0]
    assert call["model"] == "gemini-3.8-flash"
    assert "<patient_request>" in call["contents"]
    config = call["config"]
    assert config.response_mime_type == "application/json"
    enum = config.response_json_schema["properties"]["category"]["enum"]
    assert enum == ["General Medicine", "Cardiology", "UNCLEAR"]


def test_gemini_uses_configured_model():
    client = fake_client(response())
    GeminiTriageLLM(settings(AI_MODEL="gemini-3.5-flash"), client=client).classify("x rash", CATEGORIES)
    assert client.models.calls[0]["model"] == "gemini-3.5-flash"


@pytest.mark.parametrize("code", [400, 403, 429, 500])
def test_gemini_api_errors_become_service_errors(code):
    error = errors.APIError(code, {"error": {"code": code, "message": "boom", "status": "X"}})
    llm = GeminiTriageLLM(settings(), client=fake_client(error=error))
    with pytest.raises(AIServiceError):
        llm.classify("rash", CATEGORIES)


def test_gemini_network_failure_becomes_service_error():
    llm = GeminiTriageLLM(settings(), client=fake_client(error=TimeoutError()))
    with pytest.raises(AIServiceError):
        llm.classify("rash", CATEGORIES)


@pytest.mark.parametrize(
    "resp",
    [
        response(block_reason="SAFETY"),
        response(finish="FinishReason.SAFETY", text=None),
        response(finish="FinishReason.MAX_TOKENS"),
        response(text=None),
        response(text="not json"),
        response(text=json.dumps({"category": "Cardiology"})),
    ],
)
def test_gemini_unusable_responses_become_service_errors(resp):
    llm = GeminiTriageLLM(settings(), client=fake_client(resp))
    with pytest.raises(AIServiceError):
        llm.classify("rash", CATEGORIES)


def test_provider_selection_and_default_models():
    assert isinstance(create_triage_llm(settings()), GeminiTriageLLM)
    assert isinstance(create_triage_llm(settings(AI_PROVIDER="anthropic")), AnthropicTriageLLM)
    assert model_for(settings()) == "gemini-3.8-flash"
    assert model_for(settings(AI_PROVIDER="anthropic")) == "claude-opus-5-5"


def test_missing_api_key_is_reported():
    with pytest.raises(AIServiceError):
        create_triage_llm(settings(AI_API_KEY=""))


class SequenceModels:
    """Raises the queued errors in order, then returns the response."""

    def __init__(self, errors_in_order, final_response):
        self.queue = list(errors_in_order)
        self.final = final_response
        self.models_called = []

    def generate_content(self, **kwargs):
        self.models_called.append(kwargs["model"])
        if self.queue:
            raise self.queue.pop(0)
        return self.final


def api_error(code):
    return errors.APIError(code, {"error": {"code": code, "message": "x", "status": "X"}})


@pytest.mark.parametrize("code", [503, 429, 404, 500])
def test_gemini_falls_back_to_next_model_when_primary_is_unavailable(code):
    models = SequenceModels([api_error(code)], response())
    llm = GeminiTriageLLM(settings(), client=SimpleNamespace(models=models))

    result = llm.classify("chest discomfort", CATEGORIES)

    assert result.category == "Cardiology"
    assert models.models_called == ["gemini-3.8-flash", "gemini-3.5-flash-lite"]


def test_gemini_does_not_retry_a_bad_request():
    models = SequenceModels([api_error(400)], response())
    llm = GeminiTriageLLM(settings(), client=SimpleNamespace(models=models))
    with pytest.raises(AIServiceError):
        llm.classify("rash", CATEGORIES)
    assert models.models_called == ["gemini-3.8-flash"]


def test_gemini_gives_up_after_all_models_are_busy():
    models = SequenceModels([api_error(503)] * 3, response())
    llm = GeminiTriageLLM(settings(), client=SimpleNamespace(models=models))
    with pytest.raises(AIServiceError, match="busy"):
        llm.classify("rash", CATEGORIES)
    assert models.models_called == ["gemini-3.8-flash", "gemini-3.5-flash-lite", "gemini-flash-lite-latest"]


def test_fallback_can_be_disabled():
    models = SequenceModels([api_error(503)], response())
    llm = GeminiTriageLLM(settings(AI_FALLBACK_MODELS=""), client=SimpleNamespace(models=models))
    with pytest.raises(AIServiceError):
        llm.classify("rash", CATEGORIES)
    assert models.models_called == ["gemini-3.8-flash"]
