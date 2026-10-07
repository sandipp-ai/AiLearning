namespace AiLearning.Console.Services;

public class DocumentFolderIndexingService
{
    private readonly DocumentIndexingService _documentIndexingService;

    public DocumentFolderIndexingService(DocumentIndexingService documentIndexingService)
    {
        _documentIndexingService = documentIndexingService;
    }

    public async Task IndexFolderAsync(string folderPath,CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException(
                $"Documents folder not found: {folderPath}");
        }

        string[] files =
    Directory.GetFiles(
        folderPath,
        "*.*",
        SearchOption.TopDirectoryOnly)
    .Where(file =>
        file.EndsWith(".pdf",StringComparison.OrdinalIgnoreCase) ||
        file.EndsWith(".txt",StringComparison.OrdinalIgnoreCase)||
        file.EndsWith(".docx",StringComparison.OrdinalIgnoreCase))
    .ToArray();

        if (files.Length == 0)
        {
            System.Console.WriteLine(
                "No PDF documents found.");

            return;
        }

        System.Console.WriteLine(
            $"Documents folder: {folderPath}");

        foreach (string file in files)
        {
            System.Console.WriteLine(
                $"Found file: {Path.GetFileName(file)}");
        }

        System.Console.WriteLine();
        System.Console.WriteLine(
            $"PDF documents found: {files.Length}");

        foreach (string file in files)
        {
            System.Console.WriteLine();
            System.Console.WriteLine(
                "====================================");

            System.Console.WriteLine(
                $"Processing: {Path.GetFileName(file)}");

            System.Console.WriteLine(
                "====================================");

            try
            {
                await _documentIndexingService.IndexDocumentAsync(
                    file,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(
                    $"Failed to index " +
                    $"{Path.GetFileName(file)}:");

                System.Console.WriteLine(
                    ex.Message);
            }
        }

        System.Console.WriteLine();
        System.Console.WriteLine(
            "Folder indexing completed.");
    }
}