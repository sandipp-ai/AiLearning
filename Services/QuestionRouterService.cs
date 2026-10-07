using System.Text.Json;
using AiLearning.Console.Models;
using Microsoft.Extensions.AI;

namespace AiLearning.Console.Services;

public class QuestionRouterService
{
    private readonly IChatClient _chatClient;
    private readonly RagService _ragService;
    private readonly DatabaseQuestionService _databaseQuestionService;

    public QuestionRouterService(
        IChatClient chatClient,
        RagService ragService,
        DatabaseQuestionService databaseQuestionService)
    {
        _chatClient = chatClient;
        _ragService = ragService;
        _databaseQuestionService = databaseQuestionService;
    }

    public async Task<string> AskAsync(string question,CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return "Question cannot be empty.";
        }

        QuestionRoute route = await DetermineRouteAsync(question,cancellationToken);

        System.Console.WriteLine($"Question route: {route.Route}");

        return route.Route switch
        {
            "database" =>
                await _databaseQuestionService.AskAsync(
                    question,
                    cancellationToken),

            "document" =>
                await _ragService.AskAsync(
                    question),

            _ =>
                "I could not determine where to find the answer."
        };
    }

    private async Task<QuestionRoute> DetermineRouteAsync(
        string question,
        CancellationToken cancellationToken)
    {
        string prompt =
            """
            Classify where the following question should be answered from.

            Available routes:

            database
            - Structured product information stored in SQL Server.
            - Examples: product price, stock, active status,
              product details, active product count,
              out-of-stock products.

            document
            - Information contained in PDF, TXT or DOCX documents.
            - Examples: reports, policies, specifications,
              insurance information, medical reports,
              document text or document-specific details.

            unknown
            - The question cannot clearly be answered from either source.

            Return ONLY valid JSON.

            Required format:
            {
              "route": "database"
            }

            Do not include markdown.
            Do not explain your answer.

            Question:
            """ + question;

        ChatResponse response =  await _chatClient.GetResponseAsync(prompt,cancellationToken: cancellationToken);

        string json = RemoveMarkdownCodeFence(response.Text);

        try
        {
            QuestionRoute? result =
                JsonSerializer.Deserialize<QuestionRoute>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (result == null)
            {
                return Unknown();
            }

            result.Route =
                result.Route
                    .Trim()
                    .ToLowerInvariant();

            if (result.Route is not
                ("database" or "document"))
            {
                return Unknown();
            }

            return result;
        }
        catch (JsonException)
        {
            return Unknown();
        }
    }

    private static QuestionRoute Unknown()
    {
        return new QuestionRoute
        {
            Route = "unknown"
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