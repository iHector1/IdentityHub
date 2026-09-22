using IdentityHub.AuthService.Domain.Entities;

namespace IdentityHub.AuthService.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string Generate(UserCredential credential);
}