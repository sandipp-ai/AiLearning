using System.Text.Json;
using AiLearning.Console.Data;
using AiLearning.Console.Models;
using Microsoft.Extensions.AI;

namespace AiLearning.Console.Services;

public class DatabaseQuestionService
{
    private readonly ProductRepository _productRepository;
    private readonly IChatClient _chatClient;

    public DatabaseQuestionService(
        ProductRepository productRepository,
        IChatClient chatClient)
    {
        _productRepository = productRepository;
        _chatClient = chatClient;
    }

    public async Task<string> AskAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        ProductQueryIntent intent =
            await DetermineIntentAsync(
                question,
                cancellationToken);

        switch (intent.Action)
        {
            case "active_count":
                int count =
                    await _productRepository
                        .GetActiveCountAsync(
                            cancellationToken);

                return $"There are {count} active products.";

            case "product_details":
                if (string.IsNullOrWhiteSpace(
                    intent.PartNumber))
                {
                    return "A part number is required.";
                }

                var product =
                    await _productRepository
                        .GetByPartNumberAsync(
                            intent.PartNumber,
                            cancellationToken);

                if (product == null)
                {
                    return
                        $"Product {intent.PartNumber} was not found.";
                }

                return
                    $"Product {product.PartNumber} is " +
                    $"{product.Name}. " +
                    $"Price: {product.Price:C}. " +
                    $"Stock: {product.Stock}. " +
                    $"Active: {product.IsActive}.";

            case "out_of_stock":
                var products =
                    await _productRepository
                        .GetOutOfStockAsync(
                            cancellationToken);

                if (products.Count == 0)
                    return "No products are out of stock.";

                string productList =
                    string.Join(
                        ", ",
                        products.Select(
                            x => $"{x.PartNumber} ({x.Name})"));

                return
                    $"Out-of-stock products: {productList}.";

            default:
                return
                    "I could not determine the database query.";
        }
    }

    private async Task<ProductQueryIntent> DetermineIntentAsync(
        string question,
        CancellationToken cancellationToken)
    {
        string prompt =
            """
            Classify the following product database question.

            Supported actions:
            - active_count
            - product_details
            - out_of_stock
            - unknown

            If the question contains a part number,
            extract it into partNumber.

            Return ONLY valid JSON.

            Example:
            {
              "action": "product_details",
              "partNumber": "ABC-100"
            }

            Question:
            """ + question;

        ChatResponse response =
            await _chatClient.GetResponseAsync(
                prompt,
                cancellationToken: cancellationToken);

        string json =
            response.Text.Trim();

        json = RemoveMarkdownCodeFence(json);

        ProductQueryIntent? intent =
            JsonSerializer.Deserialize<ProductQueryIntent>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        return intent ??
            new ProductQueryIntent
            {
                Action = "unknown"
            };
    }

    private static string RemoveMarkdownCodeFence(
        string value)
    {
        value = value.Trim();

        if (!value.StartsWith("```"))
            return value;

        int firstNewLine =
            value.IndexOf('\n');

        int lastFence =
            value.LastIndexOf("```");

        if (firstNewLine < 0 ||
            lastFence <= firstNewLine)
        {
            return value;
        }

        return value[
            (firstNewLine + 1)..lastFence
        ].Trim();
    }
}