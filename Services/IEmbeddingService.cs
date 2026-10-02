namespace AiLearning.Console.Services;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text,CancellationToken cancellationToken = default);
}