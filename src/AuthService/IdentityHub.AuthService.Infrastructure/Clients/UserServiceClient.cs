using System.Net;
using System.Net.Http.Json;
using IdentityHub.AuthService.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace IdentityHub.AuthService.Infrastructure.Clients;

public class UserServiceClient : IUserServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly string _internalApiKey;

    public UserServiceClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _internalApiKey = configuration["InternalServices:ApiKey"]
            ?? throw new InvalidOperationException("Internal services API key is not configured.");
    }

    public async Task<UserInfo?> GetByIdAsync(Guid userId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/internal/users/{userId}");
        request.Headers.Add("X-Internal-Api-Key", _internalApiKey);
        using var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserInfo>();
    }
}
