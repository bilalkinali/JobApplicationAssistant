# Gap-Aware Draft Generation

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v2/milestone-2-prd.md

## What to build

Feed evidence review gap decisions into draft generation so generated text respects what the user decided about unmatched requirements.

Draft generation should receive approved profile facts, approved job-local custom facts, and lightweight gap decisions together. Ignored gaps should not be addressed unless they are independently supported by approved evidence. Learning-interest gaps may shape cautious wording, but they must not be treated as approved evidence. Covered gaps may support concrete claims only through the linked approved job-local custom fact.

## Acceptance criteria

- [ ] Draft generation receives saved gap decisions for the application.
- [ ] Draft generation receives approved job-local custom facts for the application.
- [ ] Draft generation excludes draft, rejected, or unapproved job-local custom facts.
- [ ] `Ignore` gap decisions tell generation not to address the requirement unless other approved evidence supports it.
- [ ] `MentionAsLearningInterest` gap decisions are available as cautious context but are not passed as approved evidence.
- [ ] `CoveredByCustomFact` gap decisions support generation only through the linked approved job-local custom fact.
- [ ] Fake AI remains deterministic when gap decisions and job-local custom facts are present.
- [ ] Real provider prompt/input contracts preserve the rule that concrete claims require approved evidence.
- [ ] Backend tests cover generation inputs for ignored gaps, learning-interest gaps, approved custom facts, and unapproved custom facts.
- [ ] No combined draft-and-audit action, copy/export guided action, provider redesign, or profile import is introduced.

## Blocked by

- docs/planning/v2/issues/04-gap-decisions-in-evidence-review.md
- docs/planning/v2/issues/05-reviewed-job-local-custom-facts.md
