namespace IdentityHub.AuthService.Domain.Entities;

public class UserCredential
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public DateTime CreatedAt { get; private set; }
    private UserCredential()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    public UserCredential(
        Guid userId,
        string email,
        string passwordHash)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
    }
}