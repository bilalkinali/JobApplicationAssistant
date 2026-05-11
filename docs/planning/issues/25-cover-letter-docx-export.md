# Cover Letter DOCX Export

Status: done
Type: AFK

## Parent

docs/planning/milestone-6-prd.md

## What to build

Add DOCX export for the current cover letter. The user should be able to download a `.docx` document for a saved application session with a generated draft. The document should include profile contact information when present, company and role context where appropriate, and the current edited cover letter text.

Keep DOCX generation isolated behind a small export module or endpoint group so document formatting does not leak into workflow behavior. The document should be simple, readable, deterministic, and based only on current saved state.

Frontend export controls are owned by the Milestone 6 export and history polish slice. This slice should add the backend DOCX export path and any shared export contract needed by callers.

Use TDD for this slice: begin with a failing public endpoint test for a successful DOCX file response, then add document content and blocker behaviors in red-green-refactor cycles.

## Acceptance criteria

- [x] Tests assert public response behavior and inspect document contents through a stable document-reading path where practical.
- [x] A public DOCX export endpoint returns a valid DOCX file response for an application with a generated draft.
- [x] DOCX export uses the current saved cover letter text and does not regenerate, re-audit, or call an AI provider.
- [x] DOCX export includes profile contact information when present.
- [x] DOCX export tolerates missing optional profile contact fields.
- [x] DOCX export includes company and role context where appropriate.
- [x] DOCX export returns a safe readable filename based on company and role metadata, with a stable fallback.
- [x] DOCX export is blocked with a plain not-found error when the application does not exist.
- [x] DOCX export is blocked with a plain validation error when no generated draft exists.
- [x] DOCX export is blocked with a plain validation error when the cover letter text is empty or whitespace.
- [x] DOCX export remains allowed when claim audit is stale.
- [x] DOCX export remains allowed when claim audit has not been run.
- [x] No frontend export controls, PDF export, custom DOCX templates, regeneration, AI provider calls, draft history, or CV export are introduced.

## Blocked by

None - can start immediately.
