# Fake Draft Generation

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/milestone-4-prd.md

## What to build

Add the first Milestone 4 generation slice. The user should be able to generate exactly one latest-state cover letter and one latest-state short motivation text for a saved application session that has job posting text and approved evidence.

Generation should extend the existing AI provider abstraction and fake provider with deterministic behavior. The generated text should use application metadata, selected language where available, profile contact and tone context where practical, approved evidence, and honest wording for unmatched requirements. Generated text should be stored in `GeneratedDraft`, not directly on the application session.

This slice should not add manual draft editing, claim audit, stale audit behavior, Ollama, diagnostics, export, or draft history.

## Acceptance criteria

- [ ] The AI provider abstraction supports draft generation.
- [ ] The fake provider generates deterministic cover letter text and short motivation text.
- [ ] Draft generation is blocked with a plain validation error when the application has no job posting text.
- [ ] Draft generation is blocked with a plain validation error when the application has no approved evidence.
- [ ] Generated text uses company and role context from the application session.
- [ ] Generated text uses selected language when available.
- [ ] Generated text uses approved evidence as the source for concrete experience claims.
- [ ] Unmatched requirements are handled with honest gap wording rather than invented experience.
- [ ] Generated cover letter and short motivation text are persisted as latest state in `GeneratedDraft`.
- [ ] Generated drafts are stored separately from the application session workflow state.
- [ ] The public API supports generating the current draft for an application.
- [ ] Backend tests cover blocked generation without job posting text, blocked generation without approved evidence, deterministic fake generation, and generated draft persistence.
- [ ] No manual draft editing, claim audit, stale audit behavior, Ollama, diagnostics, export, or draft history is introduced.

## Blocked by

None - can start immediately.
