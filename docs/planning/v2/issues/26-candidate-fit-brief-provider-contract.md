# Candidate Fit Brief Provider Contract

Status: done
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-1-prd.md

## What to build

Add a strict AI provider contract for generating a candidate fit brief from the current application, job posting or job signals, selected language, tone preference where available, and all approved profile facts. The fit brief is an AI workflow artifact that summarizes candidate fit for the specific job and maps concrete items back to supporting approved profile fact ids for traceability.

The slice should make the contract deterministic and testable across real providers and Fake AI without wiring it into the main preparation orchestration yet.

## Acceptance criteria

- [x] The AI provider interface supports candidate fit brief generation.
- [x] Candidate fit brief input includes application metadata, job posting or job signals, selected language, tone preference where available, and all approved profile facts.
- [x] Candidate fit brief output includes candidate summary, skill groups, competencies, relevant projects, transferable strengths, and risk notes.
- [x] Concrete fit brief items can reference supporting approved profile fact ids for traceability.
- [x] Output validation rejects malformed JSON, missing required arrays, and references to invalid profile fact ids.
- [x] Fake AI returns deterministic candidate fit brief artifacts suitable for tests and demos.
- [x] Provider tests cover valid output, malformed output, missing arrays, invalid ids, and deterministic Fake AI behavior.

## Blocked by

None - can start immediately.
