import hmac

from fastapi import Depends, Header, HTTPException, status

from app.utils.settings import Settings, get_settings


def require_internal_key(
    x_internal_api_key: str | None = Header(default=None, alias="X-Internal-Api-Key"),
    settings: Settings = Depends(get_settings),
) -> None:
    expected = settings.internal_api_key
    if not expected:
        raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Service is not configured.")
    if not x_internal_api_key or not hmac.compare_digest(
        x_internal_api_key.encode("utf-8"), expected.encode("utf-8")
    ):
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Unauthorized.")
