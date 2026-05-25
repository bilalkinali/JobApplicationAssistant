# Claim Audit Fit Brief Proof Boundary

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-3-prd.md

## What to build

Update claim audit so the safety net matches the new draft context shape. Audit should still flag unsupported concrete claims, but it must distinguish cautious motivation or learning-interest statements from claims of experience. It must also fail concrete claims that are supported only by candidate fit brief profile fact ids and not by approved evidence or approved job-local custom facts.

The slice should keep strict JSON validation and the existing stale-audit behavior.

## Acceptance criteria

- [ ] Claim audit input includes the final cover letter, final short motivation, approved evidence, approved custom facts, gap decisions, and candidate fit brief support mappings where available.
- [ ] Claim audit prompt states that candidate fit brief profile fact ids are traceability context, not direct proof.
- [ ] Concrete claims supported only by candidate fit brief profile fact ids are marked unsupported.
- [ ] Broad soft-skill or competency claims require approved evidence, approved custom facts, or cautious non-evidentiary wording.
- [ ] Cautious motivation and learning-interest statements are handled differently from experience claims.
- [ ] Manual edits still mark the audit stale.
- [ ] Tests cover unsupported concrete claims, fit-brief-only support rejection, cautious motivation handling, broad soft-skill claim auditing, and stale audit behavior.

## Blocked by

None - can start immediately

