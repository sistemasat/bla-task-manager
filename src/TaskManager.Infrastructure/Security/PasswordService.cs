using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TaskManager.Application.Auth;

namespace TaskManager.Infrastructure.Security;

public sealed class PasswordService : IPasswordService
{
    private readonly object _subject = new();
    private readonly PasswordHasher<object> _hasher = new(Options.Create(new PasswordHasherOptions { IterationCount = 210_000 }));
    private readonly string _dummyHash;

    public PasswordService() => _dummyHash = Hash(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));

    public string Hash(string password) => _hasher.HashPassword(_subject, password);

    public bool Verify(string password, string? hash)
    {
        // Unknown accounts still perform password verification.
        var result = _hasher.VerifyHashedPassword(_subject, hash ?? _dummyHash, password);
        return hash is not null && result != PasswordVerificationResult.Failed;
    }
}
