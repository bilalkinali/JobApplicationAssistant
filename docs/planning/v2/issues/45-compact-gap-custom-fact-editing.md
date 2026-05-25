# Compact Gap Custom Fact Editing

Status: done
Type: AFK

## Parent

docs/testing/manual-QA-result/latest-application-ui-handoff.md

## What to build

Reduce noise in unmatched requirement cards by hiding custom fact fields until the user chooses a custom-fact path. Gap cards should start as compact decision surfaces, then reveal custom fact title, summary, technologies, allowed claims, and save controls only when the user chooses `Covered by custom fact`, `Add custom fact`, or the equivalent existing action.

The slice should preserve the ability to add job-local custom facts while avoiding repeated full forms across many gaps in the default review state.

## Acceptance criteria

- [x] Unmatched requirement cards do not show custom fact form fields by default.
- [x] Choosing a custom-fact decision reveals the relevant custom fact fields for that gap.
- [x] Saved learning-interest or ignored gaps render as compact summaries instead of full editors.
- [x] At most one gap custom fact editor is expanded by default where the existing interaction model supports it.
- [x] Existing custom fact validation and persistence behavior are preserved.
- [x] The evidence review page remains usable on narrow viewports without long repeated form regions dominating the page.

## Blocked by

- docs/planning/v2/issues/44-evidence-approval-immediate-feedback.md
