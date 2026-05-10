# Basic Profile and Application CRUD

Status: done
Type: AFK

## Parent

docs/planning/v1-prd.md

## What to build

Implement the first usable end-to-end CRUD paths for the V1 foundation: profile contact/preferences management and basic application session management. The backend should expose the basic profile and application endpoints from the specification, and the frontend should provide simple screens or forms that exercise those APIs.

This slice is intentionally limited to basic CRUD. It should not implement manual profile fact UI, AI job analysis, evidence matching, draft generation, claim audit, exports, Ollama, or fake AI behavior.

## Acceptance criteria

- [x] The backend supports reading and updating the single profile record.
- [x] The backend supports listing, creating, reading, updating, and deleting application sessions.
- [x] Application sessions can store company name, role title, application URL, deadline, status, job posting text, detected language, and selected language.
- [x] The frontend includes a simple profile area for viewing and editing contact information and tone/language preferences.
- [x] The frontend includes a simple applications area for creating, listing, opening, editing, and deleting application sessions.
- [x] Basic validation prevents saving obviously invalid profile or application payloads.
- [x] API errors are returned in a plain, predictable shape suitable for later UI error handling.
- [x] The implementation remains compatible with later profile facts, AI workflow, generated draft, and export slices.

## Blocked by

- docs/planning/issues/02-configure-postgresql-ef-core-and-core-entities.md
