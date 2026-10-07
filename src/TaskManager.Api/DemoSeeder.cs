using TaskManager.Application.Auth;
using TaskManager.Application.Tasks;
using TaskManager.Domain;

namespace TaskManager.Api;

public static class DemoSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
        var tasks = scope.ServiceProvider.GetRequiredService<TaskService>();
        if (await users.FindByEmailAsync("demo@example.com", default) is not null) return;
        var user = await auth.RegisterAsync("Alex Morgan", "demo@example.com", "DemoPassword123!", default);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await tasks.CreateAsync(user.Id, "Prepare the project walkthrough", "Explain the user story, architecture and testing approach.", today.AddDays(2), default);
        await tasks.CreateAsync(user.Id, "Review ownership checks", "Try reading and editing a task from another account.", today.AddDays(1), default);
        var task = await tasks.CreateAsync(user.Id, "Define the acceptance criteria", "Keep the scope focused on personal tasks.", today, default);
        await tasks.UpdateAsync(task.Id, user.Id, task.Title, task.Description, TaskItemStatus.Completed, task.DueDate, default);
    }
}
