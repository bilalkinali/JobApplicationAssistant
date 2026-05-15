# OpenAI-Compatible Provider Selection and Diagnostics

Status: done
Type: AFK

## Parent

docs/planning/v2/lm-studio-openai-compatible-provider-prd.md

## What to build

Add the first real-provider tracer bullet for LM Studio by making `OpenAiCompatible` a selectable AI provider and exposing clear provider readiness diagnostics for the configured OpenAI-compatible endpoint.

The completed slice should let the app be configured with `Ai:Provider = OpenAiCompatible`, `Ai:Endpoint = http://localhost:1234/v1`, and a model name, then report whether the endpoint is reachable and whether the model is configured or available where the endpoint can determine it. Fake AI must remain explicitly selectable and separate from this real provider path.

## Acceptance criteria

- [x] `Ai:Provider = OpenAiCompatible` resolves to a real `IAiProvider` implementation instead of the unavailable-provider fallback.
- [x] The provider uses configured endpoint, model, timeout, and `StoreRawPayloads` options.
- [x] Endpoint values like `http://localhost:1234/v1` are accepted as the OpenAI-compatible API base URL.
- [x] Provider status and diagnostics clearly report provider, endpoint, model, endpoint reachability, and model configured state.
- [x] Diagnostics report model availability when the endpoint can provide a model list.
- [x] Diagnostics remain graceful when the endpoint is unreachable, the model is blank, or model availability cannot be determined.
- [x] Fake AI still compiles, remains selectable, and is not used as a fallback when `OpenAiCompatible` is configured.
- [x] Focused tests cover provider selection and diagnostics for reachable endpoint, unreachable endpoint, blank model, available model, unavailable model, and Fake provider separation.

## Blocked by

None - can start immediately.
