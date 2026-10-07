using TaskManager.Application.Tasks;
using TaskManager.Domain;

namespace TaskManager.Infrastructure.Persistence;

public sealed class SqliteTaskRepository(SqliteDatabase database) : ITaskRepository
{
    private readonly SqliteDatabase _database = database;
    public Task<IReadOnlyList<TaskItem>> ListAsync(Guid userId, int skip, int take, CancellationToken ct) => throw new NotImplementedException();
    public Task<TaskItem?> FindAsync(Guid id, Guid userId, CancellationToken ct) => throw new NotImplementedException();
    public Task AddAsync(TaskItem task, CancellationToken ct) => throw new NotImplementedException();
    public Task<bool> UpdateAsync(TaskItem task, CancellationToken ct) => throw new NotImplementedException();
    public Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct) => throw new NotImplementedException();
}
