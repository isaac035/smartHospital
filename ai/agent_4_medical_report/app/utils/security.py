import hmac
import os

from fastapi import Header, HTTPException


async def require_internal_key(x_internal_api_key: str | None = Header(default=None)) -> None:
    expected = os.getenv("AGENT4_INTERNAL_API_KEY", "")
    if not expected or not x_internal_api_key or not hmac.compare_digest(expected, x_internal_api_key):
        raise HTTPException(status_code=401, detail="Invalid internal API key")
