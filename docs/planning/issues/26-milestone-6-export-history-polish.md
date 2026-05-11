# Milestone 6 Export and History Polish

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/milestone-6-prd.md

## What to build

Tighten the completed Milestone 6 experience across application history and the draft/export area. The user should understand which sessions are active or archived, which drafts are ready to export, when audit is stale or missing, and what each export action will produce.

This slice should integrate the completed history, status, TXT export, and DOCX export behavior into the existing desktop-first workbench. It should include copy-to-clipboard for the current cover letter if supported naturally in the frontend. It should not add new backend export formats or new workflow capabilities.

Use TDD where practical for this slice: add behavior tests for any public backend adjustments first, and for frontend logic prefer small tests only when the existing setup supports them naturally. Do not create broad brittle visual tests.

## Acceptance criteria

- [ ] Any backend behavior adjustment starts with a failing public-interface test.
- [ ] Behaviors are added incrementally through red-green-refactor cycles rather than one large horizontal implementation.
- [ ] Tests focus on observable behavior and stable contracts rather than private component or module details.
- [ ] The application history view presents status, archive visibility, draft readiness, and audit readiness coherently.
- [ ] The draft/export area clearly warns when claim audit is stale.
- [ ] The draft/export area clearly warns when claim audit has not been run.
- [ ] Stale or missing audit warnings do not block TXT or DOCX export when a current draft exists.
- [ ] Export actions are disabled or explained when no generated draft exists.
- [ ] Export actions are disabled or explained when cover letter text is empty.
- [ ] The user can copy the current cover letter text when a generated draft exists and browser support is available.
- [ ] TXT and DOCX download actions use the backend export endpoints.
- [ ] Export errors are shown plainly without hiding the current draft.
- [ ] The UI remains desktop-first and consistent with the existing workbench.
- [ ] No PDF export, custom DOCX templates, draft history, application event history, advanced tracking, or new AI behavior is introduced.

## Blocked by

- docs/planning/issues/22-application-history-readiness-filters.md
- docs/planning/issues/23-application-final-status-tracking.md
- docs/planning/issues/24-cover-letter-txt-export.md
- docs/planning/issues/25-cover-letter-docx-export.md
