namespace AiLearning.Console.Models;

public class ProductQueryIntent
{
    public string Action { get; set; } =
        string.Empty;

    public string? PartNumber { get; set; }
}