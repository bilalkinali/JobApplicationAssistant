# Prepare Application Candidate Fit Brief State

Status: done
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-1-prd.md

## What to build

Run candidate fit brief generation as part of `Prepare application` and persist the result as the latest application workflow state on `JobApplication`. The fit brief should be generated after job analysis and before or alongside evidence matching, using all approved profile facts rather than only matched evidence.

This slice should make the prepared application state richer without changing draft generation behavior.

## Acceptance criteria

- [x] `Prepare application` generates a candidate fit brief after job analysis and before or alongside evidence matching.
- [x] Candidate fit brief generation receives all approved profile facts, not only evidence matches.
- [x] A successful preparation stores the candidate fit brief as part of the latest workflow state on `JobApplication`.
- [x] Candidate fit brief is not introduced as an independent domain concept.
- [x] A successful candidate fit brief generation records an `AiRun` step for `CandidateFitBrief`.
- [x] Existing evidence matching, strategy, draft generation, and audit contracts compile against the updated latest-state shape without changing final draft behavior.
- [x] Workflow tests cover approving imported facts and using them in a candidate fit brief.
- [x] Workflow tests cover candidate fit brief generation receiving all approved profile facts.

## Blocked by

- docs/planning/v2/issues/26-candidate-fit-brief-provider-contract.md
