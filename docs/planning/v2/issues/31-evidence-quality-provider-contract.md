# Evidence Quality Provider Contract

Status: done
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-2-prd.md

## What to build

Extend evidence matching so every supported match carries a validated quality label and reviewer-facing reason. The provider contract should distinguish strong proof, partial proof, and weak contextual matches, while leaving unsupported job signals unmatched instead of forcing a low-confidence match.

This slice should make evidence quality deterministic and testable across real providers and Fake AI before changing the review experience.

## Acceptance criteria

- [x] Evidence matching output includes `quality` and `reason` for each returned match.
- [x] Supported quality values are exactly `Strong`, `Partial`, and `Weak`.
- [x] Provider output validation rejects missing, malformed, or unknown quality values.
- [x] Unsupported job signals can remain unmatched rather than being represented as weak evidence.
- [x] Candidate fit brief context is available to evidence matching input where useful, without treating fit brief references as approved evidence.
- [x] Fake AI returns deterministic strong, partial, weak, and unmatched evidence scenarios suitable for tests and demos.
- [x] Provider tests cover valid quality values, invalid quality values, and unsupported signals becoming unmatched requirements.

## Blocked by

None - can start immediately.
