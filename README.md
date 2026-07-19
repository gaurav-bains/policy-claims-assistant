# Policy & Claims Assistant
A tool for making it easier to find answers in complex policy and regulatory documents. Starting simple, will evolve from here.

## Current state

There's a basic keyword-search API (`docs-search/`) over policy documents. It does case-insensitive substring matching against a fixed set of snippets - no ranking, no fuzzy matching. Keyword matching has real limitations for this kind of document, e.g. it doesn't handle paraphrased or conceptual questions well.