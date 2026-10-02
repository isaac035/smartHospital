from fastapi import Header, HTTPException, Depends
from app.utils.settings import Settings, get_settings

def require_internal_key(x_internal_api_key: str | None = Header(default=None, alias="X-Internal-Api-Key"), settings: Settings = Depends(get_settings)) -> None:
    if not settings.AGENT3_INTERNAL_API_KEY or x_internal_api_key != settings.AGENT3_INTERNAL_API_KEY:
        raise HTTPException(status_code=401, detail="Invalid internal API key")
