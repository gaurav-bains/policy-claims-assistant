# Source Documents

Place source PDFs for ingestion here.

Expected file: `RSCM Volume II.pdf`

The ingestion pipeline (`semantic-search-assistant`, run via `dotnet run -- ingest`) reads PDFs from this folder, extracts text, chunks it, generates embeddings, and stores the results in Postgres.
