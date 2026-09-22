namespace IdentityHub.UserService.Application.Users;

public record UpdateUserRequest(
    string FirstName,
    string LastName,
    string Email
);