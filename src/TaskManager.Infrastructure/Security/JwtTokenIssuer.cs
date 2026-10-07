using TaskManager.Application.Auth;
using TaskManager.Domain;

namespace TaskManager.Infrastructure.Security;

public sealed class JwtTokenIssuer(JwtSettings settings, TimeProvider clock) : IAccessTokenIssuer
{
    private readonly JwtSettings _settings = settings;
    private readonly TimeProvider _clock = clock;
    public AccessToken Issue(User user) => throw new NotImplementedException();
}
