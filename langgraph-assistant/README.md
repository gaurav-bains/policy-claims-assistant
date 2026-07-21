# LangGraph Assistant

A Python/LangGraph implementation of the same RAG functionality as `semantic-search-assistant/` - answering questions with grounded, cited responses over policy documents. This exists to evaluate the LangChain/LangGraph ecosystem as an alternative approach to the .NET/Semantic Kernel implementation, side by side on the same data.

## Two-service structure

```
caller -> LangGraphAssistant.Api (C#, thin proxy)  -> FastAPI service (Python, RAG logic) -> Postgres/pgvector
                port 5247                                    port 8000
```

- **`app/` (FastAPI, Python)** - the actual RAG implementation: embeds the query, retrieves chunks from pgvector, builds the grounded prompt, calls the LLM. All the real logic lives here.
- **`LangGraphAssistant.Api/` (ASP.NET Core Minimal API, C#)** - a thin layer in front of it. Validates the request (non-empty query, max length), forwards it to the FastAPI service, and shapes the response back to the caller. No RAG logic here - if the Python service is unreachable or errors, it returns a clean `502` instead of crashing or leaking a stack trace.

Both expose the same `POST /api/search` contract, so you can call either one directly, or call the C# layer and let it proxy to Python.

Neither service ingests documents. Both query the same `document_chunks` pgvector table that `semantic-search-assistant/`'s ingestion step (`dotnet run -- ingest`) populates - run ingestion there first.

## Postgres

This service reuses the **same** Postgres/pgvector instance as `semantic-search-assistant/` - it does not spin up its own. Start it from there:

```bash
cd ../semantic-search-assistant
docker compose up -d
```

Connection details default to that instance's docker-compose config (`postgresql://semantic_search:semantic_search_dev@localhost:5432/semantic_search`), overridable via a `DATABASE_URL` environment variable or a local `.env` file (gitignored).

## Running both services locally

**1. Python FastAPI service** (the RAG implementation):

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -e .
```

Set `OPENAI_API_KEY` (env var or `.env`) - it's used for both embeddings and generation, and must match the embedding model/dimensions `semantic-search-assistant` ingested with (`text-embedding-3-small`, 1536 dimensions by default; override via `EMBEDDING_MODEL` / `EMBEDDING_DIMENSIONS`).

```bash
uvicorn app.main:app --reload
```

```bash
curl http://localhost:8000/health
# {"status":"ok"}
```

**2. C# proxy layer** (in a second terminal, from `LangGraphAssistant.Api/`):

```bash
cd LangGraphAssistant.Api
dotnet run
```

Runs on `http://localhost:5247` by default and forwards to the Python service at `PythonService:BaseUrl` in `appsettings.json` (defaults to `http://localhost:8000`, matching the command above).

## Graph structure (Python service)

A two-node LangGraph `StateGraph` (`app/graph.py`):

```
retrieve -> generate -> END
```

State schema: `question`, `retrieved_chunks`, `answer`, `citations`, `has_grounded_answer`.

- **`retrieve`**: embeds the question with the same embedding model used at ingestion, runs a cosine-similarity search against `document_chunks` (top 5), and keeps only chunks at or above the similarity threshold.
- **`generate`**: if no chunks cleared the threshold, skips the LLM entirely and returns the "not enough information" answer (no hallucination). Otherwise builds a grounded prompt from the retrieved chunks - answer only from context, cite chunks inline like `[Chunk 0]`, admit when the context is insufficient - and calls the LLM.

## `POST /api/search`

Same request/response contract as `semantic-search-assistant`, so the two are interchangeable behind the same interface. (`docs-search` predates this contract and is not interchangeable with either - see its README.) Examples below call the Python service directly (port 8000); the C# proxy (port 5247) behaves identically for the success and grounding cases, and additionally enforces its own query-length limit and returns `502` if the Python service is down.

**Grounded example:**

```bash
curl -X POST http://localhost:8000/api/search \
  -H "Content-Type: application/json" \
  -d '{"query": "How soon must employees complete hazard training after hire?"}'
```

```json
{
  "answer": "Employees must complete workplace hazard training within 30 days of hire [Chunk 0].",
  "citations": [
    {
      "source": "rscm_volume_ii-pdf-en.pdf",
      "chunkIndex": 0,
      "excerpt": "All employees must complete workplace hazard training within 30 days of hire..."
    }
  ],
  "hasGroundedAnswer": true
}
```

**No relevant context found:**

```bash
curl -X POST http://localhost:8000/api/search \
  -H "Content-Type: application/json" \
  -d '{"query": "What is the company policy on parental leave?"}'
```

```json
{
  "answer": "I don't have enough information in the provided context to answer this question.",
  "citations": [],
  "hasGroundedAnswer": false
}
```

**Empty query** returns `400 Bad Request`:

```bash
curl -X POST http://localhost:8000/api/search \
  -H "Content-Type: application/json" \
  -d '{"query": ""}'
```

```json
{ "error": "Query must not be empty." }
```

**Query too long** (C# proxy only, `Validation:MaxQueryLength` in `LangGraphAssistant.Api/appsettings.json`, default 2000 chars) returns `400 Bad Request`:

```json
{ "error": "Query must not exceed 2000 characters." }
```

**Python service unreachable** (C# proxy only) returns `502 Bad Gateway` instead of crashing:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.3",
  "title": "Bad Gateway",
  "status": 502,
  "detail": "The search service is currently unavailable. Please try again shortly."
}
```
