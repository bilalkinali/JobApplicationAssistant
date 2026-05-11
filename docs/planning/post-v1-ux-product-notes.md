# Post-V1 UX and Product Notes

Status: draft
Source: conversation feedback after first manual app walkthrough

## Context

The current app proves the V1 trust workflow: profile facts, job analysis, evidence matching, evidence approval, draft generation, claim audit, and export. After trying the workflow manually, two product issues are clear:

- There are too many separate buttons and steps for normal use.
- The fake AI provider is useful for development and deterministic testing, but it does not provide enough real user value on its own.

## Observations

The workflow is structurally correct but too exposed. The user currently has to move through the application almost like an assembly line: save the application, analyze the job, match evidence, approve evidence, generate the draft, audit the draft, and export.

That separation is useful for proving the trust chain, but it makes the app feel more like a backend workflow console than an assistant.

The fake provider proves that state transitions, validation, evidence requirements, audit behavior, and export behavior work. It is not enough for serious job application value because it only recognizes a limited keyword set and does not deeply understand the job posting or the user's background.

## Product Direction

### One Prepare Application Action

Add a single primary action that can run the early workflow steps together:

- analyze the job posting
- match approved profile facts to job signals
- surface unmatched requirements
- prepare an evidence review state

The flow should stop when user judgment is needed, especially before evidence approval and before final text is used.

### Guided Next Action

Instead of showing every possible action with equal weight, the application detail view should show one clear primary next action based on the current workflow state.

Examples:

- No job posting: prompt the user to paste and save the posting.
- Posting captured but not analyzed: primary action is prepare/analyze.
- Analysis exists but no evidence review: primary action is review evidence.
- Evidence approved but no draft: primary action is generate draft.
- Draft exists but audit is missing or stale: primary action is run audit.
- Draft is ready: primary action is copy or export.

### Better Profile Fact Creation

Manual profile fact entry is trustworthy but slow. A future workflow should allow the user to paste CV text, project notes, or work history and have the app create draft profile facts.

The trust boundary should remain:

- imported facts start as Draft
- the user reviews and edits them
- only Approved facts can support matching or generation

### Real AI First Path

Fake provider mode should feel like a development/demo mode, not the main product path.

The app should make Ollama readiness more prominent and help the user understand when they are using:

- deterministic fake behavior
- a configured local model
- an unavailable or failing provider

### Human Review Checkpoints Only

The app should ask the user to review the important decisions, not babysit every backend step.

Important checkpoints:

- approve which evidence may support the application
- inspect unmatched requirements and decide how to handle gaps
- edit the generated text
- run or review claim audit before export

Less important as separate manual steps:

- triggering analysis separately
- triggering matching separately
- managing every intermediate state by hand

## Notes

These improvements should preserve the core V1 trust invariant: generated claims must trace back to approved profile facts or approved job-local custom facts, and unsupported claims should remain visible through audit.
