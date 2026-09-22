using System.Net;
using System.Net.Http.Json;
using IdentityHub.AuthService.Application.Abstractions;

namespace IdentityHub.AuthService.Infrastructure.Clients;

public class UserServiceClient : IUserServiceClient
{
    private readonly HttpClient _httpClient;

    public UserServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserInfo?> GetByIdAsync(Guid userId)
    {
        using var response = await _httpClient.GetAsync($"/api/users/{userId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserInfo>();
    }
}
