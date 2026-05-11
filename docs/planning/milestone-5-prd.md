# Milestone 5 Product Requirements Document

Status: ready-for-agent
Source: docs/planning/v1-prd.md

## Goal

Add Ollama as the first real local AI provider behind the existing AI provider workflow.

Milestone 5 should let the user keep using the deterministic fake provider, switch the backend to a configured Ollama provider, inspect provider status, run diagnostics, and receive clear errors when Ollama is unavailable or returns invalid output. Real provider responses should be constrained by strict JSON contracts, validated before they mutate workflow state, given one repair attempt, and tracked with minimal `AiRun` records.

## Problem Statement

The app can now complete the trust workflow with fake AI: analyze a job posting, match evidence, generate drafts, edit drafts, and audit claims. That deterministic path is useful for development, but it does not yet let the user try the workflow with a real local model.

The next product risk is provider variability. Ollama responses can be unavailable, malformed, incomplete, or subtly unsafe. Milestone 5 must introduce real model calls without weakening the existing trust boundary: the backend owns provider calls, outputs are structured and validated, failures are plain, and workflow state only updates after valid provider output.

## Solution

Implement `OllamaAiProvider` behind the existing internal AI provider abstraction. The provider should be selected by configuration, call the configured local Ollama endpoint and model, use backend-owned markdown prompts, request strict JSON for each AI operation, deserialize and validate that JSON into the existing workflow DTOs, and attempt one repair pass when JSON is invalid or structurally incomplete.

Add AI status and diagnostics endpoints so the UI can show read-only provider/model availability and run a manual diagnostics check. Add minimal `AiRun` tracking around AI workflow actions so failures and repairs can be diagnosed without storing raw request or response payloads by default.

Milestone 5 should not redesign the workflow or add export. The existing application detail flow should keep working with the fake provider and gain real-provider behavior only through the shared provider contract.

## User Stories

1. As a job applicant, I want the app to support Ollama, so that I can use a local real AI provider without API keys.
2. As a job applicant, I want the app to still start when Ollama is unavailable, so that setup problems do not block non-AI work.
3. As a job applicant, I want AI actions to fail with a clear unavailable message when Ollama cannot be reached, so that I know the problem is provider setup.
4. As a job applicant, I want to see the current provider and model, so that I know whether I am using fake AI or Ollama.
5. As a job applicant, I want to see provider status on the home workbench, so that I can notice provider availability before starting AI work.
6. As a job applicant, I want a settings area for AI status, so that provider details are not hidden inside the application workflow.
7. As a job applicant, I want diagnostics to be manually triggered, so that the app does not constantly call my local model.
8. As a job applicant, I want diagnostics results to explain connectivity and model issues plainly, so that I can fix local setup.
9. As a job applicant, I want job analysis through Ollama, so that company, role, language, and job signals can come from the actual posting rather than fake keyword rules.
10. As a job applicant, I want Ollama job analysis to update application state only when the response is valid, so that bad model output does not corrupt the session.
11. As a job applicant, I want evidence matching through Ollama, so that relevant approved facts can be matched with more nuance than keyword matching.
12. As a job applicant, I want Ollama matching to use only approved profile facts, so that draft or archived facts do not become evidence.
13. As a job applicant, I want unmatched requirements still surfaced clearly, so that the real provider does not hide gaps.
14. As a job applicant, I want draft generation through Ollama, so that generated cover letters and short motivation texts can be more natural while remaining grounded.
15. As a job applicant, I want Ollama draft generation constrained by approved evidence, so that it does not invent experience.
16. As a job applicant, I want Ollama claim audit, so that real-provider drafts and manual edits can still be checked against approved evidence.
17. As a job applicant, I want malformed AI output handled gracefully, so that I see an actionable error instead of broken workflow state.
18. As a job applicant, I want the app to try one repair attempt for malformed provider output, so that recoverable formatting mistakes do not force me to rerun manually.
19. As a job applicant, I want failures after repair to leave existing workflow state unchanged, so that bad output does not erase previous useful work.
20. As a developer, I want provider selection to be configuration-driven, so that fake and Ollama modes can be switched without code changes.
21. As a developer, I want Ollama endpoint, model, timeout, and raw payload storage settings in configuration, so that local model behavior is explicit.
22. As a developer, I want prompts stored as backend markdown files, so that prompt changes are reviewable.
23. As a developer, I want strict JSON contracts per AI operation, so that provider output can be deserialized into known DTOs.
24. As a developer, I want output DTO validation separated from transport calls, so that the trust rules are testable.
25. As a developer, I want one repair attempt for invalid JSON or invalid structure, so that provider tolerance is bounded.
26. As a developer, I want minimal `AiRun` records for AI workflow actions, so that provider failures can be diagnosed.
27. As a developer, I want `AiRun` records to include step, provider, model, status, timing, attempt count, and minimal summaries, so that issues can be inspected without raw payloads.
28. As a developer, I want raw request and response payload storage disabled by default, so that sensitive application data is not retained unnecessarily.
29. As a developer, I want tests for unavailable provider behavior, so that Ollama setup failures remain graceful.
30. As a developer, I want tests for invalid JSON repair and failure paths, so that model formatting drift does not silently break the workflow.

## Implementation Decisions

- Keep the existing internal AI provider abstraction as the workflow boundary for fake and Ollama providers.
- Extend the provider abstraction only where needed for status and diagnostics behavior.
- Select the active provider from configuration.
- Keep `FakeAiProvider` as the deterministic default unless configuration selects Ollama.
- Add `OllamaAiProvider` as the first real local provider.
- Configure Ollama with provider, endpoint, model, timeout seconds, and raw payload storage settings.
- Ensure the app starts even when the configured Ollama endpoint is unavailable.
- Return plain unavailable errors for AI workflow actions when Ollama cannot be reached.
- Add a read-only AI status endpoint for current provider, model, configured endpoint, and availability.
- Add a manually triggered diagnostics endpoint for connectivity and model readiness.
- Store prompts as backend markdown files grouped by AI operation.
- Request strict JSON from Ollama for job analysis, evidence matching, draft generation, and claim audit.
- Deserialize provider output into operation-specific DTOs before applying it to application state.
- Validate deserialized output for required fields, expected enum-like values, evidence boundaries, and stable identifiers where applicable.
- Allow exactly one repair attempt when the initial provider output is invalid JSON or fails structural validation.
- Fail gracefully when the repair attempt also fails.
- Do not mutate application workflow state when provider output remains invalid after repair.
- Use minimal `AiRun` tracking for AI workflow actions and diagnostics where useful.
- Record step, provider, model, status, error code, error message, attempt count, started/completed timestamps, and minimal input/output summaries.
- Do not store raw requests or responses by default.
- Preserve the existing fake-provider workflow behavior.
- Keep provider settings read-only in the frontend for this milestone.
- Show provider/model status on the home workbench and the settings AI area.
- Keep TXT export, DOCX export, application history improvements, and general UI polish out of this milestone.

## Testing Decisions

- Test behavior through public API endpoints and provider-facing interfaces where practical.
- Keep tests focused on trust boundaries: provider availability, JSON validation, repair, state mutation, and `AiRun` tracking.
- Cover provider selection from configuration.
- Cover app startup when Ollama is configured but unavailable.
- Cover AI workflow actions returning plain unavailable errors when Ollama cannot be reached.
- Cover AI status endpoint behavior for fake and Ollama configuration.
- Cover diagnostics endpoint behavior for successful and failed Ollama connectivity.
- Cover strict JSON deserialization and validation for job analysis.
- Cover strict JSON deserialization and validation for evidence matching.
- Cover strict JSON deserialization and validation for draft generation.
- Cover strict JSON deserialization and validation for claim audit.
- Cover one repair attempt after malformed JSON.
- Cover one repair attempt after structurally invalid JSON.
- Cover failure after repair leaving existing workflow state unchanged.
- Cover minimal `AiRun` records for success, repaired success, unavailable provider failure, and invalid output failure.
- Keep frontend tests light unless existing frontend setup makes a small status/diagnostics component test natural.
- Do not add export, application history, or broad visual polish tests in this milestone.

## Out of Scope

- OpenAI provider support.
- Editing provider settings in the UI.
- Streaming generation.
- Raw request or response payload storage by default.
- Prompt tuning beyond what is needed for strict JSON operation prompts.
- TXT export.
- DOCX export.
- Application history and saved-session filters beyond existing behavior.
- Custom DOCX templates.
- CV import or extraction.
- Draft history or profile fact revision history.
- Mobile-first UI work.
- Advanced application tracking or analytics.

## Further Notes

Milestone 5 is a reliability milestone as much as a provider milestone. The user should be able to experiment with a local model while the app continues to protect the workflow from malformed output, unavailable services, and unsupported claims.

The fake provider remains first-class after this milestone. It should continue to provide deterministic behavior for development, tests, and fallback workflow validation.
