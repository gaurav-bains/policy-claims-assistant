namespace SemanticSearchAssistant.Search;

public record SearchRequest(string Query);

public record Citation(string Source, int ChunkIndex, string Excerpt);

public record SearchResponse(string Answer, List<Citation> Citations, bool HasGroundedAnswer);
