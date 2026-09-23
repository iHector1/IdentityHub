using System.Text;
using IdentityHub.AIService.Application.Models;

namespace IdentityHub.AIService.Application.Services;

public static class RagPromptBuilder
{
    public const string SystemPrompt = "You are an assistant for the IdentityHub system.\nAnswer only using the provided context.\nIf the context does not contain enough information, say that the information is not available in the indexed knowledge.\nDo not invent system behavior.";

    public static string BuildUserPrompt(string question, IReadOnlyCollection<RetrievedChunk> chunks)
    {
        var context = new StringBuilder();
        foreach (var chunk in chunks)
        {
            context.AppendLine($"SOURCE: {chunk.Source}");
            context.AppendLine($"TITLE: {chunk.Title}");
            context.AppendLine(chunk.Text);
            context.AppendLine();
        }

        return $"CONTEXT:\n{context}\nQUESTION:\n{question}";
    }
}
