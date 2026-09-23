using System.Diagnostics;
using IdentityHub.AIService.Application.Abstractions;
using IdentityHub.AIService.Application.Models;
using IdentityHub.AIService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityHub.AIService.Application.Services;

public sealed class UserSummaryService(
    IUserServiceClient userServiceClient,
    IChatService chatService,
    ILogger<UserSummaryService> logger,
    IOptions<OpenAiCostOptions> costOptions)
{
    private const string SystemPrompt = "You summarize IdentityHub user information using only the data provided.";

    public async Task<AiUserSummary?> SummarizeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var user = await userServiceClient.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return null;

        var prompt = $"UserId: {user.Id}\nEmail: {user.Email}\nIsActive: {user.IsActive}";
        var result = await chatService.GenerateAsync(SystemPrompt, prompt, cancellationToken);
        stopwatch.Stop();

        var estimatedCost = EstimateCost(result);
        var usage = new AiUsage(
            result.InputTokens,
            result.OutputTokens,
            result.TotalTokens,
            estimatedCost);

        logger.LogInformation(
            "AI user summary completed for {UserId} in {LatencyMs} ms with {InputTokens} input tokens, {OutputTokens} output tokens, {TotalTokens} total tokens and {EstimatedCost} estimated cost",
            user.Id,
            stopwatch.ElapsedMilliseconds,
            usage.InputTokens,
            usage.OutputTokens,
            usage.TotalTokens,
            usage.EstimatedCost);

        return new AiUserSummary(user.Id, result.Text, stopwatch.ElapsedMilliseconds, usage);
    }

    private decimal? EstimateCost(ChatResult result)
    {
        var pricing = costOptions.Value;
        if (pricing.InputCostPerMillionTokens is not { } inputPrice ||
            pricing.OutputCostPerMillionTokens is not { } outputPrice ||
            result.InputTokens is not { } inputTokens ||
            result.OutputTokens is not { } outputTokens)
        {
            return null;
        }

        return inputTokens / 1_000_000m * inputPrice + outputTokens / 1_000_000m * outputPrice;
    }
}
