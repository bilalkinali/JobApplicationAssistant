# Application List and Detail Separation

Status: done
Type: AFK

## Parent

docs/planning/milestone-2-prd.md

## What to build

Separate application browsing from application editing so the frontend has a clearer workflow shape. The Applications area should distinguish the list of saved sessions from the selected application detail editor, preparing the UI for later analysis, evidence review, generation, and audit sections.

This slice should improve structure without adding AI behavior.

## Acceptance criteria

- [x] The frontend has a distinct application list surface for browsing saved sessions.
- [x] The frontend has a distinct application detail surface for editing the selected session.
- [x] Starting a new application opens a clean detail editor.
- [x] Opening an existing application loads that session into the detail editor.
- [x] Empty, loading, and validation-error states are clear in the relevant surface.
- [x] The layout remains desktop-first.
- [x] The UI structure can later host job analysis, evidence review, draft, and audit sections without redesigning the whole screen.

## Blocked by

- docs/planning/issues/05-application-workflow-state.md
