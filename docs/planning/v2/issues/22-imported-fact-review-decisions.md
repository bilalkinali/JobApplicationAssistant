# Imported Fact Review Decisions

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/v2.1-assisted-profile-import-prd.md

## What to build

Add the core review decisions for imported draft facts. A user can approve, edit then approve, archive, or reject individual imported facts. Approved imported facts become normal trusted profile facts and immediately work with the existing application workflow.

This slice should prove the trust boundary end-to-end: draft facts are inert, approved facts are reusable evidence.

## Acceptance criteria

- [ ] A user can approve an imported draft fact.
- [ ] A user can edit an imported draft fact before approving it.
- [ ] A user can archive an imported draft fact without approving it.
- [ ] A user can reject an imported draft fact without approving it.
- [ ] Approved imported facts become normal approved profile facts using the existing trust boundary.
- [ ] Approved imported facts can support evidence matching in the application workflow.
- [ ] Archived or rejected imported facts cannot support evidence matching, draft generation, or claim audit.
- [ ] The frontend review queue updates after each individual review decision.
- [ ] Tests cover approve, edit-approve, archive, reject, and approved imported facts being usable as normal evidence.

## Blocked by

- docs/planning/v2/issues/21-duplicate-aware-grouped-import-review-queue.md
