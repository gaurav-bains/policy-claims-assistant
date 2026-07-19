-- Applied automatically by the ingestion command on startup (idempotent).
-- Vector dimension must match the configured embedding model's output size
-- (1536 for OpenAI text-embedding-3-small / text-embedding-ada-002).

CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS document_chunks (
    id BIGSERIAL PRIMARY KEY,
    source_document TEXT NOT NULL,
    chunk_index INT NOT NULL,
    page_reference TEXT,
    content TEXT NOT NULL,
    embedding VECTOR(1536) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_document_chunks_source_document
    ON document_chunks (source_document);
