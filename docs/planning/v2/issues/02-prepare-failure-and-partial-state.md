# Prepare Failure and Partial State

Status: done
Type: AFK

## Parent

docs/planning/v2/milestone-1-prd.md

## What to build

Make preparation failure behavior safe, plain, and recoverable. Provider unavailability and invalid provider output should not corrupt existing workflow state. If job analysis succeeds but evidence matching fails, the valid analysis should remain available and the application should clearly show that preparation only partially completed.

This slice protects the V2 trust boundary by ensuring automated preparation only mutates workflow state when the backend has valid output for that step.

## Acceptance criteria

- [x] Preparation uses `FailedProviderUnavailable` when the configured provider cannot complete preparation because it is unavailable.
- [x] Preparation uses `FailedInvalidProviderOutput` when provider output cannot be safely used.
- [x] Provider unavailable preparation failures return plain error details that fit the existing AI error presentation pattern.
- [x] Invalid output preparation failures return plain error details that fit the existing AI error presentation pattern.
- [x] Preparation failures keep existing stable workflow state visible and do not overwrite prior valid analysis, matches, approved evidence, or drafts with unsafe output.
- [x] If job analysis succeeds but evidence matching fails, valid analysis state is preserved.
- [x] Analysis success plus matching failure marks `PreparationStatus` as `PartiallyPreparedAnalysisOnly`.
- [x] The partial preparation response makes clear that evidence matching needs to be retried before evidence review is ready.
- [x] `AiRun` records are written consistently with existing workflow action success and failure behavior.
- [x] Backend API tests cover provider unavailable, invalid provider output, and partial analysis-only preparation.
- [x] No new provider type, editable provider settings, history model, or frontend redesign is introduced.

## Blocked by

- docs/planning/v2/issues/01-prepare-application-orchestration.md
