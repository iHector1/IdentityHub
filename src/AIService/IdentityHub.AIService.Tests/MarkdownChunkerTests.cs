using IdentityHub.AIService.Application.Models;
using IdentityHub.AIService.Application.Services;

namespace IdentityHub.AIService.Tests;

public sealed class MarkdownChunkerTests
{
    [Fact]
    public void Chunk_WithEmptyText_ShouldReturnNoChunks()
    {
        var chunker = new MarkdownChunker();

        var result = chunker.Chunk(new KnowledgeDocument("empty.md", "Empty", "  "));

        Assert.Empty(result);
    }

    [Fact]
    public void Chunk_ShouldSplitTextWithOverlapAndStableIds()
    {
        var document = new KnowledgeDocument("guide.md", "Guide", string.Join(' ', Enumerable.Repeat("word", 20)));
        var chunker = new MarkdownChunker();

        var result = chunker.Chunk(document, chunkSize: 30, overlap: 5).ToArray();

        Assert.True(result.Length > 1);
        Assert.Equal(result.Select(chunk => chunk.Id).Distinct().Count(), result.Length);
        Assert.Equal(result[0].Id, chunker.Chunk(document, chunkSize: 30, overlap: 5).First().Id);
    }

    [Fact]
    public void Chunk_WithInvalidOptions_ShouldThrow()
    {
        var chunker = new MarkdownChunker();
        var document = new KnowledgeDocument("guide.md", "Guide", "content");

        Assert.Throws<ArgumentOutOfRangeException>(() => chunker.Chunk(document, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => chunker.Chunk(document, 10, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => chunker.Chunk(document, 10, 10));
    }
}
