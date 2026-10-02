using System.Security.Cryptography;
using System.Text;
using AiLearning.Console.Models;

namespace AiLearning.Console.Services;

public class TextChunkingService
{
    private readonly int _chunkSize;
    private readonly int _overlap;

    public TextChunkingService(
        int chunkSize = 200,
        int overlap = 40)
    {
        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(chunkSize));

        if (overlap < 0 || overlap >= chunkSize)
            throw new ArgumentOutOfRangeException(
                nameof(overlap));

        _chunkSize = chunkSize;
        _overlap = overlap;
    }

    public List<DocumentChunk> CreateChunks(
        IEnumerable<DocumentPage> pages)
    {
        var chunks = new List<DocumentChunk>();

        foreach (var page in pages)
        {
            string[] words = page.Text.Split(
                [' ', '\r', '\n', '\t'],
                StringSplitOptions.RemoveEmptyEntries);

            int start = 0;
            int chunkNumber = 1;

            while (start < words.Length)
            {
                int length = Math.Min(
                    _chunkSize,
                    words.Length - start);

                string text = string.Join(
                    " ",
                    words.Skip(start).Take(length));

                chunks.Add(new DocumentChunk
                {
                    Id = CreateChunkId(
                        page.FileName,
                        page.PageNumber,
                        chunkNumber),

                    FileName = page.FileName,
                    PageNumber = page.PageNumber,
                    ChunkNumber = chunkNumber,
                    Text = text
                });

                if (start + length >= words.Length)
                    break;

                start += _chunkSize - _overlap;

                chunkNumber++;
            }
        }

        return chunks;
    }

    private static string CreateChunkId(
        string fileName,
        int pageNumber,
        int chunkNumber)
    {
        string value =
            $"{fileName}|{pageNumber}|{chunkNumber}";

        byte[] hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(value));

        byte[] guidBytes = hash[..16];

        return new Guid(guidBytes).ToString();
    }
}