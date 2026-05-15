# Provider Readiness and Fake Mode Labeling

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/milestone-3-prd.md

## What to build

Make AI provider mode and readiness visible where the user starts AI work. The workbench and application detail workflow should plainly show whether the app is using deterministic fake AI or a real configured provider, and whether the real provider is ready.

Fake AI should be labeled as demo/test behavior. Real provider readiness should reuse the existing AI status and diagnostics contracts and show recoverable details such as provider, model, endpoint reachability where available, and model availability where available.

## Acceptance criteria

- [ ] The workbench shows the current AI provider mode and readiness state.
- [ ] The application detail workflow shows provider readiness near `Prepare application`.
- [ ] The application detail workflow shows provider readiness near draft generation when that action is available.
- [ ] Fake AI mode is plainly labeled as deterministic demo/test behavior.
- [ ] Real provider readiness includes provider and model details.
- [ ] Ollama readiness includes endpoint reachability and model availability where the existing diagnostics can determine them.
- [ ] Unavailable or failing real provider states show plain recovery guidance instead of a generic workflow failure.
- [ ] AI actions do not silently fall back from a failing real provider to fake AI.
- [ ] Focused tests cover provider readiness contracts and the visible guided-workflow states where existing test structure supports them.

## Blocked by

None - can start immediately
