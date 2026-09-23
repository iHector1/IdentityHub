using IdentityHub.AIService.Application.Models;

namespace IdentityHub.AIService.Application.Abstractions;

public interface IUserServiceClient
{
    Task<AiUserInfo?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
