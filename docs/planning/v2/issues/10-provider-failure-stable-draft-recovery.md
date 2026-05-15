# Provider Failure Stable Draft Recovery

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/milestone-3-prd.md

## What to build

Make generation and audit failures recoverable without corrupting the user's latest stable draft or workflow state. If the configured real provider is unavailable, failing, or returns invalid output, the app should block or fail plainly, record the relevant AI run behavior, and preserve any prior stable draft.

The user should understand what failed and what can be retried, while previously useful generated text remains available for review or export if it was already valid.

## Acceptance criteria

- [ ] A configured real provider that is unavailable blocks the guided draft action with recoverable provider guidance.
- [ ] Invalid provider output fails plainly and does not persist partial draft or audit state.
- [ ] If a previous stable draft exists, generation failure preserves that draft.
- [ ] If a previous current audit exists, audit failure does not falsely mark a new audit as current.
- [ ] If generation succeeds but audit fails, the app preserves the generated draft and clearly requires audit retry.
- [ ] Provider failure behavior records AI run information consistently with existing provider workflow patterns.
- [ ] The frontend shows the retry or recovery path without replacing stable draft review content with the failed attempt.
- [ ] Focused tests cover unavailable provider, invalid output, generation failure, audit failure, and stable-state preservation.

## Blocked by

- docs/planning/v2/issues/09-assistant-led-draft-generation-and-audit.md
