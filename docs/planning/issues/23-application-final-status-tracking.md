# Application Final Status Tracking

Status: done
Type: AFK

## Parent

docs/planning/milestone-6-prd.md

## What to build

Make final application tracking states simple and explicit. The user should be able to mark an application session as Applied or Archived after using it, and the history surface should reflect those states without disrupting the existing workflow-driven status changes for Draft, PostingCaptured, and ReadyForReview.

This slice should preserve the current status vocabulary and latest-state model. It should not add kanban behavior, analytics, application event history, or version history.

Use TDD for this slice: begin with a failing public behavior test for changing a saved application's final status, then implement the smallest passing change. Add follow-up behaviors one red-green-refactor cycle at a time.

## Acceptance criteria

- [x] Tests verify public behavior, including persisted status and updated history metadata, rather than internal mutation details.
- [x] The existing status vocabulary remains Draft, PostingCaptured, ReadyForReview, Applied, and Archived.
- [x] A user can explicitly mark an application as Applied.
- [x] A user can explicitly mark an application as Archived.
- [x] Final status changes update the application updated date.
- [x] Workflow actions still preserve their existing automatic status behavior.
- [x] The application history surface reflects Applied and Archived states clearly.
- [x] Archived applications remain available when the history view includes archived sessions.
- [x] Invalid statuses still return plain validation errors.
- [x] No kanban behavior, analytics, application event history, draft history, or version comparison is introduced.

## Blocked by

None - can start immediately.
