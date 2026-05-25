# Milestone 2 Real-Provider QA Handoff

Date: 2026-05-25

## Context

The app was tested against the real OpenAI-compatible provider, not Fake.

Provider status confirmed:

- Provider: `OpenAiCompatible`
- Endpoint: `http://localhost:1234/v1`
- Model used: `qwen2.5-coder-14b-instruct`
- API status returned available before live flow testing.

Ollama on `http://localhost:11434` was not reachable.

## Code Changes Made

- Evidence matching parser now tolerates malformed/unexpected unmatched requirement identifiers by deriving conservative unmatched gaps from the saved job signals.
  - `src/backend/JobApplicationAssistant.Api/Ai/OllamaAiProvider.cs`
  - `src/backend/JobApplicationAssistant.Api/Ai/OpenAiCompatibleAiProvider.cs`
- Application strategy validation now reports specific causes instead of only a generic invalid strategy message.
  - Missing required arrays
  - Invalid evidence ids
  - Invalid profile fact ids
  - Invalid unmatched requirement ids
  - Missing tone guidance
- Application strategy validation was tightened after live QA showed the local model could return a structurally valid but weak strategy:
  - `primaryAngles` must contain 2 or 3 angles.
  - `claimsToAvoid` must be non-empty when unmatched requirements exist or approved evidence includes `Partial`/`Weak`.
- Regression tests were added/updated for the above.

## Live Flow Tested

Backend was running on the launch profile API port:

- `http://127.0.0.1:5108`

Frontend was running at:

- `http://127.0.0.1:5173`

Created live test application:

- Application id: `148a6429-1890-491b-b28a-874c3bf6594a`
- Company: `Milestone 2 QA 20260525-125229`
- Role: `Backend Platform Engineer`

Live real-provider flow completed before the stricter strategy validation was added:

- `prepare` succeeded.
- Evidence matching returned 7 matches and 1 unmatched requirement.
- Approved evidence was saved.
- Kubernetes unmatched requirement was saved as `MentionAsLearningInterest`.
- `generate-draft` succeeded.
- `audit-claims` succeeded.
- Final state:
  - `preparationStatus = PreparedForEvidenceReview`
  - `hasGeneratedDraft = true`
  - `auditReadiness = Current`
  - `applicationStrategy` persisted.

In-app browser confirmed the UI showed:

- Real provider ready: `OpenAiCompatible - qwen2.5-coder-14b-instruct - Available`
- Selected application status badges:
  - `Next: Copy or export`
  - `Draft ready`
  - `Audit current`
- Application strategy summary visible.
- Generated cover letter and short motivation visible.
- Browser console error log was empty at the time checked.

## Important Finding

The real model produced a valid strategy with only:

- 1 primary angle
- 1 gap guidance item
- 0 claims to avoid
- 5 outline items

That technically passed the old schema but did not satisfy the Milestone 2 PRD intent:

- Strategy should choose 2-3 primary story angles.
- Claims to avoid should be visible when gaps or cautious evidence exist.

The stricter validation was added after this finding and the focused strategy provider tests passed, but the live app was not restarted afterward.

## Verification Completed

Passed before live QA:

- `dotnet test src\backend\JobApplicationAssistant.Api.Tests\JobApplicationAssistant.Api.Tests.csproj --filter "EvidenceMatchProviderTests|ApplicationStrategyProviderTests|recovers_incomplete_signal_coverage"`
- `dotnet test src\backend\JobApplicationAssistant.Api.Tests\JobApplicationAssistant.Api.Tests.csproj --filter "FullyQualifiedName!~PdfTextExtractorTests"`
- `npm test`
- `dotnet build src\JobApplicationAssistant.Api.sln`
- `npm run build`

Passed after tightening strategy validation:

- `dotnet test src\backend\JobApplicationAssistant.Api.Tests\JobApplicationAssistant.Api.Tests.csproj --filter ApplicationStrategyProviderTests`

Live retest after restarting the updated real-provider API:

- Backend restarted with:
  - `Ai__Provider=OpenAiCompatible`
  - `Ai__Endpoint=http://localhost:1234/v1`
  - `Ai__Model=qwen2.5-coder-14b-instruct`
  - `Ai__StoreRawPayloads=true`
- `GET http://127.0.0.1:5108/api/ai/status` returned available.
- Frontend `http://127.0.0.1:5173` returned HTTP 200.
- Created fresh live test application:
  - Application id: `f028623f-0592-4357-87fe-3d1b37379504`
  - Company: `Milestone 2 QA 20260525111340`
  - Role: `Backend Platform Engineer`
- `prepare` succeeded.
- Evidence matching returned 9 strong matches and 2 unmatched preferred requirements:
  - Kubernetes
  - Cloud-native deployment
- Non-weak evidence was approved.
- Both unmatched requirements were saved as `MentionAsLearningInterest`.
- `generate-draft` succeeded.
- Strategy repair was exercised:
  - `attemptCount = 2`
  - `primaryAngles` count = 2
  - `claimsToAvoid` count = 2
  - `gapHandlingGuidance` count = 2
- Final state:
  - `preparationStatus = PreparedForEvidenceReview`
  - `hasGeneratedDraft = true`
  - `auditReadiness = Current`
  - `applicationStrategy` persisted.

UI recheck status:

- Browser automation completed after installing frontend Playwright tooling.
- Verified through automated Chromium check:
  - Real provider badge is shown.
  - Selected application shows `Next: Copy or export`, `Draft ready`, and `Audit current`.
  - Application strategy is visible with primary angles and claims to avoid.
  - Draft and claim audit sections are visible.
  - Browser console error count was `0`.

Known unrelated issue:

- Full unfiltered backend suite fails because `PdfTextExtractorTests.ExtractAsync_returns_page_text_from_cv_with_object_streams` cannot locate its local PDF fixture.

## Current Stop Point

Live retesting after the stricter validation and browser verification is complete.

At this stop point:

- Backend is running on `http://127.0.0.1:5108` with the real OpenAI-compatible provider.
- Frontend is reachable on `http://127.0.0.1:5173`.
- Browser visual/console verification has passed.

## Resume Steps

1. Start or confirm the local OpenAI-compatible model server:
   - `http://localhost:1234/v1/models`
   - Use model `qwen2.5-coder-14b-instruct` or another available local model.
2. Start backend with real provider config:
   - `Ai__Provider=OpenAiCompatible`
   - `Ai__Endpoint=http://localhost:1234/v1`
   - `Ai__Model=qwen2.5-coder-14b-instruct`
   - `Ai__StoreRawPayloads=true`
3. Confirm:
   - `GET http://127.0.0.1:5108/api/ai/status`
4. Start or confirm frontend:
   - `http://127.0.0.1:5173`
5. Run a fresh real-provider application flow after the stricter validation:
   - Create/ensure approved profile facts.
   - Create a new application.
   - Run `prepare`.
   - Approve non-weak evidence.
   - Resolve unmatched requirements.
   - Run `generate-draft`.
   - Completed with application `f028623f-0592-4357-87fe-3d1b37379504`.
6. Expected result after validation tightening:
   - If the model returns only one primary angle or empty `claimsToAvoid` with gaps, generation should fail clearly at `ApplicationStrategy` and preserve prior state.
   - If repair succeeds, persisted strategy should have 2-3 primary angles and non-empty `claimsToAvoid` when gaps/cautious evidence exist.
   - Observed: repair succeeded and persisted 2 primary angles plus 2 claims to avoid.
7. Recheck in browser:
   - Real provider badge is shown.
   - Strategy summary is visible only after valid strategy.
   - Draft and audit state are coherent.
   - Browser console has no errors.
   - Completed after installing `@playwright/test` and Playwright Chromium.
