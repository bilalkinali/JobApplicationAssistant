# Audit and Export Action Clarity

Status: done
Type: AFK

## Parent

docs/planning/milestone-7-prd.md

## What to build

Polish the draft, claim audit, and export area so the user can clearly understand whether a draft is ready, whether claim audit is current, stale, missing, or not applicable, and what copy, TXT download, and DOCX download actions will do.

This slice should reuse the completed Milestone 6 export behavior. It should not add new export formats, regenerate content during export, or add draft history.

## Acceptance criteria

- [x] Stale claim audit warnings are visually and verbally consistent wherever they appear in the draft/export workflow.
- [x] Missing claim audit warnings are distinct from stale audit warnings.
- [x] Stale and missing audit warnings do not block TXT or DOCX export when a current non-empty draft exists.
- [x] Export controls explain when no generated draft exists.
- [x] Export controls explain when cover letter text is empty or whitespace.
- [x] Copy-to-clipboard success and failure feedback is plain and visible without hiding the current draft.
- [x] TXT and DOCX download errors are shown plainly without hiding the current draft.
- [x] Export actions make clear that they use the current edited cover letter and do not regenerate or re-audit content.
- [x] Text areas and draft panels keep stable desktop-friendly dimensions during edit, warning, and error states.
- [x] Tests are added only for shared export readiness logic or changed stable contracts.
- [x] No PDF export, custom DOCX templates, claim audit report export, draft history, or new AI behavior is introduced.

## Blocked by

None - can start immediately.
