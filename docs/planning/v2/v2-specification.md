**V2 Specification**

**Status**
Draft

**Source Inputs**
- `docs/planning/v1/post-v1-ux-product-notes.md`
- Personal product observation: fake AI remains useful for development and functional testing, but the product value depends on a working AI path that automates more of the application preparation workflow.

**Product Direction**
V2 turns the app from a step-by-step workflow console into an assistant-led application workbench.

The V1 trust workflow is structurally correct: profile facts, job analysis, evidence matching, evidence approval, draft generation, claim audit, and export. The V2 problem is that too much of that workflow is exposed as separate manual actions. The user should not have to babysit analysis, matching, draft creation, and audit as independent buttons when the app can safely run those steps in sequence.

V2 should make the app feel like it handles the application while preserving the trust boundary: concrete generated claims must trace back to approved profile facts or approved job-local custom facts, and unsupported claims must stay visible through audit.

**V2 Goals**
- Reduce normal application preparation to fewer primary actions.
- Make the working AI path the main product path, with fake AI clearly positioned as development/demo mode.
- Automate backend-safe workflow steps until user judgment is required.
- Keep human review for trust-sensitive decisions.
- Make the application detail page show one guided next action instead of many equally weighted buttons.
- Preserve latest-state workflow storage unless a V2 feature directly requires history.

**V2.0 Non-Goals**
V2.0 does not need to include:

- Assisted profile fact import
- OpenAI API support
- Multi-user accounts
- Authentication
- Advanced job tracking analytics
- CV layout editing
- Full profile fact revision history
- Full generated draft version history
- Custom DOCX template management
- Mobile-first redesign
- Visual redesign as the primary deliverable

The UI can improve where needed, but the product target is functional UX: fewer clicks, clearer next action, and more useful automation.

**Core Product Principles**

**Assistant-led, not user-babysat**
The app should run safe workflow steps automatically after the user provides the required input. The user should be asked to decide, review, approve, or edit only where judgment changes the meaning or risk of the output.

**Automation stops at trust boundaries**
The app may analyze postings, match evidence, draft text, and audit claims without separate manual triggers. It must stop before using evidence as approved proof and before treating final application text as ready.

**Fake AI is a test mode**
Fake AI should remain deterministic and valuable for testing state transitions, validation, exports, and demos. It should not be presented as the expected way to get serious application value.

**Real AI readiness is product-critical**
Because the app's value is helping the user fill out cover letters and motivation text, the real AI path must be visible, understandable, and recoverable when provider setup fails.

**Canonical Terms**

**Prepare Application**
The assistant-led workflow that analyzes the job posting, matches approved profile facts to job signals, surfaces unmatched requirements, and prepares an evidence review state.

**Guided Next Action**
The single primary action shown for the current application state. It should represent what the user most likely needs to do next.

**Human Checkpoint**
A point where automation pauses because user judgment is required. V2.0 checkpoints are evidence approval, gap handling, draft editing, claim audit review, and export/copy.

**Job-local Custom Fact**
A user-provided fact scoped to one application. It cannot support generated claims until reviewed and approved.

**Working AI Provider**
A real configured provider that can perform the application workflow with meaningful semantic understanding. In the current architecture, this means Ollama when configured and available.

**Primary Workflow**

1. User creates or opens an application.
2. User pastes the job posting.
3. The app saves the posting and offers a single `Prepare application` action.
4. `Prepare application` runs:
   - job analysis
   - evidence matching against approved profile facts
   - unmatched requirement detection
   - evidence review preparation
5. The app stops at evidence review.
6. User approves matched evidence, removes weak matches, or manually attaches existing approved profile facts.
7. If gaps remain, the app shows them in the same review flow and lets the user either ignore them, mention them carefully as learning interest, or add a job-local custom fact for review.
8. Once evidence is approved, the guided next action becomes draft generation.
9. Draft generation may run together with claim audit when provider availability and inputs allow it.
10. The app stops at draft review.
11. User edits generated cover letter and short motivation text.
12. If the user edits text, claim audit becomes stale.
13. The guided next action becomes run or refresh audit.
14. Once audit is current, the guided next action becomes copy/export.

**Guided Next Action Rules**

The application detail page should compute one primary next action from workflow state:

```text
No job posting
=> Paste and save job posting

Job posting saved, no current analysis/evidence preparation
=> Prepare application

Preparation failed because provider unavailable
=> Fix AI provider or switch mode

Application prepared, evidence needs review
=> Review evidence

Approved evidence exists, no current draft
=> Generate draft

Draft exists, audit missing
=> Run claim audit

Draft exists, audit stale after edits
=> Refresh claim audit

Draft exists, audit current
=> Copy or export
```

Secondary actions may still exist, but they should not compete visually with the guided next action.

**Prepare Application Behavior**

`Prepare application` is a backend-owned orchestration action. It should:

- require saved job posting text
- require at least one approved profile fact before evidence matching
- call the configured AI provider for job analysis
- call the configured AI provider for evidence matching
- update application workflow state only after valid provider output
- persist job signals, evidence matches, unmatched requirements, and AI run records
- return a response that tells the frontend which human checkpoint is next

If analysis succeeds but matching fails, the app should preserve valid analysis state and explain that matching needs to be retried. If provider output is invalid, the app should keep prior stable state rather than partially corrupting the application.

**Evidence Review**

Evidence review remains the most important human checkpoint.

The user should be able to:

- approve matched evidence
- remove weak or irrelevant matches
- inspect unmatched requirements in the same review flow
- mark an unmatched requirement as ignored or safe to mention as a learning interest
- add job-local custom facts
- approve or reject job-local custom facts after normalization

Only approved profile facts and approved job-local custom facts may support generated claims.

**Gap Handling**

Unmatched requirements should be actionable, but V2 should avoid turning them into a separate planning system.

Each unmatched requirement may have a lightweight handling state:

```text
Ignore
MentionAsLearningInterest
CoveredByCustomFact
```

V2 generation should use this decision to avoid overstating experience. `MentionAsLearningInterest` may shape cautious language, but it is not approved evidence.

**AI Provider UX**

The app should make provider mode obvious in the workbench and application detail view.

Provider states:

```text
Fake
ConfiguredRealAvailable
ConfiguredRealUnavailable
ConfiguredRealFailing
```

Fake mode copy should be plain: it is deterministic and useful for testing, but real provider setup is recommended for useful application text.

Real provider failure should show:

- current provider
- model
- endpoint when relevant
- whether the endpoint is reachable
- whether the configured model is available

V2 does not require editable provider settings in the frontend unless that becomes the simplest way to make setup recoverable.

**Backend Additions**

Add an orchestration endpoint:

```http
POST /api/applications/{id}/prepare
```

The endpoint should run job analysis and evidence matching as one workflow action.

Possible response shape:

```json
{
  "applicationId": "guid",
  "status": "PreparedForEvidenceReview",
  "nextAction": "ReviewEvidence",
  "completedSteps": ["AnalyzeJob", "MatchEvidence"],
  "blockedReason": null
}
```

Store lightweight unmatched requirement handling as part of the existing unmatched requirements workflow artifact:

```json
[
  {
    "jobSignalId": "string",
    "decision": "Ignore | MentionAsLearningInterest | CoveredByCustomFact",
    "customFactId": "string|null",
    "note": "string|null"
  }
]
```

**Frontend Changes**

**Application Detail**
- Replace the visible row of independent workflow buttons with one guided primary action.
- Keep secondary actions available, but visually quieter.
- Show preparation progress as a compact status, not as separate required user work.
- Make evidence review the central checkpoint after preparation, including unmatched requirement handling.
- Make draft review and audit status the central checkpoints after generation.

**Profile**
- Preserve the existing manual fact editor.

**Workbench**
- Show provider mode and readiness prominently.
- Make recent applications show their next action.
- Let the user resume an application from the next meaningful checkpoint.

**Settings / AI**
- Keep diagnostics.
- Explain fake mode versus real provider mode plainly.
- Show current provider/model availability.

**Data Model**

Reuse V1 entities where possible.

Likely additions:

```text
JobApplication
- LastPreparedAt nullable
- PreparationStatus nullable
```

Preparation status values:

```text
NotStarted
Preparing
PreparedForEvidenceReview
FailedProviderUnavailable
FailedInvalidProviderOutput
PartiallyPreparedAnalysisOnly
```

Avoid adding new profile fact statuses or full history unless a later workflow proves they are needed.

**AI Contract Use**

Keep the existing strict JSON, validation, repair attempt, and graceful failure rules for real provider output.

For draft generation, include lightweight gap handling so the provider knows which gaps may be framed as learning interest and which should be ignored.

**Milestones**

**V2.0: Guided Application Workbench**

V2.0 proves the core assistant-led application workflow: guided next action, real AI readiness, backend orchestration, evidence review, gap handling, draft generation, and the audit loop.

**Milestone 1: Guided Application Flow**
- Compute next action from application state.
- Update application list and detail UI to emphasize the next action.
- Reduce visual prominence of secondary actions.
- Add `POST /api/applications/{id}/prepare`.
- Run job analysis and evidence matching together.
- Return next checkpoint metadata.
- Wire the frontend primary action to preparation.
- Keep evidence review as the first human checkpoint.

**Milestone 2: Evidence Review That Handles Gaps**
- Keep matched evidence approval fast and clear.
- Show unmatched requirements inside the same review flow.
- Let users ignore an unmatched requirement, mention it cautiously as learning interest, or cover it with a reviewed job-local custom fact.
- Feed lightweight gap handling into draft generation.
- Ensure learning-interest wording cannot become approved evidence.

**Milestone 3: Useful AI Path and Draft Readiness**
- Make fake mode visibly a demo/test mode.
- Improve Ollama readiness messages where the user starts AI work.
- Show provider/model availability near application preparation.
- Allow draft generation and claim audit to run as a combined assistant-led action when evidence is ready.
- Stop at draft review.
- Preserve stale audit behavior after manual edits.
- Make copy/export the guided next action once audit is current.

**V2.1: Assisted Profile Fact Import**

Assisted profile fact import is valuable, but it expands V2 beyond the core assistant-led application workflow. After V2.0 is stable, V2.1 should reduce profile setup friction.

V2.1 is essential to the product UX because the application workflow depends on approved profile facts. If the user only has one manually entered skill or a thin profile, job analysis and evidence matching can run, but the generated application will not have enough credible personal evidence. The app should let the user upload a CV or paste CV text, ask the configured AI provider to extract as much useful profile information as possible, and then guide the user through review.

V2.1 term:

**Draft Profile Fact**
A profile fact created from imported text that cannot support matching or generation until the user reviews and approves it.

- Add pasted text import for CV text, project notes, work history, achievement bullets, and raw personal notes.
- Add CV file upload, with PDF and DOCX as the highest-value initial formats.
- Extract draft profile facts through the AI provider.
- Extract technical skills, tools, platforms, programming languages, projects, work history, responsibilities, achievements, education, certifications, languages, domains, and allowed claims where present.
- Save imported facts as `Draft`.
- Preserve an original imported snapshot when useful.
- Add a review queue for imported draft facts.
- Optimize the review queue for quick approve, edit, merge, split, archive, and reject decisions.
- Detect duplicate or overlapping facts against the existing profile and within the import batch.
- Require user approval before imported facts can support matching or generation.
- Keep CV layout editing, CV generation, and automatic approval out of scope.

**Acceptance Criteria**

- A normal application can move from pasted posting to evidence review with one primary action.
- The application detail view always has at most one visually dominant next action.
- `POST /api/applications/{id}/prepare` runs job analysis and evidence matching as one backend-owned orchestration action and returns the next checkpoint.
- Fake AI mode is clearly labeled as deterministic test/demo behavior.
- Real provider unavailability blocks AI actions with recoverable, plain guidance.
- Approved evidence remains required before concrete generated claims are produced.
- Lightweight gap handling affects generation without weakening the evidence trust boundary.
- Claim audit still flags unsupported claims and becomes stale after edits.
- Copy/export is presented as the final guided action once a current audited draft exists.

**Open Product Questions**

1. Should `Prepare application` run automatically immediately after saving a job posting when a real provider is available, or should it remain one explicit click?
   Recommended answer: keep one explicit click in V2, then consider auto-run after the user trusts the behavior.

2. Should generation and audit be one action?
   Recommended answer: yes, when provider availability allows it. The user cares about reviewing a ready draft, not manually triggering audit plumbing.

3. Should fake AI be allowed to run the whole V2 automated flow?
   Recommended answer: yes for development and demos, but label the output clearly as deterministic test behavior.

4. Should V2 add history?
   Recommended answer: no, unless a specific V2 workflow needs it. Latest-state storage keeps the product simpler while the automation model is still settling.
