# Milestone 4 Workflow Polish

Status: done
Type: AFK

## Parent

docs/planning/milestone-4-prd.md

## What to build

Tighten the application detail workflow around generated drafts and claim audit. The workflow should read as a clear sequence from approved evidence to generated text, editable drafts, and claim audit review before final use.

This is a polish and integration slice for Milestone 4. It should expose the completed generation, draft editing, stale audit, and audit result behavior in the desktop-first application detail view without adding new AI capabilities beyond the fake generation and audit behavior already delivered by the blocking issues.

This slice should not add Ollama, diagnostics, export, custom fact normalization, provider JSON repair, or draft history.

## Acceptance criteria

- [x] The application detail view presents draft generation after approved evidence review.
- [x] The application detail view presents claim audit after generated text.
- [x] The frontend can trigger draft generation for a saved application with approved evidence.
- [x] The frontend shows generated cover letter text and short motivation text.
- [x] The frontend lets the user edit and save the cover letter text.
- [x] The frontend lets the user edit and save the short motivation text.
- [x] The frontend shows when claim audit is stale after draft edits.
- [x] The frontend can manually trigger claim audit.
- [x] The frontend shows supported, unsupported, and needs-review audit results clearly.
- [x] Generation, editing, and audit actions have consistent button wording and loading/error states.
- [x] Missing job posting text, missing approved evidence, missing draft, and stale audit states are explained near the relevant workflow step.
- [x] Reopening an application session shows the saved draft and audit state.
- [x] The UI remains desktop-first and consistent with the existing application detail structure.
- [x] No Ollama, diagnostics, export, custom fact normalization, provider JSON repair, or draft history is introduced.

## Blocked by

- docs/planning/issues/13-fake-draft-generation.md
- docs/planning/issues/14-editable-generated-drafts.md
- docs/planning/issues/15-claim-audit.md
