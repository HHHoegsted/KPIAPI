# Prompt: Refactor one backend slice to dependency injection

Refactor one backend slice of KPIAPI to use more dependency injection.

Requirements:
- Preserve current API routes and response contracts.
- Do not change the database schema.
- Do not generate migrations.
- Keep the change incremental and easy to review.
- Prefer ASP.NET Core built-in dependency injection.
- Prefer constructor injection.
- Keep controllers thin.
- Move business logic and query logic out of controllers where practical.
- Follow existing repository and project conventions.
- Do not refactor unrelated areas.

Task flow:
1. Identify one good controller or backend slice to refactor first.
2. List the files that should be touched.
3. Implement the smallest coherent DI-based refactor for that slice.
4. Update service registration in Program.cs if needed.
5. Preserve DTOs, routes, and API behavior.
6. Mention any follow-up slices that would logically come next.

Output expectations:
- Provide full drop-in replacement files, one file at a time.
- State which file is being replaced.
- Briefly explain the role of each change.
- End with practical verification steps.

Verification:
- Build the backend.
- Check that the affected endpoints still behave the same.
- Mention any tests that should be run or added.