namespace TaskManager.Domain;

public sealed record TaskItem(
    Guid Id, Guid UserId, string Title, string? Description, TaskItemStatus Status,
    DateOnly? DueDate, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
{
    public static TaskItem Restore(Guid id, Guid userId, string title, string? description,
        TaskItemStatus status, DateOnly? dueDate, DateTimeOffset createdAt, DateTimeOffset updatedAt) =>
        throw new NotImplementedException();

    public static TaskItem Create(Guid userId, string title, string? description,
        DateOnly? dueDate, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
            throw new ValidationException("user_id", "A task must have an owner.");
        var (cleanTitle, cleanDescription) = ValidateText(title, description);
        return new(Guid.NewGuid(), userId, cleanTitle, cleanDescription,
            TaskItemStatus.Pending, dueDate, now, now);
    }

    public TaskItem Update(string title, string? description, TaskItemStatus status,
        DateOnly? dueDate, DateTimeOffset now)
    {
        var (cleanTitle, cleanDescription) = ValidateText(title, description);
        if (!Enum.IsDefined(status))
            throw new ValidationException("status", "Choose a valid task status.");
        return this with
        {
            Title = cleanTitle, Description = cleanDescription,
            Status = status, DueDate = dueDate, UpdatedAt = now
        };
    }

    private static (string Title, string? Description) ValidateText(string title, string? description)
    {
        var cleanTitle = title?.Trim();
        if (string.IsNullOrEmpty(cleanTitle) || cleanTitle.Length > 200)
            throw new ValidationException("title", "Title must contain between 1 and 200 characters.");
        var cleanDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (cleanDescription?.Length > 4000)
            throw new ValidationException("description", "Description must not exceed 4000 characters.");
        return (cleanTitle, cleanDescription);
    }
}
