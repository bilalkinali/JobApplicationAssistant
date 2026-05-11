# Ollama Job Analysis JSON Repair and AiRun Tracking

Status: done
Type: AFK

## Parent

docs/planning/milestone-5-prd.md

## What to build

Add the first real Ollama workflow operation: job posting analysis. When Ollama is the configured provider, the user should be able to run the existing job analysis action and receive company, role, detected language, selected language defaults, and job signals from a strict JSON provider response.

The operation should use a backend-owned prompt, deserialize and validate strict JSON output, attempt one repair pass for malformed or structurally invalid output, and update application state only after valid output. Each run should create minimal `AiRun` tracking for success, repaired success, unavailable provider failure, and invalid output failure.

This slice should not add Ollama evidence matching, draft generation, claim audit, export, or provider settings editing.

## Acceptance criteria

- [x] Ollama job analysis is available through the existing job analysis workflow action when Ollama is configured.
- [x] The job analysis prompt is stored as a backend markdown file.
- [x] The Ollama request asks for strict JSON matching the job analysis contract.
- [x] Valid Ollama analysis updates company, role, detected language, selected language default, and job signals.
- [x] Job analysis remains blocked with a plain validation error when the application has no job posting text.
- [x] Ollama unavailable errors are returned plainly and do not prevent app startup.
- [x] Malformed JSON receives exactly one repair attempt.
- [x] Structurally invalid JSON receives exactly one repair attempt.
- [x] Application workflow state is not mutated when output remains invalid after repair.
- [x] `AiRun` records success with step, provider, model, status, attempt count, timing, and minimal summaries.
- [x] `AiRun` records repaired success with an attempt count that reflects the repair.
- [x] `AiRun` records unavailable provider failures with error details.
- [x] `AiRun` records invalid output failures after repair with error details.
- [x] Backend tests cover valid analysis, unavailable provider, malformed JSON repair, invalid structure repair, failure after repair, unchanged state on failure, and `AiRun` records.
- [x] No Ollama evidence matching, draft generation, claim audit, export, or provider settings editing is introduced.

## Blocked by

- docs/planning/issues/17-ai-provider-configuration-status-and-diagnostics.md
