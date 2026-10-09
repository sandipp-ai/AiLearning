using System.Text.Json;
using AiLearning.Console.Models;
using Microsoft.Extensions.AI;
using System.Diagnostics;

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
            route.DatabaseQuestion ?? question,
            cancellationToken),

    "document" =>
        await _ragService.AskAsync(
            route.DocumentQuestion ?? question),

    "both" =>
        await AskBothAsync(
            question,
            route,
            cancellationToken),

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
    Analyze the user's question and determine which
    data source is required.

    Available routes:

    database
    - Structured product information stored in SQL Server.
    - Product price, stock, active status, product details,
      active product count and out-of-stock products.

    document
    - Information contained in PDF, TXT or DOCX documents.
    - Reports, policies, specifications and other
      document content.

    both
    - The question requires information from BOTH
      SQL Server and documents.

    unknown
    - The question cannot be answered from the
      available sources.

    For a database question:
    {
      "route": "database",
      "databaseQuestion": "the database-specific question",
      "documentQuestion": null
    }

    For a document question:
    {
      "route": "document",
      "databaseQuestion": null,
      "documentQuestion": "the document-specific question"
    }

    For a question requiring both:
    {
      "route": "both",
      "databaseQuestion": "only the database part",
      "documentQuestion": "only the document part"
    }

    Important:
    - Preserve important identifiers such as part numbers.
    - Do not invent identifiers.
    - Do not answer the question.
    - Only classify and split the question.
    - Return ONLY valid JSON.
    - Do not include markdown.

    User question:
    """ + question;

        var stopwatch = Stopwatch.StartNew();

        ChatResponse response =  await _chatClient.GetResponseAsync(prompt,cancellationToken: cancellationToken);

        stopwatch.Stop();

        System.Console.WriteLine($"Question routing took: {stopwatch.ElapsedMilliseconds} ms");

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

           if (result.Route is not ("database" or "document" or "both"))
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
   private async Task<string> AskBothAsync(
    string originalQuestion,
    QuestionRoute route,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(
        route.DatabaseQuestion))
    {
        return "The database part of the question could not be determined.";
    }

    if (string.IsNullOrWhiteSpace(
        route.DocumentQuestion))
    {
        return "The document part of the question could not be determined.";
    }

    System.Console.WriteLine(
        $"Database question: {route.DatabaseQuestion}");

    System.Console.WriteLine(
        $"Document question: {route.DocumentQuestion}");

    string databaseAnswer =
        await _databaseQuestionService.AskAsync(
            route.DatabaseQuestion,
            cancellationToken);

    string documentAnswer =
        await _ragService.AskAsync(
            route.DocumentQuestion);

    string prompt =
        $"""
        Answer the user's original question using ONLY
        the retrieved information below.

        DATABASE INFORMATION:
        {databaseAnswer}

        DOCUMENT INFORMATION:
        {documentAnswer}

        ORIGINAL QUESTION:
        {originalQuestion}

        Requirements:
        - Combine the information into one answer.
        - Do not invent information.
        - Preserve document source information.
        - If information is missing, state that clearly.
        - Keep the answer concise.
        """;

    ChatResponse response =
        await _chatClient.GetResponseAsync(
            prompt,
            cancellationToken: cancellationToken);

    return response.Text.Trim();
}
}