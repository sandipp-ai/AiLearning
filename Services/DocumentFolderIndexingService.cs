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

        string[] pdfFiles =
            Directory.GetFiles(
                folderPath,
                "*.pdf",
                SearchOption.TopDirectoryOnly);

        if (pdfFiles.Length == 0)
        {
            System.Console.WriteLine(
                "No PDF documents found.");

            return;
        }

        System.Console.WriteLine();
        System.Console.WriteLine(
            $"PDF documents found: {pdfFiles.Length}");

        foreach (string pdfFile in pdfFiles)
        {
            System.Console.WriteLine();
            System.Console.WriteLine(
                "====================================");

            System.Console.WriteLine(
                $"Processing: {Path.GetFileName(pdfFile)}");

            System.Console.WriteLine(
                "====================================");

            try
            {
                await _documentIndexingService.IndexPdfAsync(
                    pdfFile,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(
                    $"Failed to index " +
                    $"{Path.GetFileName(pdfFile)}:");

                System.Console.WriteLine(
                    ex.Message);
            }
        }

        System.Console.WriteLine();
        System.Console.WriteLine(
            "Folder indexing completed.");
    }
}