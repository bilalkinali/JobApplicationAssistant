# Prepare Application Orchestration

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/milestone-1-prd.md

## What to build

Add the V2 preparation path that lets a saved application move from job posting to prepared evidence review with one backend-owned action. Calling `POST /api/applications/{id}/prepare` should run job analysis and evidence matching together, persist the prepared workflow state, and return the next checkpoint for the frontend.

The slice should prove that preparation is not just two frontend-triggered calls hidden behind one button. The backend owns the orchestration, records preparation state on the application, and keeps evidence approval manual.

## Acceptance criteria

- [ ] `POST /api/applications/{id}/prepare` exists and is the public orchestration action for preparation.
- [ ] Preparation requires saved job posting text and returns a plain validation error when it is missing.
- [ ] Preparation requires at least one approved profile fact before evidence matching can run and returns a plain validation error when none exist.
- [ ] A valid preparation run performs job analysis and evidence matching through the configured AI provider in one backend-owned action.
- [ ] A successful preparation persists job signals, evidence matches, unmatched requirements, cleared approved evidence, `LastPreparedAt`, and `PreparationStatus`.
- [ ] A successful preparation marks the application `PreparedForEvidenceReview`.
- [ ] A successful preparation returns checkpoint metadata that lets the frontend show evidence review as the next action.
- [ ] Existing manual analysis and matching endpoints remain available.
- [ ] Backend API tests cover the valid path and validation blockers.
- [ ] No evidence is automatically approved.
- [ ] No assisted profile fact import, gap decision handling, draft generation, audit automation, export redesign, or new profile fact statuses are introduced.

## Blocked by

None - can start immediately.
