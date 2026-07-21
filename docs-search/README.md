# Docs Search

Basic keyword search over RSCM docs. Simple case-insensitive substring matching - no ranking, no fuzzy matching, no external dependencies.

**Note:** This is the original, pre-RAG implementation and predates the `{answer, citations, hasGroundedAnswer}` contract used by `semantic-search-assistant/` and `langgraph-assistant/`. Its `POST /api/search` returns a different shape (`{results, source}`, shown below) and has no empty-query validation - it is **not** interchangeable with the other two.

## Run locally

```bash
dotnet run
```

## Run with Docker

```bash
docker build -t docs-search .
docker run -p 8080:8080 docs-search
```

## Test

```bash
curl -X POST http://localhost:8080/api/search \
  -H "Content-Type: application/json" \
  -d '{"query": "training"}'
```

```json
{
  "results": [
    "All employees must complete workplace hazard training within 30 days of hire."
  ],
  "source": "keyword-search"
}
```
