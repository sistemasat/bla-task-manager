using System.Net.Mail;
using TaskManager.Domain;

namespace TaskManager.Application.Auth;

public sealed class AuthService(IUserRepository users, IPasswordService passwords, IAccessTokenIssuer tokens, TimeProvider clock)
{
    private readonly IUserRepository _users = users;
    private readonly IPasswordService _passwords = passwords;
    private readonly IAccessTokenIssuer _tokens = tokens;
    private readonly TimeProvider _clock = clock;

    public async Task<UserView> RegisterAsync(string name, string email, string password, CancellationToken ct)
    {
        var cleanName = name?.Trim();
        if (string.IsNullOrEmpty(cleanName) || cleanName.Length > 100)
            throw new ValidationException("name", "Name must contain between 1 and 100 characters.");
        var cleanEmail = NormalizeEmail(email);
        if (password is null || password.Length is < 12 or > 128)
            throw new ValidationException("password", "Password must contain between 12 and 128 characters.");
        var user = new User(Guid.NewGuid(), cleanName, cleanEmail, _passwords.Hash(password), _clock.GetUtcNow());
        if (!await _users.TryAddAsync(user, ct))
            throw new EmailAlreadyExistsException();
        return ToView(user);
    }

    public async Task<Session> LoginAsync(string email, string password, CancellationToken ct)
    {
        var cleanEmail = NormalizeEmail(email);
        if (string.IsNullOrEmpty(password) || password.Length > 128)
            throw new InvalidCredentialsException();
        var user = await _users.FindByEmailAsync(cleanEmail, ct);
        var valid = _passwords.Verify(password, user?.PasswordHash);
        if (user is null || !valid)
            throw new InvalidCredentialsException();
        return new(_tokens.Issue(user), ToView(user));
    }

    public async Task<UserView> GetUserAsync(Guid id, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(id, ct) ?? throw new NotFoundException();
        return ToView(user);
    }

    private static string NormalizeEmail(string email)
    {
        var value = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(value) || value.Length > 254 ||
            !MailAddress.TryCreate(value, out var parsed) || parsed.Address != value || !parsed.Host.Contains('.'))
            throw new ValidationException("email", "Enter a valid email address.");
        return value;
    }

    private static UserView ToView(User user) => new(user.Id, user.Name, user.Email);
}
