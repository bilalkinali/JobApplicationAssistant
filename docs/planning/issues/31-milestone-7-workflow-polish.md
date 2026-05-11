# Milestone 7 Workflow Polish

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/milestone-7-prd.md

## What to build

Make the completed Milestone 7 polish feel coherent across the home workbench, profile, application history, application detail, settings AI, and export surfaces. The user should experience consistent warning, blocker, loading, disabled, empty, success, and error states throughout the V1 workflow.

This is an integration polish slice. It should align the completed narrower Milestone 7 improvements without adding new product capabilities or broad redesign work.

## Acceptance criteria

- [ ] Shared presentation patterns for warnings, blockers, errors, loading states, empty states, and status badges feel consistent across major screens.
- [ ] Disabled actions explain why they are disabled when the reason is not obvious.
- [ ] Loading states for AI actions, diagnostics, saves, copy, and export are predictable and do not obscure existing workflow state unnecessarily.
- [ ] Form validation messages appear close to the relevant fields.
- [ ] Desktop layouts use space efficiently for profile facts, evidence review, drafts, audits, history, and settings.
- [ ] Text does not overlap controls or get clipped in the main desktop workflow surfaces.
- [ ] Copy and download actions provide clear success or failure feedback.
- [ ] The trust chain from approved evidence to generated draft to claim audit to export is legible.
- [ ] The UI remains desktop-first and consistent with the existing workbench structure.
- [ ] Tests are added only where integration changes affect shared behavior or stable contracts.
- [ ] No new AI provider capability, provider settings editing, export format, history/versioning, analytics, CV import, authentication, mobile-first redesign, or full visual redesign is introduced.

## Blocked by

- docs/planning/issues/27-setup-readiness-and-validation-blockers.md
- docs/planning/issues/28-workflow-error-details-and-provider-polish.md
- docs/planning/issues/29-audit-and-export-action-clarity.md
- docs/planning/issues/30-history-and-evidence-status-scanability.md
