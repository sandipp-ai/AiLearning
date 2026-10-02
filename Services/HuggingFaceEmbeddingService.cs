using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AiLearning.Console.Services;

public class HuggingFaceEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    private const int MaxAttempts = 4;

    public HuggingFaceEmbeddingService(
        HttpClient httpClient,
        string token,
        string model)
    {
        _httpClient = httpClient;
        _model = model;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }

    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                "Text cannot be empty.",
                nameof(text));
        }

        string url =
            $"https://router.huggingface.co/hf-inference/models/{_model}";

        for (int attempt = 1;
             attempt <= MaxAttempts;
             attempt++)
        {
            try
            {
                var request = new
                {
                    inputs = text
                };

                using HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        url,
                        request,
                        cancellationToken);

                string json =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return ParseEmbedding(json);
                }

                if (!IsTransientStatusCode(
                    response.StatusCode))
                {
                    throw new Exception(
                        $"Hugging Face error " +
                        $"{(int)response.StatusCode}: {json}");
                }

                if (attempt == MaxAttempts)
                {
                    throw new Exception(
                        $"Hugging Face failed after " +
                        $"{MaxAttempts} attempts. " +
                        $"Last error {(int)response.StatusCode}: " +
                        $"{json}");
                }

                TimeSpan delay =
                    GetRetryDelay(attempt);

                System.Console.WriteLine(
                    $"Hugging Face returned " +
                    $"{(int)response.StatusCode}. " +
                    $"Retrying in {delay.TotalSeconds} seconds " +
                    $"(attempt {attempt + 1}/{MaxAttempts})...");

                await Task.Delay(
                    delay,
                    cancellationToken);
            }
            catch (HttpRequestException ex)
                when (attempt < MaxAttempts)
            {
                TimeSpan delay =
                    GetRetryDelay(attempt);

                System.Console.WriteLine(
                    $"Hugging Face connection error: " +
                    $"{ex.Message}");

                System.Console.WriteLine(
                    $"Retrying in {delay.TotalSeconds} seconds " +
                    $"(attempt {attempt + 1}/{MaxAttempts})...");

                await Task.Delay(
                    delay,
                    cancellationToken);
            }
        }

        throw new Exception(
            "Unable to generate embedding.");
    }

    private static float[] ParseEmbedding(
        string json)
    {
        using JsonDocument document =
            JsonDocument.Parse(json);

        JsonElement root =
            document.RootElement;

        if (root.ValueKind !=
            JsonValueKind.Array)
        {
            throw new Exception(
                $"Unexpected Hugging Face response: {json}");
        }

        JsonElement vectorElement =
            root;

        while (
            vectorElement.GetArrayLength() > 0 &&
            vectorElement[0].ValueKind ==
                JsonValueKind.Array)
        {
            vectorElement =
                vectorElement[0];
        }

        float[] embedding =
            vectorElement
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();

        if (embedding.Length == 0)
        {
            throw new Exception(
                "Hugging Face returned an empty embedding.");
        }

        return embedding;
    }

    private static bool IsTransientStatusCode(
        HttpStatusCode statusCode)
    {
        return statusCode ==
                   HttpStatusCode.TooManyRequests ||
               statusCode ==
                   HttpStatusCode.InternalServerError ||
               statusCode ==
                   HttpStatusCode.BadGateway ||
               statusCode ==
                   HttpStatusCode.ServiceUnavailable ||
               statusCode ==
                   HttpStatusCode.GatewayTimeout;
    }

    private static TimeSpan GetRetryDelay(
        int attempt)
    {
        int seconds =
            (int)Math.Pow(2, attempt);

        return TimeSpan.FromSeconds(
            seconds);
    }
}