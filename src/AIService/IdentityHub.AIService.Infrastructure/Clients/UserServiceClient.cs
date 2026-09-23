using System.Net;
using System.Net.Http.Json;
using IdentityHub.AIService.Application.Abstractions;
using IdentityHub.AIService.Application.Models;
using Microsoft.Extensions.Configuration;

namespace IdentityHub.AIService.Infrastructure.Clients;

public sealed class UserServiceClient(
    HttpClient httpClient,
    IConfiguration configuration) : IUserServiceClient
{
    private readonly string internalApiKey = configuration["InternalServices:ApiKey"]
        ?? throw new InvalidOperationException("Internal services API key is not configured.");

    public async Task<AiUserInfo?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/internal/users/{userId}");
        request.Headers.Add("X-Internal-Api-Key", internalApiKey);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AiUserInfo>(cancellationToken: cancellationToken);
    }
}
