using System.Globalization;
using Microsoft.Data.Sqlite;
using TaskManager.Application.Tasks;
using TaskManager.Domain;

namespace TaskManager.Infrastructure.Persistence;

public sealed class SqliteTaskRepository(SqliteDatabase database) : ITaskRepository
{
    private readonly SqliteDatabase _database = database;
    private const string Columns = "id, user_id, title, description, status, due_date, created_at, updated_at";

    public async Task<IReadOnlyList<TaskItem>> ListAsync(Guid userId, int skip, int take, CancellationToken ct)
    {
        await using var connection = await _database.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM tasks WHERE user_id = $user ORDER BY created_at DESC, id LIMIT $take OFFSET $skip";
        command.Parameters.AddWithValue("$user", userId.ToString());
        command.Parameters.AddWithValue("$take", take);
        command.Parameters.AddWithValue("$skip", skip);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var tasks = new List<TaskItem>();
        while (await reader.ReadAsync(ct)) tasks.Add(Read(reader));
        return tasks;
    }

    public async Task<TaskItem?> FindAsync(Guid id, Guid userId, CancellationToken ct)
    {
        await using var connection = await _database.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM tasks WHERE id = $id AND user_id = $user";
        BindOwner(command, id, userId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Read(reader) : null;
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct)
    {
        await using var connection = await _database.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = $"INSERT INTO tasks ({Columns}) VALUES ($id, $user, $title, $description, $status, $due, $created, $updated)";
        BindTask(command, task);
        command.Parameters.AddWithValue("$created", task.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> UpdateAsync(TaskItem task, CancellationToken ct)
    {
        await using var connection = await _database.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE tasks SET title=$title, description=$description, status=$status, due_date=$due, updated_at=$updated WHERE id=$id AND user_id=$user";
        BindTask(command, task);
        return await command.ExecuteNonQueryAsync(ct) == 1;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct)
    {
        await using var connection = await _database.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM tasks WHERE id=$id AND user_id=$user";
        BindOwner(command, id, userId);
        return await command.ExecuteNonQueryAsync(ct) == 1;
    }

    private static void BindOwner(SqliteCommand command, Guid id, Guid userId)
    {
        command.Parameters.AddWithValue("$id", id.ToString());
        command.Parameters.AddWithValue("$user", userId.ToString());
    }

    private static void BindTask(SqliteCommand command, TaskItem task)
    {
        BindOwner(command, task.Id, task.UserId);
        command.Parameters.AddWithValue("$title", task.Title);
        command.Parameters.AddWithValue("$description", (object?)task.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("$status", (int)task.Status);
        command.Parameters.AddWithValue("$due", (object?)task.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$updated", task.UpdatedAt.ToString("O", CultureInfo.InvariantCulture));
    }

    private static TaskItem Read(SqliteDataReader reader) => TaskItem.Restore(
        Guid.Parse(reader.GetString(0)), Guid.Parse(reader.GetString(1)), reader.GetString(2),
        reader.IsDBNull(3) ? null : reader.GetString(3), (TaskItemStatus)reader.GetInt32(4),
        reader.IsDBNull(5) ? null : DateOnly.ParseExact(reader.GetString(5), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTimeOffset.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
        DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture));
}
