# Milestone 5 Workflow Polish

Status: in-progress
Type: AFK

## Parent

docs/planning/milestone-5-prd.md

## What to build

Tighten the user-facing Milestone 5 experience across the home workbench, settings AI area, and application detail workflow. The user should understand which provider is active, whether it is available, when diagnostics last ran, and why an AI action failed when Ollama is unavailable or returns invalid output.

This is a polish and integration slice for Milestone 5. It should make the completed Ollama provider, status, diagnostics, JSON repair, and `AiRun` failure behavior coherent in the UI without adding new AI capabilities beyond the blocking issues.

This slice should not add export, provider settings editing, streaming generation, draft history, or advanced application tracking.

## Acceptance criteria

- [x] The home workbench shows the active provider, model, and availability in a compact status area.
- [x] The settings AI area shows read-only provider, endpoint where applicable, model, availability, and diagnostics result details.
- [x] The settings AI area has consistent loading, success, unavailable, and error states for diagnostics.
- [x] Application detail AI workflow actions show clear unavailable-provider errors when Ollama cannot be reached.
- [x] Application detail AI workflow actions show clear invalid-output errors when initial output and repair both fail.
- [x] Existing workflow state remains visible after an AI action fails.
- [x] The UI wording distinguishes provider unavailability from validation blockers such as missing job posting text or missing approved evidence.
- [x] The fake provider workflow remains clear and deterministic in the UI.
- [x] The frontend does not expose raw provider requests or responses.
- [x] The UI remains desktop-first and consistent with the existing application detail structure.
- [x] A small frontend test is added only if the existing frontend setup already supports it naturally.
- [x] No export, provider settings editing, streaming generation, draft history, or advanced application tracking is introduced.

## Blocked by

- docs/planning/issues/17-ai-provider-configuration-status-and-diagnostics.md
- docs/planning/issues/18-ollama-job-analysis-json-repair-and-airun.md
- docs/planning/issues/19-ollama-evidence-matching-json-repair-and-airun.md
- docs/planning/issues/20-ollama-draft-generation-and-claim-audit-json-repair.md
