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


settings = Settings()
