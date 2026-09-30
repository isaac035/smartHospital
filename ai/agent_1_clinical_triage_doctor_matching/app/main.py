import logging

from fastapi import FastAPI

from app.routes.triage import router as triage_router

logging.basicConfig(level=logging.INFO)

app = FastAPI(
    title="Smart Hospital - Agent 1: Clinical Triage + Doctor Matching",
    version="1.0.0",
    # Internal service: no public API docs.
    docs_url=None,
    redoc_url=None,
    openapi_url=None,
)


@app.get("/health")
def health() -> dict:
    return {"status": "ok"}


app.include_router(triage_router)
