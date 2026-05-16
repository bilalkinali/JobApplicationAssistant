# PDF CV Assisted Import Foundation

Status: done
Type: AFK

## Parent

docs/planning/v2/v2.1-assisted-profile-import-prd.md

## What to build

Add the foundation for assisted profile import from an uploaded PDF CV. A user can upload a PDF CV, the app can create an import session, and the extracted CV text can be sent through the configured AI provider to produce draft profile facts that are visible for review but cannot support application claims until approved.

This slice should establish the core import operation, deterministic Fake AI behavior for tests and demos, structured extraction normalization, source snapshots where useful, and the trust boundary around imported facts.

## Acceptance criteria

- [x] A user can start an assisted profile import by uploading a PDF CV.
- [x] Non-PDF files are rejected with a clear validation error.
- [x] The import uses the configured AI provider through the existing provider abstraction.
- [x] Fake AI returns deterministic assisted-import facts and remains clearly identifiable as demo/test behavior.
- [x] Successful import creates draft imported profile facts with reusable profile fact fields where possible, including title, type, summary, fact items, technologies, allowed claims, forbidden claims, source context, and original import snapshot where useful.
- [x] Draft imported facts are not treated as approved profile facts and cannot support evidence matching, draft generation, or claim audit.
- [x] The import response gives the frontend enough metadata to route the user into review.
- [x] Backend API tests cover PDF CV import creating draft facts and preserving the trust boundary.
- [x] The import accepts PDF CV upload only; no alternate CV text entry, DOCX import, merge/split review actions, automatic approval, CV generation, or provider settings UI is introduced.

## Blocked by

None - can start immediately.
