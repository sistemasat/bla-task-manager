using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Api;
using TaskManager.Application.Auth;
using TaskManager.Application.Tasks;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);
var signingKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrEmpty(signingKey) && builder.Environment.IsDevelopment())
    signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
if (string.IsNullOrEmpty(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
    throw new InvalidOperationException("Set Jwt:SigningKey to a secret containing at least 32 bytes.");
var jwt = new JwtSettings(signingKey);
var databaseFile = builder.Configuration["Database:Path"] ?? Path.Combine(builder.Environment.ContentRootPath, "data", "tasks.db");
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databaseFile))!);
var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
{ DataSource = databaseFile, ForeignKeys = true, Pooling = false }.ToString();
builder.Services.AddSingleton(new SqliteDatabase(connectionString));
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<IAccessTokenIssuer, JwtTokenIssuer>();
builder.Services.AddScoped<IUserRepository, SqliteUserRepository>();
builder.Services.AddScoped<ITaskRepository, SqliteTaskRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false));
});
builder.Services.Configure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = context =>
{
    var errors = context.ModelState.Where(pair => pair.Value?.Errors.Count > 0)
        .ToDictionary(pair => JsonNamingPolicy.SnakeCaseLower.ConvertName(pair.Key),
            pair => pair.Value!.Errors.Select(error => string.IsNullOrEmpty(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage).ToArray());
    return new BadRequestObjectResult(new ValidationProblemDetails(errors) { Status = 400 });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true, ValidIssuer = jwt.Issuer,
        ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
        ValidateLifetime = true, RequireExpirationTime = true,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
            if (!Guid.TryParse(context.Principal?.FindFirst("sub")?.Value, out var id) || id == Guid.Empty ||
                await users.FindByIdAsync(id, context.HttpContext.RequestAborted) is null)
                context.Fail("The token does not identify an active user.");
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration["Frontend:Origin"] ?? "http://localhost:5178").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);
var app = builder.Build();
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapControllers();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
await app.Services.GetRequiredService<SqliteDatabase>().InitializeAsync();
if (app.Environment.IsDevelopment() && builder.Configuration.GetValue("Demo:Seed", true))
    await DemoSeeder.SeedAsync(app.Services);
app.Run();

public partial class Program;
