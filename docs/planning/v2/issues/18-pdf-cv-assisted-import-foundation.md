# PDF CV Assisted Import Foundation

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/v2.1-assisted-profile-import-prd.md

## What to build

Add the foundation for assisted profile import from an uploaded PDF CV. A user can upload a PDF CV, the app can create an import session, and the extracted CV text can be sent through the configured AI provider to produce draft profile facts that are visible for review but cannot support application claims until approved.

This slice should establish the core import operation, deterministic Fake AI behavior for tests and demos, structured extraction normalization, source snapshots where useful, and the trust boundary around imported facts.

## Acceptance criteria

- [ ] A user can start an assisted profile import by uploading a PDF CV.
- [ ] Non-PDF files are rejected with a clear validation error.
- [ ] The import uses the configured AI provider through the existing provider abstraction.
- [ ] Fake AI returns deterministic assisted-import facts and remains clearly identifiable as demo/test behavior.
- [ ] Successful import creates draft imported profile facts with reusable profile fact fields where possible, including title, type, summary, fact items, technologies, allowed claims, forbidden claims, source context, and original import snapshot where useful.
- [ ] Draft imported facts are not treated as approved profile facts and cannot support evidence matching, draft generation, or claim audit.
- [ ] The import response gives the frontend enough metadata to route the user into review.
- [ ] Backend API tests cover PDF CV import creating draft facts and preserving the trust boundary.
- [ ] The import accepts PDF CV upload only; no alternate CV text entry, DOCX import, merge/split review actions, automatic approval, CV generation, or provider settings UI is introduced.

## Blocked by

None - can start immediately.
