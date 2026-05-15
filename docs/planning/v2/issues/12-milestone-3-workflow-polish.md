# Milestone 3 Workflow Polish

Status: done
Type: AFK

## Parent

docs/planning/v2/milestone-3-prd.md

## What to build

Polish the Milestone 3 guided workflow so provider readiness, fake-mode labeling, draft review, audit state, and copy/export progression feel like one assistant-led path. This issue should tighten rough edges after the core slices land, without introducing new provider settings, profile import, export redesign, authentication, or broad visual redesign.

The completed milestone should be demoable as a normal application path: prepare application, review evidence and gaps, generate and audit draft, review or edit draft, refresh audit if stale, then copy/export when current.

## Acceptance criteria

- [x] The application detail page has at most one visually dominant guided next action throughout the Milestone 3 path.
- [x] Secondary workflow actions remain available only where useful and do not compete visually with the guided next action.
- [x] Fake-mode draft output is labeled plainly during review.
- [x] Provider readiness messaging is consistent between workbench, preparation, and draft generation surfaces.
- [x] Draft review, stale audit, audit refresh, and copy/export states use consistent language.
- [x] The end-to-end happy path is demoable with fake AI.
- [x] The real-provider unavailable path is demoable with recoverable readiness messaging.
- [x] Existing export mechanisms are reachable from the final guided action when audit is current.
- [x] No OpenAI support, editable provider settings, profile import, export redesign, authentication, multi-user support, or broad visual redesign is added.

## Blocked by

- docs/planning/v2/issues/08-provider-readiness-and-fake-mode-labeling.md
- docs/planning/v2/issues/09-assistant-led-draft-generation-and-audit.md
- docs/planning/v2/issues/10-provider-failure-stable-draft-recovery.md
- docs/planning/v2/issues/11-draft-review-audit-staleness-guided-actions.md
