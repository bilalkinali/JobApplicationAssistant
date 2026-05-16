# Application Detail Fit Brief Summary

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-1-prd.md

## What to build

Show a compact candidate fit brief summary on the application detail page after preparation. The user should be able to inspect whether the app understood their broader relevance for the job, including candidate summary, skill groups, competencies, projects, transferable strengths, and risk notes.

The slice should expose the generated context for review without making the fit brief editable and without changing draft generation.

## Acceptance criteria

- [ ] After preparation, the application detail page renders a compact candidate fit brief summary when one is available.
- [ ] The summary includes candidate summary, skill groups, competencies, relevant projects, transferable strengths, and risk notes.
- [ ] Risk notes or weak unsupported themes are visible enough for the user to judge whether the context overstates their experience.
- [ ] The UI does not present candidate fit brief profile fact ids as approved evidence.
- [ ] The fit brief is read-only in this milestone.
- [ ] Fake AI fit brief output remains clearly suitable for deterministic demos.
- [ ] Frontend tests cover rendering candidate summary, skill groups, competencies, projects, and risk notes where existing test seams support it.

## Blocked by

- docs/planning/v2/issues/27-prepare-application-candidate-fit-brief-state.md
