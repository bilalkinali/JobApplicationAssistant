# Cover Letter TXT Export

Status: done
Type: AFK

## Parent

docs/planning/milestone-6-prd.md

## What to build

Add plain-text export for the current cover letter. The user should be able to download the current edited cover letter as a `.txt` file from a saved application session with a generated draft. The export should use the current generated draft as the source of truth, never call an AI provider, and fail plainly when there is no application, no generated draft, or no cover letter text.

This slice should add the backend TXT export path and any shared export contract needed by callers. Frontend export controls are owned by the Milestone 6 export and history polish slice. This slice should not add DOCX export, PDF export, custom templates, regeneration, or draft history.

Use TDD for this slice: start with a failing public endpoint test for TXT export, implement the smallest passing export path, then add blocker and filename behaviors one red-green-refactor cycle at a time.

## Acceptance criteria

- [x] Tests assert observable response behavior such as status code, content type, content disposition, and text body.
- [x] A public TXT export endpoint returns the current edited cover letter text for an application with a generated draft.
- [x] TXT export uses the current saved draft text and does not regenerate, re-audit, or call an AI provider.
- [x] TXT export returns a safe readable filename based on company and role metadata, with a stable fallback.
- [x] TXT export is blocked with a plain not-found error when the application does not exist.
- [x] TXT export is blocked with a plain validation error when no generated draft exists.
- [x] TXT export is blocked with a plain validation error when the cover letter text is empty or whitespace.
- [x] TXT export remains allowed when claim audit is stale.
- [x] TXT export remains allowed when claim audit has not been run.
- [x] No frontend export controls, DOCX export, PDF export, custom templates, regeneration, AI provider calls, or draft history are introduced.

## Blocked by

None - can start immediately.

## Comments

Marked done after `dotnet build src\JobApplicationAssistant.Api.sln` and `dotnet test src\backend\JobApplicationAssistant.Api.Tests\JobApplicationAssistant.Api.Tests.csproj` passed.
