using IdentityHub.AIService.Application.Abstractions;
using IdentityHub.AIService.Application.Models;
using Microsoft.Extensions.Hosting;

namespace IdentityHub.AIService.Infrastructure.Knowledge;

public sealed class FileKnowledgeDocumentReader(IHostEnvironment environment) : IKnowledgeDocumentReader
{
    public async Task<IReadOnlyCollection<KnowledgeDocument>> ReadAsync(CancellationToken cancellationToken = default)
    {
        var knowledgePath = Path.Combine(environment.ContentRootPath, "Knowledge");
        if (!Directory.Exists(knowledgePath))
            return Array.Empty<KnowledgeDocument>();

        var documents = new List<KnowledgeDocument>();
        foreach (var file in Directory.EnumerateFiles(knowledgePath, "*.md", SearchOption.TopDirectoryOnly).OrderBy(path => path))
        {
            var text = await File.ReadAllTextAsync(file, cancellationToken);
            var source = Path.GetFileName(file);
            var title = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .FirstOrDefault(line => line.StartsWith('#'))?
                .TrimStart('#', ' ')
                ?? Path.GetFileNameWithoutExtension(file);
            documents.Add(new KnowledgeDocument(source, title, text));
        }

        return documents;
    }
}
