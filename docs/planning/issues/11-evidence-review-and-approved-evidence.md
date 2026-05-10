# Evidence Review and Approved Evidence

Status: done
Type: AFK

## Parent

docs/planning/milestone-3-prd.md

## What to build

Add the evidence review step that lets the user approve or remove matched evidence before any future generation step can use it. The reviewed result should be saved as approved evidence on the application session.

This slice makes the user the final decision-maker for evidence. Fake keyword matches can suggest evidence, but they should not automatically become approved evidence for generation.

## Acceptance criteria

- [x] The backend supports updating approved evidence for an application.
- [x] Approved evidence is persisted as latest-state workflow data on the application session.
- [x] Approved evidence entries preserve enough context to trace back to the matched job signal and supporting profile fact.
- [x] The frontend lets the user approve relevant matched evidence.
- [x] The frontend lets the user remove irrelevant matched evidence before saving approved evidence.
- [x] The frontend displays the currently approved evidence for the application.
- [x] Saving approved evidence uses plain validation or API errors when the submitted review state is invalid.
- [x] Existing job analysis and evidence matching data remain visible after approved evidence is saved.
- [x] No draft generation, claim audit, custom fact normalization, Ollama, diagnostics, or export behavior is introduced.

## Blocked by

- docs/planning/issues/10-fake-ai-evidence-matching.md
