from fastapi import FastAPI
from app.routes.recommendations import router

app = FastAPI(title="SmartHospital Agent 3 - Resource Allocation", docs_url=None, redoc_url=None)
app.include_router(router)

@app.get("/health")
def health():
    return {"status": "ok"}
