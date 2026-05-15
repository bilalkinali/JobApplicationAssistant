# Assistant-Led Draft Generation and Audit

Status: done
Type: AFK

## Parent

docs/planning/v2/milestone-3-prd.md

## What to build

Add or extend a backend-owned guided draft action that runs draft generation and claim audit together once evidence review is complete. The user should see one primary action after evidence review, not separate generation and audit plumbing.

The action should generate cover letter and short motivation text from approved profile facts, approved job-local custom facts, and explicit gap decisions. When generation succeeds, the action should run claim audit against the generated draft when provider availability and inputs allow it, persist the current draft and audit state, and stop at draft review.

## Acceptance criteria

- [x] Evidence review completion makes draft generation the guided next action when no current draft exists.
- [x] Draft generation is blocked until approved evidence exists and unmatched requirements have explicit handling decisions.
- [x] The guided draft action generates both cover letter and short motivation text.
- [x] Approved profile facts are included as approved evidence for generation.
- [x] Approved job-local custom facts are included only for their owning application.
- [x] Unapproved job-local custom facts are excluded from generation evidence.
- [x] `MentionAsLearningInterest` decisions can guide cautious wording but are not passed as approved evidence.
- [x] Ignored gaps are not emphasized unless independently supported by approved evidence.
- [x] Claim audit runs after successful generation when provider availability and inputs allow it.
- [x] The generated draft and current audit state are persisted as latest application state.
- [x] The frontend exposes this as one guided primary action rather than separate equally weighted generation and audit actions.
- [x] Focused backend tests cover successful generation, audit orchestration, generation inputs, and persisted current state.

## Blocked by

None - can start immediately
