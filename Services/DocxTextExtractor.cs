using AiLearning.Console.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AiLearning.Console.Services;

public class DocxTextExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string filePath)
    {
        return string.Equals(
            Path.GetExtension(filePath),
            ".docx",
            StringComparison.OrdinalIgnoreCase);
    }

    public List<DocumentPage> Extract(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Word document not found.",
                filePath);
        }

        using WordprocessingDocument document =
            WordprocessingDocument.Open(
                filePath,
                false);

        Body? body =
            document.MainDocumentPart?
                .Document?
                .Body;

        if (body == null)
            return [];

        string text = string.Join(
            Environment.NewLine,
            body.Descendants<Paragraph>()
                .Select(p => p.InnerText)
                .Where(t =>
                    !string.IsNullOrWhiteSpace(t)));

        if (string.IsNullOrWhiteSpace(text))
            return [];

        return
        [
            new DocumentPage
            {
                FileName = Path.GetFileName(filePath),
                PageNumber = 1,
                Text = text
            }
        ];
    }
}