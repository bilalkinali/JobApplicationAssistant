# Application Workflow State

Status: done
Type: AFK

## Parent

docs/planning/milestone-2-prd.md

## What to build

Improve application sessions from basic CRUD records into clearer workflow sessions. Application status should describe early manual progress, and the application list/detail surfaces should show enough metadata to make saved sessions easy to scan.

This slice should not add AI workflow actions or generated drafts.

## Acceptance criteria

- [x] Application status supports explicit workflow values suitable for the manual milestone.
- [x] The backend validates application status values.
- [x] Application list responses include company, role, status, selected language, detected language, deadline, created date, and updated date.
- [x] The frontend application list displays company, role, status, language, and updated date.
- [x] The frontend supports simple filtering by status and search text.
- [x] Existing create, open, edit, and delete behavior continues to work with the richer state.
- [x] API errors use the existing plain error shape.

## Blocked by

None - can start immediately.
