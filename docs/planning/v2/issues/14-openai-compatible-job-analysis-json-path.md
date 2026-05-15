# OpenAI-Compatible Job Analysis JSON Path

Status: done
Type: AFK

## Parent

docs/planning/v2/lm-studio-openai-compatible-provider-prd.md

## What to build

Make job analysis work end to end through the `OpenAiCompatible` provider using OpenAI-compatible chat completions. The provider should send a strict JSON-oriented chat completion request to the configured local endpoint, parse the assistant response into the existing job analysis result shape, and preserve the established validation and repair-attempt behavior.

The completed slice should prove that a saved application can run job analysis through LM Studio-style chat completions and persist valid parsed job signals without relying on Fake AI.

## Acceptance criteria

- [x] Job analysis calls the configured OpenAI-compatible chat completions endpoint using the configured model.
- [x] The chat completion request asks for strict JSON output and does not use OpenAI cloud.
- [x] Valid assistant JSON is parsed into the existing job analysis result shape.
- [x] Existing structural validation rules still reject malformed or incomplete job analysis output.
- [x] Existing repair-attempt behavior is preserved for invalid job analysis JSON.
- [x] Failed job analysis calls return graceful provider errors and do not corrupt application workflow state.
- [x] Raw request/response or error payloads are stored or logged for failed job analysis calls when `StoreRawPayloads` is enabled.
- [x] Backend tests prove job analysis works with `OpenAiCompatible` and persists valid parsed JSON.
- [x] Backend tests cover malformed JSON, structurally invalid JSON, empty assistant content, provider API error, timeout, and unreachable endpoint for job analysis.

## Blocked by

- docs/planning/v2/issues/13-openai-compatible-provider-selection-and-diagnostics.md
