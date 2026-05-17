# Guided Application Strategy Generation

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-2-prd.md

## What to build

Generate and persist the latest application strategy only after the user has reviewed evidence and gap decisions. Strategy generation should run as part of the guided draft path immediately before draft generation, not during prepare application, and it should preserve prior stable workflow state when the provider fails.

The slice should make the strategy a reproducible workflow artifact with its own AI run.

## Acceptance criteria

- [ ] Prepare application does not generate an application strategy.
- [ ] Guided draft generation creates an application strategy after evidence review and gap decisions are available.
- [ ] The latest application strategy is persisted as application workflow state.
- [ ] Strategy generation records an `AiRun` step for `ApplicationStrategy`.
- [ ] Gap decisions flow into strategy generation input.
- [ ] Approved custom facts flow into strategy generation input.
- [ ] Provider failure preserves the prior stable draft and preparation state.
- [ ] Tests cover strategy generation timing, persistence, `AiRun` recording, gap decision input, and provider failure recovery.

## Blocked by

- docs/planning/v2/issues/32-prepare-application-evidence-quality-state.md
- docs/planning/v2/issues/34-application-strategy-provider-contract.md

