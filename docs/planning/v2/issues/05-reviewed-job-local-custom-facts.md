# Reviewed Job-Local Custom Facts

Status: done
Type: AFK

## Parent

docs/planning/v2/milestone-2-prd.md

## What to build

Add job-local custom facts as an evidence review option for unmatched requirements that the user's reusable profile facts do not cover.

The user should be able to add an application-scoped custom fact from the evidence review flow, review it, approve or reject it, and use an approved custom fact to cover a specific unmatched requirement. Approved job-local custom facts may support generated claims only for their owning application. Draft, rejected, or unapproved custom facts must not support generated claims.

## Acceptance criteria

- [x] Evidence review lets the user add a job-local custom fact for an unmatched requirement.
- [x] Job-local custom facts are scoped to one application.
- [x] Job-local custom facts require review before they can support generated claims.
- [x] The user can approve a job-local custom fact.
- [x] The user can reject a job-local custom fact.
- [x] An unmatched requirement can be marked `CoveredByCustomFact` only when it is linked to an approved job-local custom fact.
- [x] Approved job-local custom facts are available as evidence only for their owning application.
- [x] Draft, rejected, or unapproved job-local custom facts are excluded from approved evidence.
- [x] Job-local custom facts do not appear as reusable profile facts or evidence for other applications.
- [x] Backend and frontend tests cover creation, review, approval, rejection, gap coverage, and application scoping.
- [x] No assisted profile import, automatic custom fact approval, full fact history, or separate gap planning system is introduced.

## Blocked by

- docs/planning/v2/issues/04-gap-decisions-in-evidence-review.md
