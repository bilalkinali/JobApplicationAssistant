# Import Failure Diagnostics And Output Validation

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/v2.1-assisted-profile-import-prd.md

## What to build

Make assisted import failures safe and explainable. Malformed AI output, empty extraction output, provider timeouts, unreachable endpoints, non-success responses, and invalid JSON should fail plainly, record useful diagnostics, and preserve the user's existing profile state.

This slice should keep real-provider behavior explicit: the app must not silently fall back to Fake AI when a real provider is configured.

## Acceptance criteria

- [ ] Malformed, invalid, or incomplete AI extraction output fails validation and does not persist partial imported facts.
- [ ] Empty extraction output fails with a clear message that can be shown in the import UI.
- [ ] Provider timeout, unreachable endpoint, non-success response, and invalid JSON are recorded with useful import diagnostics.
- [ ] Existing profile facts and previously created import sessions remain intact after a failed import attempt.
- [ ] Import AI runs follow existing provider diagnostics and failure recording patterns.
- [ ] `StoreRawPayloads` behavior for import failures is consistent with other AI operations.
- [ ] A configured real provider never silently falls back to Fake AI during import.
- [ ] Tests cover malformed output, empty output, timeout or unreachable provider, non-success response, invalid JSON, no state corruption, raw payload behavior, and no silent Fake fallback.

## Blocked by

- docs/planning/v2/issues/18-pdf-cv-assisted-import-foundation.md
