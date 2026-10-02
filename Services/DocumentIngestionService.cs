using AiLearning.Console.Models;

namespace AiLearning.Console.Services;

public class DocumentIngestionService
{
    private readonly IEmbeddingService _embeddingService;

    public DocumentIngestionService(
        IEmbeddingService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    public async Task EmbedAsync(
        IEnumerable<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        var chunkList = chunks.ToList();

        for (int i = 0; i < chunkList.Count; i++)
        {
            DocumentChunk chunk = chunkList[i];

            System.Console.WriteLine(
                $"Embedding chunk {i + 1}/{chunkList.Count}...");

            chunk.Embedding =
                await _embeddingService.GenerateEmbeddingAsync(
                    chunk.Text,
                    cancellationToken);
        }
    }
}