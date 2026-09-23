using System.Net;
using System.Net.Http.Json;
using IdentityHub.RoleService.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace IdentityHub.RoleService.Infrastructure.Clients;

public sealed class UserServiceClient(HttpClient httpClient, IConfiguration configuration) : IUserServiceClient
{
    private readonly string _internalApiKey = configuration["InternalServices:ApiKey"]
        ?? throw new InvalidOperationException("Internal services API key is not configured.");

    public async Task<UserServiceUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/internal/users/{userId}");
        request.Headers.Add("X-Internal-Api-Key", _internalApiKey);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserServiceUser>(cancellationToken: cancellationToken);
    }
}
