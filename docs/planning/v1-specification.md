**V1 Specification**

**Scope**
Build a personal desktop web app that generates tailored cover letters and short motivation texts for software developer job applications.

V1 focuses on:

- Manual profile/contact info
- Manual structured profile facts
- Job posting analysis
- Evidence matching and review
- Unmatched requirement handling
- Cover letter generation
- Short motivation generation
- Claim audit
- Saved application sessions
- Fake AI provider first
- Ollama provider support behind the same interface

The app should help produce strong, honest application text without inventing experience.

**Non-Scope**
V1 does not include:

- CV tailoring or CV layout preservation
- CV import/extraction yet
- OpenAI API dependency
- Authentication/login
- Multi-user support
- Full version history
- Application analytics
- Advanced job tracking/kanban
- Custom DOCX templates
- Streaming generation
- Mobile-first UI
- Encryption at rest

**Architecture**
Use a separate new application.

Backend:

- ASP.NET Core Minimal API
- Entity Framework Core
- PostgreSQL
- Vertical feature slices
- JSONB for flexible AI/workflow artifacts
- Backend-owned AI provider calls

Frontend:

- React + Vite
- TypeScript
- Desktop-first UI
- Step-by-step workflow
- Simple editable text areas for generated drafts

High-level modules:

```text
Profile
Applications
Ai
Diagnostics
Export
```

**Data Model**
Core relational entities:

```text
Profile
- Id
- FullName
- Email
- Phone
- Location
- LinkedInUrl
- GitHubUrl
- PortfolioUrl
- DefaultLanguage
- DanishTone
- EnglishTone
- CreatedAt
- UpdatedAt
```

```text
ProfileFact
- Id
- Type
- Title
- Summary
- Status
- FactItems jsonb
- Technologies jsonb
- AllowedClaims jsonb
- ForbiddenClaims jsonb
- SourceDocumentIds jsonb
- OriginalImportedSnapshot jsonb nullable
- ManuallyEdited
- CreatedAt
- UpdatedAt
```

For v1, `SourceDocumentIds` and `OriginalImportedSnapshot` can exist but remain mostly unused until CV import is added.

```text
JobApplication
- Id
- CompanyName
- RoleTitle
- ApplicationUrl
- Deadline nullable
- Status
- JobPostingText
- DetectedLanguage
- SelectedLanguage
- JobSignals jsonb
- EvidenceMatches jsonb
- UnmatchedRequirements jsonb
- ApprovedEvidence jsonb
- CoverLetterText
- ShortMotivationText
- ClaimAudit jsonb
- GeneratedAt nullable
- LastEditedAt nullable
- AuditUpdatedAt nullable
- CreatedAt
- UpdatedAt
```

```text
AiRun
- Id
- JobApplicationId nullable
- Step
- Provider
- Model
- Status
- ErrorCode nullable
- ErrorMessage nullable
- AttemptCount
- StartedAt
- CompletedAt nullable
- InputSummary jsonb nullable
- OutputSummary jsonb nullable
```

Latest-state storage only. No draft history or profile fact revision history in v1.

**AI Provider Abstraction**
Application code depends on an internal interface, not a specific provider.

```csharp
public interface IAiProvider
{
    Task<AiProviderStatus> GetStatusAsync(CancellationToken ct);
    Task<JobAnalysisResult> AnalyzeJobAsync(JobAnalysisInput input, CancellationToken ct);
    Task<EvidenceMatchResult> MatchEvidenceAsync(EvidenceMatchInput input, CancellationToken ct);
    Task<DraftGenerationResult> GenerateDraftAsync(DraftGenerationInput input, CancellationToken ct);
    Task<ClaimAuditResult> AuditClaimsAsync(ClaimAuditInput input, CancellationToken ct);
    Task<AiDiagnosticsResult> RunDiagnosticsAsync(CancellationToken ct);
}
```

Providers:

```text
FakeAiProvider
OllamaAiProvider
```

No OpenAI provider in v1. The app must start without OpenAI configuration.

**Fake Provider Behavior**
Fake provider is first-class in v1.

Purpose:

- deterministic development
- stable backend tests
- usable UI workflow before local model tuning

Behavior:

- derives job signals from simple keyword matching
- matches job signals against profile technologies/facts
- creates unmatched requirements when no approved evidence exists
- generates predictable cover letter and short motivation text from approved evidence
- creates predictable claim audit statuses

Example matching:

```text
Job contains ".NET" + profile fact has ".NET" => supported match
Job contains "Kubernetes" + no evidence => unmatched requirement
```

Fake provider should not pretend to be intelligent. It exists to validate the workflow.

**Ollama Provider Behavior**
Ollama is the first real local AI provider.

Config:

```json
{
  "Ai": {
    "Provider": "Ollama",
    "Endpoint": "http://localhost:11434",
    "Model": "llama3.1:8b",
    "TimeoutSeconds": 120,
    "StoreRawPayloads": false
  }
}
```

Behavior:

- backend calls local Ollama endpoint
- no API key required
- app starts even if Ollama is unavailable
- AI actions return clear unavailable errors when Ollama cannot be reached
- model is config-only in v1
- UI displays provider/model status read-only
- diagnostics are manually triggered

AI output handling:

- prompts stored as backend `.md` files
- outputs expected as strict JSON
- deserialize into C# DTOs
- validate DTOs
- allow one repair attempt for invalid JSON/invalid structure
- if still invalid, fail gracefully and record minimal `AiRun`

Do not store raw requests/responses by default.

**AI Pipeline**
Step-by-step workflow:

1. User creates/updates profile facts manually.
2. User creates application and pastes job posting.
3. AI analyzes job posting.
4. AI extracts:
   - company name
   - role title
   - detected language
   - job signals
   - required/preferred skills
   - responsibilities
5. AI matches job signals to approved profile facts.
6. App shows evidence review:
   - matched evidence
   - unmatched requirements
   - recommended “mention as interest to learn” options
7. User approves/removes evidence.
8. User may add job-local custom facts.
9. Custom facts are normalized by AI and require confirmation.
10. AI generates:
   - one full cover letter
   - one short motivation text
11. AI audits generated claims against approved evidence.
12. User edits draft.
13. If edited, audit is marked stale.
14. User may manually trigger claim re-check.
15. Application session is saved as latest state.

**Screens**
Desktop-first screens:

```text
Home / Workbench
- profile readiness
- new application entry
- recent applications
- AI status indicator
```

```text
Profile
- contact info
- tone/language preferences
- profile facts list
- create/edit/delete facts
```

```text
Applications List
- company
- role
- status
- language
- updated date
- simple filters
```

```text
New/Application Detail
- job posting input
- job analysis result
- evidence review
- unmatched requirements
- custom fact entry
- generated cover letter
- short motivation
- claim audit
- status metadata
```

```text
Settings / AI
- current provider
- endpoint
- model
- provider status
- manual diagnostics button
```

Warnings instead of rigid setup blocking.

Block only when:

- no job posting text
- no approved evidence/custom evidence
- AI provider unavailable for AI action

**Backend Endpoints**
Profile:

```http
GET    /api/profile
PUT    /api/profile
GET    /api/profile/facts
POST   /api/profile/facts
PUT    /api/profile/facts/{id}
DELETE /api/profile/facts/{id}
```

Applications:

```http
GET    /api/applications
POST   /api/applications
GET    /api/applications/{id}
PUT    /api/applications/{id}
DELETE /api/applications/{id}
```

AI workflow:

```http
POST /api/applications/{id}/analyze-job
POST /api/applications/{id}/match-evidence
PUT  /api/applications/{id}/approved-evidence
POST /api/applications/{id}/custom-facts/normalize
POST /api/applications/{id}/generate-draft
POST /api/applications/{id}/audit-claims
```

AI status:

```http
GET  /api/ai/status
POST /api/ai/diagnostics
```

Export:

```http
GET /api/applications/{id}/export/cover-letter.docx
GET /api/applications/{id}/export/cover-letter.txt
```

**Milestones**
Milestone 1: Project foundation

- create .NET API
- create React app
- configure PostgreSQL/EF Core
- create core entities
- add basic profile/application CRUD

Milestone 2: Manual profile facts

- contact info UI
- profile facts UI
- JSONB-backed fact fields
- validation

Milestone 3: Fake AI workflow

- `IAiProvider`
- `FakeAiProvider`
- job analysis
- evidence matching
- unmatched requirements
- evidence review UI

Milestone 4: Draft generation and audit

- generate cover letter
- generate short motivation
- editable drafts
- claim audit
- stale audit warning after edits

Milestone 5: Ollama provider

- provider config
- status endpoint
- diagnostics endpoint
- JSON validation
- one repair attempt
- minimal `AiRun` tracking

Milestone 6: Application history and export

- saved sessions
- filters
- application status metadata
- copy/download txt
- DOCX export with profile contact info

Milestone 7: Polish

- setup warnings
- plain errors with expandable details
- desktop UI tightening

**Testing Strategy**
Test rules, not every screen.

Backend unit tests:

- profile fact validation
- application minimum requirements
- fake provider keyword matching
- evidence matching behavior
- unmatched requirement classification
- custom fact normalization rules
- draft generation input restrictions
- claim audit statuses
- stale audit state after manual edit
- AI output validation
- invalid JSON repair attempt
- provider unavailable behavior
- `AiRun` minimal failure tracking

Frontend testing can stay light in v1. Prioritize backend tests for the trust and workflow rules.