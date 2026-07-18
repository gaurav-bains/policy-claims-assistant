# Docs Search

Basic keyword search over policy documents. Simple case-insensitive substring matching - no ranking, no fuzzy matching, no external dependencies.

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
  -d '{"query": "deductible"}'
```
