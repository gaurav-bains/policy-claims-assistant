from pydantic import BaseModel


class SearchRequest(BaseModel):
    query: str


class Citation(BaseModel):
    source: str
    chunkIndex: int
    excerpt: str


class SearchResponse(BaseModel):
    answer: str
    citations: list[Citation]
    hasGroundedAnswer: bool
