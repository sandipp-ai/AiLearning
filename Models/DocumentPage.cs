namespace AiLearning.Console.Models;

public class DocumentPage
{
    public required string FileName { get; set; }

    public int PageNumber { get; set; }

    public required string Text { get; set; }
}