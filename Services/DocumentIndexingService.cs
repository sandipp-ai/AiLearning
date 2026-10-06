using AiLearning.Console.Models;

namespace AiLearning.Console.Services;

public class DocumentIndexingService
{
    private readonly PdfTextExtractor _pdfExtractor;
    private readonly TextChunkingService _chunkingService;
    private readonly DocumentIngestionService _ingestionService;
    private readonly QdrantVectorStore _vectorStore;
    private readonly DocumentHashService _hashService;

    public DocumentIndexingService(
        PdfTextExtractor pdfExtractor,
        TextChunkingService chunkingService,
        DocumentIngestionService ingestionService,
        QdrantVectorStore vectorStore,
        DocumentHashService hashService)
    {
        _pdfExtractor = pdfExtractor;
        _chunkingService = chunkingService;
        _ingestionService = ingestionService;
        _vectorStore = vectorStore;
        _hashService = hashService;
    }

    public async Task IndexPdfAsync(
    string filePath,
    CancellationToken cancellationToken = default)
{
    if (!File.Exists(filePath))
    {
        throw new FileNotFoundException(
            "PDF file not found.",
            filePath);
    }
System.Console.WriteLine();
System.Console.WriteLine(
    "Calculating document hash...");

string documentHash =
    await _hashService.CalculateHashAsync(
        filePath,
        cancellationToken);

bool alreadyIndexed =
    await _vectorStore.DocumentExistsAsync(
        documentHash);

if (alreadyIndexed)
{
    System.Console.WriteLine(
        "Document is already indexed.");

    System.Console.WriteLine(
        "Skipping extraction and embedding.");

    return;
}
    System.Console.WriteLine();
    System.Console.WriteLine("Extracting PDF...");

    var pages =
        _pdfExtractor.Extract(filePath);

    System.Console.WriteLine(
        $"Pages containing text: {pages.Count}");

    var chunks =
        _chunkingService.CreateChunks(pages);

foreach (var chunk in chunks)
{
    chunk.DocumentHash =
        documentHash;
}
    System.Console.WriteLine(
        $"Chunks created: {chunks.Count}");

    if (chunks.Count == 0)
    {
        System.Console.WriteLine(
            "No chunks were created.");

        return;
    }

    const int batchSize = 10;

    int totalBatches =
        (int)Math.Ceiling(
            chunks.Count / (double)batchSize);

    System.Console.WriteLine();
    System.Console.WriteLine(
        $"Processing {chunks.Count} chunks " +
        $"in {totalBatches} batches...");

    for (int i = 0;
         i < chunks.Count;
         i += batchSize)
    {
        int batchNumber =
            (i / batchSize) + 1;

        var batch =
            chunks
                .Skip(i)
                .Take(batchSize)
                .ToList();

        System.Console.WriteLine();
        System.Console.WriteLine(
            $"Batch {batchNumber}/{totalBatches}");

        System.Console.WriteLine(
            $"Chunks {i + 1}-" +
            $"{i + batch.Count}");

        // Generate embeddings only for this batch
        await _ingestionService.EmbedAsync(
            batch,
            cancellationToken);

        // Immediately save successful batch
        await _vectorStore.UpsertChunksAsync(
            batch);

        System.Console.WriteLine(
            $"Batch {batchNumber} saved to Qdrant.");
    }

    System.Console.WriteLine();
    System.Console.WriteLine(
        $"Indexing completed successfully.");

    System.Console.WriteLine(
        $"Total chunks indexed: {chunks.Count}");
}
}