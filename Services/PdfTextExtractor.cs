using AiLearning.Console.Models;
using UglyToad.PdfPig;

namespace AiLearning.Console.Services;

public class PdfTextExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string filePath)
    {
        return Path.GetExtension(
            filePath)
            .Equals(
                ".pdf",
                StringComparison.OrdinalIgnoreCase);
    }
    
    public List<DocumentPage> Extract(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "PDF file was not found.",
                filePath);
        }

        var pages = new List<DocumentPage>();

        using var document = PdfDocument.Open(filePath);

        foreach (var page in document.GetPages())
        {
            string text = page.Text;

            if (string.IsNullOrWhiteSpace(text))
                continue;

            pages.Add(new DocumentPage
            {
                FileName = Path.GetFileName(filePath),
                PageNumber = page.Number,
                Text = text
            });
        }

        return pages;
    }
}