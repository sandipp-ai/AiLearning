using AiLearning.Console.Models;

namespace AiLearning.Console.Services;

public interface IDocumentTextExtractor
{
    bool CanHandle(string filePath);

    List<DocumentPage> Extract(string filePath);
}