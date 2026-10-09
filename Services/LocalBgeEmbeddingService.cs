using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace AiLearning.Console.Services;

public sealed class LocalBgeEmbeddingService : IEmbeddingService, IDisposable
{
    private const int Dimensions = 384;
    private const int MaxTokens = 512;

    private readonly InferenceSession _session;
    private readonly BertTokenizer _tokenizer;

    public LocalBgeEmbeddingService()
    {
        string modelDirectory = Path.Combine(
            AppContext.BaseDirectory, "Models", "BgeSmallEnV15");

        string modelPath = Path.Combine(modelDirectory, "model.onnx");
        string tokenizerPath = Path.Combine(modelDirectory, "vocab.txt");

        if (!File.Exists(modelPath) || !File.Exists(tokenizerPath))
            throw new FileNotFoundException(
                "Local BGE model or tokenizer file not found.");

        _session = new InferenceSession(modelPath);

        using var stream = File.OpenRead(tokenizerPath);
        _tokenizer = BertTokenizer.Create(stream);
        var ids = _tokenizer.EncodeToIds("Hello world");
        System.Console.WriteLine($"Token IDs: {string.Join(", ", ids)}");
    }

    public Task<float[]> GenerateEmbeddingAsync(
    string text,
    CancellationToken cancellationToken = default)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(text);
    cancellationToken.ThrowIfCancellationRequested();

    return Task.Run(
        () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GenerateEmbedding(text);
        },
        cancellationToken);
}

    private float[] GenerateEmbedding(string text)
    {
        var tokenIds = _tokenizer.EncodeToIds(text)
    .Take(MaxTokens)
    .ToList();

        int length = tokenIds.Count;

        var inputIds = new DenseTensor<long>(new[] { 1, length });
        var attentionMask = new DenseTensor<long>(new[] { 1, length });
        var tokenTypeIds = new DenseTensor<long>(new[] { 1, length });

        for (int i = 0; i < length; i++)
        {
            inputIds[0, i] = tokenIds[i];
            attentionMask[0, i] = 1;
            tokenTypeIds[0, i] = 0;
        }

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
            NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIds)
        };

        using var results = _session.Run(inputs);

        var output = results
            .First(x => x.Name == "last_hidden_state")
            .AsTensor<float>();

        var embedding = new float[Dimensions];

        // CLS pooling: first token's hidden state
        for (int i = 0; i < Dimensions; i++)
            embedding[i] = output[0, 0, i];

        // L2 normalization
        double sumSquares = 0;

        for (int i = 0; i < Dimensions; i++)
            sumSquares += embedding[i] * embedding[i];

        double magnitude = Math.Sqrt(sumSquares);

        if (magnitude > 0)
        {
            for (int i = 0; i < Dimensions; i++)
                embedding[i] = (float)(embedding[i] / magnitude);
        }

        return embedding;
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}