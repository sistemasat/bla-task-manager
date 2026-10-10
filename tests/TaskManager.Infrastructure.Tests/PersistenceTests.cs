using Microsoft.Data.Sqlite;
using TaskManager.Domain;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Tests;

public sealed class PersistenceTests : IAsyncLifetime
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"task-manager-{Guid.NewGuid()}.db");
    private readonly SqliteDatabase _database;
    private readonly SqliteTaskRepository _tasks;
    private readonly SqliteUserRepository _users;
    private readonly User _owner = new(Guid.NewGuid(), "Alex", "alex@example.com", "hash", DateTimeOffset.UtcNow);
    public PersistenceTests()
    {
        _database = new($"Data Source={_file};Foreign Keys=True;Pooling=False");
        _tasks = new(_database);
        _users = new(_database);
    }
    public Task InitializeAsync() => _database.InitializeAsync();
    public Task DisposeAsync() { File.Delete(_file); return Task.CompletedTask; }

    [Fact]
    public async Task Users_round_trip_and_duplicate_email_is_rejected()
    {
        Assert.True(await _users.TryAddAsync(_owner, default));
        Assert.Equal(_owner, await _users.FindByEmailAsync(_owner.Email, default));
        Assert.Equal(_owner, await _users.FindByIdAsync(_owner.Id, default));
        Assert.False(await _users.TryAddAsync(_owner with { Id = Guid.NewGuid() }, default));
    }

    [Fact]
    public async Task Task_round_trip_preserves_text_dates_and_status()
    {
        await _users.TryAddAsync(_owner, default);
        var task = TaskItem.Create(_owner.Id, "O'Brien's task", "'); DROP TABLE tasks; --", new DateOnly(2026, 10, 9), DateTimeOffset.UtcNow);
        await _tasks.AddAsync(task, default);
        Assert.Equal(task, await _tasks.FindAsync(task.Id, _owner.Id, default));
        var updated = task.Update("Edited", null, TaskItemStatus.Completed, null, task.CreatedAt.AddHours(1));
        Assert.True(await _tasks.UpdateAsync(updated, default));
        Assert.Equal(updated, await _tasks.FindAsync(task.Id, _owner.Id, default));
        Assert.True(await _tasks.DeleteAsync(task.Id, _owner.Id, default));
        Assert.Null(await _tasks.FindAsync(task.Id, _owner.Id, default));
    }

    [Fact]
    public async Task Foreign_user_cannot_read_update_or_delete_task()
    {
        await _users.TryAddAsync(_owner, default);
        var task = TaskItem.Create(_owner.Id, "Private", null, null, DateTimeOffset.UtcNow);
        await _tasks.AddAsync(task, default);
        var stranger = Guid.NewGuid();
        Assert.Null(await _tasks.FindAsync(task.Id, stranger, default));
        Assert.Empty(await _tasks.ListAsync(stranger, 0, 100, default));
        var foreignTask = TaskItem.Restore(task.Id, stranger, "Changed", task.Description,
            task.Status, task.DueDate, task.CreatedAt, task.UpdatedAt);
        Assert.False(await _tasks.UpdateAsync(foreignTask, default));
        Assert.False(await _tasks.DeleteAsync(task.Id, stranger, default));
        Assert.Equal(task, await _tasks.FindAsync(task.Id, _owner.Id, default));
    }

    [Fact]
    public async Task Pagination_is_stable_and_scoped_to_owner()
    {
        await _users.TryAddAsync(_owner, default);
        for (var i = 0; i < 3; i++)
            await _tasks.AddAsync(TaskItem.Create(_owner.Id, $"Task {i}", null, null, DateTimeOffset.UtcNow.AddMinutes(i)), default);
        var first = await _tasks.ListAsync(_owner.Id, 0, 2, default);
        var second = await _tasks.ListAsync(_owner.Id, 2, 2, default);
        Assert.Equal(2, first.Count);
        Assert.Single(second);
        Assert.Equal("Task 2", first[0].Title);
        Assert.DoesNotContain(second[0].Id, first.Select(t => t.Id));
    }

    [Fact]
    public async Task Task_requires_existing_user()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Orphan", null, null, DateTimeOffset.UtcNow);
        var error = await Assert.ThrowsAsync<SqliteException>(() => _tasks.AddAsync(task, default));
        Assert.Equal(19, error.SqliteErrorCode);
    }

    [Fact]
    public async Task Data_survives_a_new_database_instance()
    {
        await _users.TryAddAsync(_owner, default);
        var task = TaskItem.Create(_owner.Id, "Persisted", null, null, DateTimeOffset.UtcNow);
        await _tasks.AddAsync(task, default);
        var reopened = new SqliteDatabase($"Data Source={_file};Foreign Keys=True;Pooling=False");
        await reopened.InitializeAsync();
        Assert.Equal(task, await new SqliteTaskRepository(reopened).FindAsync(task.Id, _owner.Id, default));
    }
}
