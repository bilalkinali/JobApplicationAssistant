# Transferable Competencies And Gap Wording

Status: done
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-3-prd.md

## What to build

Make draft generation use supported transferable competencies and broader candidate context where useful, while continuing to handle gaps according to the user's review decisions. Competencies, education, business experience, communication strengths, location, language fit, and technical architecture themes may shape prose when present in the strategy or selected fit brief context, but concrete claims still need approved evidence or approved custom facts.

The slice should keep ignored gaps quiet and turn learning-interest decisions into cautious wording rather than implied experience.

## Acceptance criteria

- [x] Draft generation can use transferable competencies from the strategy or selected fit brief context when they are relevant to the job.
- [x] Concrete competency or soft-skill claims are grounded in approved evidence or approved custom facts.
- [x] `MentionAsLearningInterest` gap decisions produce cautious motivation or learning-interest wording only.
- [x] Ignored gaps are not emphasized unless independently supported by approved evidence or approved custom facts.
- [x] Tests cover cautious wording for `MentionAsLearningInterest`.
- [x] Tests cover ignored gaps not being emphasized.
- [x] Tests cover unapproved custom facts and unapproved imported profile facts being excluded from draft proof context.

## Blocked by

None - can start immediately
