namespace IdentityHub.UserService.Application.Users;

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email
);