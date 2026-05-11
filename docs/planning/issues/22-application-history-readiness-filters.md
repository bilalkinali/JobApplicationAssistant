# Application History Readiness and Filters

Status: in-progress
Type: AFK

## Parent

docs/planning/milestone-6-prd.md

## What to build

Improve the application list into the V1 application history surface. The user should be able to find saved application sessions by company or role, filter by workflow status and draft/audit readiness, hide archived sessions by default, and see whether each session has a generated draft and whether its claim audit is current, stale, missing, or not applicable.

This slice should keep application sessions as latest-state records. It should not add export endpoints, draft history, application event history, custom DOCX templates, or advanced tracking.

Use TDD for this slice: start with one failing behavior test for the public application list/history behavior, implement the smallest passing change, then repeat red-green-refactor for each additional behavior.

## Acceptance criteria

- [ ] The implementation starts with a failing test for one public application history behavior before production code changes.
- [ ] Each additional history/filter behavior is added through a red-green-refactor cycle, one behavior at a time.
- [x] Tests verify observable behavior through public API responses and natural UI-facing contracts, not private implementation details.
- [x] The application history remains ordered by most recent update.
- [x] Each application history item exposes enough summary information for company, role, status, selected language, deadline, updated date, generated draft existence, and audit readiness.
- [x] Audit readiness distinguishes current, stale, missing, and not applicable states.
- [x] The frontend can search application history by company and role.
- [x] The frontend can filter application history by status.
- [x] The frontend can filter application history by draft/audit readiness.
- [x] Archived applications are hidden from the active history view unless explicitly included.
- [x] Filtered empty states distinguish no applications from no matching applications.
- [x] Existing application detail behavior continues to work with the application selected from history.
- [x] No export endpoints, draft history, application event history, custom DOCX templates, or advanced tracking are introduced.

## Blocked by

None - can start immediately.
