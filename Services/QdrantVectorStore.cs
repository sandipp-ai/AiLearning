using AiLearning.Console.Models;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace AiLearning.Console.Services;

public class QdrantVectorStore
{
    private const string CollectionName =
        "document_chunks";

    private readonly QdrantClient _client;

    public QdrantVectorStore()
    {
        _client = new QdrantClient(
            host: "localhost",
            port: 6334);
    }

    public async Task CreateCollectionAsync()
    {
        bool exists =
            await _client.CollectionExistsAsync(
                CollectionName);

        if (exists)
        {
            System.Console.WriteLine(
                $"Collection '{CollectionName}' already exists.");

            return;
        }

        await _client.CreateCollectionAsync(
            collectionName: CollectionName,
            vectorsConfig: new VectorParams
            {
                Size = 384,
                Distance = Distance.Cosine
            });

        System.Console.WriteLine(
            $"Collection '{CollectionName}' created.");
    }

    public async Task UpsertChunkAsync(
        DocumentChunk chunk)
    {
        if (chunk.Embedding.Length != 384)
        {
            throw new ArgumentException(
                $"Expected 384 dimensions, " +
                $"but received {chunk.Embedding.Length}.");
        }

        var point = new PointStruct
        {
            Id = new PointId
            {
                Uuid = chunk.Id
            },
            Vectors = chunk.Embedding
        };

        point.Payload["fileName"] =
            chunk.FileName;

        point.Payload["pageNumber"] =
            chunk.PageNumber;

        point.Payload["chunkNumber"] =
            chunk.ChunkNumber;

        point.Payload["text"] =
            chunk.Text;

        await _client.UpsertAsync(
            collectionName: CollectionName,
            points: new[] { point });
    }

    public async Task<IReadOnlyList<ScoredPoint>>
        SearchAsync(
            float[] queryVector,
            ulong limit = 3)
    {
        if (queryVector.Length != 384)
        {
            throw new ArgumentException(
                $"Expected 384 dimensions, " +
                $"but received {queryVector.Length}.");
        }

        var results =
            await _client.QueryAsync(
                collectionName: CollectionName,
                query: queryVector,
                limit: limit,
                payloadSelector: true);

        return results;
    }

    public async Task UpsertChunksAsync(
    IEnumerable<DocumentChunk> chunks)
{
    var points = new List<PointStruct>();

    foreach (var chunk in chunks)
    {
        if (chunk.Embedding.Length != 384)
        {
            throw new ArgumentException(
                $"Chunk {chunk.Id} has " +
                $"{chunk.Embedding.Length} dimensions. " +
                "Expected 384.");
        }

        var point = new PointStruct
        {
            Id = new PointId
            {
                Uuid = chunk.Id
            },
            Vectors = chunk.Embedding
        };

        point.Payload["fileName"] =
            chunk.FileName;

        point.Payload["pageNumber"] =
            chunk.PageNumber;

        point.Payload["chunkNumber"] =
            chunk.ChunkNumber;

        point.Payload["text"] =
            chunk.Text;

        points.Add(point);
    }

    await _client.UpsertAsync(
        collectionName: CollectionName,
        points: points);
}
}