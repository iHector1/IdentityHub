using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityHub.AuthService.Domain.Entities;
using IdentityHub.AuthService.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IdentityHub.AuthService.Tests;

public sealed class JwtTokenGeneratorTests
{
    private const string Key = "test-signing-key-that-is-long-enough-for-hs256";
    private const string Issuer = "IdentityHub.AuthService.Tests";
    private const string Audience = "IdentityHub.Tests";

    [Fact]
    public void Generate_ShouldCreateSignedTokenWithExpectedClaims()
    {
        var userId = Guid.NewGuid();
        var credential = new UserCredential(userId, "user@example.com", "hash");
        var generator = new JwtTokenGenerator(CreateConfiguration());

        var tokenValue = generator.Generate(credential);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenValue);

        Assert.Equal(userId.ToString(), GetClaim(token, JwtRegisteredClaimNames.Sub));
        Assert.Equal("user@example.com", GetClaim(token, JwtRegisteredClaimNames.Email));
        Assert.False(string.IsNullOrWhiteSpace(GetClaim(token, JwtRegisteredClaimNames.Jti)));
        Assert.Equal(Issuer, token.Issuer);
        Assert.Contains(Audience, token.Audiences);
        Assert.True(token.ValidTo > DateTime.UtcNow);

        var principal = Validate(tokenValue, Key, Issuer, Audience);
        Assert.Equal(userId.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public void Validate_WithWrongSigningKey_ShouldFail()
    {
        var token = CreateToken();

        Assert.ThrowsAny<SecurityTokenException>(() =>
            Validate(token, "different-signing-key-that-is-long-enough-for-hs256", Issuer, Audience));
    }

    [Fact]
    public void Validate_WithExpiredToken_ShouldFail()
    {
        var token = CreateToken(expires: DateTime.UtcNow.AddMinutes(-1));

        Assert.ThrowsAny<SecurityTokenException>(() => Validate(token, Key, Issuer, Audience));
    }

    [Fact]
    public void Validate_WithWrongIssuer_ShouldFail()
    {
        var token = CreateToken();

        Assert.ThrowsAny<SecurityTokenException>(() => Validate(token, Key, "wrong-issuer", Audience));
    }

    [Fact]
    public void Validate_WithWrongAudience_ShouldFail()
    {
        var token = CreateToken();

        Assert.ThrowsAny<SecurityTokenException>(() => Validate(token, Key, Issuer, "wrong-audience"));
    }

    [Fact]
    public void Generate_WhenKeyIsMissing_ShouldFailClearly()
    {
        var configuration = new ConfigurationBuilder().Build();
        var generator = new JwtTokenGenerator(configuration);
        var credential = new UserCredential(Guid.NewGuid(), "user@example.com", "hash");

        var exception = Assert.Throws<InvalidOperationException>(() => generator.Generate(credential));

        Assert.Contains("JWT key", exception.Message);
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = Key,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience
            })
            .Build();

    private static string CreateToken(DateTime? expires = null)
    {
        var credential = new UserCredential(Guid.NewGuid(), "user@example.com", "hash");
        var generator = new JwtTokenGenerator(CreateConfiguration());
        var token = generator.Generate(credential);

        if (expires is null)
            return token;

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, credential.UserId.ToString()) }),
            Issuer = Issuer,
            Audience = Audience,
            NotBefore = expires.Value.AddMinutes(-2),
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
                SecurityAlgorithms.HmacSha256)
        };
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityTokenHandler().CreateToken(descriptor));
    }

    private static ClaimsPrincipal Validate(string token, string key, string issuer, string audience)
    {
        return new JwtSecurityTokenHandler().ValidateToken(
            token,
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            },
            out _);
    }

    private static string GetClaim(JwtSecurityToken token, string type) =>
        token.Claims.Single(claim => claim.Type == type).Value;
}
