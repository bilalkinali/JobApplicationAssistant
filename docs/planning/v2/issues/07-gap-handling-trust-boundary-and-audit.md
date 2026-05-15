# Gap Handling Trust Boundary and Audit

Status: done
Type: AFK

## Parent

docs/planning/v2/milestone-2-prd.md

## What to build

Complete the Milestone 2 trust boundary by making sure gap handling remains visibly separate from approved evidence and claim audit still catches unsupported draft claims.

The user should be able to see which parts of evidence review are approved proof, which are learning-interest decisions, and which are ignored gaps. After generation, claim audit should continue to treat unsupported concrete claims as unsupported, including cases where the draft overreaches from a learning-interest gap or an unapproved custom fact.

## Acceptance criteria

- [x] Evidence review clearly distinguishes approved evidence, approved job-local custom facts, ignored gaps, and learning-interest gaps.
- [x] Learning-interest decisions do not appear as approved evidence in review, generation, or audit inputs.
- [x] Ignored gaps do not appear as approved evidence in review, generation, or audit inputs.
- [x] Claim audit flags unsupported concrete claims that overreach from learning-interest wording.
- [x] Claim audit flags unsupported concrete claims that rely on unapproved job-local custom facts.
- [x] Claim audit accepts concrete claims supported by approved profile facts or approved job-local custom facts.
- [x] The guided next action reaches draft generation only after approved evidence exists and all unmatched requirements have explicit handling decisions.
- [x] Focused tests cover the trust-boundary behavior across evidence review, generation, and audit.
- [x] No broad visual redesign, provider settings UI, copy/export redesign, authentication, or multi-user work is introduced.

## Blocked by

- docs/planning/v2/issues/06-gap-aware-draft-generation.md
