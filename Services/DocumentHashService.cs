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
}