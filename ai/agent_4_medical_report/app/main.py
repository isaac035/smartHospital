from fastapi import FastAPI

from app.routes.reports import router as reports_router

app = FastAPI(title="Smart Hospital Agent 4 Medical Report", version="1.0.0")
app.include_router(reports_router, prefix="/v1/reports", tags=["reports"])


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}
