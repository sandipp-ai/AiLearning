using AiLearning.Console.Models;

namespace AiLearning.Console.Services;

public class DocumentIndexingService
{
    private readonly IEnumerable<IDocumentTextExtractor> _extractors;
    private readonly TextChunkingService _chunkingService;
    private readonly DocumentIngestionService _ingestionService;
    private readonly QdrantVectorStore _vectorStore;
    private readonly DocumentHashService _hashService;

    public DocumentIndexingService(
    IEnumerable<IDocumentTextExtractor> extractors,
    TextChunkingService chunkingService,
    DocumentIngestionService ingestionService,
    QdrantVectorStore vectorStore,
    DocumentHashService hashService)
{
    _extractors = extractors;
    _chunkingService = chunkingService;
    _ingestionService = ingestionService;
    _vectorStore = vectorStore;
    _hashService = hashService;
}

public async Task IndexDocumentAsync(
    string filePath,
    CancellationToken cancellationToken = default)
{
    // --------------------------------------------------
    // 1. Validate file
    // --------------------------------------------------

    if (!File.Exists(filePath))
    {
        throw new FileNotFoundException(
            "Document not found.",
            filePath);
    }

    string fileName = Path.GetFileName(filePath);

    System.Console.WriteLine();
    System.Console.WriteLine(
        $"Checking document: {fileName}");

    // --------------------------------------------------
    // 2. Create stable document ID
    // --------------------------------------------------

    string documentId =
        _hashService.CreateDocumentId(
            filePath);

    // --------------------------------------------------
    // 3. Calculate current document hash
    // --------------------------------------------------

    System.Console.WriteLine(
        "Calculating document hash...");

    string documentHash =
        await _hashService.CalculateHashAsync(
            filePath,
            cancellationToken);

    // --------------------------------------------------
    // 4. Check existing document in Qdrant
    // --------------------------------------------------

    string? existingHash =
        await _vectorStore.GetDocumentHashAsync(
            documentId);

    // --------------------------------------------------
    // 5. Same document + same content
    // --------------------------------------------------

    if (existingHash != null &&
        string.Equals(
            existingHash,
            documentHash,
            StringComparison.OrdinalIgnoreCase))
    {
        System.Console.WriteLine(
            "Document is already indexed and unchanged.");

        System.Console.WriteLine(
            "Skipping extraction and embedding.");

        return;
    }

    // --------------------------------------------------
    // 6. Same document + changed content
    // --------------------------------------------------

    if (existingHash != null)
    {
        System.Console.WriteLine(
            "Document has changed.");

        System.Console.WriteLine(
            "Removing old document chunks...");

        await _vectorStore.DeleteDocumentAsync(
            documentId);

        System.Console.WriteLine(
            "Old document chunks removed.");
    }
    else
    {
        System.Console.WriteLine(
            "New document detected.");
    }

    // --------------------------------------------------
    // 7. Extract PDF
    // --------------------------------------------------

IDocumentTextExtractor? extractor =
    _extractors.FirstOrDefault(
        x => x.CanHandle(filePath));

if (extractor == null)
{
    throw new NotSupportedException(
        $"Unsupported document type: " +
        $"{Path.GetExtension(filePath)}");
}

    System.Console.WriteLine();
    System.Console.WriteLine(
        "Extracting PDF...");

    var pages = extractor.Extract(filePath);

    System.Console.WriteLine($"Pages containing text: {pages.Count}");

    // --------------------------------------------------
    // 8. Create chunks
    // --------------------------------------------------

    var chunks =
        _chunkingService.CreateChunks(
            pages);

    System.Console.WriteLine(
        $"Chunks created: {chunks.Count}");

    if (chunks.Count == 0)
    {
        System.Console.WriteLine(
            "No chunks were created.");

        return;
    }

    // --------------------------------------------------
    // 9. Assign document metadata
    // --------------------------------------------------

    foreach (var chunk in chunks)
    {
        chunk.DocumentId =
            documentId;

        chunk.DocumentHash =
            documentHash;
    }

    // --------------------------------------------------
    // 10. Process chunks in batches
    // --------------------------------------------------

    const int batchSize = 10;

    int totalBatches =
        (int)Math.Ceiling(
            chunks.Count / (double)batchSize);

    System.Console.WriteLine();
    System.Console.WriteLine(
        $"Processing {chunks.Count} chunks " +
        $"in {totalBatches} batches...");

    // --------------------------------------------------
    // 11. Generate embeddings + save each batch
    // --------------------------------------------------

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

        int startChunk =
            i + 1;

        int endChunk =
            i + batch.Count;

        System.Console.WriteLine();
        System.Console.WriteLine(
            $"Batch {batchNumber}/{totalBatches}");

        System.Console.WriteLine(
            $"Chunks {startChunk}-{endChunk}");

        // Generate embeddings
        await _ingestionService.EmbedAsync(
            batch,
            cancellationToken);

        // Save successful batch immediately
        await _vectorStore.UpsertChunksAsync(
            batch);

        System.Console.WriteLine(
            $"Batch {batchNumber} saved to Qdrant.");
    }

    // --------------------------------------------------
    // 12. Completed
    // --------------------------------------------------

    System.Console.WriteLine();
    System.Console.WriteLine(
        "Indexing completed successfully.");

    System.Console.WriteLine(
        $"Document: {fileName}");

    System.Console.WriteLine(
        $"Total chunks indexed: {chunks.Count}");
}
}