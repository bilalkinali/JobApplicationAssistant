# Workflow Error Details and Provider Polish

Status: done
Type: AFK

## Parent

docs/planning/milestone-7-prd.md

## What to build

Improve user-facing error presentation for AI actions, diagnostics, saves, copy, and export attempts. The user should see plain errors that distinguish validation blockers, provider unavailability, invalid AI output, and unexpected failures, with expandable technical details where useful.

This slice should preserve existing backend-owned provider behavior and must not expose raw provider request or response payloads in the frontend.

## Acceptance criteria

- [x] AI action errors distinguish provider unavailable failures from validation blockers.
- [x] Invalid AI output errors are described plainly and do not imply that saved workflow state was corrupted.
- [x] Settings AI diagnostics shows consistent loading, success, unavailable, and error states.
- [x] Error presentation can include expandable technical details where diagnostics are useful.
- [x] Frontend error details do not expose raw provider requests or raw provider responses.
- [x] Existing application workflow state remains visible after failed AI actions.
- [x] Existing settings AI state remains visible after failed diagnostics.
- [x] Save, copy, and export failures use the same plain error presentation pattern where practical.
- [x] Fake provider status remains clearly identified as deterministic fake behavior.
- [x] Tests are added only for stable error classification helpers or public API contract changes.
- [x] No provider settings editing, streaming generation, raw payload display, or new AI capability is introduced.

## Blocked by

None - can start immediately.
