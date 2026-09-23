namespace IdentityHub.AIService.Application.Options;

public sealed class OpenAiCostOptions
{
    public const string SectionName = "OpenAI";

    public decimal? InputCostPerMillionTokens { get; set; }
    public decimal? OutputCostPerMillionTokens { get; set; }
}
