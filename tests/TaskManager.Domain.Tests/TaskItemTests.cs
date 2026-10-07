using TaskManager.Domain;

namespace TaskManager.Domain.Tests;

public class TaskItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_sets_owner_initial_status_and_timestamps()
    {
        var owner = Guid.NewGuid();
        var task = TaskItem.Create(owner, "  Prepare interview  ", "  Explain choices  ", null, Now);

        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal(owner, task.UserId);
        Assert.Equal("Prepare interview", task.Title);
        Assert.Equal("Explain choices", task.Description);
        Assert.Equal(TaskItemStatus.Pending, task.Status);
        Assert.Equal(Now, task.CreatedAt);
        Assert.Equal(Now, task.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_blank_title(string title)
    {
        var error = Assert.Throws<ValidationException>(() =>
            TaskItem.Create(Guid.NewGuid(), title, null, null, Now));
        Assert.Equal("title", error.Field);
    }

    [Fact]
    public void Create_rejects_title_over_200_characters()
    {
        Assert.Throws<ValidationException>(() =>
            TaskItem.Create(Guid.NewGuid(), new string('x', 201), null, null, Now));
    }

    [Fact]
    public void Create_accepts_maximum_lengths_and_past_due_date()
    {
        var due = new DateOnly(2026, 10, 1);
        var task = TaskItem.Create(Guid.NewGuid(), new string('x', 200), new string('x', 4000), due, Now);
        Assert.Equal(due, task.DueDate);
    }

    [Fact]
    public void Create_rejects_description_over_4000_characters()
    {
        Assert.Throws<ValidationException>(() =>
            TaskItem.Create(Guid.NewGuid(), "Task", new string('x', 4001), null, Now));
    }

    [Fact]
    public void Create_rejects_missing_owner()
    {
        Assert.Throws<ValidationException>(() => TaskItem.Create(Guid.Empty, "Task", null, null, Now));
    }

    [Fact]
    public void Update_preserves_identity_owner_and_creation_time()
    {
        var original = TaskItem.Create(Guid.NewGuid(), "Old", null, null, Now);
        var later = Now.AddMinutes(5);
        var updated = original.Update("  New  ", "  ", TaskItemStatus.Completed, new DateOnly(2026, 10, 9), later);

        Assert.Equal(original.Id, updated.Id);
        Assert.Equal(original.UserId, updated.UserId);
        Assert.Equal(Now, updated.CreatedAt);
        Assert.Equal(later, updated.UpdatedAt);
        Assert.Equal(TaskItemStatus.Completed, updated.Status);
        Assert.Equal("New", updated.Title);
        Assert.Null(updated.Description);
        Assert.Equal("Old", original.Title);
    }

    [Fact]
    public void Update_rejects_unknown_status()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Task", null, null, Now);
        var error = Assert.Throws<ValidationException>(() =>
            task.Update("Task", null, (TaskItemStatus)99, null, Now));
        Assert.Equal("status", error.Field);
    }
}
