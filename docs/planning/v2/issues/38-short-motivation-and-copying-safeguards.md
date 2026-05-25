# Short Motivation And Copying Safeguards

Status: done
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-3-prd.md

## What to build

Improve the generated short motivation so it becomes a distinct concise value proposition rather than a compressed duplicate of the cover letter. At the same time, tighten draft-generation guidance so cover letters avoid large phrase copying from the job posting, avoid generic interest statements, and avoid letting one repeated evidence item dominate unrelated paragraphs.

The slice should make the output more useful for form fields while preserving the same draft review and audit workflow.

## Acceptance criteria

- [x] Draft generation explicitly asks for the short motivation as a distinct concise pitch, not a summary of the cover letter.
- [x] Generated short motivation is non-empty when draft generation succeeds.
- [x] Tests cover short motivation being meaningfully distinct from cover letter text.
- [x] Draft generation prompt tells the provider to avoid copying large phrases from the job posting.
- [x] Draft generation prompt tells the provider to avoid generic interest statements unless grounded in concrete reasons.
- [x] Draft generation prompt tells the provider to avoid repeating one profile fact across unrelated claims unless it is the central story.
- [x] Fake provider output is updated only where needed to keep deterministic coverage of the new expectations.

## Blocked by

None - can start immediately
