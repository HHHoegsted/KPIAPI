---
applyTo: "frontend/**/*.{ts,tsx,js,jsx,css,scss},**/*.{ts,tsx,js,jsx,css,scss}"
---

# Frontend Instructions for KPIAPI

## Scope
These instructions apply to React, TypeScript, and frontend styling work.

## General frontend rules
- Follow the existing React and TypeScript patterns already used in the repository.
- Prefer consistency with the current app over introducing new frontend patterns.
- Keep the UI practical, compact, and operationally clear.
- Do not restyle unrelated pages during functional work.
- Reuse existing types, API client functions, formatting helpers, and design tokens whenever possible.

## Visual style and color system
- Preserve the existing KPIAPI visual identity.
- Do not introduce a new color palette unless explicitly asked.
- Prefer reusing existing CSS variables and tokens from `index.css` instead of hardcoding new colors.
- If a component already has a visual pattern in the codebase, match that pattern rather than inventing a new one.
- Favor consistency over novelty.

### Project color source of truth
Use the existing CSS custom properties from the frontend project as the visual source of truth.

#### Brand colors
- `--brand-900: #002d58` — primary brand color
- `--brand-700: #005e82` — hover and secondary brand color
- `--brand-300: #74a6c3` — soft accents and chart-friendly brand tone
- `--brand-100: #e1e8f7` — tinted backgrounds and soft surfaces

#### Neutral colors
- `--gray-900: #1a1a1a`
- `--gray-700: #747373`
- `--gray-500: #abaaaa`
- `--gray-300: #c7c7c7`
- `--gray-200: #dfdfdf`
- `--gray-100: #f4f4f4`

#### Accent color
- `--accent-700: #ef7800` — use sparingly for highlight or emphasis

#### App tokens
- `--bg: var(--gray-100)` — app background
- `--surface: #ffffff` — cards and surface background
- `--text: var(--gray-900)` — primary text
- `--muted: var(--gray-700)` — secondary text
- `--border: var(--gray-200)` — borders and dividers
- `--link: var(--brand-700)` — links
- `--primary: var(--brand-900)` — primary action color
- `--primary-hover: var(--brand-700)` — primary hover state

#### Button tokens
- `--button-secondary-bg: var(--brand-100)`
- `--button-secondary-hover: #cfdcf0`
- `--button-secondary-text: var(--brand-900)`

#### Run outcome colors
- `--run-succeeded-bg: var(--surface)`
- `--run-failed-bg: #f8d7da`
- `--run-partial-bg: #fff3cd`
- `--run-canceled-bg: #e2e3e5`
- `--run-running-bg: #dbeafe`

## Color usage rules
- Use the defined CSS variables instead of raw hex values whenever possible.
- Prefer `var(--primary)` and `var(--primary-hover)` for primary actions.
- Prefer `var(--surface)`, `var(--bg)`, `var(--border)`, `var(--text)`, and `var(--muted)` for general layout and typography.
- Use `--accent-700` sparingly for emphasis, not as a replacement primary color.
- Preserve the current blue-led visual identity with orange as a restrained highlight.
- Avoid introducing additional saturated colors unless explicitly required by the task.

## Dark theme rules
- KPIAPI supports a dark visual mode using the same brand family rather than a separate unrelated palette.
- In dark mode, prefer blue-toned surfaces and accents that stay consistent with the existing KPIAPI brand.
- Use lighter brand tones for primary actions on dark backgrounds.
- Keep orange as a restrained highlight color, not a dominant theme color.
- Preserve strong readability and contrast in dashboards and status views.
- Prefer existing dark-mode tokens and variables instead of inventing one-off dark colors.
- When implementing dark-mode styles, use semantic tokens such as `--bg`, `--surface`, `--text`, `--muted`, `--border`, `--primary`, and `--primary-hover`.
- Preserve the meaning of run outcome colors in dark mode while adapting them to darker surfaces.

## Operational status styling
- Preserve the existing run outcome color meanings.
- Use run outcome colors for operational state backgrounds where appropriate.
- Do not rely on color alone for critical status communication; preserve clear text labels and structure.
- Keep status styling readable and functional rather than decorative.

## Typography and visual tone
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

## Components
- Prefer small focused components when a page becomes too large.
- Extract components by responsibility, not prematurely.
- Keep props explicit and readable.
- Match the structure and style of nearby files.

## Data and API usage
- Do not invent data that the backend does not provide.
- Keep frontend types aligned with backend DTOs and the API client.
- If backend and frontend need to change together, make that alignment explicit.
- Prefer changing user-facing labels in the frontend instead of renaming backend contract fields.

## KPI display semantics
- Numeric KPI values should usually support meaningful summaries such as totals and averages.
- Boolean KPI values should be shown in a clear operational form such as yes/no or percentages, depending on context.
- Text KPI values should be shown compactly and usefully.
- Remember that backend "events" may represent units of business work rather than user-facing event concepts.
- Preserve domain meaning when choosing display text.

## UI behavior
- Prefer dashboards that are clear and easy to scan.
- Avoid decorative complexity.
- Preserve density unless explicitly asked to space things out more.
- Do not introduce a new state management library unless the repository already uses it or the task explicitly asks for it.
- Prefer stable layouts and clear hierarchy over flashy visuals.

## Code changes
- Make the smallest coherent change that solves the task.
- Do not refactor unrelated files.
- Prefer readability over cleverness.
- Keep naming explicit.
- Add comments only when they genuinely help explain intent.

## Styling changes
- When changing styles, prefer existing tokens before introducing new ones.
- If a new token is truly needed, keep it aligned with the current naming and palette structure.
- Do not hardcode a one-off hex value when an existing token already expresses the design intent.
- Preserve spacing, borders, and density patterns already established in the app.

## Responses
- Prefer full drop-in replacement files when asked to provide code.
- State which file is being replaced.
- Mention any required follow-up file changes such as types or apiClient updates.
- Keep explanations brief and practical.