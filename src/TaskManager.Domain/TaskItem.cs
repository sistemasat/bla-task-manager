namespace TaskManager.Domain;

public sealed record TaskItem
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public string Title { get; }
    public string? Description { get; }
    public TaskItemStatus Status { get; }
    public DateOnly? DueDate { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }

    private TaskItem(Guid id, Guid userId, string title, string? description,
        TaskItemStatus status, DateOnly? dueDate, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        if (id == Guid.Empty)
            throw new ValidationException("id", "A task must have an identity.");
        if (userId == Guid.Empty)
            throw new ValidationException("user_id", "A task must have an owner.");
        if (!Enum.IsDefined(status))
            throw new ValidationException("status", "Choose a valid task status.");
        var (cleanTitle, cleanDescription) = ValidateText(title, description);
        Id = id;
        UserId = userId;
        Title = cleanTitle;
        Description = cleanDescription;
        Status = status;
        DueDate = dueDate;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static TaskItem Restore(Guid id, Guid userId, string title, string? description,
        TaskItemStatus status, DateOnly? dueDate, DateTimeOffset createdAt, DateTimeOffset updatedAt) =>
        new(id, userId, title, description, status, dueDate, createdAt, updatedAt);

    public static TaskItem Create(Guid userId, string title, string? description,
        DateOnly? dueDate, DateTimeOffset now)
        => new(Guid.NewGuid(), userId, title, description, TaskItemStatus.Pending, dueDate, now, now);

    public TaskItem Update(string title, string? description, TaskItemStatus status,
        DateOnly? dueDate, DateTimeOffset now)
        => new(Id, UserId, title, description, status, dueDate, CreatedAt, now);

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
