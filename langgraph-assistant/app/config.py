import os
from dataclasses import dataclass


def _load_dotenv(path: str = ".env") -> None:
    if not os.path.exists(path):
        return
    with open(path) as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            key, _, value = line.partition("=")
            os.environ.setdefault(key.strip(), value.strip().strip('"'))


_load_dotenv()


@dataclass(frozen=True)
class Settings:
    # Same Postgres/pgvector instance used by semantic-search-assistant
    # (semantic-search-assistant/docker-compose.yml) - no second instance here.
    database_url: str = os.getenv(
        "DATABASE_URL",
        "postgresql://semantic_search:semantic_search_dev@localhost:5432/semantic_search",
    )
    openai_api_key: str = os.getenv("OPENAI_API_KEY", "")

    # Must match semantic-search-assistant's Embedding:ModelId / Dimensions -
    # this service queries chunks embedded by that ingestion step.
    embedding_model: str = os.getenv("EMBEDDING_MODEL", "text-embedding-3-small")
    embedding_dimensions: int = int(os.getenv("EMBEDDING_DIMENSIONS", "1536"))

    llm_model: str = os.getenv("LLM_MODEL", "gpt-4o-mini")

    retrieval_top_k: int = int(os.getenv("RETRIEVAL_TOP_K", "5"))
    # Cosine similarity (0-1) - same starting point as semantic-search-assistant.
    similarity_threshold: float = float(os.getenv("SIMILARITY_THRESHOLD", "0.3"))


settings = Settings()
