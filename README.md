<!-- TODO: full README content to be added -->

# Policy & Claims Assistant
A tool for making it easier to find answers in complex policy and regulatory documents. Starting simple, will evolve from here.

## Current state

There's a basic keyword-search API (`docs-search/`) over policy documents. It does case-insensitive substring matching against a fixed set of snippets - no ranking, no fuzzy matching. Keyword matching has real limitations for this kind of document, e.g. it doesn't handle paraphrased or conceptual questions well.

There's also a RAG-based API (`semantic-search-assistant/`) that ingests policy documents into a pgvector-backed Postgres store and uses Semantic Kernel plus an LLM to answer questions with grounded, cited responses drawn from the ingested content. Unlike the keyword-search approach, it works off the meaning of a question rather than literal word overlap.

There's a parallel Python implementation of the same RAG approach (`langgraph-assistant/`), built to evaluate the LangChain/LangGraph ecosystem as an alternative to the .NET/Semantic Kernel stack. It queries the same pgvector table `semantic-search-assistant/` ingests into - it doesn't ingest documents itself - and exposes the same grounded, cited-response contract as `semantic-search-assistant/`, so the two are interchangeable. It's split into two pieces: a FastAPI service that does the actual retrieval and generation via a LangGraph graph, and a thin ASP.NET Core proxy in front of it that validates requests and forwards them, mirroring the pattern of the other two services.