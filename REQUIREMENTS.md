# Assessment requirements and evidence

This checklist maps the assessment to the implementation. The two resource APIs
are separate controllers in a single host. Persistence and HTTP tests use real
adapters; they complement the domain/application unit tests.

| Requirement | Implementation | Verification |
| --- | --- | --- |
| Informal user story | README and PRESENTATION | Acceptance criteria and demo walkthrough |
| ASP.NET MVC/Web API in C# | MVC API controllers in TaskManager.Api | HttpTests |
| Application data and users in storage | SQLite tasks/users tables, IDs and additional fields | PersistenceTests |
| Task CRUD and suitable HTTP verbs/results | TasksController and TaskService | HTTP lifecycle and ownership tests |
| User creation and login | AuthController and AuthService | Registration/login and duplicate/credential tests |
| Public and protected endpoints | Public health/register/login; protected me/tasks | Anonymous and JWT validation tests |
| Independent business layer | Domain rules; Application use cases and ports | Domain/Application tests; project references |
| Independent data access | Parameterized SQLite adapters in Infrastructure | Real database tests, constraints and round trips |
| No EF, Dapper or Mediator | Explicit SQL using Microsoft.Data.Sqlite | Tracked package lockfiles |
| TDD and automated application tests | Real red/green checkpoints; unit and integration suites | Git history and CI |
| Integrated frontend framework | React with typed fetch adapter | Form/adapter tests and App screen-flow tests |
| Responsive, usable CRUD UI | Native dialogs, loading/error states and pagination | README screenshots; App CRUD and 53-task pagination tests |
| Setup documentation | README with prerequisites, commands and configuration | Locked restore and CI clean checkout |
| Demo data and credentials | Development-only DemoSeeder | Local demo login and persistent task list |
| GenAI prompt/output/validation | GENAI with representative output and real corrections | Referenced tests and commits |
| Presentation and code review | PRESENTATION and readable source | Five-minute demo route and documented tradeoffs |
| One public repository | Source, tests and evaluator documentation together | Repository contents |
| No symlinks in submission | Ordinary tracked files | Git index mode check |

The second-API wording is interpreted as a second API resource/controller, not
a separately deployed service. The exercise does not explicitly require separate
hosts. README states the implemented topology and development configuration.

The test suites do not claim complete code coverage or exhaustive browser E2E.
The frontend App tests connect real components to a mocked HTTP adapter; the
separate adapter tests validate HTTP handling, and the backend HTTP tests run
the actual authentication middleware and SQLite persistence.
