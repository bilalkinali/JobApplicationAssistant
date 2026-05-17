# Application Strategy Provider Contract

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-2-prd.md

## What to build

Add a strict AI provider contract for generating an application strategy from reviewed evidence and application context. The strategy should choose the strongest story angles, identify secondary angles, describe gap handling, list claims to avoid, guide tone, and provide a draft outline.

This slice should make strategy generation deterministic and validated before it is wired into the guided draft workflow.

## Acceptance criteria

- [ ] The AI provider interface supports application strategy generation.
- [ ] Strategy input includes job analysis, candidate fit brief, approved evidence with quality, unmatched requirements, gap decisions, approved custom facts, selected language, and tone preference.
- [ ] Strategy output includes primary angles, secondary angles, gap handling guidance, claims to avoid, tone guidance, and draft outline.
- [ ] Strategy angles reference approved evidence ids when supporting concrete claims.
- [ ] Strategy items may reference profile fact ids only for narrative context traceability.
- [ ] Output validation rejects missing arrays, invalid evidence ids, and invalid profile fact ids.
- [ ] Fake AI returns deterministic application strategy artifacts suitable for tests and demos.

## Blocked by

- docs/planning/v2/issues/31-evidence-quality-provider-contract.md

