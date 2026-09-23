namespace IdentityHub.AIService.Infrastructure.Options;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public string Model { get; set; } = "gpt-5.6-luna";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}
