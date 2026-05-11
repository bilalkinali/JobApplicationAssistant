# Editable Generated Drafts

Status: done
Type: AFK

## Parent

docs/planning/milestone-4-prd.md

## What to build

Add latest-state draft reading and manual editing for generated cover letter and short motivation text. The user should be able to reopen an application session, see the current generated draft, edit either text field, and save the edits without losing the draft.

Manual edits should update draft edit metadata and mark any existing claim audit stale, so the user can tell that the prior audit no longer describes the current text. This slice should prepare the stale-state behavior even if the full claim audit UI arrives in the next slice.

This slice should not add claim audit execution, Ollama, diagnostics, export, or draft history.

## Acceptance criteria

- [x] The application detail API response includes the current generated draft when one exists.
- [x] The public API supports saving manual edits to the current generated draft.
- [x] The cover letter text can be manually edited and saved.
- [x] The short motivation text can be manually edited and saved.
- [x] Manual draft edits update edit and updated timestamps.
- [x] Manual draft edits preserve generated timestamps.
- [x] Manual draft edits mark existing audit data stale.
- [x] Reopening an application session shows the latest saved draft text.
- [x] Saving edits fails plainly when no generated draft exists for the application.
- [x] Backend tests cover reading the current generated draft, saving manual edits, timestamp behavior, and stale audit marking.
- [x] No claim audit execution, Ollama, diagnostics, export, or draft history is introduced.

## Blocked by

- docs/planning/issues/13-fake-draft-generation.md
