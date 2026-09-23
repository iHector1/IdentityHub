using System.Net;
using System.Net.Http.Json;
using IdentityHub.RoleService.Application.Abstractions;

namespace IdentityHub.RoleService.Infrastructure.Clients;

public sealed class UserServiceClient(HttpClient httpClient) : IUserServiceClient
{
    public async Task<UserServiceUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"/api/users/{userId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserServiceUser>(cancellationToken: cancellationToken);
    }
}
