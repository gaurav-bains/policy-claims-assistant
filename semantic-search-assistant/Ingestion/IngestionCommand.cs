using Microsoft.Extensions.AI;

namespace SemanticSearchAssistant.Ingestion;

public static class IngestionCommand
{
    public static async Task RunAsync(IConfiguration configuration, string contentRootPath)
    {
        var ingestionOptions = configuration.GetSection(IngestionOptions.SectionName).Get<IngestionOptions>()
            ?? new IngestionOptions();
        var embeddingOptions = configuration.GetSection(EmbeddingOptions.SectionName).Get<EmbeddingOptions>()
            ?? new EmbeddingOptions();

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres configuration.");

        var sourceDocumentsPath = Path.GetFullPath(
            Path.Combine(contentRootPath, ingestionOptions.SourceDocumentsPath));

        if (!Directory.Exists(sourceDocumentsPath))
        {
            throw new DirectoryNotFoundException($"Source documents folder not found: {sourceDocumentsPath}");
        }

        var pdfFiles = Directory.GetFiles(sourceDocumentsPath, "*.pdf", SearchOption.TopDirectoryOnly);
        if (pdfFiles.Length == 0)
        {
            Console.WriteLine($"No PDF files found in {sourceDocumentsPath}. Nothing to ingest.");
            return;
        }

        var embeddingGenerator = EmbeddingGeneratorFactory.Create(embeddingOptions);

        var chunkStore = new ChunkStore(connectionString);
        await chunkStore.EnsureSchemaAsync();

        var chunkSizeChars = ingestionOptions.ChunkSizeTokens * IngestionOptions.CharsPerToken;
        var overlapChars = ingestionOptions.ChunkOverlapTokens * IngestionOptions.CharsPerToken;

        foreach (var pdfFile in pdfFiles)
        {
            var sourceDocument = Path.GetFileName(pdfFile);
            Console.WriteLine($"Ingesting {sourceDocument}...");

            var pages = PdfTextExtractor.ExtractPages(pdfFile);
            var chunks = TextChunker.ChunkPages(pages, chunkSizeChars, overlapChars);

            Console.WriteLine($"  {pages.Count} pages -> {chunks.Count} chunks");

            foreach (var chunk in chunks)
            {
                var embeddingResult = await embeddingGenerator.GenerateAsync(chunk.Content);
                var vector = embeddingResult.Vector.ToArray();

                await chunkStore.InsertChunkAsync(
                    sourceDocument,
                    chunk.ChunkIndex,
                    chunk.PageReference,
                    chunk.Content,
                    vector);

                Console.WriteLine($"  chunk {chunk.ChunkIndex} ({chunk.PageReference}) stored");
            }
        }

        Console.WriteLine("Ingestion complete.");
    }
}
