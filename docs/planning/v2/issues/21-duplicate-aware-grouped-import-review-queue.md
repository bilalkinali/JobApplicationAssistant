# Duplicate-Aware Grouped Import Review Queue

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/v2.1-assisted-profile-import-prd.md

## What to build

Add the review queue shape for imported draft facts. A user should see extracted facts grouped by type or theme, with enough source context to verify each fact and duplicate or overlapping facts clearly identified before review decisions are made.

This slice should make the review queue feel organized rather than like a random extraction dump.

## Acceptance criteria

- [ ] A user can list draft facts for an import session.
- [ ] Draft facts are grouped by type or theme in the API response and frontend review queue.
- [ ] Each draft fact shows concise source context from the uploaded PDF CV.
- [ ] Duplicate or overlapping draft facts are detected within the import batch.
- [ ] Duplicate or overlapping draft facts are detected against existing profile facts.
- [ ] Duplicate indicators do not automatically reject, merge, approve, or archive facts.
- [ ] The review queue avoids exposing the full raw document everywhere while preserving enough context for verification.
- [ ] Tests cover grouped listing, source context display data, duplicate detection within a batch, and duplicate detection against existing facts.

## Blocked by

- docs/planning/v2/issues/18-pdf-cv-assisted-import-foundation.md
