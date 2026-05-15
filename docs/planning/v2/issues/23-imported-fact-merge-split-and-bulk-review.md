# Imported Fact Merge Split And Bulk Review

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/v2.1-assisted-profile-import-prd.md

## What to build

Add faster review controls for larger imports. A user can merge overlapping imported facts, split an overbroad imported fact, bulk approve selected facts, and bulk archive selected facts while preserving the same trust boundary and source context expectations.

This slice should make review efficient after a real CV import produces many useful facts.

## Acceptance criteria

- [ ] A user can merge selected imported draft facts into a single draft fact before approval.
- [ ] Merged facts preserve useful source context and duplicate metadata where practical.
- [ ] A user can split an imported draft fact into multiple narrower draft facts before approval.
- [ ] Split facts preserve useful source context where practical.
- [ ] A user can bulk approve selected imported draft facts.
- [ ] A user can bulk archive selected imported draft facts.
- [ ] Bulk actions do not approve duplicate or archived facts unexpectedly.
- [ ] The frontend review queue supports merge, split, bulk approve, and bulk archive without requiring a full page reload.
- [ ] Tests cover merge, split, bulk approve, bulk archive, and trust-boundary behavior after each action.

## Blocked by

- docs/planning/v2/issues/22-imported-fact-review-decisions.md
