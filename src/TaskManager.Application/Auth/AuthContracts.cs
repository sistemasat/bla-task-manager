using TaskManager.Domain;

namespace TaskManager.Application.Auth;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string? hash);
}

public interface IAccessTokenIssuer
{
    AccessToken Issue(User user);
}

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
public sealed record UserView(Guid Id, string Name, string Email);
public sealed record Session(AccessToken AccessToken, UserView User);
public sealed class EmailAlreadyExistsException() : Exception("An account with this email already exists.");
public sealed class InvalidCredentialsException() : Exception("Invalid email or password.");
