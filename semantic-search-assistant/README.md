# Semantic Search Assistant

An AI-native alternative to `docs-search/`. Instead of case-insensitive substring matching, this service uses Semantic Kernel for embeddings and pgvector (PostgreSQL) for similarity search over document content, plus an LLM to generate a grounded answer with citations.

## Run local Postgres (with pgvector)

```bash
docker compose up -d
```

This starts a PostgreSQL 16 instance with the pgvector extension on `localhost:5432`, using the `semantic_search` database, user, and password defined in `docker-compose.yml`. Data is persisted to `./.pgdata`.

To stop it:

```bash
docker compose down
```

## Ingest documents

Place source PDFs in `../docs/source-documents/`, then run:

```bash
dotnet run -- ingest
```

This extracts text (PdfPig), chunks it (~500 tokens with ~50 token overlap), generates embeddings, and stores each chunk in the `document_chunks` Postgres table.

## Run the API

```bash
dotnet run
```

### `POST /api/search`

Embeds the query, retrieves the top matching chunks from Postgres, and asks the configured LLM to answer using only that retrieved context, citing which chunk(s) it used.

**Grounded example:**

```bash
curl -X POST http://localhost:5224/api/search \
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

**No relevant context found** (nothing in the retrieved chunks clears the similarity threshold, so the LLM isn't called and no answer is hallucinated):

```bash
curl -X POST http://localhost:5224/api/search \
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
curl -X POST http://localhost:5224/api/search \
  -H "Content-Type: application/json" \
  -d '{"query": ""}'
```

```json
{ "error": "Query must not be empty." }
```

## Configuration

| Setting | Purpose | Default |
|---|---|---|
| `Embedding:Provider` / `ModelId` / `ApiKey` | Embedding model used for both ingestion and query. Falls back to `OPENAI_API_KEY` env var. | OpenAI `text-embedding-3-small` |
| `Llm:Provider` / `ModelId` / `ApiKey` | Chat model used to generate answers - `OpenAI` or `Anthropic`. Falls back to `OPENAI_API_KEY` / `ANTHROPIC_API_KEY` env var. | OpenAI `gpt-4o-mini` |
| `Retrieval:TopK` | Number of chunks retrieved per query. | 5 |
| `Retrieval:SimilarityThreshold` | Minimum cosine similarity (0-1) for a chunk to be used as grounding. Starting point - tune once real data is ingested. | 0.3 |

Note: Anthropic's Claude API has no embeddings endpoint, so `Embedding:Provider` only supports OpenAI. `Llm:Provider` (the answer-generation step) supports either.

Secrets can be set in `appsettings.json`, as environment variables, or in a local `.env` file (gitignored).
