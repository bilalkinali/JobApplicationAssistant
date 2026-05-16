# PDF Text Extraction Boundary

Status: done
Type: AFK

## Parent

docs/planning/v2/v2.1-assisted-profile-import-prd.md

## What to build

Add the PDF text extraction boundary for the assisted profile import workflow. A user's uploaded PDF CV is converted to text before AI extraction, and the extracted text feeds the same draft-fact import session created by the foundation slice.

The slice should keep document layout editing out of scope and keep file text extraction separated from profile persistence and review decisions.

## Acceptance criteria

- [x] Uploaded PDF CV files are converted to text before AI extraction.
- [x] The PDF extraction boundary reports clear validation for unreadable, empty, encrypted, or malformed PDFs.
- [x] PDF imports reuse the same AI extraction operation and draft imported fact model as the import foundation.
- [x] Successful PDF import creates an import session that routes into review without approving any facts automatically.
- [x] File parsing concerns are isolated enough that workflow tests do not require brittle binary fixture parsing.
- [x] Tests cover successful extracted PDF text import at the service boundary and unreadable, empty, encrypted, or malformed PDF validation.
- [x] The extraction path accepts PDF CV upload only; no alternate CV text entry, DOCX import, CV layout preservation, CV editing, cloud OCR, external document service, or automatic approval is introduced.

## Blocked by

- docs/planning/v2/issues/18-pdf-cv-assisted-import-foundation.md
