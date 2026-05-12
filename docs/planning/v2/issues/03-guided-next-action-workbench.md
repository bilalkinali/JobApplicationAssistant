# Guided Next Action Workbench

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/milestone-1-prd.md

## What to build

Update the application workbench so the user sees one dominant guided next action instead of several equal-weight workflow buttons. The guided action should use the new preparation endpoint when the saved application has a job posting that has not yet been prepared, and it should lead to evidence review once preparation succeeds.

Existing workflow actions may remain as quieter secondary actions for recovery or debugging, but the normal path should feel assistant-led: save the posting, prepare the application, then review evidence.

## Acceptance criteria

- [ ] Application detail computes one guided next action from current application state.
- [ ] An application with no saved job posting guides the user to paste and save the posting.
- [ ] An application with a saved posting and no prepared evidence state shows `Prepare application` as the dominant action.
- [ ] Activating `Prepare application` calls `POST /api/applications/{id}/prepare`.
- [ ] While preparation is running, the UI shows a clear busy state without hiding existing workflow state.
- [ ] Successful preparation refreshes application state and shows evidence review as the next checkpoint.
- [ ] Preparation validation blockers are shown plainly near the guided action.
- [ ] Provider unavailable or invalid-output preparation failures point the user toward AI readiness or diagnostics without hiding existing workflow state.
- [ ] Existing analysis, matching, and evidence review controls do not visually compete with the guided next action.
- [ ] Recent application/workbench surfaces show the next meaningful action where the existing layout naturally supports it.
- [ ] Focused frontend tests cover next-action selection and the prepare action path where the existing frontend test structure allows it.
- [ ] No gap decision UI, draft/audit automation, copy/export guided final action, profile fact import, or visual redesign beyond action hierarchy is introduced.

## Blocked by

- docs/planning/v2/issues/01-prepare-application-orchestration.md
- docs/planning/v2/issues/02-prepare-failure-and-partial-state.md
