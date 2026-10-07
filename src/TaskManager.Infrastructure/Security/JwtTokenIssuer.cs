using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Application.Auth;
using TaskManager.Domain;

namespace TaskManager.Infrastructure.Security;

public sealed class JwtTokenIssuer(JwtSettings settings, TimeProvider clock) : IAccessTokenIssuer
{
    private readonly JwtSettings _settings = settings;
    private readonly TimeProvider _clock = clock;
    public AccessToken Issue(User user)
    {
        var now = _clock.GetUtcNow();
        var expires = now.AddMinutes(_settings.LifetimeMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_settings.Issuer, _settings.Audience,
            [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())],
            notBefore: now.UtcDateTime, expires: expires.UtcDateTime, signingCredentials: credentials);
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
