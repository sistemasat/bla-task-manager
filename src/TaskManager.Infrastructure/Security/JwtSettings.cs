namespace TaskManager.Infrastructure.Security;

public sealed record JwtSettings(string SigningKey, string Issuer = "task-manager", string Audience = "task-manager-web", int LifetimeMinutes = 30);
