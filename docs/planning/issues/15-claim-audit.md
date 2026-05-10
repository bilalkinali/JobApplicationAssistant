# Claim Audit

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/milestone-4-prd.md

## What to build

Add deterministic claim audit for the current generated draft. The user should be able to manually run audit for an application draft and receive structured results that identify supported, unsupported, and needs-review claims.

Audit should extend the AI provider abstraction and fake provider with deterministic behavior. Results should be stored as structured JSON on `GeneratedDraft`, include audit timestamps, reference approved evidence where possible, and clear the stale state for the current draft text when the audit is rerun after edits.

This slice should not add Ollama, diagnostics, strict JSON validation, repair attempts, export, or draft history.

## Acceptance criteria

- [ ] The AI provider abstraction supports claim audit.
- [ ] The fake provider produces deterministic claim audit results.
- [ ] The public API supports manually running claim audit for the current generated draft.
- [ ] Claim audit is blocked with a plain validation error when no generated draft exists.
- [ ] Claim audit compares draft text against approved evidence.
- [ ] Claim audit results can classify claims as supported.
- [ ] Claim audit results can classify claims as unsupported.
- [ ] Claim audit results can classify claims as needs-review.
- [ ] Supported audit results reference approved evidence where possible.
- [ ] Claim audit results are stored as structured JSON on `GeneratedDraft`.
- [ ] Running claim audit updates the audit timestamp.
- [ ] Running claim audit after manual edits clears the stale state for the current draft text.
- [ ] Backend tests cover supported, unsupported, needs-review, persisted audit JSON, audit timestamp updates, and clearing stale audit state after rerun.
- [ ] No Ollama, diagnostics, strict JSON validation, repair attempts, export, or draft history is introduced.

## Blocked by

- docs/planning/issues/14-editable-generated-drafts.md
