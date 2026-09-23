using IdentityHub.UserService.Domain.Entities;
using IdentityHub.UserService.Application.Abstractions;
using IdentityHub.UserService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.UserService.Infrastructure.Repositories;

public class EfUserRepository : IUserRepository
{
    private readonly UserDbContext _dbContext;

    public EfUserRepository(UserDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<User>> GetAllAsync()
    {
        return await _dbContext.Users.AsNoTracking().ToListAsync();
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
    }

    public async Task AddAsync(User user)
    {
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
    }

}
