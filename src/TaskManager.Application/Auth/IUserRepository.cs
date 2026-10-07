using TaskManager.Domain;

namespace TaskManager.Application.Auth;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct);
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct);
    Task<bool> TryAddAsync(User user, CancellationToken ct);
}
