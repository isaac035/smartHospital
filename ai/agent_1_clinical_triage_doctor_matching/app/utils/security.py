import hmac

from fastapi import Depends, Header, HTTPException, status

from app.utils.settings import Settings, get_settings

INTERNAL_KEY_HEADER = "X-Internal-Api-Key"


def require_internal_key(
    x_internal_api_key: str | None = Header(default=None, alias=INTERNAL_KEY_HEADER),
    settings: Settings = Depends(get_settings),
) -> None:
    """Only the ASP.NET Core API (which holds the shared secret) may call this
    service. Flutter and the public internet must never reach it directly."""
    expected = settings.internal_api_key
    if not expected:
        # Fail closed: a missing secret must never mean "open to everyone".
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="Service is not configured.",
        )
    if not x_internal_api_key or not hmac.compare_digest(
        x_internal_api_key.encode("utf-8"), expected.encode("utf-8")
    ):
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Unauthorized.")
