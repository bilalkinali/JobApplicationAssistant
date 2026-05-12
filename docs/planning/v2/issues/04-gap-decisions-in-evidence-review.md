# Gap Decisions in Evidence Review

Status: in-progress
Type: AFK

## Parent

docs/planning/v2/milestone-2-prd.md

## What to build

Extend evidence review so unmatched requirements appear in the same checkpoint as matched evidence, and the user can make lightweight decisions for each gap.

This slice should let the user review matched evidence as before while also marking unmatched requirements as ignored or as safe to mention cautiously as learning interest. Those decisions should persist with the application workflow state and affect the guided next action: unresolved gaps keep the user at evidence review, while resolved gaps allow the workflow to move toward draft generation when approved evidence exists.

## Acceptance criteria

- [x] Evidence review shows unmatched requirements alongside matched evidence in one review flow.
- [x] Matched evidence approval remains available and does not require using gap handling.
- [x] Each unmatched requirement can be marked `Ignore`.
- [x] Each unmatched requirement can be marked `MentionAsLearningInterest`.
- [x] Gap decisions persist and survive page refreshes.
- [x] Updating an existing gap decision replaces the earlier decision for that unmatched requirement.
- [x] Unresolved unmatched requirements keep evidence review as the guided next action.
- [x] Resolved unmatched requirements allow the workflow to move toward draft generation when approved evidence exists.
- [x] The UI distinguishes approved evidence from learning-interest gap handling.
- [ ] Focused backend and frontend tests cover saving, updating, displaying, and using gap decisions for guided next action selection.
- [x] No job-local custom fact creation, draft generation changes, provider UX redesign, or profile fact import is introduced.

## Blocked by

None - can start immediately.
