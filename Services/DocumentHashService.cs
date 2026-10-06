using System.Security.Cryptography;

namespace AiLearning.Console.Services;

public class DocumentHashService
{
    public async Task<string> CalculateHashAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found.",filePath);
        }

        await using FileStream stream = File.OpenRead(filePath);

        byte[] hash = await SHA256.HashDataAsync(stream,cancellationToken);

        return Convert.ToHexString(hash);
    }

    public string CreateDocumentId(
    string filePath)
{
    string normalizedPath =
        Path.GetFullPath(filePath)
            .Trim()
            .ToLowerInvariant();

    byte[] hash =
        System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(
                normalizedPath));

    byte[] guidBytes =
        hash[..16];

    return new Guid(guidBytes)
        .ToString();
}
}