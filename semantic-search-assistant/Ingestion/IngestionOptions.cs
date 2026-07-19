namespace SemanticSearchAssistant.Ingestion;

public class IngestionOptions
{
    public const string SectionName = "Ingestion";

    public string SourceDocumentsPath { get; set; } = "../docs/source-documents";
    public int ChunkSizeTokens { get; set; } = 500;
    public int ChunkOverlapTokens { get; set; } = 50;

    // Rough approximation used when a real tokenizer isn't available.
    public const int CharsPerToken = 4;
}

public class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    public string Provider { get; set; } = "OpenAI";
    public string ModelId { get; set; } = "text-embedding-3-small";
    public int Dimensions { get; set; } = 1536;
    public string? ApiKey { get; set; }

    // Opt-in escape hatch for networks where the TLS stack can't reach an OCSP responder to confirm certificate revocation status (seen on some sandboxed/ corporate networks). Leave false unless you hit a RevocationStatusUnknown SSL error when calling the embedding API.
    public bool DisableCertificateRevocationCheck { get; set; } = false;
}
