# Refactor `App.tsx` Handoff

## Scope

This is an implementation handoff only. The refactor should keep behavior, UI layout, CSS class names, labels, and copy unchanged unless a label is genuinely tied to a bug. The first pass should split the current `App.tsx` into smaller files without redesigning the app or introducing a new state library.

## Current Problems

`src/frontend/job-application-assistant.web/src/App.tsx` is doing too many jobs at once:

- Defines core frontend domain types for profiles, profile facts, applications, workflow results, evidence review, generated drafts, claim audits, AI diagnostics, and inline feedback.
- Defines constants and empty form objects for multiple features.
- Owns all route/view selection for `home`, `profile`, `applications`, and `settings`.
- Owns all data loading and mutations for profile, profile facts, assisted CV import, applications, workflow actions, draft editing, exports, and AI diagnostics.
- Owns all form state for profile editing, profile fact editing, application editing, evidence review drafts, custom fact drafts, generated draft edits, filters, import selection, busy flags, feedback messages, and global errors.
- Parses serialized API fields into UI-ready structures with `useMemo`.
- Computes workflow readiness, trust-chain state, export state, audit state, summaries, labels, tones, counts, and disabled reasons.
- Renders every major view and most reusable UI fragments in one large JSX tree.
- Defines reusable inputs, badges, summary components, API helpers, mapping helpers, parsing helpers, label helpers, and date formatting helpers in the same file.

The file is hard to maintain because understanding a change requires scanning unrelated features. For example, changing profile fact import review requires passing through top-level profile state, imported queue state, profile fact editor state, bulk selection state, API calls, focus behavior, and JSX all in one file. Changing the application workflow requires scanning application CRUD, job analysis, evidence matching, gap decisions, custom facts, draft generation, draft editing, claim audit, export controls, and view rendering together.

Duplicated or unclear ownership:

- Profile fact selection and imported draft fact selection are separate workflows but share the same profile fact editor state.
- Evidence review state is partly saved application data and partly unsaved draft state (`approvedEvidenceDraft`, `gapDecisionsDraft`, `reviewedWeakMatchIds`, `customFactDrafts`, `isEvidenceReviewEditing`).
- Generated draft state is split between `selectedApplication.generatedDraft`, local `generatedDraftForm`, export feedback, and audit refresh behavior.
- Application list filters are local UI state but live next to API mutation and workflow state.
- Many helper functions near the bottom are pure display or mapping helpers, but their location inside `App.tsx` makes them look coupled to the root component.
- API helpers are generic enough to live outside `App.tsx`, while feature-specific mutation flows should stay close to the feature that owns their state.

## Proposed Component Structure

Keep the first refactor shallow and mechanical: extract coherent view sections and shared helpers while leaving the existing data flow intact. Prefer prop drilling for this pass over contexts or global stores.

Suggested frontend layout:

```text
src/frontend/job-application-assistant.web/src/
  App.tsx
  api/
    client.ts
  components/
    AppShell.tsx
    FeedbackMessages.tsx
    FormControls.tsx
    StatusBadge.tsx
  features/
    home/
      HomeView.tsx
    profile/
      ProfileView.tsx
      ProfileFormPanel.tsx
      ProfileImportPanel.tsx
      ImportReviewQueues.tsx
      ProfileFactsPanel.tsx
      ProfileFactEditor.tsx
    applications/
      ApplicationsView.tsx
      ApplicationListPanel.tsx
      ApplicationEditor.tsx
      ApplicationWorkflowPanel.tsx
      PreparationSection.tsx
      EvidenceReviewSection.tsx
      DraftEditorSection.tsx
      ExportPanel.tsx
      SettingsView.tsx
    shared/
      summaries.tsx
      labels.ts
      mappers.ts
      parsers.ts
      types.ts
```

The folder names can be adjusted, but keep one principle: feature folders should contain UI that only one feature uses; `components/` should contain generic UI only.

### `App.tsx`

Responsibility:

- Own top-level data that crosses views.
- Load initial profile, profile facts, applications, and AI status.
- Own current `view`, global `error`, and global `notice`.
- Select the current application and derive high-level state needed by multiple child views.
- Pass callbacks and state into extracted views.

Props received: none.

### `components/AppShell.tsx`

Responsibility:

- Render the sidebar, navigation buttons, workspace header, AI status pill, global error, global success notice, and child content.

Suggested props:

- `view: View`
- `onViewChange: (view: View) => void`
- `title: string`
- `aiStatus: AiProviderStatus | null`
- `error: ErrorPresentation | null`
- `notice: string | null`
- `children: React.ReactNode`

### `components/FormControls.tsx`

Responsibility:

- Move `Field`, `Select`, and `Textarea` out of `App.tsx` unchanged.

Suggested props:

- Keep the current prop shapes for each component.

### `components/FeedbackMessages.tsx`

Responsibility:

- Move `ErrorMessage` and `InlineFeedbackMessage` unchanged.

Suggested props:

- `ErrorMessage`: `error`, optional `compact`
- `InlineFeedbackMessage`: `feedback`

### `components/StatusBadge.tsx`

Responsibility:

- Move `StatusBadge` unchanged.

Suggested props:

- `tone: string`
- `children: string`

### `features/home/HomeView.tsx`

Responsibility:

- Render the home dashboard panels.

Suggested props:

- `profileReadiness`
- `applications: ApplicationSession[]`
- `approvedProfileFactCount: number`
- `aiStatus: AiProviderStatus | null`
- `aiDiagnostics: AiDiagnostics | null`
- `aiDiagnosticsLastRanAt: string | null`
- `onReviewProfile: () => void`
- `onNewApplication: () => void`
- `onOpenApplication: (application: ApplicationSession) => void`

### `features/profile/ProfileView.tsx`

Responsibility:

- Compose the profile page only. It should not contain the full profile implementation long term.

Suggested props:

- `profile`, `profileReadiness`, `onProfileChange`, `onSaveProfile`
- `profileFacts`, profile fact selection state, selected fact state, and profile fact editor callbacks
- import review queues, import busy/feedback/file callbacks, and import review callbacks
- `aiStatus`

First-pass implementation can pass a wider prop object from `App.tsx`; after extraction, split into `ProfileFormPanel`, `ProfileImportPanel`, `ImportReviewQueues`, `ProfileFactsPanel`, and `ProfileFactEditor`.

### `features/profile/ProfileFormPanel.tsx`

Responsibility:

- Render only contact/tone fields and the save button.

Suggested props:

- `profile: ProfileForm`
- `profileReadiness`
- `onChange: (profile: ProfileForm) => void`
- `onSubmit: (event: FormEvent<HTMLFormElement>) => void`

### `features/profile/ProfileImportPanel.tsx`

Responsibility:

- Render assisted CV import file input, fake AI warning, busy state, and import feedback.

Suggested props:

- `aiStatus: AiProviderStatus | null`
- `busy: boolean`
- `feedback: InlineFeedback | null`
- `fileInputRef: RefObject<HTMLInputElement | null>`
- `onFileChange: (file: File | null) => void`
- `onSubmit: (event: FormEvent<HTMLFormElement>) => void`

### `features/profile/ImportReviewQueues.tsx`

Responsibility:

- Render import review groups, queue bulk actions, duplicate indicators, and selection checkboxes.

Suggested props:

- `queues: ImportedDraftFactReviewQueue[]`
- `selectedImportedDraftFactIds: string[]`
- `containerRef: RefObject<HTMLDivElement | null>`
- `selectedIdsForQueue: (queue: ImportedDraftFactReviewQueue) => string[]`
- `onToggleSelection: (factId: string) => void`
- `onOpenFact: (fact: ProfileFact) => void`
- `onMerge: (queue: ImportedDraftFactReviewQueue) => void`
- `onBulkApprove: (queue: ImportedDraftFactReviewQueue) => void`
- `onBulkArchive: (queue: ImportedDraftFactReviewQueue) => void`

### `features/profile/ProfileFactsPanel.tsx`

Responsibility:

- Render fact bulk actions and the fact card list.

Suggested props:

- `profileFacts: ProfileFact[]`
- `selectedProfileFactId: string | null`
- `selectedProfileFactIds: string[]`
- `allProfileFactsSelected: boolean`
- `onToggleAll: () => void`
- `onToggleFact: (factId: string) => void`
- `onOpenFact: (fact: ProfileFact) => void`
- `onDeleteSelected: () => void`

### `features/profile/ProfileFactEditor.tsx`

Responsibility:

- Render profile fact edit/create fields, imported draft fact split field, and fact/import actions.

Suggested props:

- `profileFactForm: ProfileFactForm`
- `selectedProfileFactId: string | null`
- `selectedImportedDraftFact: ProfileFact | undefined`
- `selectedImportReviewQueue: ImportedDraftFactReviewQueue | null | undefined`
- `splitImportedDraftFacts: string`
- `onFormChange: (form: ProfileFactForm) => void`
- `onSplitChange: (value: string) => void`
- `onSave: (event: FormEvent<HTMLFormElement>) => void`
- `onApproveImport: () => void`
- `onArchiveImport: () => void`
- `onSplitImport: () => void`
- `onRejectImport: () => void`
- `onClear: () => void`
- `onDelete: () => void`

### `features/applications/ApplicationsView.tsx`

Responsibility:

- Compose application list and application editor/workflow panels.

Suggested props:

- `filteredApplications`
- `applications`
- filter state and filter setters
- selected application state
- application form state and form callbacks
- workflow state and workflow callbacks
- draft/export/evidence review state and callbacks
- `approvedProfileFactCount`
- `aiStatus`, `aiDiagnostics`, `aiDiagnosticsLastRanAt`

This will start with many props. That is acceptable for the first extraction because it makes state ownership visible. Split further only after each child component is stable.

### `features/applications/ApplicationListPanel.tsx`

Responsibility:

- Render application filters, session list, empty states, and application selection.

Suggested props:

- `applications: ApplicationSession[]`
- `filteredApplications: ApplicationSession[]`
- `selectedApplicationId: string | null`
- `approvedProfileFactCount: number`
- `includeArchivedApplications: boolean`
- `applicationSearch: string`
- `applicationStatusFilter: string`
- `applicationReadinessFilter: string`
- `onSearchChange: (value: string) => void`
- `onStatusFilterChange: (value: string) => void`
- `onReadinessFilterChange: (value: string) => void`
- `onIncludeArchivedChange: (value: boolean) => void`
- `onNewApplication: () => void`
- `onOpenApplication: (application: ApplicationSession) => void`

### `features/applications/ApplicationEditor.tsx`

Responsibility:

- Render application detail fields, save/delete/clear actions, final status actions, and then include `ApplicationWorkflowPanel`.

Suggested props:

- `selectedApplication: ApplicationSession | undefined`
- `selectedApplicationId: string | null`
- `applicationForm: ApplicationForm`
- `workflowBusy: string | null`
- `onApplicationFormChange: (form: ApplicationForm) => void`
- `onSaveApplication: (event: FormEvent<HTMLFormElement>) => void`
- `onStartNewApplication: () => void`
- `onDeleteApplication: () => void`
- `onMarkApplied: () => void`
- `onArchive: () => void`
- `children?: React.ReactNode` for the workflow panel, or render the workflow panel directly with props

### `features/applications/ApplicationWorkflowPanel.tsx`

Responsibility:

- Compose preparation, evidence review, generation, strategy, draft editor, and disabled export state.
- Keep workflow order and markup identical.

Suggested props:

- `selectedApplication`, `hasGeneratedDraft`, `hasSavedJobPosting`, `hasSavedApprovedEvidence`
- readiness/action state: `guidedNextAction`, `jobAnalysisState`, `evidenceMatchingState`, `draftGenerationState`, `coverLetterExportState`
- parsed data: `jobSignals`, `evidenceMatches`, `unmatchedRequirements`, `savedApprovedEvidence`, `savedGapDecisions`, `customFacts`, `candidateFitBrief`, `applicationStrategy`, `claimAudit`, `draftQualityCheck`
- derived counts/summaries: `approvedEvidenceDraftSummaryCount`, `approvedRecommendedEvidenceCount`, `recommendedEvidenceMatches`, `currentApprovedCustomFactEvidence`, `currentGapDecisionCount`, `auditSummary`, `effectiveAuditReadiness`, `effectiveAuditExportNotice`
- refs: `evidenceReviewRef`, `draftReviewRef`, `exportPanelRef`
- busy state and workflow callbacks: `onRunGuidedNextAction`, `onPrepareApplication`, `onAnalyzeJob`, `onMatchEvidence`, `onGenerateDraft`, `onSaveApprovedEvidence`, `onResetEvidenceReview`
- evidence/draft callbacks and state from the sections below

### `features/applications/PreparationSection.tsx`

Responsibility:

- Render Step 1 preparation details, manual job analysis action, job signal columns, and candidate fit summary.

Suggested props:

- `selectedApplication`
- `open: boolean`
- `summary: string`
- `canRunPreparation: boolean`
- `workflowBusy: string | null`
- `workflowBusyReason: string | null`
- `jobAnalysisState`
- `jobSignals: JobSignalsDocument`
- `candidateFitBrief: CandidateFitBrief | null`
- `onPrepareApplication: () => void`
- `onAnalyzeJob: () => void`

### `features/applications/EvidenceReviewSection.tsx`

Responsibility:

- Render Step 2 evidence matching, matched evidence cards, unmatched requirements, gap decisions, custom fact editor, approved evidence list, and job-local facts.

Suggested props:

- `open`, `summary`, `evidenceReviewRef`
- `evidenceMatchingState`, `workflowBusy`, `workflowBusyReason`
- `evidenceMatches`, `recommendedEvidenceMatches`, `approvedEvidenceDraft`
- `approvedRecommendedEvidenceCount`, `approvedEvidenceDraftSummaryCount`
- `reviewedWeakMatchIds`
- `unmatchedRequirements`, `savedGapDecisions`, `gapDecisionsDraft`
- `showCompactGapDecisionReview`, `isEvidenceReviewEditing`
- `customFacts`, `customFactDrafts`, `expandedCustomFactRequirementId`, `currentApprovedCustomFactEvidence`
- callbacks: `onMatchEvidence`, `onApproveMatch`, `onApproveRecommendedEvidence`, `onReviewWeakMatch`, `onRemoveApprovedEvidence`, `onSetEvidenceReviewEditing`, `onDecideGap`, `onToggleCustomFactEditor`, `onUpdateCustomFactDraft`, `onCreateCustomFact`, `onUpdateCustomFactStatus`, `onResetEvidenceReview`, `onSaveApprovedEvidence`

### `features/applications/DraftEditorSection.tsx`

Responsibility:

- Render generated draft fields, quality warning, audit refresh, export panel, and claim audit list.

Suggested props:

- `selectedApplication`
- `generatedDraftForm`
- `hasUnsavedDraftEdits`
- `effectiveAuditReadiness`
- `effectiveAuditExportNotice`
- `isDraftQualityBlocked`
- `draftQualityCheck`
- `claimAudit`
- `auditSummary`
- `coverLetterExportState`
- `exportBusy`
- `exportFeedback`
- `workflowBusy`
- `workflowBusyReason`
- `canRefreshClaimAudit`
- `aiStatus`
- refs: `draftReviewRef`, `exportPanelRef`
- callbacks: `onDraftFormChange`, `onSaveGeneratedDraft`, `onRefreshClaimAudit`, `onCopyCoverLetter`, `onDownloadCoverLetter`, `onClearExportFeedback`

### `features/applications/ExportPanel.tsx`

Responsibility:

- Render copy/download controls for either enabled or disabled export state.

Suggested props:

- `coverLetterExportState`
- `exportBusy`
- `exportFeedback`
- `effectiveAuditExportNotice`
- `disabled?: boolean`
- `hasGeneratedDraft?: boolean`
- `panelRef?: RefObject<HTMLElement | null>`
- `onCopyCoverLetter: () => void`
- `onDownloadCoverLetter: (format: "txt" | "docx") => void`

### `features/applications/SettingsView.tsx`

Responsibility:

- Render AI settings and diagnostics result.

Suggested props:

- `aiStatus: AiProviderStatus | null`
- `aiDiagnostics: AiDiagnostics | null`
- `aiDiagnosticsError: ErrorPresentation | null`
- `aiDiagnosticsBusy: boolean`
- `aiDiagnosticsLastRanAt: string | null`
- `onRunAiDiagnostics: () => void`

## Supporting Files

### `api/client.ts`

Move generic API helpers out of `App.tsx`:

- `apiBaseUrl`
- `apiGet`
- `apiSend`
- `apiSendForm`
- `apiDelete`
- `readResponse`

Keep the helper behavior identical, including error handling. `downloadCoverLetter` can keep direct `fetch` in `App.tsx` or use `apiBaseUrl` from this file for the first pass.

### `features/shared/types.ts`

Move local type definitions out of `App.tsx` when they are needed by extracted components:

- `ProfileForm`, `ProfileFactForm`, `ProfileFact`
- import review response/queue types
- `ApplicationForm`, `ApplicationSession`, `PrepareApplicationResult`
- `GeneratedDraft`, `GeneratedDraftForm`
- `View`
- `JobSignalsDocument`, `EvidenceMatch`, `CustomFact`, `CustomFactDraft`, `UnmatchedRequirement`, `GapDecision`, `ClaimAudit`, `DraftQualityCheck`
- `AiProviderStatus`, `AiDiagnostics`, `InlineFeedback`

Do this early enough that extracted components can import shared types without circular imports.

### `features/shared/mappers.ts`

Move mapping and form helpers:

- `emptyProfile`
- `emptyApplication`
- `emptyProfileFact`
- `toProfileForm`
- `toProfileFactForm`
- `toProfileFact`
- `toApplicationForm`
- `toApplicationSession`
- `toApplicationPayload`

### `features/shared/parsers.ts`

Move JSON parsing helpers:

- `parseJobSignals`
- `parseClaimAudit`
- `parseDraftQualityCheck`
- `parseJsonArray`
- `parseJsonObject`

Only move these if imports stay simple. It is acceptable to leave them in `App.tsx` until the components that need them are extracted.

### `features/shared/labels.ts`

Move pure display helpers and labels:

- `applicationStatuses`
- `auditReadinessOptions`
- `profileFactStatuses`
- `pageTitle`
- `titleCase`
- `formatDate`
- `formatDateTime`
- status/readiness label and tone helpers
- gap decision label/tone/class helpers
- custom fact status helpers
- `countLabel`
- `disabledTitle`

Avoid over-splitting this file during the first pass. If it becomes too broad later, split by feature.

### `features/shared/summaries.tsx`

Move UI summary components:

- `SignalColumn`
- `CandidateFitBriefSummary`
- `ApplicationStrategySummary`
- `ProviderReadinessSummary`

These components use existing parsed data and should not own data loading.

## State Ownership

### Keep in `App.tsx` for the first extraction

Keep state in `App.tsx` when it coordinates multiple views, API reloads, or selected application/session identity:

- `view`
- `profile`
- `profileFacts`
- `applications`
- `selectedApplicationId`
- `applicationForm`
- `selectedProfileFactId`
- `profileFactForm`
- `error`
- `notice`
- `aiStatus`
- `aiDiagnostics`
- `aiDiagnosticsError`
- `aiDiagnosticsBusy`
- `aiDiagnosticsLastRanAt`
- top-level loaders and mutation callbacks that update multiple state groups

This keeps the first refactor low risk. Child components receive state and callbacks, render markup, and call back into the existing behavior.

### Move into child components when extracted

Move purely local UI state only after the relevant component has been extracted and the behavior is easy to verify:

- Application list filters can move into `ApplicationListPanel` because they only affect the list display.
- `profileImportFile` can move into `ProfileImportPanel` if `importProfilePdfCv` is changed to receive the selected file directly.
- `selectedImportedDraftFactIds` can move into `ImportReviewQueues` if bulk action callbacks receive selected IDs instead of reading root state.
- `selectedProfileFactIds` can move into `ProfileFactsPanel` if delete callbacks receive selected IDs.
- `expandedCustomFactRequirementId` and `customFactDrafts` can move into `EvidenceReviewSection` because they are local to the gap/custom fact editor.
- `exportFeedback` can move into `DraftEditorSection` or `ExportPanel` only if copy/download callbacks return feedback instead of setting root state.

Do not move these during the first component extraction unless the extraction is otherwise awkward. The initial goal is smaller files, not a new state architecture.

### Hooks to consider later

Hooks are optional and should come after the component split. Add them only if they reduce prop noise or consolidate feature behavior with clear ownership:

- `useProfileData` for profile, facts, fact CRUD, import review queues, and assisted import.
- `useApplicationsData` for application loading, selected application replacement, form save/delete/status, and archived include behavior.
- `useEvidenceReviewDraft` for approved evidence draft, weak match review, gap decisions, custom fact draft state, reset/save handlers, and derived counts.
- `useGeneratedDraftWorkflow` for draft form edits, save/audit/generate, export state, export feedback, and stale audit handling.
- `useAiDiagnostics` for AI status, diagnostics, diagnostics errors, and refresh.

Avoid creating hooks that merely wrap one setter or pass through the same long list of fields. A hook should improve locality by owning a coherent workflow.

## Suggested Extraction Order

Each step should compile and keep the app working before moving to the next.

1. Move generic UI controls.
   - Create `components/FormControls.tsx`, `components/FeedbackMessages.tsx`, and `components/StatusBadge.tsx`.
   - Import them back into `App.tsx`.
   - No behavior changes.

2. Move shared types and constants needed by components.
   - Create `features/shared/types.ts`.
   - Create `features/shared/labels.ts` only for constants and simple display helpers needed immediately.
   - Keep imports explicit and avoid barrel files at first.

3. Move API helpers.
   - Create `api/client.ts`.
   - Export `apiBaseUrl`, `apiGet`, `apiSend`, `apiSendForm`, and `apiDelete`.
   - Update `App.tsx` imports.
   - Keep response parsing behavior identical.

4. Extract shell/navigation.
   - Create `components/AppShell.tsx`.
   - Move sidebar, navigation, workspace header, status pill, `ErrorMessage`, and notice rendering.
   - `App.tsx` should render the selected view inside `AppShell`.

5. Extract `HomeView`.
   - Create `features/home/HomeView.tsx`.
   - Move only the home JSX.
   - Pass existing derived values and callbacks from `App.tsx`.

6. Extract settings.
   - Create `features/applications/SettingsView.tsx` or `features/settings/SettingsView.tsx`.
   - Move AI settings and diagnostics JSX.
   - Keep `runAiDiagnostics` in `App.tsx` for now.

7. Extract profile page in layers.
   - Start with `ProfileFormPanel`.
   - Then `ProfileImportPanel`.
   - Then `ImportReviewQueues`.
   - Then `ProfileFactsPanel`.
   - Then `ProfileFactEditor`.
   - Finally add `ProfileView` as a composition wrapper if useful.

8. Extract application list and editor.
   - Create `ApplicationListPanel` for filters and sessions.
   - Create `ApplicationEditor` for form fields, save/delete/clear, and final status actions.
   - Keep workflow JSX inside `App.tsx` until the list/editor split is stable.

9. Extract application workflow in layers.
   - Create `PreparationSection`.
   - Create `EvidenceReviewSection`.
   - Create `DraftEditorSection`.
   - Create `ExportPanel`.
   - Create `ApplicationWorkflowPanel` as the composition wrapper.

10. Move pure helpers after their callers are stable.
    - Move mappers, parsers, labels, summary components, and count helpers into shared files.
    - Prefer one helper move at a time, with imports updated immediately.

11. Consider hooks only after the component split.
    - If prop lists are still hard to work with, extract one coherent hook at a time.
    - Start with `useEvidenceReviewDraft` or `useGeneratedDraftWorkflow`, because those have the clearest local state clusters.

## Constraints

- Do not change behavior.
- Do not redesign the UI.
- Do not rename labels, headings, button text, warnings, or status text unless required by a bug.
- Do not introduce context, reducers, state machines, route libraries, or new data-fetching libraries for this refactor.
- Do not create barrel files until imports have settled.
- Keep CSS class names unchanged.
- Keep API request paths, methods, payload shapes, and response normalization unchanged.
- Keep refs and scroll behavior intact.
- Keep the existing `readiness.ts`, `evidenceReview.ts`, `candidateFitBrief.ts`, `applicationStrategy.ts`, `exportControls.ts`, and `errorPresentation.ts` helpers as they are unless a direct import cleanup is needed.
- Do not combine this with feature work.

## Implementation Checklist

- [x] Create `src/frontend/job-application-assistant.web/src/components/FormControls.tsx`.
- [x] Move `Field`, `Select`, and `Textarea` from `App.tsx`.
- [x] Create `src/frontend/job-application-assistant.web/src/components/FeedbackMessages.tsx`.
- [x] Move `ErrorMessage` and `InlineFeedbackMessage`.
- [x] Create `src/frontend/job-application-assistant.web/src/components/StatusBadge.tsx`.
- [x] Move `StatusBadge`.
- [x] Create `src/frontend/job-application-assistant.web/src/features/shared/types.ts`.
- [x] Move shared frontend types needed by extracted components.
- [x] Create `src/frontend/job-application-assistant.web/src/api/client.ts`.
- [x] Move API helper functions and `apiBaseUrl`.
- [x] Create `src/frontend/job-application-assistant.web/src/components/AppShell.tsx`.
- [x] Move shell, navigation, workspace header, global error, and notice rendering.
- [x] Create `src/frontend/job-application-assistant.web/src/features/home/HomeView.tsx`.
- [x] Move home view JSX.
- [x] Create `src/frontend/job-application-assistant.web/src/features/applications/SettingsView.tsx` or `src/frontend/job-application-assistant.web/src/features/settings/SettingsView.tsx`.
- [x] Move AI settings JSX.
- [x] Create `src/frontend/job-application-assistant.web/src/features/profile/ProfileFormPanel.tsx`.
- [x] Move contact/tone form JSX.
- [x] Create `src/frontend/job-application-assistant.web/src/features/profile/ProfileImportPanel.tsx`.
- [x] Move assisted CV import JSX.
- [x] Create `src/frontend/job-application-assistant.web/src/features/profile/ImportReviewQueues.tsx`.
- [x] Move imported draft fact review queue JSX.
- [x] Create `src/frontend/job-application-assistant.web/src/features/profile/ProfileFactsPanel.tsx`.
- [x] Move fact bulk actions and fact list JSX.
- [x] Create `src/frontend/job-application-assistant.web/src/features/profile/ProfileFactEditor.tsx`.
- [x] Move profile fact editor JSX.
- [ ] Optionally create `src/frontend/job-application-assistant.web/src/features/profile/ProfileView.tsx` to compose profile panels.
- [x] Create `src/frontend/job-application-assistant.web/src/features/applications/ApplicationListPanel.tsx`.
- [x] Move application filters and session list JSX.
- [x] Create `src/frontend/job-application-assistant.web/src/features/applications/ApplicationEditor.tsx`.
- [x] Move application detail form and final status actions.
- [x] Create `src/frontend/job-application-assistant.web/src/features/applications/PreparationSection.tsx`.
- [x] Move Step 1 workflow JSX.
- [x] Create `src/frontend/job-application-assistant.web/src/features/applications/EvidenceReviewSection.tsx`.
- [x] Move Step 2 evidence/gap/custom fact review JSX.
- [x] Create `src/frontend/job-application-assistant.web/src/features/applications/DraftEditorSection.tsx`.
- [x] Move generated draft editing, draft quality, and claim audit JSX.
- [x] Create `src/frontend/job-application-assistant.web/src/features/applications/ExportPanel.tsx`.
- [x] Move enabled and disabled export controls.
- [x] Create `src/frontend/job-application-assistant.web/src/features/applications/ApplicationWorkflowPanel.tsx`.
- [x] Compose preparation, evidence review, generation, strategy, draft editor, and export sections.
- [ ] Create `src/frontend/job-application-assistant.web/src/features/shared/mappers.ts` after component imports stabilize.
- [ ] Move empty form objects and mapping helpers.
- [ ] Create `src/frontend/job-application-assistant.web/src/features/shared/parsers.ts` after parsed data consumers are clear.
- [ ] Move JSON parsing helpers.
- [ ] Create or expand `src/frontend/job-application-assistant.web/src/features/shared/labels.ts`.
- [ ] Move pure label, tone, count, date, and disabled-title helpers.
- [x] Create `src/frontend/job-application-assistant.web/src/features/shared/summaries.tsx`.
- [x] Move `SignalColumn`, `CandidateFitBriefSummary`, `ApplicationStrategySummary`, and `ProviderReadinessSummary`.
- [x] Run `npm --prefix src/frontend/job-application-assistant.web run build`.
- [ ] Manually smoke test the home, profile, applications, evidence review, draft export, and AI settings views.

## Notes for the Implementer

Expect the first few extracted components to have large prop lists. That is a temporary and useful sign that the current state ownership is being made explicit. Resist fixing that with context or a reducer during the first pass. Once the markup is separated, the real state clusters will be easier to see and can be moved into feature hooks one at a time.
