using TaskManager.Domain;

namespace TaskManager.Application.Tasks;

public sealed class TaskService(ITaskRepository repository, TimeProvider clock)
{
    public Task<IReadOnlyList<TaskItem>> ListAsync(Guid userId, int skip, int take, CancellationToken ct)
    {
        if (skip < 0 || take is < 1 or > 200)
            throw new ValidationException("pagination", "Skip must be non-negative and take must be between 1 and 200.");
        return _repository.ListAsync(userId, skip, take, ct);
    }

    public async Task<TaskItem> GetAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var task = await _repository.FindAsync(id, userId, ct);
        if (task is null || task.UserId != userId)
            throw new NotFoundException();
        return task;
    }

    public async Task<TaskItem> CreateAsync(Guid userId, string title, string? description, DateOnly? dueDate, CancellationToken ct)
    {
        var task = TaskItem.Create(userId, title, description, dueDate, _clock.GetUtcNow());
        await _repository.AddAsync(task, ct);
        return task;
    }

    public async Task<TaskItem> UpdateAsync(Guid id, Guid userId, string title, string? description, TaskItemStatus status, DateOnly? dueDate, CancellationToken ct)
    {
        var current = await GetAsync(id, userId, ct);
        var updated = current.Update(title, description, status, dueDate, _clock.GetUtcNow());
        if (!await _repository.UpdateAsync(updated, ct))
            throw new NotFoundException();
        return updated;
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct)
    {
        if (!await _repository.DeleteAsync(id, userId, ct))
            throw new NotFoundException();
    }

    private readonly ITaskRepository _repository = repository;
    private readonly TimeProvider _clock = clock;
}
