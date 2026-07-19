# Semantic Search Assistant

An AI-native alternative to `docs-search/`. Instead of case-insensitive substring matching, this service is scaffolded to use Semantic Kernel for embeddings and pgvector (PostgreSQL) for similarity search over document content.

No endpoint logic is implemented yet - this is just the project scaffold with dependencies installed.

## Run local Postgres (with pgvector)

```bash
docker compose up -d
```

This starts a PostgreSQL 16 instance with the pgvector extension on `localhost:5432`, using the `semantic_search` database, user, and password defined in `docker-compose.yml`. Data is persisted to `./.pgdata`.

To stop it:

```bash
docker compose down
```

## Run the API

```bash
dotnet run
```
