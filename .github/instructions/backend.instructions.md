---
applyTo: "KPIAPI/**/*.cs,**/*.cs"
---

# Backend Instructions for KPIAPI

## Scope
These instructions apply to backend C# and ASP.NET Core work.

## Architecture
- Follow the existing ASP.NET Core architecture already present in the repository.
- Keep controllers thin.
- Prefer moving business logic and query composition into services when appropriate.
- Do not introduce new architectural layers unless explicitly asked.
- Preserve existing route structure and DTO contracts unless the task explicitly requires changing them.

## Dependency injection
- Prefer ASP.NET Core built-in dependency injection.
- Prefer constructor injection over static access or service location.
- Refactor incrementally rather than rewriting the whole backend at once.
- When introducing services, wire them through Program.cs using the simplest appropriate lifetime.
- Do not inject DbContext directly into controllers if service extraction is the better fit for the existing direction of the codebase.

## Entity Framework Core
- Match existing EF Core patterns already used in the project.
- Prefer explicit and readable queries.
- Avoid unnecessary abstractions over DbContext.
- Be careful with Includes, grouping, and aggregates so query intent stays clear.
- Do not suggest destructive migrations unless explicitly requested.
- Do not recommend dropping or recreating the database unless explicitly asked.

## API contracts
- Preserve request and response contracts unless explicitly told to change them.
- Do not casually rename public fields, DTO properties, route segments, or controller actions that affect consumers.
- If backend contract changes are required, clearly identify the frontend and type updates that must follow.

## Domain semantics
- Runs contain run events.
- KPI values belong to run events.
- Heartbeat and timeout logic are operationally important.
- Backend naming may use "event" even when the frontend should use more user-friendly business wording.
- Preserve domain meaning over cosmetic renaming.

## Code changes
- Make the smallest reasonable change that solves the task.
- Do not refactor unrelated files.
- Prefer explicit code over clever abstractions.
- Match the style of neighboring files.
- Add comments only when they clarify intent that is not already obvious from the code.

## Responses
- Prefer full drop-in replacement files when asked to provide code.
- State which file is being replaced.
- Keep explanations concise and practical.
- Mention verification steps after code changes.