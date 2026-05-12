# Guided Next Action Workbench

Status: done
Type: AFK

## Parent

docs/planning/v2/milestone-1-prd.md

## What to build

Update the application workbench so the user sees one dominant guided next action instead of several equal-weight workflow buttons. The guided action should use the new preparation endpoint when the saved application has a job posting that has not yet been prepared, and it should lead to evidence review once preparation succeeds.

Existing workflow actions may remain as quieter secondary actions for recovery or debugging, but the normal path should feel assistant-led: save the posting, prepare the application, then review evidence.

## Acceptance criteria

- [x] Application detail computes one guided next action from current application state.
- [x] An application with no saved job posting guides the user to paste and save the posting.
- [x] An application with a saved posting and no prepared evidence state shows `Prepare application` as the dominant action.
- [x] Activating `Prepare application` calls `POST /api/applications/{id}/prepare`.
- [x] While preparation is running, the UI shows a clear busy state without hiding existing workflow state.
- [x] Successful preparation refreshes application state and shows evidence review as the next checkpoint.
- [x] Preparation validation blockers are shown plainly near the guided action.
- [x] Provider unavailable or invalid-output preparation failures point the user toward AI readiness or diagnostics without hiding existing workflow state.
- [x] Existing analysis, matching, and evidence review controls do not visually compete with the guided next action.
- [x] Recent application/workbench surfaces show the next meaningful action where the existing layout naturally supports it.
- [x] Focused frontend tests cover next-action selection and the prepare action path where the existing frontend test structure allows it.
- [x] No gap decision UI, draft/audit automation, copy/export guided final action, profile fact import, or visual redesign beyond action hierarchy is introduced.

## Blocked by

- docs/planning/v2/issues/01-prepare-application-orchestration.md
- docs/planning/v2/issues/02-prepare-failure-and-partial-state.md
