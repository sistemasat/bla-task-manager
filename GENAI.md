# GenAI development record

## Use and boundaries

Codex assisted with code, tests and documentation during development. Generated
output was checked through compilation, automated tests, dependency audits and
local browser interaction. The application itself has no GenAI runtime feature
and sends no task data to a model. This document addresses the mandatory GenAI
portion of the assessment.

## Prompt

The following is a consolidated prompt for reproducing the task use-case slice;
it summarizes the constraints used during iterative development rather than
claiming to be a verbatim transcript of every interaction:

```text
Implement a personal task-management slice in C# using Clean Architecture.
Application owns ITaskRepository; Domain owns TaskItem validation. Infrastructure
will implement the repository using Microsoft.Data.Sqlite and parameterized SQL.
Do not use Entity Framework, Dapper, Mediator or a generic repository.

Start with failing behavior tests for create, get, list, update and delete.
Each operation receives the authenticated user ID. Never accept ownership from
the request body. Return the same not-found behavior for missing and foreign-owned
tasks. Update must preserve task identity, owner and creation time and detect
when its repository update affects no row. Use TimeProvider for timestamps.

Require a trimmed title of 1-200 characters, optional description up to 4000,
pending/in_progress/completed status and optional calendar due date. Allow overdue
dates. Reject invalid pagination and keep the dependency rule pointing inward.
Keep comments sparse and in English. Show actual test results and identify
assumptions and limitations. Work in small, reviewable changes.
```

## Sample accepted output

Excerpt from `src/TaskManager.Application/Tasks/TaskService.cs`:

```csharp
public async Task<TaskItem> GetAsync(Guid id, Guid userId, CancellationToken ct)
{
    var task = await _repository.FindAsync(id, userId, ct);
    if (task is null || task.UserId != userId)
        throw new NotFoundException();
    return task;
}

public async Task<TaskItem> UpdateAsync(Guid id, Guid userId, string title,
    string? description, TaskItemStatus status, DateOnly? dueDate, CancellationToken ct)
{
    var current = await GetAsync(id, userId, ct);
    var updated = current.Update(title, description, status, dueDate, _clock.GetUtcNow());
    if (!await _repository.UpdateAsync(updated, ct))
        throw new NotFoundException();
    return updated;
}
```

This output remains subject to validation. An interface name alone does not
prove isolation: the SQL adapter includes owner conditions, and integration
tests exercise the real database and authentication middleware.

## Validation and actual corrections

| Finding | Correction and evidence |
| --- | --- |
| Inherited private NuGet sources caused restore errors | Added repository-level public-only `nuget.config`; evaluator setup does not depend on machine credentials. |
| Dependency restore reported vulnerable transitive SQLite/OpenAPI packages | Updated package versions and the SQLite native bundle; warnings were not suppressed. |
| Frontend test selectors passed runtime tests but used an unsupported typed option | Removed the unsupported option in commit `26a8c6d`; type checking remains part of the frontend build. |
| Browser-driven date input showed a value that did not reach the request | Added a failing input-event regression test in `882637d`; handled native input events in `25db7ef`, then verified a saved date through the browser. |
| A new task dialog initially focused its close control | Explicitly focused the title after opening the native modal in `25db7ef`. |

Domain/application tests cover text validation, timestamps, pagination and owner
rejection. SQLite tests cover real persistence, duplicate email and foreign keys.
HTTP tests cover anonymous access, expired/wrong-signature tokens, credential
errors, validation and foreign-owner CRUD. Frontend tests cover form values,
busy/error behavior, dates and request/error handling. Connected App tests also
exercise registration, login, CRUD, validation recovery, pagination beyond 50
tasks and session expiry both on 401 and at the token's deadline. HTTP checks
reject missing, null, unknown and numeric update statuses without changing the
stored task, and verify a 53-task list across page boundaries.
Expired, wrong-signature, wrong-issuer and wrong-audience HTTP checks use
existing users and verify a valid-token baseline before testing rejection.
Browser checks supplement
the automated suites; they are not a comprehensive end-to-end test suite.

## Review considerations

Never send production credentials or personal data in prompts. Review generated
authentication and SQL independently of happy paths. Prefer standard libraries
for cryptography, keep dependencies pinned, run their vulnerability checks, and
read the accepted implementation until every decision can be explained.
GitHub's [responsible-use guidance](https://docs.github.com/en/copilot/responsible-use/inline-suggestions)
also emphasizes reviewing and testing generated suggestions; it is general
guidance here, not a claim that Copilot was the tool used.

No coverage percentage, production readiness or exhaustive security testing is
claimed. The limits described in README remain part of the review.
