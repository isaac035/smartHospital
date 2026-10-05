import logging

from fastapi import FastAPI

from app.routes.optimization import router as optimization_router

logging.basicConfig(level=logging.INFO)

app = FastAPI(
    title="Smart Hospital - Agent 2: Appointment Optimization",
    version="1.0.0",
    docs_url=None,
    redoc_url=None,
    openapi_url=None,
)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


app.include_router(optimization_router)
