# Prepared Application Final Review First Layout

Status: done
Type: AFK

## Parent

docs/testing/manual-QA-result/latest-application-ui-handoff.md

## What to build

Change the application detail page default layout for prepared applications with an existing generated draft so the final review task appears before historical preparation context. The top of the page should make the current blocker obvious when draft quality prevents export, and it should guide the user toward revising or regenerating the draft before copy/export actions.

The slice should preserve access to preparation, evidence, and strategy context, but those completed sections should no longer dominate the default review state once the user has a draft.

## Acceptance criteria

- [x] For a prepared application with a generated draft and `NeedsRevision` quality, the top workflow summary names revise/regenerate draft as the next action instead of copy/export.
- [x] The first viewport shows why export is blocked when draft quality is `NeedsRevision`.
- [x] Draft quality, regenerate/save controls, cover letter editor, short motivation editor, export state, and claim audit summary appear before completed preparation details.
- [x] Completed preparation sections remain accessible from the application detail page.
- [x] The page has one visually dominant current-action section in the prepared-with-draft state.
- [x] Existing draft copy/export behavior remains available once draft quality and audit state allow it.

## Blocked by

None - can start immediately
