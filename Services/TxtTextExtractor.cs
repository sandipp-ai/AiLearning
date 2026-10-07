using AiLearning.Console.Models;

namespace AiLearning.Console.Services;

public class TxtTextExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string filePath)
    {
        return string.Equals(
            Path.GetExtension(filePath),
            ".txt",
            StringComparison.OrdinalIgnoreCase);
    }

    public List<DocumentPage> Extract(
        string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Text file not found.",
                filePath);
        }

        string text =
            File.ReadAllText(filePath);

        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

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