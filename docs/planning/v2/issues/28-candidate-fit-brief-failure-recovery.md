# Candidate Fit Brief Failure Recovery

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-1-prd.md

## What to build

Make candidate fit brief generation safe inside preparation. Provider unavailability, validation failures, and malformed model output should return plain errors while preserving the prior stable application workflow state.

The slice should prove that preparation can fail during the new fit brief step without corrupting previously prepared state or silently clearing review progress.

## Acceptance criteria

- [ ] Provider unavailable during candidate fit brief generation returns a plain provider error.
- [ ] Malformed or invalid candidate fit brief output returns a plain validation error.
- [ ] Failed candidate fit brief generation preserves the prior stable application workflow state.
- [ ] Failed candidate fit brief generation does not clear existing approved evidence, job signals, or previously stored stable artifacts.
- [ ] Failure paths record enough diagnostic information for the existing preparation failure surface to remain useful.
- [ ] Workflow tests cover provider unavailable and malformed JSON preserving prior stable state.

## Blocked by

- docs/planning/v2/issues/27-prepare-application-candidate-fit-brief-state.md
