using TaskManager.Domain;

namespace TaskManager.Application.Tasks;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> ListAsync(Guid userId, int skip, int take, CancellationToken ct);
    Task<TaskItem?> FindAsync(Guid id, Guid userId, CancellationToken ct);
    Task AddAsync(TaskItem task, CancellationToken ct);
    Task<bool> UpdateAsync(TaskItem task, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct);
}
