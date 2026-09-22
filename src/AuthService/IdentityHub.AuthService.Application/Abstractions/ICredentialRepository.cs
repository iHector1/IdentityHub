using IdentityHub.AuthService.Domain.Entities;

namespace IdentityHub.AuthService.Application.Abstractions;

public interface ICredentialRepository
{
    Task<UserCredential?> GetByEmailAsync(string email);

    Task AddAsync(UserCredential credential);
}