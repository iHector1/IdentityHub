namespace IdentityHub.AIService.Infrastructure.Options;

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";
    public string Url { get; set; } = "http://localhost:6333";
    public string CollectionName { get; set; } = "identityhub_knowledge";
}
