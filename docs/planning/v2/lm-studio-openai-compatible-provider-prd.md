# LM Studio OpenAI-Compatible Provider PRD

Status: ready-for-agent
Source: User request on 2026-05-15

## Problem Statement

V2.0 is already scoped, but the real AI path is blocked because the application cannot yet use LM Studio as the local model provider. The existing V2 workflows need a serious local AI provider for job analysis, evidence matching, draft generation, and claim audit without depending on OpenAI cloud or falling back to deterministic fake behavior.

The user needs to configure LM Studio at an OpenAI-compatible endpoint such as `http://localhost:1234/v1`, select a local model, and have the backend call that provider through the existing AI abstraction. When LM Studio is unreachable, misconfigured, slow, or returns invalid output, the workflow should fail gracefully, preserve application state, and record useful diagnostics and raw payloads when configured.

## Solution

Add an `OpenAiCompatible` local AI provider path that uses OpenAI-compatible chat completions against the configured endpoint, model, timeout, and raw-payload setting. LM Studio is the expected runtime for this provider, with the server assumed to be running at `http://localhost:1234`.

The provider should plug into the existing `IAiProvider` abstraction and reuse the established V2 behavior: strict JSON prompts, strict JSON parsing, validation, one repair attempt where existing workflows support it, `AiRun` recording, and graceful failure without corrupting workflow state. Fake AI remains available as a separate deterministic demo/test provider, but V2 testing of the real path should use `OpenAiCompatible`.

Provider diagnostics should clearly report the configured provider, endpoint, model, endpoint reachability, and model configuration or availability where the LM Studio-compatible API can determine it.

## User Stories

1. As a job applicant, I want the app to use my local LM Studio model for job analysis, so that my job posting can be interpreted without cloud AI.
2. As a job applicant, I want the app to use my local LM Studio model for evidence matching, so that profile facts can be matched to job requirements through the real provider path.
3. As a job applicant, I want `POST /api/applications/{id}/prepare` to use LM Studio when configured, so that preparation proves the real V2 workflow works locally.
4. As a job applicant, I want draft generation to use LM Studio, so that generated cover letter and short motivation text come from the configured local model.
5. As a job applicant, I want claim audit to use LM Studio, so that trust feedback can be produced locally.
6. As a job applicant, I want provider diagnostics to show provider, endpoint, and model, so that I can see which local AI setup the app is trying to use.
7. As a job applicant, I want diagnostics to show whether the endpoint is reachable, so that I can tell whether LM Studio is running and listening.
8. As a job applicant, I want diagnostics to show whether a model is configured and available when the provider can determine it, so that I can fix model configuration quickly.
9. As a job applicant, I want invalid JSON from the model to fail plainly, so that unsupported output does not become trusted workflow state.
10. As a job applicant, I want provider timeouts to fail gracefully, so that a slow local model does not leave the application in a corrupted state.
11. As a job applicant, I want unreachable endpoint failures to explain the configured endpoint, so that I can restart or reconfigure LM Studio.
12. As a job applicant, I want model or API errors to be preserved as recoverable provider failures, so that I know the action can be retried after fixing the provider.
13. As a job applicant, I want previous stable drafts and workflow checkpoints preserved when a real provider call fails, so that failed local AI work does not destroy useful progress.
14. As a developer, I want LM Studio support implemented as an OpenAI-compatible provider, so that the integration is not tied to a proprietary LM Studio-only API shape.
15. As a developer, I want the provider selected by `Ai:Provider`, so that Fake, Ollama, and OpenAI-compatible local provider paths remain explicit.
16. As a developer, I want endpoint, model, timeout, and raw-payload behavior driven by `Ai` configuration, so that local setup can change without code edits.
17. As a developer, I want no OpenAI cloud dependency, so that V2 real-provider testing stays local.
18. As a developer, I want no authentication added unless required for compatibility, so that the LM Studio default local server remains simple to use.
19. As a developer, I want strict prompt contracts to request JSON output from chat completions, so that existing parsing and validation rules remain meaningful.
20. As a developer, I want failed calls to store or log raw request/response payloads when `StoreRawPayloads` is enabled, so that local model failures can be diagnosed.
21. As a developer, I want Fake AI to keep compiling and remain selectable, so that deterministic tests and demos continue to work separately from the real provider.
22. As a developer, I want focused tests around the OpenAI-compatible provider path, so that LM Studio support does not regress job analysis, evidence matching, draft generation, audit, or diagnostics.

## Implementation Decisions

- Add an `OpenAiCompatible` provider implementation behind the existing `IAiProvider` interface.
- Treat LM Studio as an OpenAI-compatible local provider, not as OpenAI cloud integration.
- Support configuration shaped as:

```json
{
  "Ai": {
    "Provider": "OpenAiCompatible",
    "Endpoint": "http://localhost:1234/v1",
    "Model": "local-model-name",
    "TimeoutSeconds": 120,
    "StoreRawPayloads": true
  }
}
```

- Use the configured endpoint as the OpenAI-compatible API base URL and call chat completions under that base, supporting values like `http://localhost:1234/v1`.
- Use the configured model in every chat completion request.
- Use the configured timeout for provider HTTP calls.
- Do not add authentication for the LM Studio default local server path.
- Do not send calls to OpenAI cloud endpoints as part of this PRD.
- Wire provider selection so `Ai:Provider = OpenAiCompatible` resolves to the new provider while `Fake` remains deterministic demo/test mode and `Ollama` remains separate if still supported.
- Reuse existing prompt source material and strengthen provider request messages where needed so each operation asks for strict JSON output only.
- Preserve existing strict JSON parsing, structural validation, repair attempt behavior, and `AiProviderException`-based graceful failure rules.
- Map OpenAI-compatible chat completion response content into the same internal result parsers used by the current real-provider workflow.
- Treat invalid provider envelopes, empty choices, empty assistant content, malformed JSON, structurally invalid JSON, duplicate/conflicting evidence, and unsupported audit statuses as invalid provider output.
- Treat unreachable endpoint, timeout, non-success HTTP status, model/API error responses, and cancellation caused by provider timeout as provider unavailable or provider API failure according to existing error semantics.
- Ensure failed analysis, matching, generation, and audit calls record `AiRun` failure details consistently with current workflow behavior.
- Ensure raw payload capture/logging includes enough request and response/error context for diagnosis when `StoreRawPayloads` is enabled, while preserving existing behavior when it is disabled.
- Extend provider diagnostics so the response clearly includes provider, endpoint, model, endpoint reachability, and model configuration or availability.
- Check endpoint reachability through an OpenAI-compatible route that LM Studio supports, preferring a lightweight models endpoint when available.
- Report `modelConfigured` as false when the configured model value is blank or missing.
- Report `modelAvailable` when the provider can compare the configured model to the endpoint's available model list; otherwise report that availability could not be determined without treating that alone as an endpoint failure.
- Keep diagnostics recoverable and plain: misconfigured provider state should be visible without requiring a workflow action to fail first.
- Keep V2 workflow state rules unchanged: failed AI calls must not persist partial analysis, matching, draft, or audit state in a way that corrupts the application workflow.
- Do not change the V2 specification file as part of this PRD.

## Testing Decisions

- Test public behavior through provider and API contracts rather than private helper methods.
- Add focused unit tests for the OpenAI-compatible provider request shape, response parsing, JSON repair attempt behavior, and error mapping using a fake HTTP handler.
- Add diagnostics tests for reachable endpoint, unreachable endpoint, blank model, available model, unavailable model, and model availability unknown when the endpoint cannot report models.
- Add backend workflow tests proving job analysis works with `OpenAiCompatible` and persists valid parsed JSON.
- Add backend workflow tests proving evidence matching works with `OpenAiCompatible` and persists valid parsed JSON.
- Add backend workflow tests proving `POST /api/applications/{id}/prepare` performs analysis and matching through `OpenAiCompatible`, not Fake AI.
- Add backend workflow tests proving draft generation works with `OpenAiCompatible` after evidence review is ready.
- Add backend workflow tests proving claim audit works with `OpenAiCompatible` and persists current audit state when valid.
- Add failure tests for invalid JSON, structurally invalid JSON, timeout, unreachable endpoint, non-success HTTP status, model/API error response, and empty assistant content.
- Add raw-payload tests where existing persistence seams support them, especially failed provider calls with `StoreRawPayloads` enabled.
- Keep Fake AI tests intact and add or adjust provider-selection tests proving Fake remains explicitly selectable and separate from `OpenAiCompatible`.
- Use existing Ollama provider, AI status, application workflow, prepare, draft generation, and claim audit tests as prior art for assertions and fixture shape.

## Out of Scope

- No changes to the V2 specification.
- No new V2 product features.
- No profile fact import.
- No UI redesign.
- No OpenAI cloud integration.
- No authentication unless a compatibility requirement is discovered during implementation.
- No Docker or networking changes unless required for endpoint reachability.
- No editable provider settings UI.
- No broad prompt redesign beyond what is required to request strict JSON through chat completions.
- No changes to evidence approval, gap decisions, draft review, export, or audit staleness rules except where necessary to preserve existing failure behavior.

## Further Notes

The purpose of this PRD is to unblock V2 real-provider testing locally. LM Studio should be treated as the expected OpenAI-compatible server, with `http://localhost:1234/v1` as the canonical endpoint shape, but the implementation should stay generic enough for other local OpenAI-compatible servers.

Acceptance hinges on the real provider path being visible and distinct: Fake AI can remain useful for deterministic test/demo mode, but successful V2 workflow testing should be able to run against `OpenAiCompatible` with LM Studio.
