from functools import lru_cache
from typing import Literal

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Runtime configuration. Every value comes from the environment (or a local,
    git-ignored .env file) - nothing secret is hard-coded."""

    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", extra="ignore")

    # LLM provider: "gemini" (Google Gemini) or "anthropic" (Claude)
    ai_provider: Literal["gemini", "anthropic"] = Field(default="gemini", alias="AI_PROVIDER")
    ai_api_key: str = Field(default="", alias="AI_API_KEY")
    # Empty = the provider's default model (see llm_client.DEFAULT_MODELS).
    ai_model: str = Field(default="", alias="AI_MODEL")
    # Gemini only: tried in order when the primary model is overloaded, rate-limited,
    # or unavailable to this key. Comma-separated; empty disables fallback.
    ai_fallback_models: str = Field(default="gemini-3.5-flash-lite,gemini-flash-lite-latest", alias="AI_FALLBACK_MODELS")
    ai_timeout_seconds: float = Field(default=20.0, alias="AI_TIMEOUT_SECONDS")
    # Anthropic only: if the primary model declines, the API re-runs the request
    # on a fallback model inside the same call.
    ai_enable_fallbacks: bool = Field(default=True, alias="AI_ENABLE_FALLBACKS")

    # Shared secret the ASP.NET Core API presents on every call. If unset, the
    # service refuses all triage requests (fail closed).
    internal_api_key: str = Field(default="", alias="AGENT1_INTERNAL_API_KEY")

    # Below this confidence the triage falls back to the default category.
    min_confidence: float = Field(default=0.5, alias="AGENT1_MIN_CONFIDENCE")
    max_recommendations: int = Field(default=5, alias="AGENT1_MAX_RECOMMENDATIONS")


@lru_cache
def get_settings() -> Settings:
    return Settings()
