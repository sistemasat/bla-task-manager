using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskManager.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"task-manager-api-{Guid.NewGuid()}.db");
    public const string SigningKey = "test-only-api-signing-key-with-at-least-32-bytes";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Path", _file);
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("Demo:Seed", "false");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) File.Delete(_file);
    }
}
