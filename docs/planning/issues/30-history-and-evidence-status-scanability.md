# History and Evidence Status Scanability

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/milestone-7-prd.md

## What to build

Improve scanability for application history, profile fact statuses, and job-local custom fact statuses. The user should be able to distinguish active and archived sessions, filtered empty states, approved evidence, draft evidence, archived evidence, pending custom facts, approved custom facts, and rejected custom facts without studying dense text.

This slice should refine presentation and wording around existing states. It should not add new status values, advanced tracking, kanban, analytics, or history/versioning.

## Acceptance criteria

- [ ] Application history clearly distinguishes active, applied, and archived sessions.
- [ ] Archived applications remain visually distinct when visible.
- [ ] Application history empty states distinguish no applications from no matching filtered results.
- [ ] Application history filters remain easy to scan and do not introduce advanced tracking concepts.
- [ ] Profile fact statuses clearly distinguish Draft, Approved, and Archived.
- [ ] Job-local custom fact statuses clearly distinguish PendingConfirmation, Approved, and Rejected.
- [ ] Evidence review wording reinforces that only approved evidence should support generated claims.
- [ ] Status presentation stays consistent with the existing desktop-first workbench style.
- [ ] Destructive actions in these areas are clearly labeled and confirmable where the existing flow supports it.
- [ ] Tests are added only for extracted status-formatting helpers or changed public contracts.
- [ ] No new workflow status values, application event history, analytics, kanban, or profile fact revision history is introduced.

## Blocked by

None - can start immediately.
