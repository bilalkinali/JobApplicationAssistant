# Draft Review Audit Staleness and Guided Actions

Status: done
Type: AFK

## Parent

docs/planning/v2/milestone-3-prd.md

## What to build

Complete the draft review checkpoint by keeping claim audit status honest after manual edits and using it to drive the guided next action. After generation and audit, the app should stop at draft review. When the user edits draft text, the audit becomes stale and the guided next action becomes refresh audit. When the audit is current, copy/export becomes the guided final action.

## Acceptance criteria

- [x] After successful assistant-led generation and audit, the workflow stops at draft review.
- [x] Draft review shows the generated cover letter, short motivation text, and current audit feedback.
- [x] Manual edits to cover letter or short motivation text mark the audit stale.
- [x] A stale audit makes `Refresh claim audit` the guided next action.
- [x] Refreshing audit updates the current audit state for the edited draft.
- [x] Unsupported or weak claims remain visible in audit feedback after refresh.
- [x] A current audit makes copy/export the guided next action.
- [x] Existing copy/export mechanisms remain available without export format redesign.
- [x] Focused tests cover manual edit staleness, audit refresh, current-audit next action, and copy/export next action.

## Blocked by

- docs/planning/v2/issues/09-assistant-led-draft-generation-and-audit.md
