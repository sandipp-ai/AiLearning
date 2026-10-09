using Microsoft.Extensions.AI;

namespace AiLearning.Console.Services;

public class RagService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly QdrantVectorStore _vectorStore;
    private readonly IChatClient _chatClient;

    public RagService(
        IEmbeddingService embeddingService,
        QdrantVectorStore vectorStore,
        IChatClient chatClient)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _chatClient = chatClient;
    }

    public async Task<string> AskAsync(
        string question)
    {
        // 1. Embed question

        float[] questionEmbedding;

        using (new PerformanceTimer("RAG - Local BGE Embedding"))
        {
            questionEmbedding =
                await _embeddingService.GenerateEmbeddingAsync(question);
        }

        // 2. Search Qdrant

        var results =            await _vectorStore.SearchAsync(
                questionEmbedding,
                limit: 3);

        if (results.Count == 0)
        {
            return
                "I could not find relevant information " +
                "in the document.";
        }

        // 3. Build context

        var contextParts =
            new List<string>();

        foreach (var result in results)
        {
            string fileName =
                result.Payload.TryGetValue(
                    "fileName",
                    out var fileValue)
                    ? fileValue.StringValue
                    : "Unknown";

            long pageNumber =
                result.Payload.TryGetValue(
                    "pageNumber",
                    out var pageValue)
                    ? pageValue.IntegerValue
                    : 0;

            string text =
                result.Payload.TryGetValue(
                    "text",
                    out var textValue)
                    ? textValue.StringValue
                    : string.Empty;

            contextParts.Add(
                $"""
                Source: {fileName}
                Page: {pageNumber}

                {text}
                """);
        }

        string context =
            string.Join(
                Environment.NewLine +
                Environment.NewLine,
                contextParts);

        // 4. Ask LLM

        var messages =
            new List<ChatMessage>
            {
                new(
                    ChatRole.System,
                    """
                    You are a document question-answering assistant.

                    Answer using only the provided document context.

                    Rules:
                    - Do not use outside knowledge.
                    - Do not invent missing information.
                    - Prefer actual measured values over general
                      explanatory information.
                    - If the answer cannot be determined from the
                      context, say:
                      "I could not find the answer in the document."
                    - Include the source filename and page number.
                    - Keep the answer concise.
                    """),

                new(
                    ChatRole.User,
                    $"""
                    DOCUMENT CONTEXT:

                    {context}

                    QUESTION:

                    {question}
                    """)
            };

        var response =
            await _chatClient.GetResponseAsync(
                messages);

        return response.Text;
    }
}