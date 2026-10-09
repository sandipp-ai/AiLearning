using AiLearning.Console.Configuration;
using AiLearning.Console.Data;
using AiLearning.Console.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets<Program>();
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

builder.Services.AddDbContext<AiLearningDbContext>(
    options =>
    {
        string connectionString =
            builder.Configuration.GetConnectionString(
                "AiLearningDatabase")
            ?? throw new InvalidOperationException(
                "Database connection string not found.");

        options.UseSqlServer(connectionString);
    });

builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<DatabaseInitializer>();

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IEmbeddingService, LocalBgeEmbeddingService>();

    builder.Services.AddSingleton<IChatClient>(serviceProvider =>
    {
        IConfiguration configuration =
            serviceProvider.GetRequiredService<IConfiguration>();

        string apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException(
                "Gemini API key not found.");

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(
                "https://generativelanguage.googleapis.com/v1beta/openai")
        };

        var client = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            options);

        return client
    .GetChatClient("gemini-3.8-flash")
    .AsIChatClient();
    });

    builder.Services.AddSingleton<IDocumentTextExtractor,PdfTextExtractor>();
    builder.Services.AddSingleton<IDocumentTextExtractor,TxtTextExtractor>();
    builder.Services.AddSingleton<IDocumentTextExtractor,DocxTextExtractor>();
    builder.Services.AddSingleton(new TextChunkingService(chunkSize: 200,overlap: 40));
    builder.Services.AddSingleton<DocumentHashService>();
    builder.Services.AddSingleton<QdrantVectorStore>();
    builder.Services.AddSingleton<DocumentIngestionService>();
    builder.Services.AddSingleton<DocumentIndexingService>();
    builder.Services.AddSingleton<DocumentFolderIndexingService>();
    builder.Services.AddSingleton<RagService>();
    builder.Services.AddScoped<DatabaseQuestionService>();
    builder.Services.AddScoped<QuestionRouterService>();

using IHost host = builder.Build();
var localEmbedding = host.Services.GetRequiredService<IEmbeddingService>();

float[] vector = await localEmbedding.GenerateEmbeddingAsync(
    "What does the employee benefits document say about annual paid leave?");

double magnitude = Math.Sqrt(vector.Sum(x => (double)x * x));

System.Console.WriteLine($"Dimensions: {vector.Length}");
System.Console.WriteLine($"Vector magnitude: {magnitude:F6}");

using IServiceScope scope = host.Services.CreateScope();
IServiceProvider services = scope.ServiceProvider;

var databaseInitializer = services.GetRequiredService<DatabaseInitializer>();

await databaseInitializer.SeedAsync();

var vectorStore = services.GetRequiredService<QdrantVectorStore>();
await vectorStore.CreateCollectionAsync();
var folderIndexingService =
    services.GetRequiredService<DocumentFolderIndexingService>();

var questionRouterService =
    services.GetRequiredService<QuestionRouterService>();

var productRepository =
    services.GetRequiredService<ProductRepository>();

var appSettings =
    services.GetRequiredService<IOptions<AppSettings>>().Value;

string documentsPath =
    Path.GetFullPath(
        Path.Combine(
            AppContext.BaseDirectory,
            appSettings.DocumentsPath));

System.Console.WriteLine(
    $"Documents path: {documentsPath}");

// --------------------------------------------------
// Main menu
// --------------------------------------------------

while (true)
{
    System.Console.WriteLine();
    System.Console.WriteLine("AI Learning");
    System.Console.WriteLine("-----------------------------");
    System.Console.WriteLine("1. Ingest Documents");
    System.Console.WriteLine("2. Ask Question");
    System.Console.WriteLine("3. View Database Products");
    System.Console.WriteLine("0. Exit");
    System.Console.WriteLine();

    System.Console.Write("Select: ");

    string? choice = System.Console.ReadLine();

    if (choice == "0")
        break;

    // --------------------------------------------------
    // Ingest documents
    // --------------------------------------------------

    if (choice == "1")
    {
        await folderIndexingService
            .IndexFolderAsync(
                documentsPath);

        continue;
    }

    // --------------------------------------------------
    // Document question
    // --------------------------------------------------

    if (choice == "2")
{
    System.Console.Write(
        "Question: ");

    string? question =
        System.Console.ReadLine();

    if (string.IsNullOrWhiteSpace(question))
        continue;

    try
    {
        System.Console.WriteLine();
        System.Console.WriteLine(
            "Analyzing question...");

        string answer =
            await questionRouterService
                .AskAsync(question);

        System.Console.WriteLine();
        System.Console.WriteLine(
            "AI Answer:");

        System.Console.WriteLine(
            answer);
    }
    catch (Exception ex)
    {
        System.Console.WriteLine();
        System.Console.WriteLine(
            $"Question failed: {ex.Message}");
    }

    continue;
}

    // --------------------------------------------------
    // View products
    // --------------------------------------------------

    if (choice == "3")
    {
        var products =
            await productRepository
                .GetAllAsync();

        System.Console.WriteLine();
        System.Console.WriteLine(
            "Products");

        System.Console.WriteLine(
            "------------------------------------------------");

        foreach (var product in products)
        {
            System.Console.WriteLine(
                $"{product.PartNumber} | " +
                $"{product.Name} | " +
                $"Price: {product.Price:C} | " +
                $"Stock: {product.Stock} | " +
                $"Active: {product.IsActive}");
        }

        continue;
    }
  
    System.Console.WriteLine(
        "Invalid option.");
}