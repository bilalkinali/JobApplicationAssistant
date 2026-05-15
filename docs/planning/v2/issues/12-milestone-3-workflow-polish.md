# Milestone 3 Workflow Polish

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/milestone-3-prd.md

## What to build

Polish the Milestone 3 guided workflow so provider readiness, fake-mode labeling, draft review, audit state, and copy/export progression feel like one assistant-led path. This issue should tighten rough edges after the core slices land, without introducing new provider settings, profile import, export redesign, authentication, or broad visual redesign.

The completed milestone should be demoable as a normal application path: prepare application, review evidence and gaps, generate and audit draft, review or edit draft, refresh audit if stale, then copy/export when current.

## Acceptance criteria

- [ ] The application detail page has at most one visually dominant guided next action throughout the Milestone 3 path.
- [ ] Secondary workflow actions remain available only where useful and do not compete visually with the guided next action.
- [ ] Fake-mode draft output is labeled plainly during review.
- [ ] Provider readiness messaging is consistent between workbench, preparation, and draft generation surfaces.
- [ ] Draft review, stale audit, audit refresh, and copy/export states use consistent language.
- [ ] The end-to-end happy path is demoable with fake AI.
- [ ] The real-provider unavailable path is demoable with recoverable readiness messaging.
- [ ] Existing export mechanisms are reachable from the final guided action when audit is current.
- [ ] No OpenAI support, editable provider settings, profile import, export redesign, authentication, multi-user support, or broad visual redesign is added.

## Blocked by

- docs/planning/v2/issues/08-provider-readiness-and-fake-mode-labeling.md
- docs/planning/v2/issues/09-assistant-led-draft-generation-and-audit.md
- docs/planning/v2/issues/10-provider-failure-stable-draft-recovery.md
- docs/planning/v2/issues/11-draft-review-audit-staleness-guided-actions.md
