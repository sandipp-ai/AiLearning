namespace AiLearning.Console.Models;

public class DocumentRecord
{
    public string DocumentId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string ContentHash { get; set; } = string.Empty;

    public DateTime IndexedAtUtc { get; set; }
}