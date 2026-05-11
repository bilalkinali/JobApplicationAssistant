# Fake AI Evidence Matching

Status: done
Type: AFK

## Parent

docs/planning/milestone-3-prd.md

## What to build

Add deterministic evidence matching between analyzed job signals and Approved profile facts. The user should be able to run matching after job analysis and see which approved evidence supports the posting, along with unmatched requirements that have no approved supporting evidence.

This slice should keep matching honest: Draft and Archived profile facts must not be used, and unmatched requirements should remain visible rather than being softened into fake experience.

## Acceptance criteria

- [x] The fake provider supports evidence matching against analyzed job signals.
- [x] Evidence matching uses only Approved profile facts.
- [x] Draft and Archived profile facts are excluded from matching.
- [x] Matching is blocked with a plain validation error when no approved profile facts exist.
- [x] Matching is blocked or clearly unavailable when the application has no analyzed job signals.
- [x] Matching results identify the job signal and supporting profile fact.
- [x] Unmatched requirements are persisted when no approved profile fact supports a signal.
- [x] Unmatched requirements include interest-to-learn recommendations where the fake provider can classify them that way.
- [x] The frontend application detail view can trigger evidence matching after analysis.
- [x] The frontend shows matched evidence and unmatched requirements separately.
- [x] No approved evidence review, generation, claim audit, Ollama, diagnostics, or export behavior is introduced.

## Blocked by

- docs/planning/issues/09-fake-ai-job-analysis.md
