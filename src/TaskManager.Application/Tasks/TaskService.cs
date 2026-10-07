using TaskManager.Domain;

namespace TaskManager.Application.Tasks;

public sealed class TaskService(ITaskRepository repository, TimeProvider clock)
{
    public Task<IReadOnlyList<TaskItem>> ListAsync(Guid userId, int skip, int take, CancellationToken ct)
        => throw new NotImplementedException();
    public Task<TaskItem> GetAsync(Guid id, Guid userId, CancellationToken ct)
        => throw new NotImplementedException();
    public Task<TaskItem> CreateAsync(Guid userId, string title, string? description, DateOnly? dueDate, CancellationToken ct)
        => throw new NotImplementedException();
    public Task<TaskItem> UpdateAsync(Guid id, Guid userId, string title, string? description, TaskItemStatus status, DateOnly? dueDate, CancellationToken ct)
        => throw new NotImplementedException();
    public Task DeleteAsync(Guid id, Guid userId, CancellationToken ct)
        => throw new NotImplementedException();

    private readonly ITaskRepository _repository = repository;
    private readonly TimeProvider _clock = clock;
}
