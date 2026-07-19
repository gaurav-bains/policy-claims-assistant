namespace SemanticSearchAssistant.Search;

public class LlmOptions
{
    public const string SectionName = "Llm";

    // "OpenAI" or "Anthropic"
    public string Provider { get; set; } = "OpenAI";
    public string ModelId { get; set; } = "gpt-4o-mini";
    public string? ApiKey { get; set; }

    // See EmbeddingOptions.DisableCertificateRevocationCheck - same escape hatch, same default.
    public bool DisableCertificateRevocationCheck { get; set; } = false;
}

public class RetrievalOptions
{
    public const string SectionName = "Retrieval";

    public int TopK { get; set; } = 5;

    // Cosine similarity (0-1). Starting point - tune once real data is ingested.
    public double SimilarityThreshold { get; set; } = 0.3;
}
