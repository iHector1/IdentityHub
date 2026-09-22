using IdentityHub.AuthService.Application.Abstractions;
using IdentityHub.AuthService.Domain.Entities;
using IdentityHub.AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.AuthService.Infrastructure.Repositories;

public class EfCredentialRepository : ICredentialRepository
{
    private readonly AuthDbContext _dbContext;

    public EfCredentialRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserCredential?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _dbContext.Credentials
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail);
    }

    public async Task AddAsync(UserCredential credential)
    {
        await _dbContext.Credentials.AddAsync(credential);
        await _dbContext.SaveChangesAsync();
    }
}