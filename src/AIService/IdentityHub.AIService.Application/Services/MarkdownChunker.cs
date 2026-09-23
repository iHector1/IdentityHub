using System.Security.Cryptography;
using System.Text;
using IdentityHub.AIService.Application.Models;

namespace IdentityHub.AIService.Application.Services;

public sealed class MarkdownChunker
{
    public const int DefaultChunkSize = 700;
    public const int DefaultOverlap = 80;

    public IReadOnlyCollection<KnowledgeChunk> Chunk(
        KnowledgeDocument document,
        int chunkSize = DefaultChunkSize,
        int overlap = DefaultOverlap)
    {
        if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));
        if (overlap < 0 || overlap >= chunkSize) throw new ArgumentOutOfRangeException(nameof(overlap));

        var text = document.Text.Trim();
        if (text.Length == 0) return Array.Empty<KnowledgeChunk>();

        var chunks = new List<KnowledgeChunk>();
        var start = 0;
        var index = 0;
        while (start < text.Length)
        {
            var end = Math.Min(start + chunkSize, text.Length);
            if (end < text.Length)
            {
                var boundary = text.LastIndexOfAny(new[] { ' ', '\n', '\r', '\t' }, end - 1, end - start);
                if (boundary > start + chunkSize / 2)
                    end = boundary;
            }

            var chunkText = text[start..end].Trim();
            if (chunkText.Length > 0)
            {
                var id = CreateId(document.Source, index);
                chunks.Add(new KnowledgeChunk(id, document.Source, document.Title, index, chunkText));
                index++;
            }

            if (end >= text.Length) break;
            start = Math.Max(end - overlap, start + 1);
        }

        return chunks;
    }

    private static string CreateId(string source, int index)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{source}:{index}"));
        return new Guid(bytes[..16]).ToString("D");
    }
}
