using IdentityHub.AIService.Application.Abstractions;
using IdentityHub.AIService.Application.Models;
using IdentityHub.AIService.Application.Options;
using IdentityHub.AIService.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IdentityHub.AIService.Tests;

public sealed class UserSummaryServiceTests
{
    [Fact]
    public async Task SummarizeAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        var chat = new FakeChatService();
        var service = CreateService(new FakeUserServiceClient(null), chat);

        var result = await service.SummarizeAsync(Guid.NewGuid());

        Assert.Null(result);
        Assert.Null(chat.UserPrompt);
    }

    [Fact]
    public async Task SummarizeAsync_ShouldUseOnlySafeUserDataAndReturnUsage()
    {
        var userId = Guid.NewGuid();
        var chat = new FakeChatService(new ChatResult("The user is active.", 50, 25, 75));
        var service = CreateService(
            new FakeUserServiceClient(new AiUserInfo(userId, "user@example.com", true)),
            chat);

        var result = await service.SummarizeAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(50, result.Usage.InputTokens);
        Assert.Equal(25, result.Usage.OutputTokens);
        Assert.Equal(75, result.Usage.TotalTokens);
        Assert.Contains($"UserId: {userId}", chat.UserPrompt);
        Assert.Contains("Email: user@example.com", chat.UserPrompt);
        Assert.Contains("IsActive: True", chat.UserPrompt);
        Assert.DoesNotContain("Password", chat.UserPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Token", chat.UserPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SummarizeAsync_WithConfiguredPrices_ShouldEstimateCost()
    {
        var chat = new FakeChatService(new ChatResult("Summary", 100, 50, 150));
        var service = CreateService(
            new FakeUserServiceClient(new AiUserInfo(Guid.NewGuid(), "user@example.com", false)),
            chat,
            new OpenAiCostOptions
            {
                InputCostPerMillionTokens = 2m,
                OutputCostPerMillionTokens = 4m
            });

        var result = await service.SummarizeAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Equal(0.0004m, result.Usage.EstimatedCost);
    }

    private static UserSummaryService CreateService(
        IUserServiceClient client,
        FakeChatService chat,
        OpenAiCostOptions? costOptions = null) =>
        new(
            client,
            chat,
            NullLogger<UserSummaryService>.Instance,
            Options.Create(costOptions ?? new OpenAiCostOptions()));

    private sealed class FakeUserServiceClient(AiUserInfo? user) : IUserServiceClient
    {
        public Task<AiUserInfo?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(user);
    }

    private sealed class FakeChatService(ChatResult? result = null) : IChatService
    {
        public string? UserPrompt { get; private set; }

        public Task<ChatResult> GenerateAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default)
        {
            UserPrompt = userPrompt;
            return Task.FromResult(result ?? new ChatResult("Summary", 10, 5, 15));
        }
    }
}
