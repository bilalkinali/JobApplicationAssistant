# Setup Readiness and Validation Blockers

Status: done
Type: AFK

## Parent

docs/planning/milestone-7-prd.md

## What to build

Tighten setup readiness and validation blocker presentation across the home workbench, profile area, and application detail workflow. The user should understand when profile/contact setup is merely incomplete, when approved evidence is missing, and when an action is truly blocked by V1 workflow rules.

This slice should keep warnings non-blocking unless the existing workflow already requires a hard blocker. It should not add new profile requirements, new AI behavior, provider settings editing, or export formats.

## Acceptance criteria

- [x] Profile readiness language is consistent between the home workbench and profile area.
- [x] Missing contact details are presented as warnings, not blockers, unless a specific existing action requires them.
- [x] Missing approved profile facts are presented as a readiness warning before generation.
- [x] Setup warnings guide the user toward the relevant profile or fact area.
- [x] The application detail workflow clearly explains blockers for missing job posting text.
- [x] The application detail workflow clearly explains blockers for missing approved profile facts or approved custom facts.
- [x] Warnings do not block creating, editing, or reviewing applications.
- [x] Disabled AI workflow actions explain missing-data blockers without taking ownership of provider error presentation.
- [x] Existing workflow state remains visible while readiness warnings and blockers are shown.
- [x] Tests are added only for extracted readiness/blocker helpers or changed public contracts.
- [x] No new AI provider behavior, export format, CV import, history model, or mobile-first redesign is introduced.

## Blocked by

None - can start immediately.
