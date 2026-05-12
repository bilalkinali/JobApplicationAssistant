# Prepare Failure and Partial State

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/milestone-1-prd.md

## What to build

Make preparation failure behavior safe, plain, and recoverable. Provider unavailability and invalid provider output should not corrupt existing workflow state. If job analysis succeeds but evidence matching fails, the valid analysis should remain available and the application should clearly show that preparation only partially completed.

This slice protects the V2 trust boundary by ensuring automated preparation only mutates workflow state when the backend has valid output for that step.

## Acceptance criteria

- [ ] Preparation uses `FailedProviderUnavailable` when the configured provider cannot complete preparation because it is unavailable.
- [ ] Preparation uses `FailedInvalidProviderOutput` when provider output cannot be safely used.
- [ ] Provider unavailable preparation failures return plain error details that fit the existing AI error presentation pattern.
- [ ] Invalid output preparation failures return plain error details that fit the existing AI error presentation pattern.
- [ ] Preparation failures keep existing stable workflow state visible and do not overwrite prior valid analysis, matches, approved evidence, or drafts with unsafe output.
- [ ] If job analysis succeeds but evidence matching fails, valid analysis state is preserved.
- [ ] Analysis success plus matching failure marks `PreparationStatus` as `PartiallyPreparedAnalysisOnly`.
- [ ] The partial preparation response makes clear that evidence matching needs to be retried before evidence review is ready.
- [ ] `AiRun` records are written consistently with existing workflow action success and failure behavior.
- [ ] Backend API tests cover provider unavailable, invalid provider output, and partial analysis-only preparation.
- [ ] No new provider type, editable provider settings, history model, or frontend redesign is introduced.

## Blocked by

- docs/planning/v2/issues/01-prepare-application-orchestration.md
