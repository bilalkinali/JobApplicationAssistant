# Candidate Fit Brief Traceability Boundary

Status: done
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-1-prd.md

## What to build

Enforce the hard trust boundary for candidate fit brief profile fact references. `CandidateFitBrief.profileFactIds` are traceability only and must never be treated as approved evidence for final claims, evidence review, draft generation, or claim audit.

This slice should make the boundary explicit in contracts and tests so later creative generation work can safely consume the fit brief as writing context without turning it into proof.

## Acceptance criteria

- [x] Candidate fit brief contracts describe profile fact ids as traceability-only references.
- [x] Evidence review does not treat candidate fit brief profile fact ids as approved evidence.
- [x] Draft generation does not treat candidate fit brief profile fact ids as approved evidence.
- [x] Claim audit does not treat candidate fit brief profile fact ids as approved evidence.
- [x] Final-draft claims still require per-application approved evidence or approved job-local custom facts.
- [x] Tests cover candidate fit brief profile fact ids remaining traceability-only and not becoming approved evidence.
- [x] No automatic profile fact approval is introduced.

## Blocked by

- docs/planning/v2/issues/27-prepare-application-candidate-fit-brief-state.md
