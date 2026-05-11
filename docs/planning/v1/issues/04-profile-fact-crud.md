# Profile Fact CRUD

Status: done
Type: AFK

## Parent

docs/planning/milestone-2-prd.md

## What to build

Add the manual profile fact workflow needed for trustworthy evidence management. The user should be able to list, create, edit, approve, archive, and delete profile facts from the Profile area, with backend validation and plain API errors.

This slice remains manual and non-AI. It should not implement evidence matching, custom fact normalization, generation, or claim audit.

## Acceptance criteria

- [x] The backend supports listing profile facts.
- [x] The backend supports creating profile facts with type, title, summary, technologies, allowed claims, forbidden claims, and status.
- [x] The backend supports updating profile facts.
- [x] The backend supports deleting profile facts.
- [x] Profile fact status is limited to Draft, Approved, and Archived.
- [x] Basic validation blocks missing titles, missing summaries, invalid status values, and malformed structured fields.
- [x] The frontend Profile area includes a profile facts section for creating, listing, editing, approving, archiving, and deleting facts.
- [x] Archived facts remain visible but visually separate from active evidence.
- [x] API errors use the existing plain error shape.

## Blocked by

None - can start immediately.
