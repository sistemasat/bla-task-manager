using TaskManager.Application.Auth;

namespace TaskManager.Infrastructure.Security;

public sealed class PasswordService : IPasswordService
{
    public string Hash(string password) => throw new NotImplementedException();
    public bool Verify(string password, string? hash) => throw new NotImplementedException();
}
