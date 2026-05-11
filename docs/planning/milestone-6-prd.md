# Milestone 6 Product Requirements Document

Status: ready-for-agent
Source: docs/planning/v1-prd.md

## Goal

Turn generated application drafts into usable application artifacts, and make saved application sessions easier to find, inspect, and manage.

Milestone 6 should improve the existing application history experience, strengthen status metadata around saved sessions, add focused filters for the application list, and provide export paths for the current cover letter as plain text and DOCX with profile contact information.

## Problem Statement

The app can now guide a user from profile evidence through job analysis, evidence review, generated drafts, claim audit, and real local provider behavior. The workflow produces useful text, but the final output still lives mainly inside the application detail screen. The user needs a practical way to return to past sessions, identify which applications are draft/reviewed/applied/archived, and move the current cover letter into external job portals or document submission flows.

Without export, the trust workflow stops one step short of the real-world task. Without stronger application history behavior, saved sessions become harder to navigate as the user creates more applications.

## Solution

Milestone 6 adds an application history and export layer on top of the existing latest-state workflow.

The application list should become a reliable history surface: it should keep saved sessions ordered by recent activity, expose status, company, role, language, deadline, draft/audit readiness, and updated date, and provide simple filters/search that help the user find active, applied, or archived work.

The export behavior should use the current generated draft as the source of truth. The user should be able to copy or download the cover letter as plain text and download a DOCX cover letter that includes profile contact information plus the current cover letter text. Export should be blocked when there is no generated draft, and should warn or clearly indicate when the current claim audit is stale or missing without preventing export.

## User Stories

1. As a job applicant, I want saved application sessions listed as history, so that I can return to previous applications.
2. As a job applicant, I want the history list ordered by most recent update, so that current work is easy to find.
3. As a job applicant, I want each history item to show company and role, so that I can recognize the application quickly.
4. As a job applicant, I want each history item to show application status, so that I can distinguish draft, review, applied, and archived work.
5. As a job applicant, I want each history item to show selected language when available, so that I know which language the draft targets.
6. As a job applicant, I want each history item to show deadline when available, so that urgent applications stand out.
7. As a job applicant, I want each history item to show updated date, so that I can tell when I last worked on it.
8. As a job applicant, I want each history item to show whether a generated draft exists, so that I know whether export is possible.
9. As a job applicant, I want each history item to show whether claim audit is current, stale, missing, or not applicable, so that I know whether the draft is ready to trust.
10. As a job applicant, I want to search applications by company and role, so that I can find a saved session quickly.
11. As a job applicant, I want to filter applications by status, so that I can focus on active, applied, or archived sessions.
12. As a job applicant, I want to filter applications by draft readiness, so that I can find sessions that are ready to export.
13. As a job applicant, I want archived applications kept visible only when I ask for them, so that old sessions do not clutter active work.
14. As a job applicant, I want an empty state for filtered history, so that I understand whether there are no applications or just no matching filters.
15. As a job applicant, I want application status updates to be simple and explicit, so that I can mark a session as Applied or Archived after using it.
16. As a job applicant, I want application history to keep using latest-state sessions, so that V1 does not introduce complicated version history.
17. As a job applicant, I want to copy the current cover letter text, so that I can paste it into job portals.
18. As a job applicant, I want to download the current cover letter as plain text, so that I can save or inspect it outside the app.
19. As a job applicant, I want plain-text export to include the current cover letter exactly as edited, so that manual refinements are preserved.
20. As a job applicant, I want plain-text export to use a readable filename, so that downloaded files are easy to identify.
21. As a job applicant, I want to download the current cover letter as DOCX, so that I can submit it as a document attachment.
22. As a job applicant, I want DOCX export to include my profile contact information, so that the submitted document has the needed applicant details.
23. As a job applicant, I want DOCX export to include company, role, and date context where appropriate, so that the document feels tied to the application.
24. As a job applicant, I want DOCX export to use the current generated draft text, so that export never silently regenerates content.
25. As a job applicant, I want export blocked when there is no generated draft, so that I do not download an empty or misleading document.
26. As a job applicant, I want export to warn when the claim audit is stale, so that I know to re-run audit before sending if needed.
27. As a job applicant, I want export to warn when claim audit has not been run, so that I know the draft has not been checked.
28. As a job applicant, I want export warnings to avoid blocking manual choice, so that I can still export when I intentionally accept the risk.
29. As a job applicant, I want export errors to be plain, so that I know whether the problem is missing profile info, missing draft text, or document generation failure.
30. As a job applicant, I want exported files to avoid unsupported claims being hidden, so that the trust workflow remains visible before final use.
31. As a developer, I want export behavior behind backend endpoints, so that document generation and data access stay centralized.
32. As a developer, I want TXT export to be deterministic, so that tests can assert exact output and headers.
33. As a developer, I want DOCX generation isolated behind a small export module, so that document formatting does not leak into application workflow endpoints.
34. As a developer, I want export filenames built from sanitized company and role metadata, so that response headers remain safe.
35. As a developer, I want export endpoints to read profile contact information through the existing profile model, so that contact details have one source of truth.
36. As a developer, I want application list contracts to carry summary readiness fields where useful, so that the frontend does not parse large workflow JSON just to render history.
37. As a developer, I want application history filters implemented through stable request parameters or local UI filtering, so that later pagination can be added without changing user concepts.
38. As a developer, I want backend tests for export blockers and generated file responses, so that the final artifact path stays reliable.
39. As a developer, I want frontend changes to stay within the existing desktop-first workbench, so that Milestone 6 does not become a redesign.
40. As a developer, I want custom DOCX templates, draft version history, and CV export left out, so that this milestone remains focused.

## Implementation Decisions

- Keep Milestone 6 focused on application history, status metadata, and export.
- Continue using `JobApplication` and `GeneratedDraft` as latest-state records.
- Do not add draft history, application event history, profile fact revision history, or version comparison.
- Treat the application list as the application history surface for V1.
- Keep the existing status vocabulary: Draft, PostingCaptured, ReadyForReview, Applied, and Archived.
- Preserve the current status mutation behavior from workflow actions, while allowing the user to update final tracking states explicitly.
- Add or refine summary fields for application list responses where they reduce frontend parsing of workflow JSON.
- Include generated draft existence, audit timestamp, stale audit flag, and export readiness in application history summaries where practical.
- Keep filters simple: search text, status, archive visibility, and draft/audit readiness.
- Keep filtering compatible with current local frontend state unless backend query parameters become simpler and clearer.
- Add backend export behavior as a focused export module or endpoint group.
- Provide a plain-text cover letter export endpoint for the current generated draft.
- Provide a DOCX cover letter export endpoint for the current generated draft.
- Use the current edited cover letter text as the export source of truth.
- Do not export short motivation text in this milestone unless it is needed as a small plain copy affordance in the existing draft screen.
- Include profile contact details in DOCX export when present.
- Allow missing optional contact fields without failing DOCX export.
- Block export when the application session does not exist.
- Block export when the application has no generated draft.
- Block export when the generated cover letter text is empty or whitespace.
- Return plain validation errors for blocked export cases.
- Do not regenerate, re-audit, or call an AI provider during export.
- Surface stale or missing audit state near export controls as a warning, not a hard backend blocker.
- Generate safe filenames from company name and role title with a fallback to the application id.
- Keep DOCX formatting simple, readable, and deterministic.
- Do not add custom DOCX templates.
- Do not add PDF export.
- Do not add OpenAI provider support, streaming generation, CV import, or mobile-first redesign in this milestone.

## Testing Decisions

- Test behavior through public API endpoints where practical.
- Keep tests focused on external behavior: returned status codes, response content type, content disposition, exported text contents, and blocker errors.
- Cover application list ordering by updated date.
- Cover application list summary fields for generated draft and audit readiness if backend summary fields are added.
- Cover status metadata persistence when an application is marked Applied or Archived.
- Cover plain-text export returning the current edited cover letter.
- Cover plain-text export using a safe filename.
- Cover plain-text export blocked when the application does not exist.
- Cover plain-text export blocked when no generated draft exists.
- Cover plain-text export blocked when cover letter text is empty.
- Cover DOCX export returning a valid DOCX content type and file response.
- Cover DOCX export including profile contact information when present.
- Cover DOCX export including the current edited cover letter text.
- Cover DOCX export tolerating missing optional profile fields.
- Cover DOCX export blocked when no generated draft exists.
- Cover export behavior when claim audit is stale or missing, verifying export is still allowed once a draft exists.
- Keep frontend tests light unless existing frontend setup makes small filter/export control tests natural.
- Do not add tests for custom templates, draft history, PDF export, or AI provider calls in this milestone.

## Out of Scope

- Custom DOCX templates.
- PDF export.
- CV export, CV tailoring, CV import, or CV layout preservation.
- Draft history or version comparison.
- Application event history beyond latest-state application sessions.
- Profile fact revision history.
- Application analytics.
- Advanced job tracking or kanban.
- Exporting all workflow evidence or claim audit reports as separate documents.
- Streaming generation.
- OpenAI provider support.
- Mobile-first redesign.
- Authentication or multi-user support.

## Further Notes

Milestone 6 is the point where the workflow becomes practically usable outside the app. The implementation should stay deliberately boring: export the current trusted text, show the audit state clearly, and make saved sessions easy to find.

The most important product boundary is that export must not create new claims. It should package the latest user-visible draft, not regenerate or reinterpret it.
