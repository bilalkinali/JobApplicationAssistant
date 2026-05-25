# Evidence Approval Immediate Feedback

Status: done
Type: AFK

## Parent

docs/testing/manual-QA-result/latest-application-ui-handoff.md

## What to build

Improve evidence approval feedback inside unmatched requirement review so approving a suggested evidence item produces immediate visible confirmation at the clicked location. The user should not need to scroll to the approved evidence list to know that the action worked.

The slice should also make the post-save evidence review state easier to scan by favoring compact saved gap decisions over fully editable gap cards until the user explicitly chooses to edit the review.

## Acceptance criteria

- [x] Approving a suggested evidence item updates the clicked card immediately with an approved state.
- [x] Approved evidence actions are disabled, replaced with `Remove from approved`, or otherwise clearly changed after approval.
- [x] A nearby approved evidence count or equivalent local summary updates immediately after approval.
- [x] Saved gap decisions are shown as compact summary cards by default after evidence review has been saved.
- [x] Full editable unmatched requirement cards are available behind an `Edit evidence review` action or equivalent.
- [x] Existing evidence approval persistence and draft generation inputs continue to use the saved approved evidence.

## Blocked by

None - can start immediately
