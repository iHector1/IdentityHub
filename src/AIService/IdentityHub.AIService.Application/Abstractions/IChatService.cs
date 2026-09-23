namespace IdentityHub.AIService.Application.Abstractions;

public interface IChatService
{
    Task<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
