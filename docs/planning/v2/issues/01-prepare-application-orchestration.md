# Prepare Application Orchestration

Status: done
Type: AFK

## Parent

docs/planning/v2/milestone-1-prd.md

## What to build

Add the V2 preparation path that lets a saved application move from job posting to prepared evidence review with one backend-owned action. Calling `POST /api/applications/{id}/prepare` should run job analysis and evidence matching together, persist the prepared workflow state, and return the next checkpoint for the frontend.

The slice should prove that preparation is not just two frontend-triggered calls hidden behind one button. The backend owns the orchestration, records preparation state on the application, and keeps evidence approval manual.

## Acceptance criteria

- [x] `POST /api/applications/{id}/prepare` exists and is the public orchestration action for preparation.
- [x] Preparation requires saved job posting text and returns a plain validation error when it is missing.
- [x] Preparation requires at least one approved profile fact before evidence matching can run and returns a plain validation error when none exist.
- [x] A valid preparation run performs job analysis and evidence matching through the configured AI provider in one backend-owned action.
- [x] A successful preparation persists job signals, evidence matches, unmatched requirements, cleared approved evidence, `LastPreparedAt`, and `PreparationStatus`.
- [x] A successful preparation marks the application `PreparedForEvidenceReview`.
- [x] A successful preparation returns checkpoint metadata that lets the frontend show evidence review as the next action.
- [x] Existing manual analysis and matching endpoints remain available.
- [x] Backend API tests cover the valid path and validation blockers.
- [x] No evidence is automatically approved.
- [x] No assisted profile fact import, gap decision handling, draft generation, audit automation, export redesign, or new profile fact statuses are introduced.

## Blocked by

None - can start immediately.
