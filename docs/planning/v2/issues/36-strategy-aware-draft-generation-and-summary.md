# Strategy-Aware Draft Generation And Summary

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-2-prd.md

## What to build

Use the persisted application strategy to guide draft generation and show a compact read-only strategy summary near draft review. The draft should use strategy angles and gap guidance without making unsupported or weakly supported claims look stronger than the approved evidence allows.

The slice should expose the writing plan and feed it into generation without adding editable strategy screens or broad claim audit changes.

## Acceptance criteria

- [ ] Draft generation receives the latest application strategy when one has been generated.
- [ ] Primary angles, secondary angles, gap guidance, claims to avoid, tone guidance, and draft outline are available to the draft provider.
- [ ] Weak evidence does not support direct experience claims in generated draft inputs.
- [ ] Partial evidence guides cautious wording rather than overstated experience claims.
- [ ] A compact read-only strategy summary is shown near draft review or immediately before generation.
- [ ] The summary includes primary angles, gap guidance, claims to avoid, and draft outline.
- [ ] Tests cover strategy being passed to draft generation and strategy summary rendering where existing test seams support it.

## Blocked by

- docs/planning/v2/issues/33-evidence-review-quality-ux.md
- docs/planning/v2/issues/35-guided-application-strategy-generation.md

