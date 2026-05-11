# Milestone 1 Product Requirements Document

Status: done
Source: docs/planning/v1-prd.md

Completion note: Child issues 01 through 03 are marked done, and the implemented backend/frontend surface now supports the project foundation, persistence model, and basic CRUD paths described here.

## Goal

Create the separate V1 application foundation with a backend, frontend, persistence model, and first usable profile/application CRUD paths.

## Problem Statement

The user needs a dedicated personal desktop web app for trustworthy job application writing, but the project first needs a clean foundation before workflow, AI, audit, export, or polish features can safely land.

Without this milestone, later milestones would have to build on unclear project structure, unstable persistence, or placeholder data flows. That would make the trust-first application workflow harder to test and extend, especially once profile evidence, generated drafts, provider failures, and export behavior are added.

## Solution

Milestone 1 establishes the V1 app as a separate ASP.NET Core Minimal API backend and React + Vite + TypeScript frontend. It adds PostgreSQL persistence through Entity Framework Core, models the core domain entities, and exposes basic profile and application session CRUD.

The user should be able to open the app shell, maintain basic profile contact and preference information, and create, view, update, and delete simple application sessions. The implementation should deliberately stop before manual profile facts, AI workflow behavior, generated drafts, claim audit, provider diagnostics, and export controls become user-facing features.

## User Stories

1. As a job applicant, I want a dedicated app shell for job application work, so that this workflow is separate from unrelated tools.
2. As a job applicant, I want a basic desktop-first frontend, so that later Home, Profile, Applications, and Settings areas have a stable place to live.
3. As a job applicant, I want the backend to be reachable through a simple health or root endpoint, so that the application foundation can be checked before feature work starts.
4. As a job applicant, I want to maintain my contact information, so that later generated documents can use accurate personal details.
5. As a job applicant, I want to maintain my default language and tone preferences, so that later drafts can start from my usual application style.
6. As a job applicant, I want to create a basic application session from job metadata and pasted posting text, so that each job has a saved workspace.
7. As a job applicant, I want to list saved application sessions, so that I can return to previous application work.
8. As a job applicant, I want to open and edit an application session, so that company, role, deadline, language, status, and posting details stay current.
9. As a job applicant, I want to delete an application session, so that abandoned or mistaken sessions can be removed.
10. As a job applicant, I want basic validation for profile and application inputs, so that obviously incomplete or malformed data is caught early.
11. As a job applicant, I want plain API errors, so that frontend screens can explain save failures without exposing implementation details.
12. As a developer, I want a clear backend project shape, so that later vertical feature slices can add profile, application, AI, diagnostics, and export behavior without restructuring the app.
13. As a developer, I want a clear frontend project shape, so that later workflow screens can be added without replacing the initial shell.
14. As a developer, I want PostgreSQL and Entity Framework Core configured early, so that later workflow state uses the same persistence foundation as basic CRUD.
15. As a developer, I want Profile modeled as the owner of contact information and language/tone preferences, so that later generation and export features have a stable source of user details.
16. As a developer, I want ProfileFact modeled early even before profile fact UI exists, so that later evidence management can build on the same schema.
17. As a developer, I want JobApplication modeled as latest-state workflow storage, so that later analysis, matching, generation, and audit steps can attach to one application session.
18. As a developer, I want GeneratedDraft modeled separately from JobApplication, so that generated text and application workflow state have clear ownership.
19. As a developer, I want AiRun modeled early, so that later provider failures and diagnostics can be recorded without changing the core schema.
20. As a developer, I want JSON-backed workflow artifacts configured for PostgreSQL JSONB where appropriate, so that AI and workflow data can evolve during V1.
21. As a developer, I want the foundation to avoid OpenAI configuration, so that the app can start without external API dependencies.
22. As a developer, I want the foundation to avoid AI behavior, exports, diagnostics, and advanced workflow UI, so that Milestone 1 remains a small, stable base.

## Implementation Decisions

- Build a separate V1 application rather than extending another app.
- Use ASP.NET Core Minimal API for the backend foundation.
- Use React, Vite, and TypeScript for the frontend foundation.
- Keep the first frontend shell desktop-first and ready to host Home, Profile, Applications, and Settings areas.
- Configure PostgreSQL persistence through Entity Framework Core.
- Add a database context available to application code.
- Model Profile with contact information, language/tone preferences, and timestamps.
- Model ProfileFact with evidence fields, JSON-backed fact data, Draft, Approved, and Archived status, and timestamps.
- Model JobApplication with posting metadata, selected/detected language, workflow JSON fields, custom facts JSON, and timestamps.
- Model GeneratedDraft separately from JobApplication with cover letter text, short motivation text, claim audit JSON, generated/edit/audit timestamps, and timestamps.
- Model AiRun with step, provider, model, status, error details, attempt count, timing, and minimal input/output summaries.
- Use PostgreSQL JSONB for flexible workflow artifacts where appropriate.
- Use latest-state storage only; do not add draft history or profile fact revision history.
- Add basic profile read/update behavior before profile fact management.
- Add basic application list, create, read, update, and delete behavior before richer workflow state.
- Allow application sessions to store company name, role title, application URL, deadline, status, job posting text, detected language, and selected language.
- Return plain, predictable API errors suitable for later UI error presentation.
- Keep Milestone 1 free of OpenAI dependency and configuration.
- Keep manual profile fact UI, AI workflow behavior, generated drafts, claim audit, exports, Ollama, and diagnostics out of this milestone.

## Testing Decisions

- Prioritize backend tests where foundation behavior affects stable contracts.
- Test public API behavior rather than internal persistence or component implementation details.
- Cover profile read/update behavior and validation for obvious invalid inputs.
- Cover application session list, create, read, update, and delete behavior.
- Cover application payload validation and plain API error shape.
- Cover persistence-facing behavior enough to prove the core entities and database context support later milestones.
- Keep frontend tests light for the initial shell unless a stable public interaction is worth locking down.
- Do not add AI provider, evidence matching, draft generation, claim audit, diagnostics, or export tests in this milestone.

## Out of Scope

- Manual profile fact management UI.
- Profile fact approval and archive workflows.
- Profile readiness warnings.
- Application list/detail workflow separation beyond basic CRUD.
- Job posting analysis.
- Evidence matching.
- Unmatched requirement handling.
- Job-local custom fact normalization.
- Draft generation.
- Editable generated drafts.
- Claim audit.
- Fake AI provider behavior.
- Ollama provider behavior.
- AI diagnostics.
- TXT or DOCX export.
- CV import or extraction.
- Authentication or multi-user support.
- Draft history or profile fact revision history.
- OpenAI provider support.

## Further Notes

Milestone 1 is the boring foundation that makes the later trust workflow possible. The important product outcome is not polished application writing yet; it is a clean app boundary, a stable data model, and enough CRUD behavior to prove the backend/frontend path before the workflow becomes more specialized.
