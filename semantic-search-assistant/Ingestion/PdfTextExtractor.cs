using UglyToad.PdfPig;

namespace SemanticSearchAssistant.Ingestion;

public record PageText(int PageNumber, string Text);

public static class PdfTextExtractor
{
    public static List<PageText> ExtractPages(string pdfFilePath)
    {
        using var document = PdfDocument.Open(pdfFilePath);

        var pages = new List<PageText>();
        foreach (var page in document.GetPages())
        {
            pages.Add(new PageText(page.Number, page.Text));
        }

        return pages;
    }
}
