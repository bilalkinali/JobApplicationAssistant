# Milestone 3 Product Requirements Document

Status: done
Source: docs/planning/v1-prd.md

Completion note: Child issues 09 through 12 are marked done, and the implemented backend/frontend surface now supports fake job analysis, evidence matching, unmatched requirements, and approved evidence review.

## Goal

Add the deterministic fake AI workflow for job posting analysis, evidence matching, unmatched requirement handling, and evidence review.

## Problem Statement

The app now has a manual evidence base and clearer application workspace, but the user still has to interpret job postings and connect requirements to profile facts by hand. That keeps the product from delivering the core trust-first workflow: analyze what a job asks for, match it against approved evidence, expose gaps, and let the user decide which evidence may shape later generated text.

Milestone 3 needs to introduce AI workflow behavior without depending on local model quality or provider setup. The fake provider should make the workflow deterministic, inspectable, and testable before draft generation, claim audit, and Ollama are added.

## Solution

Implement the backend-owned AI provider abstraction and a first-class `FakeAiProvider` that can analyze pasted job postings, extract simple job signals, match those signals against approved profile facts, classify unmatched requirements, and support evidence review.

The user should be able to open an application session, run job analysis on its pasted job posting, see extracted company/role/language/signals, run evidence matching, inspect matched evidence and gaps, and approve or remove matched evidence before any future generation step. This milestone should stop at reviewed evidence. It should not generate cover letters, generate short motivation text, audit claims, call Ollama, or export documents.

## User Stories

1. As a job applicant, I want to analyze a pasted job posting, so that I can see what the role appears to ask for.
2. As a job applicant, I want the app to extract company name and role title when possible, so that the application session is easier to recognize.
3. As a job applicant, I want the app to detect the posting language, so that later draft language can start from the job context.
4. As a job applicant, I want to keep or adjust the selected application language, so that detection does not override my intent.
5. As a job applicant, I want extracted job signals to be visible, so that I can review the fake provider's interpretation.
6. As a job applicant, I want required skills, preferred skills, and responsibilities separated where possible, so that I understand the shape of the role.
7. As a job applicant, I want analysis blocked when there is no job posting text, so that empty sessions do not produce fake workflow artifacts.
8. As a job applicant, I want deterministic analysis results, so that repeated runs are predictable during early workflow development.
9. As a job applicant, I want approved profile facts matched against job signals, so that relevant evidence is surfaced for the application.
10. As a job applicant, I want draft and archived profile facts excluded from matching, so that only reviewed evidence is used.
11. As a job applicant, I want matched evidence to show which profile fact supports which job signal, so that the connection is understandable.
12. As a job applicant, I want unmatched requirements called out, so that missing evidence is explicit instead of hidden.
13. As a job applicant, I want gaps to include interest-to-learn recommendations where appropriate, so that I can handle missing requirements honestly later.
14. As a job applicant, I want evidence matching blocked when there are no approved profile facts, so that the app does not imply unsupported experience.
15. As a job applicant, I want to approve matched evidence before generation exists, so that later draft generation has a user-reviewed evidence set.
16. As a job applicant, I want to remove irrelevant matched evidence, so that weak keyword matches do not become approved evidence.
17. As a job applicant, I want approved evidence saved on the application session, so that I can leave and return without losing review decisions.
18. As a job applicant, I want analysis and matching errors shown plainly near the workflow step, so that I know what to fix.
19. As a job applicant, I want the application detail view to show analysis, evidence matches, unmatched requirements, and approved evidence in order, so that the workflow is easy to follow.
20. As a developer, I want application code to depend on an internal AI provider interface, so that fake and real providers share one contract.
21. As a developer, I want the fake provider to use simple keyword matching, so that backend tests can cover the workflow without model variability.
22. As a developer, I want job signals, evidence matches, unmatched requirements, and approved evidence stored as latest-state workflow artifacts, so that later milestones can extend the same session.
23. As a developer, I want backend tests for fake analysis and matching rules, so that trust-critical behavior remains stable.
24. As a developer, I want the milestone to avoid Ollama and prompt repair behavior, so that provider abstraction lands before real provider complexity.

## Implementation Decisions

- Keep Milestone 3 focused on fake AI workflow through evidence review.
- Introduce an internal AI provider abstraction for job analysis and evidence matching.
- Implement `FakeAiProvider` as the only provider required for this milestone.
- Use deterministic keyword-oriented behavior rather than model calls.
- Analyze job posting text into company name, role title, detected language, selected language defaults, and job signals.
- Represent job signals as flexible latest-state workflow artifacts suitable for JSONB storage.
- Match only Approved profile facts against job signals.
- Exclude Draft and Archived profile facts from evidence matching.
- Persist evidence matches, unmatched requirements, and approved evidence on the application session as latest state.
- Treat unmatched requirements as explicit workflow data, not as validation errors.
- Allow the frontend to run analysis and matching from the application detail workflow.
- Allow the frontend to approve or remove matched evidence and save the resulting approved evidence.
- Keep job-local custom fact normalization out of this milestone.
- Keep draft generation, short motivation generation, editable drafts, and claim audit out of this milestone.
- Keep Ollama provider behavior, diagnostics, strict JSON output validation, repair attempts, and `AiRun` tracking out of this milestone unless a small stub is required by the provider abstraction.
- Use plain errors and existing validation style for missing job posting text, missing approved profile facts, and failed AI workflow actions.
- Preserve the desktop-first application detail structure created in Milestone 2.

## Testing Decisions

- Test backend behavior through public workflow endpoints where practical.
- Test the fake provider as deterministic business behavior, not as an implementation detail of a future real provider.
- Cover job posting analysis for common keyword and language cases.
- Cover evidence matching between job signals and approved profile facts.
- Cover exclusion of Draft and Archived profile facts from matching.
- Cover unmatched requirement classification when no approved evidence supports a signal.
- Cover evidence approval persistence on the application session.
- Cover blocking analysis without job posting text.
- Cover blocking matching without approved profile facts.
- Keep frontend tests light unless the existing frontend setup already makes a small workflow component test natural.
- Do not add Ollama, generated draft, claim audit, export, or diagnostics tests in this milestone.

## Out of Scope

- Draft generation.
- Short motivation generation.
- Editable generated drafts.
- Claim audit.
- Stale audit warning after edits.
- Job-local custom fact normalization.
- Ollama provider support.
- AI diagnostics.
- Strict JSON provider output validation.
- Invalid JSON repair attempts.
- Minimal `AiRun` tracking.
- TXT or DOCX export.
- CV import or extraction.

## Further Notes

The fake provider is part of the product strategy, not throwaway scaffolding. Its job is to make the trust workflow concrete: every later generated claim should be able to trace back to approved evidence or an explicitly acknowledged gap.

This milestone should leave the product with a demoable path from pasted job posting to reviewed evidence, while deliberately stopping before text generation.
