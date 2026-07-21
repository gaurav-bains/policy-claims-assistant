import asyncpg
from pgvector.asyncpg import register_vector

from app.config import settings

_pool: asyncpg.Pool | None = None


async def get_pool() -> asyncpg.Pool:
    global _pool
    if _pool is None:
        _pool = await asyncpg.create_pool(settings.database_url, init=register_vector)
    return _pool


async def search_chunks(embedding: list[float], top_k: int) -> list[dict]:
    """Cosine similarity search against the document_chunks table populated by
    semantic-search-assistant's ingestion step. Read-only - never writes here."""
    pool = await get_pool()
    async with pool.acquire() as conn:
        rows = await conn.fetch(
            """
            SELECT source_document, chunk_index, page_reference, content,
                   1 - (embedding <=> $1) AS similarity
            FROM document_chunks
            ORDER BY embedding <=> $1
            LIMIT $2
            """,
            embedding,
            top_k,
        )
    return [dict(row) for row in rows]
