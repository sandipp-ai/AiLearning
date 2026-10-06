namespace AiLearning.Console.Models;

public class DocumentChunk
{
    public required string Id { get; set; }

    public required string FileName { get; set; }

    public int PageNumber { get; set; }

    public int ChunkNumber { get; set; }

    public required string Text { get; set; }

    public float[] Embedding { get; set; } = [];
    public string DocumentHash { get; set; } = string.Empty;
}