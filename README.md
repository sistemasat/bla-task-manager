# Task Manager

A small task management application built with ASP.NET Core, React and SQLite.

## User story

As a user, I want to register, sign in and manage my own tasks with a title,
description, status and due date so I can organize my work.

## Acceptance criteria

- Users can register and sign in.
- Authenticated users can create, read, update and delete their own tasks.
- A user cannot access another user's tasks.
- Invalid input produces actionable validation errors.
- The application includes demo credentials and seeded tasks.
- The interface works on desktop and mobile.

## Structure

- `Domain`: entities and task rules, without framework dependencies.
- `Application`: use cases and ports.
- `Infrastructure`: SQLite access, password hashing and token issuance.
- `Api`: HTTP controllers and authentication.
- `web`: React client.
- `tests`: domain, application, persistence and HTTP tests.

Dependencies point inward. Entity Framework, Dapper and Mediator are not used.

## Development approach

Behavior is developed through a failing test, a minimal implementation and
refactoring. Commits describe actual changes. Tests reused as a reference are
not presented as evidence of a new TDD cycle.
