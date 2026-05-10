# Fake AI Job Analysis

Status: done
Type: AFK

## Parent

docs/planning/milestone-3-prd.md

## What to build

Add the first fake AI workflow slice for analyzing a pasted job posting. The user should be able to run analysis from an application detail view and see deterministic extracted workflow data such as company, role, detected language, selected language defaults, and job signals.

This slice should introduce the internal AI provider abstraction only as far as needed for fake job analysis. It should not implement evidence matching, draft generation, claim audit, Ollama, diagnostics, or export behavior.

## Acceptance criteria

- [x] The backend has an internal AI provider abstraction for job analysis.
- [x] A fake provider can analyze job posting text deterministically.
- [x] Job analysis is blocked with a plain validation error when the application has no job posting text.
- [x] Running analysis updates the application with extracted job signals and detected language.
- [x] Company name and role title are updated when the fake analysis can infer them.
- [x] Selected language is defaulted from detected language when no explicit selection already exists.
- [x] The frontend application detail view can trigger job analysis.
- [x] The frontend shows extracted job signals, detected language, and inferred metadata after analysis.
- [x] No evidence matching, generation, claim audit, Ollama, diagnostics, or export UI is introduced.

## Blocked by

None - can start immediately.
