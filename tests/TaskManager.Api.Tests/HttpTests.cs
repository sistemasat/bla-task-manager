using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace TaskManager.Api.Tests;

public class HttpTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Health_is_public_and_tasks_require_authentication()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/tasks")).StatusCode);
    }

    [Fact]
    public async Task Registration_login_and_task_lifecycle_use_expected_http_contract()
    {
        using var client = factory.CreateClient();
        await SignIn(client);
        var me = await client.GetStringAsync("/api/auth/me");
        Assert.DoesNotContain("hash", me, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", me, StringComparison.OrdinalIgnoreCase);
        var response = await client.PostAsJsonAsync("/api/tasks", new { title = "Prepare demo", description = "Show CRUD", due_date = "2026-10-09" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();
        Assert.Equal("pending", created.GetProperty("status").GetString());
        Assert.Equal("2026-10-09", created.GetProperty("due_date").GetString());
        var updated = await client.PutAsJsonAsync($"/api/tasks/{id}", new { title = "Demo ready", description = (string?)null, status = "completed", due_date = (string?)null });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("completed", (await updated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
        Assert.Single(await client.GetFromJsonAsync<JsonElement[]>("/api/tasks") ?? []);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/tasks/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/tasks/{id}")).StatusCode);
    }

    [Fact]
    public async Task Another_user_cannot_get_update_delete_or_list_private_task()
    {
        using var owner = factory.CreateClient();
        using var stranger = factory.CreateClient();
        await SignIn(owner);
        await SignIn(stranger);
        var created = await owner.PostAsJsonAsync("/api/tasks", new { title = "Private" });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/tasks/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PutAsJsonAsync($"/api/tasks/{id}", new { title = "Changed", status = "completed" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.DeleteAsync($"/api/tasks/{id}")).StatusCode);
        Assert.Empty(await stranger.GetFromJsonAsync<JsonElement[]>("/api/tasks") ?? []);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/tasks/{id}")).StatusCode);
    }

    [Fact]
    public async Task Invalid_requests_return_problem_details_and_do_not_persist()
    {
        using var client = factory.CreateClient();
        await SignIn(client);
        var response = await client.PostAsJsonAsync("/api/tasks", new { title = " " });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("title", out _));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/tasks", new { title = "Task", due_date = "bad-date" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/tasks?take=201")).StatusCode);
        Assert.Empty(await client.GetFromJsonAsync<JsonElement[]>("/api/tasks") ?? []);
    }

    [Fact]
    public async Task Duplicate_email_returns_conflict_and_bad_password_returns_unauthorized()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", new { name = "Alex", email, password = "StrongPassword!" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", new { name = "Alex", email, password = "StrongPassword!" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword!" })).StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Expired_or_wrong_signature_tokens_are_rejected(bool expired)
    {
        using var client = factory.CreateClient();
        var key = expired ? ApiFactory.SigningKey : "different-key-with-at-least-32-bytes";
        var end = expired ? DateTime.UtcNow.AddMinutes(-1) : DateTime.UtcNow.AddMinutes(30);
        var token = new JwtSecurityToken("task-manager", "task-manager-web", [new Claim("sub", Guid.NewGuid().ToString())],
            notBefore: DateTime.UtcNow.AddHours(-1), expires: end,
            signingCredentials: new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/tasks")).StatusCode);
    }

    private static async Task SignIn(HttpClient client)
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/auth/register", new { name = "Alex", email, password = "StrongPassword!" });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "StrongPassword!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            session.GetProperty("access_token").GetProperty("token").GetString());
    }
}
