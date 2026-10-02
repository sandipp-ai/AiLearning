using AiLearning.Console.Models;

namespace AiLearning.Console.Services;

public class SemanticSearchService
{
    private readonly IEmbeddingService _embeddingService;

    public SemanticSearchService(
        IEmbeddingService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    public async Task<List<(DocumentChunk Chunk, double Score)>>
        SearchAsync(
            string question,
            IEnumerable<DocumentChunk> chunks,
            int topK = 3)
    {
        float[] questionVector =
            await _embeddingService.GenerateEmbeddingAsync(question);

        return chunks
            .Where(x => x.Embedding.Length > 0)
            .Select(chunk => (
                Chunk: chunk,
                Score: CosineSimilarity(
                    questionVector,
                    chunk.Embedding)))
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();
    }

    private static double CosineSimilarity(
        float[] vectorA,
        float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
        {
            throw new ArgumentException(
                "Vectors must have the same length.");
        }

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }

        if (magnitudeA == 0 || magnitudeB == 0)
            return 0;

        return dotProduct /
               (Math.Sqrt(magnitudeA) *
                Math.Sqrt(magnitudeB));
    }
}