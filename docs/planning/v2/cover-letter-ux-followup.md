# Cover Letter UX Follow-up

## Context

The cover-letter workflow UI was tightened so the main path reads as:

1. Prepare automatically.
2. Review evidence.
3. Generate and audit.

The UI now also supports approving suggested evidence and resetting saved evidence decisions.

## Items To Take Care Of

### Browser flow needs a stable end-to-end test

Manual browser verification reached the updated workflow UI, but fresh AI preparation intermittently ended as `FailedProviderUnavailable` even when `/api/ai/status` reported the OpenAI-compatible provider as available.

Follow-up:

- Add or keep a lightweight Playwright smoke test for the happy path once local AI/provider availability is stable enough.
- Prefer testing against a deterministic/fake provider for UI flow coverage, and leave real local AI generation as a separate manual/diagnostic test.

### Evidence review could still be more one-click

The new `Approve suggested evidence` button selects strong and partial matches, but unmatched requirements still need manual gap decisions.

Follow-up:

- Consider an explicit `Accept suggested review` action that approves recommended evidence and applies conservative default gap decisions.
- Keep weak matches manual.
- Make sure defaults do not cause unsupported claims in generated drafts.

### Provider-unavailable state should be easier to diagnose

The UI points users to AI settings, but the preparation failure can still feel surprising when status says the provider is available.

Follow-up:

- Surface the failed workflow step and provider error details near the Step 1 panel.
- Check whether preparation calls are timing out, receiving invalid output, or failing on a later provider call after the initial status check.

## Verification Already Done

- `npm test`
- `npm run build`
- Browser smoke attempt against the Vejle Kommune AI-builder flow from `docs/testing/local-test/CV Technical Skills Extraction - 2026-05-16 23.25.md`

