using System.Globalization;
using Microsoft.Data.Sqlite;
using TaskManager.Application.Auth;
using TaskManager.Domain;

namespace TaskManager.Infrastructure.Persistence;

public sealed class SqliteUserRepository(SqliteDatabase database) : IUserRepository
{
    private readonly SqliteDatabase _database = database;
    public Task<User?> FindByEmailAsync(string email, CancellationToken ct)
        => FindAsync("SELECT id, name, email, password_hash, created_at FROM users WHERE email = $value", email, ct);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct)
        => FindAsync("SELECT id, name, email, password_hash, created_at FROM users WHERE id = $value", id.ToString(), ct);

    private async Task<User?> FindAsync(string sql, string value, CancellationToken ct)
    {
        await using var connection = await _database.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$value", value);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        return new(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2),
            reader.GetString(3), DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture));
    }

    public async Task<bool> TryAddAsync(User user, CancellationToken ct)
    {
        await using var connection = await _database.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO users (id, name, email, password_hash, created_at) VALUES ($id, $name, $email, $hash, $created)";
        command.Parameters.AddWithValue("$id", user.Id.ToString());
        command.Parameters.AddWithValue("$name", user.Name);
        command.Parameters.AddWithValue("$email", user.Email);
        command.Parameters.AddWithValue("$hash", user.PasswordHash);
        command.Parameters.AddWithValue("$created", user.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        try { await command.ExecuteNonQueryAsync(ct); return true; }
        catch (SqliteException error) when (error.SqliteExtendedErrorCode == 2067) { return false; }
    }
}
