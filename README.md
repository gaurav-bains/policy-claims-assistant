# Policy & Claims Assistant

A comparison of three approaches to answering natural-language questions about complex policy and regulatory documents — from basic keyword search through two different retrieval-augmented generation (RAG) implementations — with answers grounded in and cited to the specific source sections.

## Why this project

Policy-heavy domains (workers' compensation, insurance, healthcare regulation, government services) share a common problem: the people who need answers — case workers, employers, applicants — are stuck manually searching long, dense manuals to find the right section. Getting it wrong isn't just inconvenient, it can lead to inconsistent or incorrect decisions.

I wanted to build something that tackled that problem properly rather than a toy "chat with a PDF" demo, and to compare two different ways of building it: real retrieval quality, real citations back to source, and honest handling of the cases where the system doesn't have enough grounded context to answer confidently.

**Data source:** this project uses WorkSafeBC's publicly available Rehabilitation Services and Claims Manual (RSCM), Volume II, as realistic source material. I chose this domain because it's public, genuinely complex, and a good real-world stand-in for the kind of policy-heavy content this pattern is built for. **This project is not affiliated with, endorsed by, or built for WorkSafeBC** — it's an independent technical project that happens to use their public documents as representative data.

## What's in this repo

Three implementations, same problem, same API contract (`POST /api/search`):

### `docs-search/`
A basic keyword-search API. Case-insensitive substring matching against policy snippets — no ranking, no fuzzy matching. Included as a baseline: it's fast and simple, but doesn't handle paraphrased or conceptual questions well, which is exactly the gap the other two implementations address.

### `semantic-search-assistant/`
A RAG implementation built on the .NET/Microsoft AI stack. Ingests the RSCM into a pgvector-backed Postgres store, then uses Semantic Kernel to embed questions, retrieve relevant chunks, and generate grounded, cited answers via an LLM. Built entirely in C# / ASP.NET Core.

### `langgraph-assistant/`
A parallel RAG implementation built on the Python/LangChain stack, to compare the two ecosystems on the same problem. It queries the *same* pgvector table `semantic-search-assistant/` ingests into — it doesn't re-ingest documents — and exposes the same grounded, cited-response contract. Two pieces: a FastAPI service doing the actual retrieval and generation via a LangGraph graph, and a thin ASP.NET Core proxy in front of it that validates and forwards requests, mirroring the pattern of the other two services.

## Why two AI implementations, not one

Rather than pick a single stack, this project builds the same functionality twice — once on the Microsoft/.NET side (Semantic Kernel), once on the Python/LangChain side (LangGraph) — to compare them directly on identical infrastructure (same vector store, same documents, same API contract). Real teams often face exactly this kind of ecosystem decision when adopting AI tooling; building both rather than reading about the tradeoffs was the more useful exercise.

## Stack

| Component | `semantic-search-assistant/` | `langgraph-assistant/` |
|---|---|---|
| API | C# / ASP.NET Core Minimal API | C# / ASP.NET Core (thin proxy) + Python / FastAPI |
| Orchestration | Semantic Kernel | LangGraph |
| Vector store | pgvector (PostgreSQL) | pgvector (shared with semantic-search-assistant) |
| LLM | OpenAI (`gpt-4o-mini`) by default - Claude API also supported, configurable | OpenAI (`gpt-4o-mini`) |

Embeddings are OpenAI (`text-embedding-3-small`) on both - Claude has no embeddings API.

## Status / scope

This is a focused technical project, not a production system:
- No authentication, no multi-tenant access control
- No real or internal WorkSafeBC data — public RSCM Volume II only
- No fine-tuning — orchestration and retrieval quality are the focus
- Not built or tested for production-scale load

**Not yet done, on the roadmap:**
- Deployment to Azure Functions
- Observability/tracing (LangSmith)
- Ingesting a second source document (OHS Regulation)
- Re-ranking step for retrieval quality
- Automated tests

## Setup

Each folder has its own README with specific run instructions. At a high level:

1. Start the shared Postgres/pgvector instance via `docker-compose up` in `semantic-search-assistant/`
2. Run the ingestion step in `semantic-search-assistant/` to embed the RSCM into pgvector
3. Run `semantic-search-assistant/` (`dotnet run`) to try the Semantic Kernel implementation
4. Run `langgraph-assistant/`'s Python service (`uvicorn`) and its C# proxy (`dotnet run`) to try the LangGraph implementation — both point at the same Postgres instance from step 1
5. `docs-search/` runs standalone (`dotnet run`), no dependencies

All three expose `POST /api/search` with a `{ "query": "string" }` request body.
