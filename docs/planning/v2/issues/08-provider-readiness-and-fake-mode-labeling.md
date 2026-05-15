# Provider Readiness and Fake Mode Labeling

Status: done
Type: AFK

## Parent

docs/planning/v2/milestone-3-prd.md

## What to build

Make AI provider mode and readiness visible where the user starts AI work. The workbench and application detail workflow should plainly show whether the app is using deterministic fake AI or a real configured provider, and whether the real provider is ready.

Fake AI should be labeled as demo/test behavior. Real provider readiness should reuse the existing AI status and diagnostics contracts and show recoverable details such as provider, model, endpoint reachability where available, and model availability where available.

## Acceptance criteria

- [x] The workbench shows the current AI provider mode and readiness state.
- [x] The application detail workflow shows provider readiness near `Prepare application`.
- [x] The application detail workflow shows provider readiness near draft generation when that action is available.
- [x] Fake AI mode is plainly labeled as deterministic demo/test behavior.
- [x] Real provider readiness includes provider and model details.
- [x] Ollama readiness includes endpoint reachability and model availability where the existing diagnostics can determine them.
- [x] Unavailable or failing real provider states show plain recovery guidance instead of a generic workflow failure.
- [x] AI actions do not silently fall back from a failing real provider to fake AI.
- [x] Focused tests cover provider readiness contracts and the visible guided-workflow states where existing test structure supports them.

## Blocked by

None - can start immediately
