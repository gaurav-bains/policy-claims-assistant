# LangGraph Assistant

A Python/LangGraph implementation of the same RAG functionality as `semantic-search-assistant/` - ingesting policy documents into pgvector and answering questions with grounded, cited responses. This exists to evaluate the LangChain/LangGraph ecosystem as an alternative approach to the .NET/Semantic Kernel implementation, side by side on the same data.

No RAG logic is implemented yet - this is just the project scaffold with dependencies installed and a health check endpoint.

## Postgres

This service reuses the **same** Postgres/pgvector instance as `semantic-search-assistant/` - it does not spin up its own. Start it from there:

```bash
cd ../semantic-search-assistant
docker compose up -d
```

Connection details default to that instance's docker-compose config (`postgresql://semantic_search:semantic_search_dev@localhost:5432/semantic_search`), overridable via a `DATABASE_URL` environment variable or a local `.env` file (gitignored).

## Setup

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -e .
```

## Run

```bash
uvicorn app.main:app --reload
```

Then check:

```bash
curl http://localhost:8000/health
```

```json
{ "status": "ok" }
```
