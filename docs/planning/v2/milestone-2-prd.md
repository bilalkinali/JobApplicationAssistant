# V2 Milestone 2 Product Requirements Document

Status: ready-for-agent
Source: docs/planning/v2/v2-specification.md

## Goal

Make evidence review handle both matched evidence and unmatched requirements in one human checkpoint, so the user can approve proof, handle gaps, and safely move toward draft generation without overstating experience.

## Problem Statement

Milestone 1 makes the app assistant-led up to evidence review, but the review checkpoint is still incomplete if it only asks the user to approve matched profile facts. Job postings often contain requirements that the profile does not clearly cover. If those gaps are left as passive analysis output, the user must mentally track what to ignore, what to frame as learning interest, and what could be covered by a job-specific fact.

The user needs one evidence review flow that makes matches fast to approve while also turning unmatched requirements into explicit, lightweight decisions. Those decisions must help draft generation avoid unsupported claims without weakening the rule that generated concrete claims need approved evidence.

## Solution

Milestone 2 expands evidence review into a complete gap-handling checkpoint.

The evidence review flow should show matched evidence and unmatched requirements together. The user can approve or remove matched evidence, then decide how each unmatched requirement should be handled: ignore it, mention it cautiously as a learning interest, or cover it with a reviewed job-local custom fact.

Gap handling should be persisted as lightweight latest-state workflow data and included in draft generation. `MentionAsLearningInterest` may guide cautious wording, but it must never become approved evidence. Only approved profile facts and approved job-local custom facts may support concrete generated claims.

## User Stories

1. As a job applicant, I want matched evidence approval to stay fast, so that evidence review does not become slower after gap handling is added.
2. As a job applicant, I want unmatched requirements shown in the same review flow as matched evidence, so that I can make all trust decisions in one place.
3. As a job applicant, I want to ignore an unmatched requirement, so that irrelevant or low-priority gaps do not distract from the application.
4. As a job applicant, I want to mark an unmatched requirement as a learning interest, so that the generated draft can mention growth interest without claiming experience I do not have.
5. As a job applicant, I want to cover an unmatched requirement with a job-local custom fact, so that application-specific experience can be used after I review it.
6. As a job applicant, I want job-local custom facts to require review before they support generation, so that quick notes do not silently become proof.
7. As a job applicant, I want rejected or unapproved job-local custom facts to stay out of generated claims, so that unsupported information is not used accidentally.
8. As a job applicant, I want gap decisions to survive page refreshes, so that I do not lose review work while preparing an application.
9. As a job applicant, I want draft generation to respect ignored gaps, so that the draft does not call attention to requirements I chose not to address.
10. As a job applicant, I want draft generation to respect learning-interest gaps, so that it can use cautious language without pretending the gap is covered.
11. As a job applicant, I want draft generation to use approved job-local custom facts as evidence, so that relevant job-specific information can strengthen the draft.
12. As a job applicant, I want the UI to distinguish approved evidence from learning-interest notes, so that I understand what can support claims.
13. As a job applicant, I want the guided next action to remain evidence review while unresolved gap decisions require my judgment, so that the app does not push me into generation too early.
14. As a job applicant, I want to proceed when gaps are intentionally ignored or marked as learning interest, so that perfect evidence coverage is not required.
15. As a job applicant, I want claim audit to continue flagging unsupported claims, so that gap handling does not bypass the final trust check.
16. As a developer, I want gap handling stored as latest-state workflow data, so that Milestone 2 stays aligned with the V2 storage model.
17. As a developer, I want gap handling attached to unmatched requirements rather than a separate planning system, so that the feature remains lightweight.
18. As a developer, I want job-local custom facts scoped to one application, so that they do not pollute the reusable profile fact library.
19. As a developer, I want draft generation to receive explicit gap decisions, so that provider prompts can avoid unsupported claims consistently.
20. As a developer, I want learning-interest decisions to be excluded from approved evidence inputs, so that the trust boundary is enforceable in code and tests.
21. As a developer, I want existing fake AI behavior to cover gap-handling paths deterministically, so that functional tests and demos remain reliable.
22. As a developer, I want this milestone to avoid broader draft automation, provider UX redesign, and profile import, so that the slice stays focused on the evidence checkpoint.

## Implementation Decisions

- Expand evidence review so matched evidence and unmatched requirements are reviewed in one flow.
- Keep matched evidence approval behavior fast and clear.
- Add lightweight handling state for unmatched requirements.
- Support gap decisions: `Ignore`, `MentionAsLearningInterest`, and `CoveredByCustomFact`.
- Store each gap decision against the relevant unmatched requirement or job signal.
- Preserve latest-state workflow storage; do not introduce full gap decision history.
- Add job-local custom facts scoped to a single application.
- Require job-local custom facts to be reviewed and approved before they can support generated claims.
- Treat approved job-local custom facts as evidence only for their owning application.
- Keep unapproved, rejected, or draft job-local custom facts out of evidence matching and generated claim support.
- Feed gap decisions into draft generation.
- Feed approved job-local custom facts into draft generation alongside approved profile facts.
- Ensure `MentionAsLearningInterest` can influence cautious draft language but cannot be passed as approved evidence.
- Ensure `Ignore` tells draft generation not to address the requirement unless supported by other approved evidence.
- Keep the guided next action at evidence review while matched evidence or gap decisions still require user judgment.
- Allow generation once evidence is approved and all unmatched requirements have an explicit handling decision.
- Preserve claim audit as the final unsupported-claim safety net.
- Keep fake AI deterministic across the new gap-handling and generation inputs.

## Testing Decisions

- Test public workflow behavior rather than private implementation details.
- Prioritize backend tests for persistence and generation input contracts because those enforce the trust boundary.
- Cover saving gap decisions for unmatched requirements.
- Cover updating gap decisions after an earlier decision was saved.
- Cover approving, rejecting, and using job-local custom facts within one application.
- Cover job-local custom facts not appearing as evidence for other applications.
- Cover `MentionAsLearningInterest` being available to generation as cautious context but not as approved evidence.
- Cover `Ignore` excluding the unmatched requirement from generation guidance unless independently supported.
- Cover `CoveredByCustomFact` requiring an approved job-local custom fact before it can support generated claims.
- Cover draft generation receiving approved profile facts, approved job-local custom facts, and gap handling decisions together.
- Cover draft generation excluding unapproved job-local custom facts.
- Cover claim audit still identifying unsupported claims after gap handling is used.
- Cover frontend evidence review interactions where existing frontend test structure supports them.
- Cover that unresolved gap decisions keep evidence review as the guided next action.
- Cover that resolved gap decisions allow the workflow to move toward draft generation when approved evidence exists.
- Use existing application workflow and AI provider tests as prior art, especially tests around preparation, evidence approval, draft generation, and claim audit.

## Out of Scope

- Assisted profile fact import.
- Draft profile fact review queue.
- New reusable profile fact statuses.
- Full profile fact revision history.
- Full generated draft version history.
- Automatically approving matched evidence.
- Automatically approving job-local custom facts.
- Turning unmatched requirements into a separate planning or task system.
- Broad provider readiness UX changes.
- Fake mode labeling improvements beyond what is necessary for the new paths.
- Combined draft generation and claim audit automation.
- Copy/export guided final action.
- Editable AI provider settings.
- OpenAI provider support.
- Authentication or multi-user support.
- Mobile-first redesign.
- Visual redesign beyond making the evidence and gap review checkpoint clear.

## Further Notes

Milestone 2 is the trust-boundary milestone for V2. It should make evidence review feel complete without making it heavy: approve the good matches, make explicit decisions about gaps, optionally add reviewed job-local facts, and then let generation use those decisions without inventing unsupported proof.
