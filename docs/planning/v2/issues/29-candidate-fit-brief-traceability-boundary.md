# Candidate Fit Brief Traceability Boundary

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-1-prd.md

## What to build

Enforce the hard trust boundary for candidate fit brief profile fact references. `CandidateFitBrief.profileFactIds` are traceability only and must never be treated as approved evidence for final claims, evidence review, draft generation, or claim audit.

This slice should make the boundary explicit in contracts and tests so later creative generation work can safely consume the fit brief as writing context without turning it into proof.

## Acceptance criteria

- [ ] Candidate fit brief contracts describe profile fact ids as traceability-only references.
- [ ] Evidence review does not treat candidate fit brief profile fact ids as approved evidence.
- [ ] Draft generation does not treat candidate fit brief profile fact ids as approved evidence.
- [ ] Claim audit does not treat candidate fit brief profile fact ids as approved evidence.
- [ ] Final-draft claims still require per-application approved evidence or approved job-local custom facts.
- [ ] Tests cover candidate fit brief profile fact ids remaining traceability-only and not becoming approved evidence.
- [ ] No automatic profile fact approval is introduced.

## Blocked by

- docs/planning/v2/issues/27-prepare-application-candidate-fit-brief-state.md
