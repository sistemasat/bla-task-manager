namespace TaskManager.Domain;

public sealed record TaskItem(
    Guid Id, Guid UserId, string Title, string? Description, TaskItemStatus Status,
    DateOnly? DueDate, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
{
    public static TaskItem Create(Guid userId, string title, string? description,
        DateOnly? dueDate, DateTimeOffset now) => throw new NotImplementedException();

    public TaskItem Update(string title, string? description, TaskItemStatus status,
        DateOnly? dueDate, DateTimeOffset now) => throw new NotImplementedException();
}
