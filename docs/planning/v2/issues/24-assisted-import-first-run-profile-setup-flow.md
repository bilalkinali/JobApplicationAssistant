# Assisted Import First Run Profile Setup Flow

Status: done
Type: AFK

## Parent

docs/planning/v2/v2.1-assisted-profile-import-prd.md

## What to build

Connect assisted import into the first-run and profile enrichment experience. A user with a thin or empty profile should be guided toward uploading a PDF CV, reviewing extracted facts, approving the strong ones, and then using those facts to make the next application workflow stronger.

This slice should turn the imported-fact machinery into the intended product path: upload PDF, review, approve, apply.

## Acceptance criteria

- [x] The profile area offers assisted import as a clear setup and enrichment path.
- [x] A user can upload a PDF CV from the profile import entry point.
- [x] After import, the user is taken to the grouped review queue with the new draft facts ready for review.
- [x] After approving imported facts, the UI makes it clear that those facts are now available for applications.
- [x] A thin-profile user can complete the path from import to approved facts to evidence matching for a saved application.
- [x] Provider failures during first-run import show actionable diagnostics without damaging the existing profile.
- [x] The UI keeps Fake AI import behavior clearly labeled when Fake AI is configured.
- [x] End-to-end or integration tests cover the first-run path from assisted import through approved facts improving an application workflow.

## Blocked by

- docs/planning/v2/issues/19-pdf-text-extraction-boundary.md
- docs/planning/v2/issues/20-import-failure-diagnostics-and-output-validation.md
- docs/planning/v2/issues/23-imported-fact-merge-split-and-bulk-review.md
