using TaskManager.Application.Auth;
using TaskManager.Domain;

namespace TaskManager.Infrastructure.Persistence;

public sealed class SqliteUserRepository(SqliteDatabase database) : IUserRepository
{
    private readonly SqliteDatabase _database = database;
    public Task<User?> FindByEmailAsync(string email, CancellationToken ct) => throw new NotImplementedException();
    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct) => throw new NotImplementedException();
    public Task<bool> TryAddAsync(User user, CancellationToken ct) => throw new NotImplementedException();
}
