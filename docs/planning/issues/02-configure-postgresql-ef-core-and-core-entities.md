# Configure PostgreSQL, EF Core, and Core Entities

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/v1-prd.md

## What to build

Add persistence for the V1 app using PostgreSQL and Entity Framework Core. Model the core relational entities from the specification so later slices can build profile, application, AI run, and generated draft workflows on a stable database foundation.

This slice should define the entity model and database context for Profile, ProfileFact, JobApplication, GeneratedDraft, and AiRun. Flexible AI and workflow artifacts should be represented in a way that maps to PostgreSQL JSONB. Profile facts should support Draft, Approved, and Archived statuses. Job-local custom facts should be represented on the application as latest-state JSON.

## Acceptance criteria

- [ ] The backend is configured to use PostgreSQL through Entity Framework Core.
- [ ] A database context is available to application code.
- [ ] Profile is modeled with contact information, language/tone preferences, and timestamps.
- [ ] ProfileFact is modeled with evidence fields, JSON-backed fact data, Draft/Approved/Archived status, and timestamps.
- [ ] JobApplication is modeled with posting metadata, selected/detected language, workflow JSON fields, custom facts JSON, and timestamps.
- [ ] GeneratedDraft is modeled separately from JobApplication with draft text, claim audit JSON, generated/edit/audit timestamps, and timestamps.
- [ ] AiRun is modeled with step, provider, model, status, error details, attempt count, timing, and minimal input/output summaries.
- [ ] JSON-backed workflow artifacts are configured for PostgreSQL JSONB where appropriate.
- [ ] Latest-state storage is used; no draft history or profile fact revision history is added.

## Blocked by

- docs/planning/issues/01-project-foundation-app-shells.md
