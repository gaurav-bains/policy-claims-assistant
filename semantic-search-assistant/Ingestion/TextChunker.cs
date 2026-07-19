using System.Text;

namespace SemanticSearchAssistant.Ingestion;

public record DocumentChunk(int ChunkIndex, string Content, string PageReference);

public static class TextChunker
{
    public static List<DocumentChunk> ChunkPages(IReadOnlyList<PageText> pages, int chunkSizeChars, int overlapChars)
    {
        if (chunkSizeChars <= overlapChars)
        {
            throw new ArgumentException("Chunk size must be greater than overlap.");
        }

        var pageBoundaries = new List<(int StartOffset, int PageNumber)>();
        var fullText = new StringBuilder();

        foreach (var page in pages)
        {
            pageBoundaries.Add((fullText.Length, page.PageNumber));
            fullText.Append(page.Text);
            fullText.Append('\n');
        }

        var text = fullText.ToString();
        var chunks = new List<DocumentChunk>();
        var step = chunkSizeChars - overlapChars;

        var index = 0;
        var position = 0;

        while (position < text.Length)
        {
            var length = Math.Min(chunkSizeChars, text.Length - position);
            var content = text.Substring(position, length).Trim();

            if (content.Length > 0)
            {
                var startPage = PageForOffset(pageBoundaries, position);
                var endPage = PageForOffset(pageBoundaries, position + length - 1);
                var pageReference = startPage == endPage ? $"p.{startPage}" : $"p.{startPage}-{endPage}";

                chunks.Add(new DocumentChunk(index, content, pageReference));
                index++;
            }

            if (position + length >= text.Length)
            {
                break;
            }

            position += step;
        }

        return chunks;
    }

    private static int PageForOffset(List<(int StartOffset, int PageNumber)> boundaries, int offset)
    {
        var page = boundaries.Count > 0 ? boundaries[0].PageNumber : 1;

        foreach (var (StartOffset, PageNumber) in boundaries)
        {
            if (StartOffset <= offset)
            {
                page = PageNumber;
            }
            else
            {
                break;
            }
        }

        return page;
    }
}
