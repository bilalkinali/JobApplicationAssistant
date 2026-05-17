# Guided Application Strategy Generation

Status: done
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-2-prd.md

## What to build

Generate and persist the latest application strategy only after the user has reviewed evidence and gap decisions. Strategy generation should run as part of the guided draft path immediately before draft generation, not during prepare application, and it should preserve prior stable workflow state when the provider fails.

The slice should make the strategy a reproducible workflow artifact with its own AI run.

## Acceptance criteria

- [x] Prepare application does not generate an application strategy.
- [x] Guided draft generation creates an application strategy after evidence review and gap decisions are available.
- [x] The latest application strategy is persisted as application workflow state.
- [x] Strategy generation records an `AiRun` step for `ApplicationStrategy`.
- [x] Gap decisions flow into strategy generation input.
- [x] Approved custom facts flow into strategy generation input.
- [x] Provider failure preserves the prior stable draft and preparation state.
- [x] Tests cover strategy generation timing, persistence, `AiRun` recording, gap decision input, and provider failure recovery.

## Blocked by

- docs/planning/v2/issues/32-prepare-application-evidence-quality-state.md
- docs/planning/v2/issues/34-application-strategy-provider-contract.md
