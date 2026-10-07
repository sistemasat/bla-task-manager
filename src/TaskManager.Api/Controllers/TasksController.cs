using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Tasks;
using TaskManager.Domain;

namespace TaskManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(TaskService service) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirst("sub")!.Value);
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskView>>> List(CancellationToken ct, int skip = 0, int take = 100)
        => Ok((await service.ListAsync(UserId, skip, take, ct)).Select(TaskView.From));
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskView>> Get(Guid id, CancellationToken ct)
        => Ok(TaskView.From(await service.GetAsync(id, UserId, ct)));
    [HttpPost]
    public async Task<ActionResult<TaskView>> Create(CreateTaskRequest request, CancellationToken ct)
    {
        var task = await service.CreateAsync(UserId, request.Title, request.Description, request.DueDate, ct);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, TaskView.From(task));
    }
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskView>> Update(Guid id, UpdateTaskRequest request, CancellationToken ct)
        => Ok(TaskView.From(await service.UpdateAsync(id, UserId, request.Title, request.Description,
            request.Status!.Value, request.DueDate, ct)));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, UserId, ct);
        return NoContent();
    }
}

public sealed record CreateTaskRequest([Required] string Title, string? Description, DateOnly? DueDate);
public sealed record UpdateTaskRequest([Required] string Title, string? Description, [Required] TaskItemStatus? Status, DateOnly? DueDate);
public sealed record TaskView(Guid Id, string Title, string? Description, TaskItemStatus Status,
    DateOnly? DueDate, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
{
    public static TaskView From(TaskItem task) => new(task.Id, task.Title, task.Description,
        task.Status, task.DueDate, task.CreatedAt, task.UpdatedAt);
}
