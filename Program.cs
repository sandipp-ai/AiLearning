using AiLearning.Console.Data;
using AiLearning.Console.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;

// --------------------------------------------------
// Documents path
// --------------------------------------------------

string documentsPath = Path.GetFullPath(
    Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "Documents"));

System.Console.WriteLine(
    $"Documents path: {documentsPath}");

// --------------------------------------------------
// Configuration
// --------------------------------------------------

var configuration =
    new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile(
            "appsettings.json",
            optional: false,
            reloadOnChange: false)
        .AddUserSecrets<Program>()
        .Build();

string? token =
    configuration["HuggingFace:Token"];

if (string.IsNullOrWhiteSpace(token))
{
    System.Console.WriteLine(
        "Hugging Face token not found.");

    return;
}

// --------------------------------------------------
// Database
// --------------------------------------------------

string connectionString =
    configuration.GetConnectionString(
        "AiLearningDatabase")
    ?? throw new InvalidOperationException(
        "Database connection string not found.");

var dbOptions =
    new DbContextOptionsBuilder<AiLearningDbContext>()
        .UseSqlServer(connectionString)
        .Options;

await using var dbContext =
    new AiLearningDbContext(dbOptions);

var databaseInitializer =
    new DatabaseInitializer(dbContext);

await databaseInitializer.SeedAsync();

var productRepository =
    new ProductRepository(dbContext);

// --------------------------------------------------
// Embedding client
// --------------------------------------------------

using var httpClient =
    new HttpClient();

IEmbeddingService embeddingService =
    new HuggingFaceEmbeddingService(
        httpClient,
        token,
        "BAAI/bge-small-en-v1.5");

// --------------------------------------------------
// GPT-OSS client
// --------------------------------------------------

var openAiOptions =
    new OpenAIClientOptions
    {
        Endpoint = new Uri(
            "https://router.huggingface.co/v1")
    };

var aiClient =
    new OpenAIClient(
        new ApiKeyCredential(token),
        openAiOptions);

IChatClient chatClient =
    aiClient
        .GetChatClient(
            "openai/gpt-oss-120b")
        .AsIChatClient();

// --------------------------------------------------
// Database question service
// IMPORTANT: productRepository and chatClient
// must exist before creating this service.
// --------------------------------------------------

var databaseQuestionService =
    new DatabaseQuestionService(
        productRepository,
        chatClient);

// --------------------------------------------------
// Qdrant
// --------------------------------------------------

var vectorStore =
    new QdrantVectorStore();

await vectorStore.CreateCollectionAsync();

// --------------------------------------------------
// Document extraction
// --------------------------------------------------

var extractors =
    new List<IDocumentTextExtractor>
    {
        new PdfTextExtractor(),
        new TxtTextExtractor(),
        new DocxTextExtractor()
    };

var chunkingService =
    new TextChunkingService(
        chunkSize: 200,
        overlap: 40);

var ingestionService =
    new DocumentIngestionService(
        embeddingService);

var hashService =
    new DocumentHashService();

// --------------------------------------------------
// Document indexing
// --------------------------------------------------

var indexingService =
    new DocumentIndexingService(
        extractors,
        chunkingService,
        ingestionService,
        vectorStore,
        hashService);

var folderIndexingService =
    new DocumentFolderIndexingService(
        indexingService);

// --------------------------------------------------
// Document RAG
// --------------------------------------------------

var ragService =
    new RagService(
        embeddingService,
        vectorStore,
        chatClient);

var questionRouterService =
    new QuestionRouterService(
        chatClient,
        ragService,
        databaseQuestionService);

// --------------------------------------------------
// Main menu
// --------------------------------------------------

while (true)
{
    System.Console.WriteLine();
    System.Console.WriteLine("AI Learning");
    System.Console.WriteLine("-----------------------------");
    System.Console.WriteLine("1. Ingest Documents");
    System.Console.WriteLine("2. Ask Document Question");
    System.Console.WriteLine("3. View Database Products");
    System.Console.WriteLine("4. Ask Database Question");
    System.Console.WriteLine("0. Exit");
    System.Console.WriteLine();

    System.Console.Write("Select: ");

    string? choice =
        System.Console.ReadLine();

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