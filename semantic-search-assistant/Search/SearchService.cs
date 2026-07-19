using Microsoft.Extensions.AI;
using SemanticSearchAssistant.Ingestion;

namespace SemanticSearchAssistant.Search;

public class SearchService(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    ChunkStore chunkStore,
    IChatClient chatClient,
    RetrievalOptions retrievalOptions,
    LlmOptions llmOptions)
{
    private const int ExcerptLength = 200;

    public async Task<SearchResponse> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await embeddingGenerator.GenerateAsync(query, cancellationToken: cancellationToken);

        var results = await chunkStore.SearchAsync(
            queryEmbedding.Vector.ToArray(),
            retrievalOptions.TopK,
            cancellationToken);

        var groundedChunks = results
            .Where(r => r.Similarity >= retrievalOptions.SimilarityThreshold)
            .ToList();

        if (groundedChunks.Count == 0)
        {
            return new SearchResponse(
                PromptBuilder.NotEnoughInformationAnswer,
                [],
                HasGroundedAnswer: false);
        }

        var systemPrompt = PromptBuilder.BuildSystemPrompt();
        var userPrompt = PromptBuilder.BuildUserPrompt(query, groundedChunks);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        };

        var chatResponse = await chatClient.GetResponseAsync(
            messages,
            new ChatOptions { ModelId = llmOptions.ModelId },
            cancellationToken);

        var citations = groundedChunks
            .Select(chunk => new Citation(
                chunk.SourceDocument,
                chunk.ChunkIndex,
                Excerpt(chunk.Content)))
            .ToList();

        return new SearchResponse(chatResponse.Text, citations, HasGroundedAnswer: true);
    }

    private static string Excerpt(string content)
    {
        var trimmed = content.Trim();
        return trimmed.Length <= ExcerptLength
            ? trimmed
            : trimmed[..ExcerptLength].TrimEnd() + "...";
    }
}
