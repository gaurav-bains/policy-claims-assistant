using System.Text;
using SemanticSearchAssistant.Ingestion;

namespace SemanticSearchAssistant.Search;

public static class PromptBuilder
{
    public const string NotEnoughInformationAnswer =
        "I don't have enough information in the provided context to answer this question.";

    public static string BuildSystemPrompt()
    {
        return $"""
            You are a policy document assistant. Answer the user's question using ONLY the
            context chunks provided below - do not use any outside knowledge.

            Cite the chunk(s) you used inline, like [Chunk 0], next to the claims they support.

            If the context does not contain enough information to answer the question, respond
            with exactly: "{NotEnoughInformationAnswer}"
            """;
    }

    public static string BuildUserPrompt(string query, IReadOnlyList<ChunkSearchResult> chunks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Context:");
        sb.AppendLine();

        foreach (var chunk in chunks)
        {
            sb.AppendLine($"[Chunk {chunk.ChunkIndex}] (source: {chunk.SourceDocument}, {chunk.PageReference})");
            sb.AppendLine(chunk.Content);
            sb.AppendLine();
        }

        sb.AppendLine($"Question: {query}");

        return sb.ToString();
    }
}
