import logging

from fastapi import FastAPI

from app.config import settings

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="LangGraph Assistant")


@app.on_event("startup")
def log_config() -> None:
    logger.info("Configured Postgres target: %s", settings.database_url)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}
