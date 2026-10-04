from functools import lru_cache

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", extra="ignore")

    gemini_api_key: str = Field(default="", alias="GEMINI_API_KEY")
    gemini_model: str = Field(default="gemini-2.5-flash", alias="GEMINI_MODEL")
    internal_api_key: str = Field(default="", alias="AGENT2_INTERNAL_API_KEY")
    max_alternatives: int = Field(default=4, ge=0, le=10, alias="AGENT2_MAX_ALTERNATIVES")
    gemini_timeout_seconds: float = Field(default=8, gt=0, le=30, alias="AGENT2_GEMINI_TIMEOUT_SECONDS")


@lru_cache
def get_settings() -> Settings:
    return Settings()
