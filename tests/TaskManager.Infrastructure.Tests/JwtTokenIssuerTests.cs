using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Domain;
using TaskManager.Infrastructure.Security;

namespace TaskManager.Infrastructure.Tests;

public class JwtTokenIssuerTests
{
    private readonly JwtSettings _settings = new("test-only-signing-key-with-at-least-32-bytes");

    [Fact]
    public void Token_contains_owner_and_expiry_and_has_valid_signature()
    {
        var user = new User(Guid.NewGuid(), "Alex", "alex@example.com", "private-hash", DateTimeOffset.UtcNow);
        var before = DateTimeOffset.UtcNow;
        var access = new JwtTokenIssuer(_settings, TimeProvider.System).Issue(user);
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(access.Token, Validation(_settings.SigningKey), out _);
        Assert.Equal(user.Id.ToString(), principal.FindFirst("sub")?.Value);
        Assert.DoesNotContain("password", access.Token, StringComparison.OrdinalIgnoreCase);
        Assert.InRange(access.ExpiresAt, before.AddMinutes(30), DateTimeOffset.UtcNow.AddMinutes(30));
    }

    [Fact]
    public void Wrong_signing_key_cannot_validate_token()
    {
        var user = new User(Guid.NewGuid(), "Alex", "alex@example.com", "hash", DateTimeOffset.UtcNow);
        var access = new JwtTokenIssuer(_settings, TimeProvider.System).Issue(user);
        Assert.ThrowsAny<SecurityTokenException>(() => new JwtSecurityTokenHandler().ValidateToken(access.Token,
            Validation("different-test-signing-key-with-at-least-32-bytes"), out _));
    }

    private TokenValidationParameters Validation(string key) => new()
    {
        ValidateIssuer = true, ValidIssuer = _settings.Issuer,
        ValidateAudience = true, ValidAudience = _settings.Audience,
        ValidateLifetime = true, RequireExpirationTime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ClockSkew = TimeSpan.Zero
    };
}
