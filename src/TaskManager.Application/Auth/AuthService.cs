namespace TaskManager.Application.Auth;

public sealed class AuthService(IUserRepository users, IPasswordService passwords, IAccessTokenIssuer tokens, TimeProvider clock)
{
    private readonly IUserRepository _users = users;
    private readonly IPasswordService _passwords = passwords;
    private readonly IAccessTokenIssuer _tokens = tokens;
    private readonly TimeProvider _clock = clock;

    public Task<UserView> RegisterAsync(string name, string email, string password, CancellationToken ct)
        => throw new NotImplementedException();
    public Task<Session> LoginAsync(string email, string password, CancellationToken ct)
        => throw new NotImplementedException();
    public Task<UserView> GetUserAsync(Guid id, CancellationToken ct)
        => throw new NotImplementedException();
}
