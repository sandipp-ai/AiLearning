using AiLearning.Console.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;


var hashService = new DocumentHashService();
// --------------------------------------------------
// PDF
// --------------------------------------------------

    string documentsPath = Path.GetFullPath(
    Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "Documents"));

System.Console.WriteLine($"Documents path: {documentsPath}");
// --------------------------------------------------
// Configuration
// --------------------------------------------------

var configuration =
    new ConfigurationBuilder()
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
// Qdrant
// --------------------------------------------------

var vectorStore =
    new QdrantVectorStore();

await vectorStore.CreateCollectionAsync();
System.Console.WriteLine(
    "DEBUG: Qdrant initialization completed.");
// --------------------------------------------------
// PDF services
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

// --------------------------------------------------
// Indexing service
// --------------------------------------------------

var indexingService =
    new DocumentIndexingService(
        extractors,
        chunkingService,
        ingestionService,
        vectorStore,
        hashService);

var folderIndexingService = new DocumentFolderIndexingService(indexingService);

// --------------------------------------------------
// RAG service
// --------------------------------------------------

var ragService =
    new RagService(
        embeddingService,
        vectorStore,
        chatClient);

System.Console.WriteLine(
    "DEBUG: Starting main menu.");
    
while (true)
{
    
    System.Console.WriteLine();
    System.Console.WriteLine("AI Learning");
    System.Console.WriteLine("--------------------");
    System.Console.WriteLine("1. Ingest PDFs");
    System.Console.WriteLine("2. Ask question");
    System.Console.WriteLine("0. Exit");
    System.Console.WriteLine();

    System.Console.Write("Select: ");

    string? choice =
        System.Console.ReadLine();

    if (choice == "0")
        break;

    if (choice == "1")
    {
        await folderIndexingService.IndexFolderAsync(documentsPath);
        continue;
    }

    if (choice == "2")
    {
        System.Console.Write(
            "Question: ");

        string? question =
            System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(question))
            continue;

        System.Console.WriteLine();
        System.Console.WriteLine(
            "Generating answer...");

        string answer =
            await ragService.AskAsync(
                question);

        System.Console.WriteLine();
        System.Console.WriteLine(
            "AI Answer:");

        System.Console.WriteLine();
        System.Console.WriteLine(
            answer);

        continue;
    }

    System.Console.WriteLine(
        "Invalid option.");
}        