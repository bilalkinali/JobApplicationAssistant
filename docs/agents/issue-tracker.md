# Issue Tracker: Local Markdown

Issues, PRDs, and specifications for this repo live as markdown files under versioned folders in `docs/planning`.

## Conventions

- V1 planning lives in `docs/planning/v1`.
- V1 implementation issues live in `docs/planning/v1/issues`.
- V2 planning lives in `docs/planning/v2`.
- Future V2 implementation issues should live in `docs/planning/v2/issues` when they are created.
- Triage state is recorded as a `Status:` line near the top of each issue file.
- Comments and conversation history append to the bottom of the file under a `## Comments` heading.

## When a Skill Says "Publish to the Issue Tracker"

Create a new markdown file under the relevant version folder, or under that version's `issues` folder for implementation issues.

## When a Skill Says "Fetch the Relevant Ticket"

Read the file at the referenced path. The user will normally pass the path directly.
