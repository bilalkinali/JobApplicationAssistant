# Milestone 3 Workflow Polish

Status: done
Type: AFK

## Parent

docs/planning/milestone-3-prd.md

## What to build

Tighten the application detail workflow around the completed fake AI analysis, evidence matching, unmatched requirement handling, and approved evidence review. The workflow should read as a clear sequence from pasted posting to reviewed evidence, while making missing prerequisites and disabled actions obvious.

This is a polish and integration slice for Milestone 3. It should not add new AI capabilities beyond the fake analysis, matching, and review behavior already delivered by the blocking issues.

## Acceptance criteria

- [x] The application detail view presents job posting, analysis, evidence matches, unmatched requirements, and approved evidence in a clear workflow order.
- [x] Analysis, matching, and review actions have consistent button wording and loading/error states.
- [x] Missing job posting text, missing analysis, and missing approved profile facts are explained near the relevant action.
- [x] Unmatched requirements remain visible after evidence review.
- [x] Approved evidence remains visible when reopening an application session.
- [x] Empty states guide the user toward the next workflow action.
- [x] The UI remains desktop-first and consistent with the Milestone 2 application detail structure.
- [x] No draft generation, claim audit, custom fact normalization, Ollama, diagnostics, export, or advanced job tracking UI is introduced.

## Blocked by

- docs/planning/issues/09-fake-ai-job-analysis.md
- docs/planning/issues/10-fake-ai-evidence-matching.md
- docs/planning/issues/11-evidence-review-and-approved-evidence.md
