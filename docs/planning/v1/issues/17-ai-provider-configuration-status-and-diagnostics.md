# AI Provider Configuration, Status, and Diagnostics

Status: done
Type: AFK

## Parent

docs/planning/milestone-5-prd.md

## What to build

Add the configuration and read-only status foundation for Milestone 5. The app should be able to select the active AI provider from configuration, report the configured provider/model status through an AI status endpoint, and run a manual diagnostics endpoint without requiring any application workflow action.

The user should see the current provider/model availability on the home workbench and in the settings AI area. Fake provider status should remain deterministic and available. Ollama status and diagnostics should report unavailable clearly when the local endpoint cannot be reached, while the app itself still starts normally.

This slice should not implement Ollama workflow operations, strict JSON repair for workflow responses, prompt files, or export behavior.

## Acceptance criteria

- [x] The backend selects the active AI provider from configuration.
- [x] The configuration supports provider, endpoint, model, timeout seconds, and raw payload storage settings.
- [x] The fake provider remains available through the same configured provider selection path.
- [x] The app starts when Ollama is configured but unavailable.
- [x] A public AI status endpoint returns provider, model, endpoint where applicable, availability, and a plain status message.
- [x] A public diagnostics endpoint can be triggered manually.
- [x] Diagnostics report fake provider readiness deterministically.
- [x] Diagnostics report Ollama connectivity and model readiness when Ollama is configured.
- [x] Diagnostics return clear unavailable information when Ollama cannot be reached.
- [x] The home workbench shows current provider/model status.
- [x] The settings AI area shows read-only provider/model/status details.
- [x] The settings AI area can trigger diagnostics and show the result.
- [x] Backend tests cover provider selection, fake status, unavailable Ollama status, and diagnostics behavior.
- [x] No Ollama workflow operations, strict JSON repair for workflow responses, prompt files, export, or provider settings editing is introduced.

## Blocked by

None - can start immediately.
