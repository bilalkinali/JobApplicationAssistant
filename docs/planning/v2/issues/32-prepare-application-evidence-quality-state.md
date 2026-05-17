# Prepare Application Evidence Quality State

Status: done
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-2-prd.md

## What to build

Wire evidence quality through preparation state so reviewed applications persist the latest match quality, match reason, and unmatched requirements. Preparing an application should keep the current manual evidence review flow intact while ensuring weak matches are never treated as already approved proof.

The slice should make quality part of the backend workflow state before the frontend adds stronger review affordances.

## Acceptance criteria

- [x] Prepared application state persists evidence match quality and reason.
- [x] Unmatched requirements remain available as unsupported job signals after preparation.
- [x] Weak evidence is not preselected or auto-approved during preparation.
- [x] Strong and partial evidence preserve the existing manual approve/remove review behavior.
- [x] Evidence matching continues to avoid duplicate or conflicting signal handling covered by the existing workflow.
- [x] API responses expose quality, reason, and unmatched requirements to the application detail experience.
- [x] Backend tests cover weak evidence not being auto-approved and quality surviving preparation state round trips.

## Blocked by

- docs/planning/v2/issues/31-evidence-quality-provider-contract.md
