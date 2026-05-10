# Milestone 2 Product Requirements Document

Status: ready-for-agent
Source: docs/planning/v1-prd.md

## Goal

Improve the CRUD foundation into a more usable job application workflow, including better application state, profile fact management, and basic UI separation.

## Problem Statement

The V1 foundation can store a profile and basic application sessions, but it is still closer to raw CRUD than a useful job application workflow. The user needs a clearer place to maintain trustworthy profile evidence, a better application session model for tracking workflow state, and a frontend layout that separates profile evidence, application list, and application detail work before AI-assisted features are added.

Without this milestone, later job analysis, evidence matching, draft generation, and claim audit work would land on a shallow UI and incomplete workflow state. That would make the trust-first application flow harder to understand, test, and extend.

## Solution

Milestone 2 adds the manual workflow layer between basic CRUD and AI behavior. The app should support profile fact management, richer application session state, and clearer frontend separation between the list of applications and the selected application detail.

The user should be able to maintain structured profile facts manually, approve or archive evidence, create and organize application sessions with more useful metadata, and navigate between application list and application detail views without mixing all work into one panel. This milestone remains fully manual and non-AI.

## User Stories

1. As a job applicant, I want to create profile facts manually, so that the app has reliable evidence before AI matching exists.
2. As a job applicant, I want to edit profile facts, so that my experience and evidence stay current.
3. As a job applicant, I want to delete profile facts, so that incorrect or obsolete evidence can be removed.
4. As a job applicant, I want profile facts to include a type, title, summary, technologies, allowed claims, and forbidden claims, so that evidence is structured enough for later matching.
5. As a job applicant, I want profile facts to support Draft, Approved, and Archived states, so that only reviewed evidence is considered ready.
6. As a job applicant, I want archived profile facts to stay visible but separate, so that old evidence does not accidentally shape future drafts.
7. As a job applicant, I want empty or incomplete profile facts blocked by validation, so that weak evidence does not enter the workflow.
8. As a job applicant, I want an applications list separate from the application detail editor, so that finding a session and working on a session are distinct tasks.
9. As a job applicant, I want application sessions to show company, role, status, language, and updated date, so that I can scan prior applications quickly.
10. As a job applicant, I want simple application filters, so that the list remains usable as more sessions are saved.
11. As a job applicant, I want application status to represent workflow progress, so that a session can move beyond a generic draft label.
12. As a job applicant, I want job posting text and application metadata grouped in the detail view, so that the application editor feels like a workflow screen rather than a database form.
13. As a job applicant, I want setup warnings when my profile has no approved facts, so that I know what is missing before later generation steps.
14. As a job applicant, I want plain validation errors near the relevant work area, so that I can correct inputs without guessing.
15. As a developer, I want profile fact endpoints and contracts to be ready before AI matching, so that later provider work can depend on stable evidence data.
16. As a developer, I want application state represented explicitly, so that later analysis, evidence review, generation, and audit steps can extend it without redesigning CRUD.
17. As a developer, I want the frontend split into clearer workflow areas, so that later screens can be added without growing one monolithic component.

## Implementation Decisions

- Keep Milestone 2 manual and non-AI.
- Add profile fact CRUD behavior to the Profile module.
- Use the existing ProfileFact entity and Draft, Approved, and Archived status vocabulary.
- Treat Approved profile facts as the future source of reusable evidence for matching and generation.
- Keep SourceDocumentIds and OriginalImportedSnapshot available for future import work, but do not build CV import behavior.
- Add API contracts for profile fact create, update, list, and delete behavior.
- Return API errors in the same plain error shape established by the CRUD foundation.
- Improve application session state without implementing AI workflow actions.
- Keep JobApplication as latest-state storage.
- Represent application status with values that can support early workflow progress, such as Draft, PostingCaptured, ReadyForReview, Applied, and Archived.
- Separate the frontend application list from the selected application detail editor.
- Keep the Home workbench lightweight, using readiness indicators rather than deep workflow controls.
- Preserve the existing backend/frontend project split and vertical feature slice direction.
- Avoid introducing OpenAI, Ollama, FakeAiProvider, generated drafts, claim audit, export, or diagnostics in this milestone.

## Testing Decisions

- Prioritize backend behavior tests for profile fact validation and application state rules.
- Test through public API behavior where practical rather than internal implementation details.
- Cover creating, updating, listing, deleting, approving, and archiving profile facts.
- Cover validation failures for missing fact title, missing summary, invalid status, and malformed structured fields.
- Cover application status validation and list filtering behavior.
- Keep frontend tests light unless a small component-level test is already natural in the existing frontend setup.
- Do not add AI provider tests in this milestone.

## Out of Scope

- Job posting analysis.
- Evidence matching.
- Unmatched requirement handling.
- Job-local custom fact normalization.
- Draft generation.
- Claim audit.
- Fake AI provider behavior.
- Ollama provider behavior.
- AI diagnostics.
- TXT or DOCX export.
- CV import or extraction.
- Draft history or profile fact revision history.

## Further Notes

This milestone should make the app feel like a usable manual workflow, not a finished AI application. The important product outcome is a trustworthy evidence base and clearer application workspace that later AI slices can build on without inventing new foundations.
