import logging

from fastapi import FastAPI
from fastapi.responses import JSONResponse

from app.config import settings
from app.graph import rag_graph
from app.models import SearchRequest, SearchResponse

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="LangGraph Assistant")


@app.on_event("startup")
def log_config() -> None:
    logger.info("Configured Postgres target: %s", settings.database_url)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/api/search", response_model=None)
async def search(request: SearchRequest) -> SearchResponse | JSONResponse:
    if not request.query or not request.query.strip():
        return JSONResponse(status_code=400, content={"error": "Query must not be empty."})

    result = await rag_graph.ainvoke({"question": request.query})

    return SearchResponse(
        answer=result["answer"],
        citations=result["citations"],
        hasGroundedAnswer=result["has_grounded_answer"],
    )
