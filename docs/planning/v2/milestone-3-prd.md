# V2 Milestone 3 Product Requirements Document

Status: ready-for-agent
Source: docs/planning/v2/v2-specification.md

## Goal

Make the useful AI path obvious and recoverable, then let the assistant generate and audit draft text as one guided action once evidence review is complete.

## Problem Statement

Milestone 1 makes application preparation assistant-led, and Milestone 2 makes evidence review handle both matched evidence and gaps. The workflow still needs to prove that the main product path is a useful AI-assisted drafting path, not a set of fake/demo workflow buttons.

The user needs to know whether they are using deterministic fake AI or a real configured provider before they start AI work. When the real provider is unavailable, the app should explain what is wrong where the user is trying to work. Once evidence is ready, the user should not have to separately trigger draft generation and claim audit plumbing. The assistant should generate draft text, run the audit when possible, then stop at draft review so the user can inspect and edit the result.

## Solution

Milestone 3 makes provider mode and readiness visible in the application workbench and application detail workflow, especially near preparation and draft generation actions.

Fake AI should be clearly labeled as deterministic demo/test behavior. Ollama readiness should be shown in plain language, including provider, model, endpoint reachability where available, and model availability where available. Provider failures should point to a recoverable next step rather than leaving the user with a generic failed action.

When evidence review is complete, the guided next action becomes an assistant-led draft action. That action should generate the cover letter and short motivation text, run or refresh claim audit when provider availability and inputs allow it, persist the resulting draft and audit state, and stop at draft review. Manual edits should continue to make the audit stale. Once the audit is current, the guided next action becomes copy/export.

## User Stories

1. As a job applicant, I want fake AI mode to be visibly labeled as demo/test behavior, so that I do not mistake deterministic output for serious model-generated application text.
2. As a job applicant, I want the workbench to show whether my real AI provider is available, so that I know whether the app can produce useful draft text.
3. As a job applicant, I want provider readiness shown near application preparation, so that I understand why prepare may be blocked before I press the action.
4. As a job applicant, I want provider readiness shown near draft generation, so that I understand whether the assistant can generate and audit text.
5. As a job applicant, I want provider failures explained with provider, model, and endpoint details where available, so that I can fix Ollama setup without guessing.
6. As a job applicant, I want unavailable real AI to block AI actions with plain recovery guidance, so that the app does not silently fall back to lower-value behavior.
7. As a job applicant, I want deterministic fake AI to remain available for demos and workflow testing, so that the application flow can still be exercised without a real provider.
8. As a job applicant, I want evidence review completion to lead to one clear draft action, so that I do not have to choose between generation and audit buttons.
9. As a job applicant, I want draft generation to use approved evidence and explicit gap decisions, so that the draft does not invent unsupported experience.
10. As a job applicant, I want the assistant to run claim audit after generation when possible, so that I can review a draft with trust feedback already attached.
11. As a job applicant, I want generation and audit failures to preserve the latest stable draft and workflow state, so that a failed provider call does not destroy useful work.
12. As a job applicant, I want the app to stop at draft review after generation and audit, so that I can inspect and edit the cover letter and motivation text.
13. As a job applicant, I want manual draft edits to make the audit stale, so that trust feedback is not presented as current after I change text.
14. As a job applicant, I want stale audit state to lead to a clear refresh-audit action, so that I know how to make the trust check current again.
15. As a job applicant, I want current audit state to make copy/export the next guided action, so that I can finish the application without scanning old workflow controls.
16. As a job applicant, I want unsupported or weak claims to remain visible after audit, so that I can revise before copying or exporting.
17. As a job applicant, I want fake-mode generated drafts to be labeled plainly, so that I understand their development/demo nature during review.
18. As a developer, I want provider readiness to reuse the existing AI status and diagnostics contracts, so that readiness is consistent across the app.
19. As a developer, I want combined draft generation and audit to be backend-owned orchestration, so that the frontend does not hide two separate calls behind one button.
20. As a developer, I want the draft action to reuse existing AI provider contracts, strict JSON handling, repair attempts, and `AiRun` recording, so that real and fake providers stay aligned.
21. As a developer, I want claim audit staleness to remain based on persisted draft edits, so that the trust boundary is enforced by backend state.
22. As a developer, I want this milestone to avoid OpenAI support, editable provider settings, profile import, and export redesign, so that V2.0 stays focused on the assistant-led application path.

## Implementation Decisions

- Make provider mode visible in the workbench and application detail workflow.
- Label fake AI as deterministic demo/test mode wherever it can affect user interpretation of generated output.
- Keep fake AI available for deterministic workflow testing and demos.
- Treat a real configured provider, currently Ollama, as the useful AI path for serious application text.
- Reuse existing AI status and diagnostics behavior for provider readiness.
- Show provider readiness near `Prepare application` and draft generation actions.
- Include current provider, model, endpoint reachability where available, and model availability where available in readiness messaging.
- Use plain blocked states when a real provider is configured but unavailable or failing.
- Do not silently fall back from real provider failure to fake AI for user-facing application work.
- Compute the guided next action as `Generate draft` when evidence review is complete and no current draft exists.
- Add or extend a backend-owned orchestration action for draft readiness that generates draft text and runs claim audit when provider availability and inputs allow it.
- Generate both cover letter and short motivation text from approved profile facts, approved job-local custom facts, and explicit gap decisions.
- Ensure `MentionAsLearningInterest` can guide cautious language but does not become approved evidence.
- Ensure ignored gaps are not emphasized unless independently supported by approved evidence.
- Run claim audit against the generated draft immediately after successful generation when possible.
- Persist the generated draft and current audit state as latest application state.
- Preserve the latest stable draft if generation or audit fails after a prior draft exists.
- Stop the guided workflow at draft review after generation and audit complete.
- Preserve existing editable draft behavior.
- Preserve stale audit behavior after manual edits.
- Compute the guided next action as `Refresh claim audit` when a draft exists and its audit is stale.
- Compute the guided next action as copy/export when a draft exists and audit is current.
- Keep existing copy/export mechanisms as the final action surface; do not redesign export formats in this milestone.

## Testing Decisions

- Test public workflow behavior rather than private implementation details.
- Prioritize backend tests for orchestration, provider failure handling, draft persistence, and audit staleness because these enforce the trust boundary.
- Cover fake provider mode being reported as deterministic demo/test behavior in readiness contracts.
- Cover Ollama provider readiness reporting available, unavailable endpoint, and unavailable model states where existing test seams support it.
- Cover AI actions returning recoverable blocked responses when the configured real provider is unavailable.
- Cover generation being blocked until evidence review is complete.
- Cover successful assistant-led draft generation producing cover letter and short motivation text from approved evidence and gap decisions.
- Cover generation including approved job-local custom facts for the owning application.
- Cover generation excluding unapproved job-local custom facts and learning-interest decisions from approved evidence.
- Cover successful generation followed by claim audit in the combined guided action.
- Cover audit results being persisted with the generated draft as current state.
- Cover provider failure preserving a prior stable draft and workflow state.
- Cover manual edits marking the audit stale.
- Cover stale audit causing the guided next action to become audit refresh.
- Cover current audit causing the guided next action to become copy/export.
- Cover frontend guided next-action behavior where existing frontend test structure supports it.
- Use existing AI status, draft generation, claim audit, and application workflow tests as prior art.

## Out of Scope

- Assisted profile fact import.
- Draft profile fact review queue.
- New reusable profile fact statuses.
- Full profile fact revision history.
- Full generated draft version history.
- Automatic evidence approval.
- Automatic job-local custom fact approval.
- OpenAI provider support.
- Editable provider settings in the frontend.
- Authentication or multi-user support.
- Custom DOCX template management.
- Export format redesign.
- Mobile-first redesign.
- Broad visual redesign beyond provider readiness, guided action clarity, and draft review polish needed for this milestone.

## Further Notes

Milestone 3 is the point where V2 should feel like an assistant rather than a workflow console: the app explains whether useful AI is ready, handles generation and audit together once the user has reviewed evidence, and then stops at the next human checkpoint. The trust boundary remains unchanged: concrete claims need approved evidence, gap decisions must not masquerade as proof, and audit feedback must be current before copy/export becomes the guided final action.
