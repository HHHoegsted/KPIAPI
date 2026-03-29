# Copilot Instructions for KPIAPI

## Project context
KPIAPI is a KPI tracking system for automation and robot runs.

The solution contains:
- A C# ASP.NET Core API backend
- A React frontend
- A PostgreSQL database
- Docker Compose for local and deployment orchestration

The system is used to track:
- robots
- runs
- run events
- KPI values attached to run events
- dashboard and history views
- operational status such as heartbeat and stalled runs

Favor practical, maintainable solutions over clever abstractions.

---

## General rules
- Preserve existing architecture and conventions.
- Prefer consistency with the current codebase over generic best-practice rewrites.
- Make the smallest reasonable change that solves the task.
- Do not refactor unrelated code.
- Keep changes easy to review.
- Match the style of neighboring files.
- Prefer explicit, readable code over clever abstractions.
- Avoid overengineering.
- Avoid large speculative abstractions.
- Prefer explicit names over overly short names.
- Add comments only where they genuinely clarify intent.

---

## Output format when assisting
- When asked to modify code, prefer full drop-in replacement files rather than partial snippets, unless explicitly asked for a diff or a smaller excerpt.
- For multi-file changes, provide one file at a time unless explicitly asked for several at once.
- State which file is being replaced.
- Keep explanations brief and practical.
- Mention any required follow-up files if relevant.

---

## Backend rules
- Follow existing ASP.NET Core patterns already used in the project.
- Keep controllers thin.
- Put business logic in the appropriate service or domain layer if such a pattern already exists in the codebase.
- Prefer moving business logic and query composition out of controllers when appropriate.
- Use Entity Framework Core in the same style already present in the project.
- Do not introduce unnecessary layers, frameworks, or patterns.
- Preserve route structure and naming conventions unless explicitly asked to change them.
- Preserve existing route structure and DTO contracts unless the task explicitly requires changing them.

### Dependency injection
- Prefer ASP.NET Core built-in dependency injection.
- Prefer constructor injection over service location, static access, or service locator patterns.
- Refactor incrementally, one controller or service slice at a time.
- When introducing services, wire them through Program.cs using the simplest appropriate lifetime.
- Do not inject DbContext directly into controllers if service extraction is a better fit for the existing direction of the codebase.
- Move business logic and query logic out of controllers where practical.
- Preserve routes, DTOs, and API contracts unless explicitly asked to change them.

### Entity Framework Core
- Match existing EF Core patterns already used in the project.
- Prefer explicit and readable queries.
- Prefer explicit, readable LINQ over overly abstract query helpers.
- Avoid unnecessary abstractions over DbContext.
- Be careful with Includes, grouping, and aggregates so query intent stays clear.
- Be careful with migrations; do not generate or suggest destructive schema changes unless explicitly requested.
- Do not suggest dropping or recreating the database unless explicitly asked.

### API contracts
- Do not rename public API fields unless explicitly asked.
- Backend field names and API contracts are important; preserve them unless the task explicitly requires a contract change.
- Preserve request and response contracts unless explicitly told to change them.
- Do not casually rename public fields, DTO properties, route segments, or controller actions that affect consumers.
- If backend contract changes are required, clearly identify the frontend and type updates that must follow.

### Backend domain semantics
- Runs contain run events.
- KPI values belong to run events.
- Heartbeat and timeout logic are operationally important.
- Backend naming may use "event" even when the frontend should use more user-friendly business wording.
- Preserve domain meaning over cosmetic renaming.

---

## Frontend rules
- Follow the existing React and TypeScript patterns in the repository.
- Prefer consistency with the current app over introducing new frontend patterns.
- Prefer small focused components when a page becomes too large.
- Extract components by responsibility, not prematurely.
- Keep props explicit and readable.
- Reuse existing types, API client patterns, formatting helpers, and design tokens whenever possible.
- Keep UI density reasonably compact unless explicitly asked otherwise.
- Prefer clear operational dashboards over decorative UI.
- Prefer stable layouts and clear hierarchy over flashy visuals.
- Do not introduce a new state management library unless the repository already uses it or the task explicitly asks for it.
- Do not restyle unrelated pages during a functional task.
- When changing labels or wording, preserve the underlying domain meaning.
- Prefer changing user-facing labels in the frontend instead of renaming backend contract fields.

### Visual style and color system
- For frontend visual changes, treat `frontend/src/index.css` design tokens as the source of truth.
- Preserve the existing KPIAPI visual identity.
- Do not introduce a new color palette unless explicitly asked.
- Prefer reusing existing CSS variables and tokens from `index.css` instead of hardcoding new colors.
- If a component already has a visual pattern in the codebase, match that pattern rather than inventing a new one.
- Favor consistency over novelty.

#### Project color source of truth
Use the existing CSS custom properties from the frontend project as the visual source of truth.

##### Brand colors
- `--brand-900: #002d58` — primary brand color
- `--brand-700: #005e82` — hover and secondary brand color
- `--brand-300: #74a6c3` — soft accents and chart-friendly brand tone
- `--brand-100: #e1e8f7` — tinted backgrounds and soft surfaces

##### Neutral colors
- `--gray-900: #1a1a1a`
- `--gray-700: #747373`
- `--gray-500: #abaaaa`
- `--gray-300: #c7c7c7`
- `--gray-200: #dfdfdf`
- `--gray-100: #f4f4f4`

##### Accent color
- `--accent-700: #ef7800` — use sparingly for highlight or emphasis

##### App tokens
- `--bg: var(--gray-100)` — app background
- `--surface: #ffffff` — cards and surface background
- `--text: var(--gray-900)` — primary text
- `--muted: var(--gray-700)` — secondary text
- `--border: var(--gray-200)` — borders and dividers
- `--link: var(--brand-700)` — links
- `--primary: var(--brand-900)` — primary action color
- `--primary-hover: var(--brand-700)` — primary hover state

##### Button tokens
- `--button-secondary-bg: var(--brand-100)`
- `--button-secondary-hover: #cfdcf0`
- `--button-secondary-text: var(--brand-900)`

##### Run outcome colors
- `--run-succeeded-bg: var(--surface)`
- `--run-failed-bg: #f8d7da`
- `--run-partial-bg: #fff3cd`
- `--run-canceled-bg: #e2e3e5`
- `--run-running-bg: #dbeafe`

### Color usage rules
- Use the defined CSS variables instead of raw hex values whenever possible.
- Prefer `var(--primary)` and `var(--primary-hover)` for primary actions.
- Prefer `var(--surface)`, `var(--bg)`, `var(--border)`, `var(--text)`, and `var(--muted)` for general layout and typography.
- Use `--accent-700` sparingly for emphasis, not as a replacement primary color.
- Preserve the current blue-led visual identity with orange as a restrained highlight.
- Avoid introducing additional saturated colors unless explicitly required by the task.

### Dark theme rules
- KPIAPI supports a dark visual mode using the same brand family rather than a separate unrelated palette.
- In dark mode, prefer blue-toned surfaces and accents that stay consistent with the existing KPIAPI brand.
- Use lighter brand tones for primary actions on dark backgrounds.
- Keep orange as a restrained highlight color, not a dominant theme color.
- Preserve strong readability and contrast in dashboards and status views.
- Prefer existing dark-mode tokens and variables instead of inventing one-off dark colors.
- When implementing dark-mode styles, use semantic tokens such as `--bg`, `--surface`, `--text`, `--muted`, `--border`, `--primary`, and `--primary-hover`.
- Preserve the meaning of run outcome colors in dark mode while adapting them to darker surfaces.

### Operational status styling
- Preserve the existing run outcome color meanings.
- Use run outcome colors for operational state backgrounds where appropriate.
- Do not rely on color alone for critical status communication; preserve clear text labels and structure.
- Keep status styling readable and functional rather than decorative.

### Typography and visual tone
- Preserve the existing font stack:
  - Arial
  - Verdana
  - system-ui
  - -apple-system
  - "Segoe UI"
  - Roboto
  - "Helvetica Neue"
  - sans-serif
- Keep typography straightforward, legible, and operationally focused.
- Maintain a clean, dashboard-oriented visual style rather than a marketing-style interface.

### Styling changes
- When changing styles, prefer existing tokens before introducing new ones.
- If a new token is truly needed, keep it aligned with the current naming and palette structure.
- Do not hardcode a one-off hex value when an existing token already expresses the design intent.
- Preserve spacing, borders, and density patterns already established in the app.

### Frontend data and API usage
- Do not invent data that the backend does not provide.
- Keep frontend types aligned with backend DTOs and the API client.
- If backend and frontend need to change together, make that alignment explicit.

---

## KPI and domain semantics
- A run can contain multiple run events.
- KPI values belong to run events.
- "Events" in backend and domain naming may represent units of business work, not necessarily user-facing "events".
- User-facing wording may differ from backend naming if that improves clarity.
- Numeric KPI displays should support domain-appropriate summaries such as totals and averages.
- Boolean KPI displays should usually be presented clearly, for example as yes/no or percentages, depending on context.
- Text KPI displays should be summarized in a compact and useful way.
- Remember that backend "events" may represent units of business work rather than user-facing event concepts.
- Preserve domain meaning when choosing display text.
- Heartbeat and run completion logic are operationally important; preserve their intent.

---

## Database and data safety
- Treat existing database data as valuable.
- Prefer additive, safe changes over destructive changes.
- When discussing Docker or deployment updates, assume the database should be preserved unless explicitly told otherwise.
- If a change could risk stored data, clearly warn about it.
- Do not recommend volume deletion unless explicitly requested.
- Be cautious with migration advice and production update instructions.

---

## Docker and deployment rules
- Respect the existing container-based setup.
- Prefer targeted rebuilds when only one part of the system changed, if that fits the task.
- Do not suggest unnecessary full resets.
- When giving Docker commands, keep them copy-pasteable.
- When relevant, distinguish between rebuilding images, recreating containers, and deleting volumes.
- Default toward preserving persisted PostgreSQL data.

---

## API client and contract rules
- Preserve compatibility between frontend and backend where possible.
- If backend and frontend both need changes, make them align explicitly.
- Do not invent response fields that are not actually supplied by the backend.
- Keep TypeScript types in sync with backend DTOs.
- If introducing a new endpoint or response shape, update both the API client and types accordingly.

---

## Testing and verification
- When changing backend behavior, update or add relevant tests if the repository already has a testing pattern for that area.
- When changing frontend behavior, ensure types and rendering logic still align.
- Include practical verification steps.
- Prefer commands and checks that match the actual project tooling already in the repository.
- If you are unsure whether a command exists in the repo, say so instead of inventing one.

---

## What to avoid
- Do not rewrite the whole project to fit a preferred architecture.
- Do not introduce new infrastructure without being asked.
- Do not change domain terminology casually.
- Do not make destructive database recommendations by default.
- Do not provide pseudo-code when the user asked for concrete implementation.
- Do not provide partial code when a full-file replacement is more useful.
- Do not assume missing files, classes, or commands exist without checking the provided code.

---

## Preferred behavior for larger tasks
For larger changes:
1. Briefly identify the touched files.
2. Preserve existing patterns.
3. Implement the smallest coherent slice.
4. Keep backend, frontend, and types aligned.
5. Summarize what changed and how to verify it.

---

## Priority order
When in doubt, optimize for:
1. correctness
2. compatibility with existing code
3. data safety
4. maintainability
5. clarity
6. minimal change surface