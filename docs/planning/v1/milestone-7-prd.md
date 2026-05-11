# Milestone 7 Product Requirements Document

Status: ready-for-agent
Source: docs/planning/v1-prd.md

## Goal

Polish the V1 workflow so the app feels coherent, trustworthy, and ready for regular personal use.

Milestone 7 should tighten setup warnings, validation blockers, provider and workflow error presentation, and desktop-first UI consistency across the home workbench, profile, application history, application detail, settings AI, and export surfaces.

## Problem Statement

The app now supports the core V1 workflow: profile facts, saved application sessions, fake AI, Ollama provider behavior, evidence review, draft generation, claim audit, application history, and TXT/DOCX export. The main remaining risk is not missing capability, but unclear guidance.

When setup is incomplete, an AI provider is unavailable, a draft cannot be generated, an audit is stale, or export is blocked, the user needs to understand what happened, what is still usable, and what action to take next. The UI should make the trust workflow legible without adding new product scope.

## Solution

Milestone 7 adds a focused polish pass over the existing V1 experience.

The app should present setup readiness as helpful warnings instead of rigid blockers, reserve hard blocking for the existing V1 rules, show plain user-facing errors with optional expandable technical details, and make workflow state easier to scan. The desktop-first UI should feel consistent across screens, with stable controls, clear empty states, predictable loading and disabled states, and wording that distinguishes profile readiness, validation blockers, provider failures, invalid AI output, stale audits, missing audits, and export blockers.

This milestone should not add new AI capabilities, new export formats, provider settings editing, history/versioning, analytics, CV import, authentication, or mobile-first redesign.

## User Stories

1. As a job applicant, I want the home workbench to show profile readiness, so that I know whether my profile is strong enough for generation.
2. As a job applicant, I want missing contact details to appear as warnings, so that I can keep working while understanding what may be absent from exports.
3. As a job applicant, I want missing approved profile facts to appear as a clear readiness warning, so that I know why generation may later be blocked.
4. As a job applicant, I want setup warnings to link or guide me to the relevant screen, so that I can fix incomplete setup quickly.
5. As a job applicant, I want warnings to avoid blocking unrelated work, so that I can still create and review applications.
6. As a job applicant, I want generation blockers to be explicit, so that I know when missing job posting text or approved evidence prevents an AI action.
7. As a job applicant, I want provider unavailable errors to be distinct from validation blockers, so that I do not confuse setup issues with missing workflow data.
8. As a job applicant, I want invalid AI output errors to be understandable, so that model failures do not feel like corrupted app state.
9. As a job applicant, I want existing workflow state to remain visible after errors, so that I do not lose confidence in saved work.
10. As a job applicant, I want expandable technical details for errors, so that I can inspect diagnostics when needed without cluttering the normal workflow.
11. As a job applicant, I want plain error messages to avoid raw provider payloads, so that sensitive job and profile data is not unnecessarily exposed.
12. As a job applicant, I want the application detail workflow to show the current step readiness, so that I can tell what action is available next.
13. As a job applicant, I want stale claim audit warnings to be visually consistent wherever they appear, so that I understand the same risk in the detail and export areas.
14. As a job applicant, I want missing claim audit warnings to be visually distinct from stale audit warnings, so that I know whether an audit has never run or has become outdated.
15. As a job applicant, I want export blockers to explain missing draft or empty cover letter text, so that I know what to fix before downloading.
16. As a job applicant, I want export warnings to avoid blocking export when a current draft exists, so that I can make an informed final choice.
17. As a job applicant, I want application history filters and empty states to be easy to scan, so that I can distinguish no applications from no matching results.
18. As a job applicant, I want archived applications to remain visually distinct, so that old sessions do not distract from active work.
19. As a job applicant, I want loading states to be consistent, so that AI actions, diagnostics, saves, and exports feel predictable.
20. As a job applicant, I want disabled actions to explain why they are disabled, so that I know whether the issue is data, provider availability, or workflow state.
21. As a job applicant, I want profile fact statuses to be visually clear, so that Draft, Approved, and Archived evidence cannot be confused.
22. As a job applicant, I want job-local custom fact statuses to be visually clear, so that PendingConfirmation, Approved, and Rejected facts are easy to review.
23. As a job applicant, I want the settings AI area to summarize provider, model, status, and last diagnostics coherently, so that provider setup is understandable.
24. As a job applicant, I want the fake provider to remain clearly identified, so that deterministic fake behavior is not mistaken for real model output.
25. As a job applicant, I want desktop layouts to use space efficiently, so that profile facts, evidence, drafts, audits, and history remain comfortable to scan.
26. As a job applicant, I want text areas and workflow panels to keep stable dimensions, so that editing drafts does not make the page jump unexpectedly.
27. As a job applicant, I want form validation messages to be close to the relevant fields, so that correcting data is straightforward.
28. As a job applicant, I want destructive actions to be clearly labeled and confirmable where appropriate, so that I do not accidentally remove useful work.
29. As a job applicant, I want copy and download actions to provide clear success or failure feedback, so that I know whether the final text is ready outside the app.
30. As a developer, I want shared presentation patterns for warnings, blockers, and errors, so that the UI does not drift across screens.
31. As a developer, I want error detail models to keep user-facing messages separate from technical details, so that API and UI contracts stay stable.
32. As a developer, I want frontend state for loading, success, disabled, and error cases to be explicit, so that async workflow actions are easier to maintain.
33. As a developer, I want polish changes to reuse existing endpoints and DTOs where possible, so that Milestone 7 does not become a backend redesign.
34. As a developer, I want any backend adjustments to support clearer public contracts, so that the frontend does not infer readiness from large workflow JSON unnecessarily.
35. As a developer, I want tests only where polish changes affect stable behavior or shared contracts, so that this milestone improves confidence without creating brittle visual coverage.

## Implementation Decisions

- Keep Milestone 7 focused on polish, clarity, and consistency across the completed V1 workflow.
- Preserve the existing latest-state workflow model for applications, generated drafts, approved evidence, claim audit, and export.
- Preserve the existing hard blockers: missing job posting text, missing approved profile facts or approved custom facts for generation, unavailable provider for AI actions, missing generated draft for export, and empty cover letter text for export.
- Treat incomplete profile/contact setup as warnings unless an existing endpoint requires the data for a specific action.
- Present setup readiness on the home workbench and in the profile area using the same readiness language.
- Distinguish warning, blocker, unavailable provider, invalid AI output, stale audit, missing audit, and export error states in user-facing copy.
- Provide expandable technical details for errors where diagnostics are useful.
- Do not expose raw provider request or response payloads in frontend error details.
- Keep existing workflow state visible after failed AI, diagnostics, save, copy, or export actions.
- Reuse the current AI status and diagnostics concepts rather than adding editable provider settings.
- Reuse existing application history and export behavior rather than adding new export formats.
- Use shared UI patterns for empty states, disabled actions, inline validation, loading indicators, status badges, and warning banners where the current frontend structure allows it.
- Keep the UI desktop-first and consistent with the existing workbench structure.
- Tighten layout density and scanability without introducing a full redesign.
- Avoid introducing new routes unless an existing screen cannot reasonably carry the needed clarity.
- Keep copy specific to the action the user is taking instead of using generic failure messages.
- Add small backend response fields only when they make readiness or error presentation meaningfully clearer.
- Do not add draft history, profile fact revision history, advanced job tracking, analytics, OpenAI provider support, streaming generation, CV import, PDF export, or custom DOCX templates.

## Testing Decisions

- Test observable behavior and stable public contracts, not private component implementation details.
- Prioritize backend tests only when public API contracts or readiness/error response shapes change.
- Prioritize small frontend tests only where the existing setup naturally supports shared state formatting, readiness helpers, or error presentation logic.
- Cover setup readiness helper behavior if it is extracted into a shared module.
- Cover error classification behavior if provider, validation, and invalid-output states are normalized for the UI.
- Cover export readiness behavior if summary fields or shared helpers are adjusted.
- Cover that stale or missing audit warnings do not block export when a current non-empty draft exists.
- Cover that disabled actions remain tied to the documented blockers when shared action-state logic is introduced.
- Avoid brittle snapshot tests for broad page layout.
- Avoid visual regression testing unless the project already has a lightweight pattern for it.
- Do not add tests for new AI capabilities, new export formats, history/versioning, analytics, CV import, authentication, or mobile-first behavior because those are out of scope.

## Out of Scope

- New AI provider capabilities.
- OpenAI provider support.
- Editing provider settings in the UI.
- Streaming generation.
- Raw provider payload display or storage by default.
- New export formats, including PDF.
- Custom DOCX templates.
- Exporting claim audit reports as separate documents.
- Draft history, profile fact revision history, or application event history.
- Advanced application tracking, kanban, analytics, reminders, or follow-up automation.
- CV import, CV extraction, CV tailoring, or CV layout preservation.
- Authentication, login, or multi-user support.
- Mobile-first redesign.
- Full visual redesign or rebranding.

## Further Notes

Milestone 7 should make the completed V1 feel calm and dependable. The important product behavior already exists; this milestone is about making the state of that behavior obvious to the user.

The trust boundary remains the center of the product. Polish should reinforce the chain from approved evidence to generated draft to claim audit to export, while making warnings and failures plain enough that the user can keep moving without guessing.
