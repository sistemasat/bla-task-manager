using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application;
using TaskManager.Application.Auth;
using TaskManager.Domain;

namespace TaskManager.Api;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            ValidationException => 400,
            InvalidCredentialsException => 401,
            NotFoundException => 404,
            EmailAlreadyExistsException => 409,
            _ => 500
        };
        if (status == 500) logger.LogError(exception, "An unexpected request failure occurred.");
        var problem = new ProblemDetails
        {
            Status = status, Title = status == 500 ? "An unexpected error occurred." : exception.Message,
            Instance = context.Request.Path
        };
        if (exception is ValidationException validation)
            problem.Extensions["errors"] = new Dictionary<string, string[]> { [validation.Field] = [validation.Message] };
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, (JsonSerializerOptions?)null, "application/problem+json", ct);
        return true;
    }
}
