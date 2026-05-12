# V2 Milestone 1 Product Requirements Document

Status: ready-for-agent
Source: docs/planning/v2/v2-specification.md

## Goal

Create the first V2 guided application workbench slice: one clear next action, one backend-owned preparation action, and evidence review as the first human checkpoint.

## Problem Statement

The current app proves the V1 trust workflow, but the workflow is still too exposed. A user has to move through separate actions for job analysis, evidence matching, evidence approval, draft generation, audit, and export. That makes the app feel like a workflow console instead of an assistant that helps handle the application.

The first V2 milestone should reduce the user's operational burden without weakening the trust boundary. The app should guide the user toward the next meaningful action, combine safe backend workflow steps, and stop when user judgment is required.

## Solution

Milestone 1 introduces a guided application flow centered on `Prepare application`.

The application detail view should show one dominant guided next action based on the current application state. When a saved job posting is ready, the guided action should call `POST /api/applications/{id}/prepare`. That backend action should run job analysis and evidence matching together, persist valid workflow state, record preparation status, and return the next checkpoint for the frontend.

The first human checkpoint after preparation remains evidence review. The user still decides which evidence may support the application. Existing manual workflow actions may remain available as secondary actions, but they should not visually compete with the guided next action.

## User Stories

1. As a job applicant, I want the application detail view to show one clear next action, so that I do not have to choose between several workflow buttons.
2. As a job applicant, I want a saved job posting to lead to `Prepare application`, so that I can move from posting capture to evidence review with one primary action.
3. As a job applicant, I want preparation to run job analysis and evidence matching together, so that I do not have to babysit backend-safe steps.
4. As a job applicant, I want the app to stop at evidence review after preparation, so that I still control which evidence may support generated claims.
5. As a job applicant, I want weak or missing setup to be explained plainly, so that I know why preparation cannot run.
6. As a job applicant, I want provider failures to keep my existing workflow state stable, so that a failed AI call does not corrupt my application session.
7. As a job applicant, I want invalid AI output to fail plainly, so that I understand the app could not safely use the provider response.
8. As a job applicant, I want successful preparation to show evidence review as the next checkpoint, so that I know where my judgment is needed.
9. As a job applicant, I want any old analysis and matching buttons to be visually secondary, so that the guided action is the obvious path.
10. As a job applicant, I want recent applications to communicate the next meaningful action where practical, so that I can resume work without inspecting every section.
11. As a developer, I want preparation to be backend-owned orchestration, so that the frontend does not simply hide two separate API calls behind one button.
12. As a developer, I want preparation status saved on the application, so that the UI can explain whether preparation has not started, succeeded, partially succeeded, or failed.
13. As a developer, I want the preparation endpoint to reuse the existing AI provider abstraction, so that fake and Ollama providers keep the same workflow boundary.
14. As a developer, I want preparation to preserve valid analysis when matching fails, so that successful work is not discarded unnecessarily.
15. As a developer, I want preparation failures to follow existing plain AI error behavior, so that provider unavailable and invalid output states remain consistent.
16. As a developer, I want this milestone to avoid profile fact import, draft automation, audit automation, and export redesign, so that the first V2 slice stays focused.

## Implementation Decisions

- Add `POST /api/applications/{id}/prepare`.
- Treat `prepare` as a backend-owned orchestration action that runs job analysis and evidence matching together.
- Require saved job posting text before preparation can run.
- Require at least one approved profile fact before evidence matching can run.
- Reuse the configured AI provider for job analysis and evidence matching.
- Reuse existing validation, strict JSON handling, repair attempt, unavailable-provider behavior, invalid-output behavior, and `AiRun` recording patterns.
- Persist job signals, evidence matches, unmatched requirements, and cleared approved evidence when preparation succeeds.
- Add preparation state to the application latest-state model with `LastPreparedAt` and `PreparationStatus`.
- Use preparation status values: `NotStarted`, `Preparing`, `PreparedForEvidenceReview`, `FailedProviderUnavailable`, `FailedInvalidProviderOutput`, and `PartiallyPreparedAnalysisOnly`.
- If analysis succeeds but matching fails, preserve valid analysis state, mark `PartiallyPreparedAnalysisOnly`, and make the next checkpoint explain that evidence matching needs to be retried.
- If provider output is invalid before safe state can be updated, keep the prior stable workflow state.
- Return response metadata that lets the frontend show the next checkpoint, such as `ReviewEvidence`, provider/setup blocker, or retry preparation.
- Compute one guided next action from application state in the frontend.
- Show `Prepare application` when a saved job posting exists and the application is not currently prepared for evidence review.
- Show evidence review as the next checkpoint after preparation succeeds.
- Keep evidence approval manual; preparation must not automatically approve evidence.
- Existing manual workflow actions may remain as secondary controls for debugging or recovery, but the guided action is the only visually dominant workflow action.

## Testing Decisions

- Test public behavior rather than private implementation details.
- Prioritize backend API tests because `prepare` becomes the stable workflow contract.
- Cover `POST /api/applications/{id}/prepare` rejecting missing job posting text.
- Cover `POST /api/applications/{id}/prepare` rejecting evidence matching when no approved profile facts exist.
- Cover successful preparation running analysis and matching as one action and returning the next checkpoint.
- Cover successful preparation persisting job signals, evidence matches, unmatched requirements, `LastPreparedAt`, and `PreparedForEvidenceReview`.
- Cover successful preparation clearing stale approved evidence from earlier matches.
- Cover provider unavailable behavior setting a failure status, returning a plain error, and keeping existing stable workflow state.
- Cover invalid provider output behavior setting a failure status, returning a plain error, and keeping existing stable workflow state.
- Cover analysis success plus matching failure preserving analysis and marking `PartiallyPreparedAnalysisOnly`.
- Cover frontend next-action logic with focused tests where existing frontend test structure allows it.
- Cover that a saved posting with no preparation shows `Prepare application`.
- Cover that a prepared application shows evidence review as the next checkpoint.
- Cover that provider unavailable state points the user toward AI readiness or diagnostics.

## Out of Scope

- Assisted profile fact import.
- V2.1 imported fact review queue.
- New profile fact statuses.
- Full profile fact revision history.
- Full generated draft version history.
- Automatic evidence approval.
- Gap handling decisions beyond showing unmatched requirements already produced by preparation.
- Draft generation and claim audit automation.
- Stale audit behavior changes.
- Copy/export guided final action.
- Editable AI provider settings.
- OpenAI provider support.
- Authentication or multi-user support.
- Mobile-first redesign.
- Visual redesign beyond making the guided action clear and secondary actions quieter.

## Further Notes

This milestone should prove the core V2 direction without trying to finish all of V2.0. The important product outcome is that the application detail view begins to feel assistant-led: the user sees what to do next, the app handles safe preparation work, and the workflow stops when evidence approval requires human judgment.
