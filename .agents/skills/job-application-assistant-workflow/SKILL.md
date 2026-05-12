---
name: job-application-assistant-workflow
description: Use whenever working inside the JobApplicationAssistant repository, especially for implementing issues, updating planning docs, changing API endpoints, AI workflow logic, prompts, draft generation, evidence matching, claim audit, profile facts, gap decisions, exports, migrations, or related tests.
---

# JobApplicationAssistant Workflow

Goal: avoid wasting tokens on broad repo exploration while still preserving local consistency.

## Core rules

- Do not broadly scan the repo.
- Do not read entire large files unless required.
- Use targeted search first, then read only the relevant surrounding lines.
- Prefer ±80 lines around a located symbol instead of full-file reads.
- Do not inspect frontend files for backend-only issues.
- Do not inspect migrations unless the change affects persistence shape.
- Do not inspect unrelated V1/V2 planning docs unless needed.
- If both V1 and V2 issue files exist, prefer V2 unless the user says otherwise.
- Do not run tests or builds unless the user asks.

## Backend map

Main API project:

- `src/backend/JobApplicationAssistant.Api`

High-value locations:

- Endpoints: `Endpoints/`
- Contracts: `Contracts/`
- AI providers/options: `Ai/`
- AI prompts: `Ai/Prompts/`
- Domain entities: `Domain/`
- EF DbContext: `Data/ApplicationDbContext.cs`
- Exports: `Exports/`
- Migrations: `Migrations/`

Main workflow files:

- `Endpoints/ApplicationEndpoints.cs`
- `Endpoints/ProfileEndpoints.cs`
- `Endpoints/AiEndpoints.cs`
- `Contracts/ApplicationContracts.cs`
- `Contracts/ProfileContracts.cs`
- `Contracts/AiContracts.cs`
- `Ai/IAiProvider.cs`
- `Ai/FakeAiProvider.cs`
- `Ai/OllamaAiProvider.cs`
- `Domain/JobApplication.cs`
- `Domain/Profile.cs`
- `Domain/ProfileFact.cs`
- `Domain/GeneratedDraft.cs`
- `Domain/AiRun.cs`

Tests:

- `src/backend/JobApplicationAssistant.Api.Tests`

## Workflow

1. Read the requested issue/task.
2. Identify the feature area.
3. If editing docs/issues, inspect the target file first.
4. If the docs/issues formatting convention is unclear, inspect at most 1-2 nearby completed files in the same folder.
5. If editing code, search only in likely folders first.
6. Read only the relevant code ranges.
7. Make the smallest coherent change.
8. Update only directly related tests/docs.
9. Stop.

## Issue completion rule

When asked to mark an issue as done/completed:

- Update the issue status/title if present.
- Mark completed checklist items as `[x]`.
- Preserve the existing formatting style.
- If unsure, inspect 1-2 nearby completed issue files in the same folder.
- Do not scan unrelated issue folders or other versions unless needed.

## Progress output

Keep progress short.

Do not list every command.

Only mention:

- blocker
- risky assumption
- important finding
- final result

## Updating this skill

Do not update this skill for every new file.

Only update it when:

- a new folder becomes a common navigation anchor
- a new file becomes a repeated high-value entry point
- the repo structure changes significantly