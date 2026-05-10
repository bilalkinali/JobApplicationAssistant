**V1 Product Requirements Document**

Status: ready-for-agent
Source: docs/planning/v1-specification.md

## Problem Statement

Software developer job applications often require tailored cover letters and short motivation texts, but writing them repeatedly is slow and error-prone. The user needs a personal desktop web app that can turn manually maintained profile facts and a pasted job posting into strong, honest application text without inventing experience.

The product must support a trust-first workflow: profile facts are explicit, job requirements are analyzed, evidence is matched and reviewed, gaps are surfaced, generated claims are audited, and the final text remains editable by the user.

## Solution

Build a separate personal desktop web app for creating tailored software developer job application text.

The app will let the user maintain contact details, tone preferences, and structured profile facts. For each job application, the user pastes a job posting, runs AI-assisted analysis, reviews matched evidence and unmatched requirements, optionally adds job-local custom facts, then generates a cover letter and short motivation text. The generated draft is stored separately from the application session and can be edited. Claims are audited against approved profile facts and approved custom facts.

V1 uses a fake AI provider first for deterministic development and stable workflow testing, with Ollama available behind the same backend-owned provider abstraction. OpenAI is intentionally out of scope for V1.

## User Stories

1. As a job applicant, I want to maintain my contact information, so that generated application documents include accurate personal details.
2. As a job applicant, I want to set my default application language, so that drafts start from my normal preference.
3. As a job applicant, I want to define Danish and English tone preferences, so that generated text matches how I want to present myself.
4. As a job applicant, I want to create structured profile facts manually, so that the app has reliable evidence to use in applications.
5. As a job applicant, I want profile facts to include summaries, technologies, allowed claims, and forbidden claims, so that generated text stays grounded.
6. As a job applicant, I want profile facts to have Draft, Approved, and Archived statuses, so that only reviewed facts are used as evidence.
7. As a job applicant, I want to edit profile facts, so that my evidence stays current.
8. As a job applicant, I want to delete profile facts, so that obsolete or incorrect information can be removed.
9. As a job applicant, I want archived facts to remain separate from approved facts, so that old experience does not accidentally appear in new drafts.
10. As a job applicant, I want to create an application session from a pasted job posting, so that each job has its own saved workflow state.
11. As a job applicant, I want the app to detect company name and role title from a posting, so that the session is easier to organize.
12. As a job applicant, I want the app to detect the posting language, so that generated text can match the job context.
13. As a job applicant, I want to choose the final application language, so that I can override detection when needed.
14. As a job applicant, I want the app to extract job signals, required skills, preferred skills, and responsibilities, so that I understand what the posting asks for.
15. As a job applicant, I want extracted requirements to be visible before generation, so that I can review whether the analysis looks reasonable.
16. As a job applicant, I want approved profile facts matched against job signals, so that generated text uses relevant evidence.
17. As a job applicant, I want unmatched requirements called out, so that I can avoid pretending to have experience I do not have.
18. As a job applicant, I want the app to suggest requirements that can be mentioned as interest to learn, so that gaps can be handled honestly.
19. As a job applicant, I want to approve or remove matched evidence before generation, so that I control what the draft may claim.
20. As a job applicant, I want generation blocked when there is no job posting text, so that drafts are not created from empty input.
21. As a job applicant, I want generation blocked when there are no approved profile facts or approved custom facts, so that drafts are not invented.
22. As a job applicant, I want AI actions blocked when the selected provider is unavailable, so that failures are clear.
23. As a job applicant, I want to add job-local custom facts, so that job-specific context can be used without permanently changing my profile.
24. As a job applicant, I want custom facts normalized by AI before use, so that they fit the same evidence model as profile facts.
25. As a job applicant, I want custom facts to require confirmation, so that unreviewed job-local claims are not used.
26. As a job applicant, I want custom facts to support PendingConfirmation, Approved, and Rejected states, so that I can control their lifecycle.
27. As a job applicant, I want to generate one full cover letter, so that I can apply with a tailored document.
28. As a job applicant, I want to generate one short motivation text, so that I can answer short application form prompts.
29. As a job applicant, I want generated drafts stored separately from the application session, so that application state and generated text have clear ownership.
30. As a job applicant, I want generated drafts to be editable, so that I can refine the final wording manually.
31. As a job applicant, I want edits to mark the claim audit stale, so that I know the audit no longer reflects the current text.
32. As a job applicant, I want to manually re-run claim audit, so that edited drafts can be checked again.
33. As a job applicant, I want claim audit results to compare generated claims against approved evidence, so that unsupported claims are visible.
34. As a job applicant, I want application sessions saved as latest state, so that I can return to recent applications.
35. As a job applicant, I want an applications list with company, role, status, language, and updated date, so that I can find prior sessions.
36. As a job applicant, I want simple filters on applications, so that the list stays manageable.
37. As a job applicant, I want a home workbench with profile readiness, new application entry, recent applications, and AI status, so that common work starts in one place.
38. As a job applicant, I want setup warnings instead of rigid blocking, so that I can use parts of the app before everything is configured.
39. As a job applicant, I want plain errors with expandable details, so that I can understand provider or validation failures without being overwhelmed.
40. As a job applicant, I want to copy or download generated text as plain text, so that I can paste it into job portals.
41. As a job applicant, I want to export a cover letter as DOCX with contact info, so that I can submit it as a document.
42. As a developer, I want an internal AI provider abstraction, so that fake and Ollama providers can share one workflow contract.
43. As a developer, I want a deterministic fake provider, so that the workflow can be built and tested before local model tuning.
44. As a developer, I want the fake provider to use simple keyword matching, so that its behavior is understandable and stable.
45. As a developer, I want Ollama support behind configuration, so that the app can use a local real AI provider without API keys.
46. As a developer, I want the app to start even when Ollama is unavailable, so that setup problems do not prevent non-AI work.
47. As a developer, I want AI prompts stored as backend markdown files, so that prompt changes are reviewable.
48. As a developer, I want AI outputs expected as strict JSON, so that provider responses can be deserialized and validated.
49. As a developer, I want one repair attempt for invalid AI JSON or structure, so that transient model formatting failures can recover.
50. As a developer, I want minimal AiRun records for failures, so that provider issues are diagnosable without storing raw payloads.
51. As a developer, I want raw requests and responses disabled by default, so that sensitive application data is not unnecessarily retained.
52. As a developer, I want backend-owned AI calls, so that provider behavior and data handling remain centralized.
53. As a developer, I want JSONB for flexible workflow artifacts, so that job signals, matches, requirements, audits, and custom facts can evolve during V1.
54. As a developer, I want latest-state storage only in V1, so that the initial product avoids history complexity.

## Implementation Decisions

- Build a separate new application for V1.
- Use ASP.NET Core Minimal API for the backend.
- Use Entity Framework Core with PostgreSQL for persistence.
- Use vertical feature slices around Profile, Applications, Ai, Diagnostics, and Export.
- Use JSONB for flexible AI and workflow artifacts such as job signals, evidence matches, unmatched requirements, approved evidence, custom facts, claim audit, AiRun input summaries, and AiRun output summaries.
- Use React, Vite, and TypeScript for the desktop-first frontend.
- Keep generated drafts editable through simple text areas.
- Model Profile as the owner of contact information and language/tone preferences.
- Model ProfileFact as manually curated evidence with Draft, Approved, and Archived statuses.
- Include SourceDocumentIds and OriginalImportedSnapshot on ProfileFact for future CV import, but leave them mostly unused in V1.
- Model JobApplication as the latest-state application workflow session.
- Store job-local custom facts on JobApplication as JSON with id, title, summary, technologies, allowedClaims, and status.
- Use PendingConfirmation, Approved, and Rejected for job-local custom fact status.
- Model GeneratedDraft separately from JobApplication, with cover letter text, short motivation text, claim audit, generated timestamp, edit timestamp, and audit timestamp.
- Track AiRun records for AI workflow execution with step, provider, model, status, errors, attempt count, timing, and minimal input/output summaries.
- Do not implement draft history or profile fact revision history in V1.
- Depend on an internal IAiProvider interface rather than provider-specific application code.
- Implement FakeAiProvider as a first-class provider for deterministic development and stable backend tests.
- Implement OllamaAiProvider as the first real local AI provider.
- Do not include an OpenAI provider in V1.
- Ensure the app starts without OpenAI configuration.
- Configure Ollama through provider, endpoint, model, timeout seconds, and raw payload storage settings.
- Keep provider/model status read-only in the UI.
- Make diagnostics manually triggered.
- Store prompts as backend markdown files.
- Require strict JSON AI outputs and deserialize them into C# DTOs.
- Validate deserialized AI output DTOs before applying them.
- Allow one repair attempt for invalid JSON or invalid AI output structure.
- Fail gracefully and record a minimal AiRun when AI output remains invalid.
- Do not store raw requests or responses by default.
- Provide profile endpoints for reading and updating profile information and managing profile facts.
- Provide application endpoints for CRUD application sessions.
- Provide AI workflow endpoints for job analysis, evidence matching, approved evidence update, custom fact normalization, draft generation, and claim audit.
- Provide AI status and diagnostics endpoints.
- Provide TXT and DOCX export endpoints for cover letters.
- Use warnings instead of rigid setup blocking wherever possible.
- Block only when job posting text is missing, approved profile facts or approved custom facts are missing, or the AI provider is unavailable for the requested AI action.

## Testing Decisions

- Prioritize backend tests because trust and workflow rules carry the highest product risk in V1.
- Keep frontend tests light in V1.
- Test behavior through public interfaces rather than implementation details.
- Focus tests on rules and workflow outcomes, not every screen.
- Test profile fact validation, including status behavior and evidence fields.
- Test application minimum requirements, including blocking without job posting text.
- Test that draft generation requires approved profile facts or approved custom facts.
- Test fake provider keyword matching.
- Test evidence matching behavior.
- Test unmatched requirement classification.
- Test custom fact normalization rules.
- Test draft generation input restrictions.
- Test claim audit statuses.
- Test stale audit state after manual draft edits.
- Test AI output validation.
- Test invalid JSON repair attempt behavior.
- Test provider unavailable behavior.
- Test minimal AiRun failure tracking.

## Out of Scope

- CV tailoring or CV layout preservation.
- CV import or extraction.
- OpenAI API dependency.
- Authentication or login.
- Multi-user support.
- Full version history.
- Application analytics.
- Advanced job tracking or kanban.
- Custom DOCX templates.
- Streaming generation.
- Mobile-first UI.
- Encryption at rest.
- Raw AI request or response storage by default.

## Further Notes

V1 should remain deliberately small and trust-centered. The fake provider is not a placeholder hack; it is part of the product strategy for validating the workflow before depending on local model quality. Ollama support should use the same application flow and fail clearly when unavailable.

The main product risk is unsupported claim generation. The core workflow must therefore preserve a visible chain from approved profile facts and approved custom facts to generated text and claim audit results.

This PRD was synthesized from `docs/planning/v1-specification.md` and published using the configured local markdown issue tracker/docs setup.
