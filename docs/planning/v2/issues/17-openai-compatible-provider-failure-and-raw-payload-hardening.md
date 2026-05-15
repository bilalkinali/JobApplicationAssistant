# OpenAI-Compatible Provider Failure and Raw Payload Hardening

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/lm-studio-openai-compatible-provider-prd.md

## What to build

Harden the complete `OpenAiCompatible` real-provider path so LM Studio failures are recoverable, diagnosable, and clearly separate from Fake AI. This slice should tighten cross-workflow behavior after the main operation paths exist: failed calls should preserve workflow state, record useful `AiRun` failure information, honor `StoreRawPayloads`, and surface provider diagnostics that help the user recover without changing V2 feature scope.

The completed slice should make the local real AI path ready for V2 testing across prepare, draft generation, and claim audit.

## Acceptance criteria

- [ ] Invalid JSON, timeout, unreachable endpoint, non-success HTTP status, model/API error responses, and empty assistant content are mapped to graceful provider failures across all OpenAI-compatible operations.
- [ ] Failed provider calls record `AiRun` details consistently with existing workflow behavior.
- [ ] Failed provider calls store or log raw request, response, and error context when `StoreRawPayloads` is enabled.
- [ ] Raw payload behavior remains disabled when `StoreRawPayloads` is false.
- [ ] Failed analysis, matching, generation, and audit calls do not corrupt prepared state, evidence review state, current draft state, or current audit state.
- [ ] Provider diagnostics remain clear after failures and include provider, endpoint, model, endpoint reachability, and model configured or available state where possible.
- [ ] The app never silently falls back from `OpenAiCompatible` to Fake AI after a real-provider failure.
- [ ] Fake AI remains clearly separate as deterministic demo/test mode and still compiles.
- [ ] Focused regression tests cover cross-workflow failure behavior and raw-payload behavior for the real provider path.
- [ ] Existing V2 workflow, Fake AI, and Ollama tests continue to pass or are intentionally updated only where provider-neutral contracts changed.

## Blocked by

- docs/planning/v2/issues/16-openai-compatible-draft-generation-and-claim-audit.md
