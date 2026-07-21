from typing import TypedDict

from langchain_openai import ChatOpenAI, OpenAIEmbeddings
from langgraph.graph import END, StateGraph

from app.config import settings
from app.db import search_chunks

NOT_ENOUGH_INFO_ANSWER = (
    "I don't have enough information in the provided context to answer this question."
)

SYSTEM_PROMPT = f"""You are a policy document assistant. Answer the user's question using ONLY the
context chunks provided below - do not use any outside knowledge.

Cite the chunk(s) you used inline, like [Chunk 0], next to the claims they support.

If the context does not contain enough information to answer the question, respond
with exactly: "{NOT_ENOUGH_INFO_ANSWER}\""""

_EXCERPT_LENGTH = 200


class RetrievedChunk(TypedDict):
    source_document: str
    chunk_index: int
    page_reference: str
    content: str
    similarity: float


class Citation(TypedDict):
    source: str
    chunkIndex: int
    excerpt: str


class GraphState(TypedDict):
    question: str
    retrieved_chunks: list[RetrievedChunk]
    answer: str
    citations: list[Citation]
    has_grounded_answer: bool


# Lazy singletons so importing this module (and /health) doesn't require an API key.
_embeddings: OpenAIEmbeddings | None = None
_llm: ChatOpenAI | None = None


def _get_embeddings() -> OpenAIEmbeddings:
    global _embeddings
    if _embeddings is None:
        _embeddings = OpenAIEmbeddings(
            model=settings.embedding_model,
            dimensions=settings.embedding_dimensions,
            api_key=settings.openai_api_key or None,
        )
    return _embeddings


def _get_llm() -> ChatOpenAI:
    global _llm
    if _llm is None:
        _llm = ChatOpenAI(model=settings.llm_model, api_key=settings.openai_api_key or None)
    return _llm


def _excerpt(content: str) -> str:
    trimmed = content.strip()
    if len(trimmed) <= _EXCERPT_LENGTH:
        return trimmed
    return trimmed[:_EXCERPT_LENGTH].rstrip() + "..."


def _build_user_prompt(question: str, chunks: list[RetrievedChunk]) -> str:
    lines = ["Context:", ""]
    for chunk in chunks:
        lines.append(
            f"[Chunk {chunk['chunk_index']}] "
            f"(source: {chunk['source_document']}, {chunk['page_reference']})"
        )
        lines.append(chunk["content"])
        lines.append("")
    lines.append(f"Question: {question}")
    return "\n".join(lines)


async def retrieve_node(state: GraphState) -> dict:
    """Embed the question and similarity-search the shared pgvector table,
    keeping only chunks above the grounding threshold."""
    query_embedding = await _get_embeddings().aembed_query(state["question"])
    rows = await search_chunks(query_embedding, settings.retrieval_top_k)
    grounded = [row for row in rows if row["similarity"] >= settings.similarity_threshold]
    return {"retrieved_chunks": grounded}


async def generate_node(state: GraphState) -> dict:
    """Build a grounded prompt from the retrieved chunks and call the LLM -
    or, if nothing cleared the threshold, skip the LLM call entirely rather
    than let it hallucinate an answer."""
    chunks = state["retrieved_chunks"]

    if not chunks:
        return {
            "answer": NOT_ENOUGH_INFO_ANSWER,
            "citations": [],
            "has_grounded_answer": False,
        }

    user_prompt = _build_user_prompt(state["question"], chunks)
    response = await _get_llm().ainvoke(
        [("system", SYSTEM_PROMPT), ("user", user_prompt)]
    )

    citations: list[Citation] = [
        {
            "source": chunk["source_document"],
            "chunkIndex": chunk["chunk_index"],
            "excerpt": _excerpt(chunk["content"]),
        }
        for chunk in chunks
    ]

    return {
        "answer": response.content,
        "citations": citations,
        "has_grounded_answer": True,
    }


def build_graph():
    graph = StateGraph(GraphState)
    graph.add_node("retrieve", retrieve_node)
    graph.add_node("generate", generate_node)
    graph.set_entry_point("retrieve")
    graph.add_edge("retrieve", "generate")
    graph.add_edge("generate", END)
    return graph.compile()


rag_graph = build_graph()
