# LangGraph Assistant

A Python/LangGraph implementation of the same RAG functionality as `semantic-search-assistant/` - answering questions with grounded, cited responses over policy documents. This exists to evaluate the LangChain/LangGraph ecosystem as an alternative approach to the .NET/Semantic Kernel implementation, side by side on the same data.

This service does **not** ingest documents itself. It queries the same `document_chunks` pgvector table that `semantic-search-assistant/`'s ingestion step (`dotnet run -- ingest`) populates - run ingestion there first.

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

Set `OPENAI_API_KEY` (env var or `.env`) - it's used for both embeddings and generation, and must match the embedding model/dimensions `semantic-search-assistant` ingested with (`text-embedding-3-small`, 1536 dimensions by default; override via `EMBEDDING_MODEL` / `EMBEDDING_DIMENSIONS`).

## Run

```bash
uvicorn app.main:app --reload
```

```bash
curl http://localhost:8000/health
```

```json
{ "status": "ok" }
```

## Graph structure

A two-node LangGraph `StateGraph` (`app/graph.py`):

```
retrieve -> generate -> END
```

State schema: `question`, `retrieved_chunks`, `answer`, `citations`, `has_grounded_answer`.

- **`retrieve`**: embeds the question with the same embedding model used at ingestion, runs a cosine-similarity search against `document_chunks` (top 5), and keeps only chunks at or above the similarity threshold.
- **`generate`**: if no chunks cleared the threshold, skips the LLM entirely and returns the "not enough information" answer (no hallucination). Otherwise builds a grounded prompt from the retrieved chunks - answer only from context, cite chunks inline like `[Chunk 0]`, admit when the context is insufficient - and calls the LLM.

## `POST /api/search`

Same request/response contract as `docs-search` and `semantic-search-assistant`, so all three are interchangeable behind the same interface.

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
      "source": "RSCM Volume II.pdf",
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
