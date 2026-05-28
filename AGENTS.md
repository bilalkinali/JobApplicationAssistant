# Agent Instructions

Keep progress updates minimal. Do not narrate every file read or command. Only report blockers, assumptions, or final result.

When implementation is requested, make the requested code changes only and stop.

After completing a task, include a short "Vault note" section when the work contains durable project knowledge.

The Vault note should summarize:

- what changed
- why it changed
- important decisions
- failed attempts or issues encountered
- tests or manual QA performed
- follow-up questions

Do not create project-memory files in this repository unless explicitly asked.

## Project Memory Handoff

The application repository remains the product/source-code workspace.

The Obsidian vault is located at `C:\Users\Bilal Kinali\Documents\ObsidianVaults\JobApplicationAssistantVault`.

The application agent may write project-memory handoff notes only under `C:\Users\Bilal Kinali\Documents\ObsidianVaults\JobApplicationAssistantVault\raw\inbox\`.

The application agent must not edit vault wiki files, `index.md`, `log.md`, `AGENTS.md`, or existing raw files unless explicitly instructed.

The application agent must not write memory notes for every trivial code change.

Create a memory handoff note only when the task contains durable project knowledge.

Create a handoff note when the work includes:

- architectural or workflow decisions
- changed project direction
- failed attempts or important debugging findings
- manual QA findings
- prompt/AI behavior issues
- important implementation trade-offs
- unresolved follow-up questions
- behavior changes that future Codex runs should know

Do not create a handoff note for:

- tiny styling fixes
- mechanical renames
- simple dependency updates
- formatting-only changes
- changes with no durable reasoning value

When creating a handoff note:

- Write a new Markdown file under `C:\Users\Bilal Kinali\Documents\ObsidianVaults\JobApplicationAssistantVault\raw\inbox\`
- Use filename format: `YYYY-MM-DD-short-topic.md`
- Keep it concise and factual
- Include these sections:

```markdown
# [Short title]

## Source
Application repo task.

## Summary
What changed.

## Why it matters
Why this should be remembered.

## Decisions
Only actual decisions. Use "None" if none.

## Failed attempts / issues
Only if relevant. Use "None" if none.

## Tests / QA
Commands run or manual checks performed.

## Follow-up
Open questions or next steps. Use "None" if none.
```

At the end of future implementation tasks, mention whether a vault handoff note was created.

## Agent skills

### Issue tracker

Issues and PRDs are tracked as local markdown files. See `docs/agents/issue-tracker.md`.

### Triage labels

This repo uses `ready-for-agent`, `in-progress`, `blocked`, and `done` as local workflow states. See `docs/agents/triage-labels.md`.

### Domain docs

Project planning and domain documentation live under `docs/planning`. See `docs/agents/domain.md`.
