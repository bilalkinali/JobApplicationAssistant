# Expanded CV Import Fact Coverage

Status: done
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-1-prd.md

## What to build

Revise assisted profile import so a rich CV produces a broader library of granular, reviewable draft profile facts instead of a small set of high-signal evidence snippets. Imported facts should cover technical skills, tools, competencies, projects, work history, education, languages, business experience, transferable strengths, allowed claims, and risky claims to avoid.

The slice should keep the existing profile fact trust boundary intact: extraction may propose facts, but those facts remain draft until a user approves them.

## Acceptance criteria

- [x] Full CV import no longer prefers only `1-5` high-signal facts.
- [x] A rich CV import can produce draft facts for skills, tools, projects, work history, education, languages, competencies, transferable strengths, allowed claims, and forbidden or risky claims.
- [x] Imported facts remain in `Draft` status until the user reviews and approves them.
- [x] Draft competencies and transferable strengths are not available for application evidence matching until approved.
- [x] The import prompt and output contract describe the expanded fact coverage clearly enough for local model output to be validated.
- [x] Public API or workflow tests cover a rich CV producing multiple draft fact types.
- [x] Public API or workflow tests cover imported competencies and transferable strengths remaining unusable for matching until approved.

## Blocked by

None - can start immediately.
