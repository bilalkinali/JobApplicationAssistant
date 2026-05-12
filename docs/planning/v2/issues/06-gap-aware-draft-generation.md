# Gap-Aware Draft Generation

Status: done
Type: AFK

## Parent

docs/planning/v2/milestone-2-prd.md

## What to build

Feed evidence review gap decisions into draft generation so generated text respects what the user decided about unmatched requirements.

Draft generation should receive approved profile facts, approved job-local custom facts, and lightweight gap decisions together. Ignored gaps should not be addressed unless they are independently supported by approved evidence. Learning-interest gaps may shape cautious wording, but they must not be treated as approved evidence. Covered gaps may support concrete claims only through the linked approved job-local custom fact.

## Acceptance criteria

- [x] Draft generation receives saved gap decisions for the application.
- [x] Draft generation receives approved job-local custom facts for the application.
- [x] Draft generation excludes draft, rejected, or unapproved job-local custom facts.
- [x] `Ignore` gap decisions tell generation not to address the requirement unless other approved evidence supports it.
- [x] `MentionAsLearningInterest` gap decisions are available as cautious context but are not passed as approved evidence.
- [x] `CoveredByCustomFact` gap decisions support generation only through the linked approved job-local custom fact.
- [x] Fake AI remains deterministic when gap decisions and job-local custom facts are present.
- [x] Real provider prompt/input contracts preserve the rule that concrete claims require approved evidence.
- [x] Backend tests cover generation inputs for ignored gaps, learning-interest gaps, approved custom facts, and unapproved custom facts.
- [x] No combined draft-and-audit action, copy/export guided action, provider redesign, or profile import is introduced.

## Blocked by

- docs/planning/v2/issues/04-gap-decisions-in-evidence-review.md
- docs/planning/v2/issues/05-reviewed-job-local-custom-facts.md
