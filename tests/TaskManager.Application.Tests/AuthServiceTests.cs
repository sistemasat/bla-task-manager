using TaskManager.Application.Auth;
using TaskManager.Domain;

namespace TaskManager.Application.Tests;

public class AuthServiceTests
{
    private readonly FakeUsers _users = new();
    private readonly AuthService _service;
    public AuthServiceTests() => _service = new(_users, new FakePasswords(), new FakeTokens(), TimeProvider.System);

    [Fact]
    public async Task Registration_normalizes_email_and_persists_hash_instead_of_password()
    {
        var view = await _service.RegisterAsync("  Alex  ", "  Alex@Example.com  ", "StrongPassword!", default);
        Assert.Equal("Alex", view.Name);
        Assert.Equal("alex@example.com", view.Email);
        Assert.NotNull(_users.Stored);
        Assert.Equal("hashed:StrongPassword!", _users.Stored.PasswordHash);
        Assert.NotEqual(Guid.Empty, view.Id);
    }

    [Theory]
    [InlineData("", "alex@example.com", "StrongPassword!", "name")]
    [InlineData("Alex", "not-an-email", "StrongPassword!", "email")]
    [InlineData("Alex", "Alex <alex@example.com>", "StrongPassword!", "email")]
    [InlineData("Alex", "alex@example.com", "short", "password")]
    public async Task Registration_rejects_invalid_input_before_persistence(string name, string email, string password, string field)
    {
        var error = await Assert.ThrowsAsync<ValidationException>(() => _service.RegisterAsync(name, email, password, default));
        Assert.Equal(field, error.Field);
        Assert.Null(_users.Stored);
    }

    [Fact]
    public async Task Registration_reports_duplicate_email()
    {
        await _service.RegisterAsync("Alex", "alex@example.com", "StrongPassword!", default);
        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() =>
            _service.RegisterAsync("Other", "ALEX@EXAMPLE.COM", "OtherPassword!", default));
    }

    [Fact]
    public async Task Login_returns_token_and_public_user_details()
    {
        var user = await _service.RegisterAsync("Alex", "alex@example.com", "StrongPassword!", default);
        var session = await _service.LoginAsync("ALEX@EXAMPLE.COM", "StrongPassword!", default);
        Assert.Equal(user, session.User);
        Assert.Equal("test-token", session.AccessToken.Token);
    }

    [Theory]
    [InlineData("alex@example.com", "IncorrectPassword!")]
    [InlineData("unknown@example.com", "StrongPassword!")]
    public async Task Login_returns_same_error_for_unknown_email_and_wrong_password(string email, string password)
    {
        await _service.RegisterAsync("Alex", "alex@example.com", "StrongPassword!", default);
        var error = await Assert.ThrowsAsync<InvalidCredentialsException>(() => _service.LoginAsync(email, password, default));
        Assert.Equal("Invalid email or password.", error.Message);
    }

    private sealed class FakeUsers : IUserRepository
    {
        public User? Stored { get; private set; }
        public Task<User?> FindByEmailAsync(string email, CancellationToken ct) => Task.FromResult(Stored?.Email == email ? Stored : null);
        public Task<User?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Stored?.Id == id ? Stored : null);
        public Task<bool> TryAddAsync(User user, CancellationToken ct)
        {
            if (Stored?.Email == user.Email) return Task.FromResult(false);
            Stored = user;
            return Task.FromResult(true);
        }
    }

    private sealed class FakePasswords : IPasswordService
    {
        public string Hash(string password) => "hashed:" + password;
        public bool Verify(string password, string? hash) => hash == Hash(password);
    }

    private sealed class FakeTokens : IAccessTokenIssuer
    {
        public AccessToken Issue(User user) => new("test-token", DateTimeOffset.UtcNow.AddMinutes(30));
    }
}
