from functools import lru_cache
from pydantic_settings import BaseSettings, SettingsConfigDict

class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")
    AGENT3_INTERNAL_API_KEY: str = ""
    AGENT3_MAX_RECOMMENDATIONS: int = 30

@lru_cache
def get_settings() -> Settings:
    return Settings()
