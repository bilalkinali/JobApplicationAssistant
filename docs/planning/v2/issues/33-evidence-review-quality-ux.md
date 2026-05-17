# Evidence Review Quality UX

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-2-prd.md

## What to build

Update evidence review so users can judge match strength before approving evidence. Strong, partial, and weak matches should be visually distinct, include the provider reason, and make weak matches feel deliberately questionable without adding a complex new review screen.

The slice should help the user avoid approving weak proof by accident while preserving the familiar evidence approval workflow.

## Acceptance criteria

- [ ] Evidence review displays quality labels for strong, partial, and weak matches.
- [ ] Evidence review displays the reason for each match where available.
- [ ] Weak matches are visually distinct from strong and partial matches.
- [ ] Weak matches require deliberate opt-in review before they can support generation.
- [ ] Partial matches are presented as usable but cautious evidence rather than full-strength proof.
- [ ] Unsupported job signals remain visible as unmatched instead of appearing as weak evidence.
- [ ] Frontend tests cover quality labels and weak-match review behavior where existing test seams support it.

## Blocked by

- docs/planning/v2/issues/32-prepare-application-evidence-quality-state.md

