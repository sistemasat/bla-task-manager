using TaskManager.Application.Tasks;
using TaskManager.Domain;

namespace TaskManager.Application.Tests;

public class TaskServiceTests
{
    private readonly Guid _owner = Guid.NewGuid();
    private readonly FakeTaskRepository _repository = new();
    private readonly TaskService _service;
    public TaskServiceTests() => _service = new(_repository, TimeProvider.System);

    [Fact]
    public async Task Create_persists_validated_task_with_authenticated_owner()
    {
        var task = await _service.CreateAsync(_owner, "  Task  ", null, null, default);
        Assert.Same(task, _repository.Stored);
        Assert.Equal(_owner, task.UserId);
        Assert.Equal("Task", task.Title);
    }

    [Fact]
    public async Task Invalid_create_does_not_write_to_repository()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(_owner, " ", null, null, default));
        Assert.Null(_repository.Stored);
    }

    [Fact]
    public async Task Get_rejects_foreign_task_even_if_repository_returns_it()
    {
        _repository.Stored = TaskItem.Create(Guid.NewGuid(), "Private", null, null, DateTimeOffset.UtcNow);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(_repository.Stored.Id, _owner, default));
        Assert.Equal(_owner, _repository.RequestedOwner);
    }

    [Fact]
    public async Task Update_cannot_change_foreign_task()
    {
        _repository.Stored = TaskItem.Create(Guid.NewGuid(), "Private", null, null, DateTimeOffset.UtcNow);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(_repository.Stored.Id, _owner,
            "Changed", null, TaskItemStatus.Completed, null, default));
        Assert.Equal("Private", _repository.Stored.Title);
        Assert.False(_repository.Updated);
    }

    [Fact]
    public async Task Update_persists_new_state_without_changing_owner()
    {
        _repository.Stored = TaskItem.Create(_owner, "Old", null, null, DateTimeOffset.UtcNow);
        var changed = await _service.UpdateAsync(_repository.Stored.Id, _owner, "New", null,
            TaskItemStatus.Completed, new DateOnly(2026, 10, 9), default);
        Assert.True(_repository.Updated);
        Assert.Equal(_owner, changed.UserId);
        Assert.Equal(TaskItemStatus.Completed, changed.Status);
    }

    [Fact]
    public async Task Delete_passes_authenticated_owner_and_reports_missing_task()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(Guid.NewGuid(), _owner, default));
        Assert.Equal(_owner, _repository.RequestedOwner);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, 0)]
    [InlineData(0, 201)]
    public async Task List_rejects_invalid_pagination(int skip, int take)
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.ListAsync(_owner, skip, take, default));
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        public TaskItem? Stored { get; set; }
        public Guid RequestedOwner { get; private set; }
        public bool Updated { get; private set; }
        public Task<IReadOnlyList<TaskItem>> ListAsync(Guid userId, int skip, int take, CancellationToken ct)
        {
            RequestedOwner = userId;
            return Task.FromResult<IReadOnlyList<TaskItem>>([]);
        }
        public Task<TaskItem?> FindAsync(Guid id, Guid userId, CancellationToken ct)
        {
            RequestedOwner = userId;
            return Task.FromResult(Stored);
        }
        public Task AddAsync(TaskItem task, CancellationToken ct) { Stored = task; return Task.CompletedTask; }
        public Task<bool> UpdateAsync(TaskItem task, CancellationToken ct)
        {
            Updated = true; Stored = task; return Task.FromResult(true);
        }
        public Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct)
        {
            RequestedOwner = userId;
            return Task.FromResult(false);
        }
    }
}
