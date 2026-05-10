# Milestone 4 Product Requirements Document

Status: ready-for-agent
Source: docs/planning/v1-prd.md

## Goal

Add draft generation and claim audit on top of the reviewed evidence workflow.

Milestone 4 should let the user generate one full cover letter and one short motivation text from the saved application session, approved evidence, and profile context. The generated text should be editable, saved as latest state, and auditable against the evidence that the user approved in Milestone 3.

## Problem Statement

The app can now analyze a job posting, match approved profile facts, surface unmatched requirements, and let the user approve evidence. The workflow still stops before producing application text. That means the user has the trustworthy evidence chain, but still has to manually turn it into a cover letter and short motivation response.

The next product risk is unsupported claim generation. Draft generation must therefore be constrained by reviewed evidence and followed by claim audit. If the user edits generated text, the app must make it clear that the previous audit may no longer describe the current draft.

## Solution

Milestone 4 adds deterministic fake-provider generation and audit behavior without introducing Ollama or real model variability yet. The user should be able to open an application session that has approved evidence, generate a cover letter and short motivation text, review and edit those drafts, see claim audit results, and manually re-run audit after edits.

Generated drafts should be stored separately from the application session in `GeneratedDraft`, while application workflow state remains on `JobApplication`. The fake provider should produce predictable text that only uses application metadata, profile contact/tone context, approved evidence, and explicitly safe wording for unmatched requirements. Claim audit should compare generated claims against approved evidence and flag unsupported or needs-review claims in a stable, testable way.

## User Stories

1. As a job applicant, I want to generate a full cover letter, so that I can quickly create tailored application text from reviewed evidence.
2. As a job applicant, I want to generate a short motivation text, so that I can answer short application form prompts.
3. As a job applicant, I want generation blocked when there is no job posting text, so that drafts are not created from empty context.
4. As a job applicant, I want generation blocked when there is no approved evidence, so that drafts do not invent experience.
5. As a job applicant, I want generated text to use approved evidence, so that every concrete claim has a traceable source.
6. As a job applicant, I want generated text to include company and role context, so that the draft feels tailored to the application session.
7. As a job applicant, I want generated text to respect the selected language when available, so that the draft matches my chosen application language.
8. As a job applicant, I want generated text to use my tone preferences where practical, so that the draft sounds like my intended style.
9. As a job applicant, I want unmatched requirements handled honestly, so that gaps are not rewritten as experience I do not have.
10. As a job applicant, I want the generated cover letter and short motivation saved with the application, so that I can return to them later.
11. As a job applicant, I want generated drafts stored separately from the application session, so that workflow state and generated text have clear ownership.
12. As a job applicant, I want to edit the generated cover letter, so that I can refine the final text manually.
13. As a job applicant, I want to edit the short motivation text, so that I can adapt it to a portal-specific prompt.
14. As a job applicant, I want manual edits saved, so that changes are not lost when I leave the application session.
15. As a job applicant, I want edits to mark the claim audit stale, so that I know the audit no longer reflects the current text.
16. As a job applicant, I want to run claim audit manually, so that I can check generated or edited drafts before using them.
17. As a job applicant, I want claim audit results to identify supported claims, so that I can trust the evidence-backed parts of the draft.
18. As a job applicant, I want claim audit results to identify unsupported claims, so that I can remove or rewrite risky wording.
19. As a job applicant, I want claim audit results to identify claims that need review, so that ambiguous wording is not silently treated as safe.
20. As a job applicant, I want audit results to reference the approved evidence where possible, so that I understand why a claim is considered supported.
21. As a job applicant, I want draft generation and audit errors shown plainly, so that I know whether I need more evidence, a job posting, or a provider fix.
22. As a job applicant, I want the application detail workflow to show draft generation after evidence review, so that the screen follows the real work order.
23. As a job applicant, I want the application detail workflow to show claim audit after generated text, so that review happens before final use.
24. As a developer, I want generation and audit added to the existing AI provider abstraction, so that fake and future Ollama providers share the same workflow contract.
25. As a developer, I want fake generation to be deterministic, so that backend tests can lock down trust-critical behavior.
26. As a developer, I want claim audit to be deterministic in this milestone, so that later real-provider audit can be compared against a stable baseline.
27. As a developer, I want generated draft persistence tested through public API behavior, so that refactors do not break saved workflow behavior.
28. As a developer, I want stale audit behavior tested, so that manual edits cannot quietly reuse an old audit.

## Implementation Decisions

- Keep Milestone 4 focused on fake-provider draft generation and claim audit.
- Extend the internal AI provider abstraction with draft generation and claim audit operations.
- Keep `FakeAiProvider` as the only provider required in this milestone.
- Use deterministic draft generation based on profile context, application metadata, approved evidence, and selected language.
- Generate exactly one cover letter text and one short motivation text per application session as latest state.
- Store generated text in `GeneratedDraft`, not directly on `JobApplication`.
- Keep latest-state storage only; do not add draft history.
- Add API behavior for generating a draft, reading the current generated draft with the application detail workflow, saving manual draft edits, and running claim audit.
- Block draft generation when the application has no job posting text.
- Block draft generation when the application has no approved evidence.
- Treat unmatched requirements as context for honest gap wording, not as approved evidence.
- Represent claim audit as structured JSON on `GeneratedDraft`.
- Include audit timestamps so the UI can distinguish current and stale audit results.
- Mark audit stale when either generated text field is manually edited after audit.
- Let users manually re-run audit after edits.
- Keep job-local custom fact normalization out of this milestone unless a tiny compatibility placeholder is unavoidable.
- Keep Ollama provider behavior, diagnostics, strict JSON validation, repair attempts, and `AiRun` tracking out of this milestone.
- Keep TXT and DOCX export out of this milestone.
- Preserve the desktop-first application detail workflow created in Milestones 2 and 3.

## Testing Decisions

- Test backend behavior through public workflow endpoints wherever practical.
- Test fake provider generation and audit as deterministic business behavior.
- Cover draft generation blocked without job posting text.
- Cover draft generation blocked without approved evidence.
- Cover generation from approved evidence into both cover letter and short motivation text.
- Cover that generated drafts are persisted separately from the application session.
- Cover manual draft edits and updated timestamps.
- Cover that editing a draft marks the audit stale.
- Cover claim audit statuses for supported, unsupported, and needs-review claims where the fake audit can classify them deterministically.
- Cover that re-running audit clears the stale state for the current draft text.
- Keep frontend tests light unless a small workflow component test becomes natural in the existing frontend setup.
- Do not add Ollama, diagnostics, export, or provider JSON repair tests in this milestone.

## Out of Scope

- Ollama provider support.
- AI provider status and diagnostics.
- Strict JSON output validation for real model responses.
- Invalid JSON repair attempts.
- Minimal `AiRun` tracking.
- Job-local custom fact normalization.
- TXT export.
- DOCX export.
- CV import or extraction.
- Draft history or version comparison.
- Advanced application tracking or analytics.

## Further Notes

This milestone is where the trust workflow first produces user-facing prose. The implementation should favor boring, traceable text over clever generation. It is better for the fake provider to sound plain and auditable than fluent but vague.

The most important invariant is that concrete experience claims in generated drafts should trace back to approved evidence or be flagged by claim audit. Unsupported claims are the core product risk.
