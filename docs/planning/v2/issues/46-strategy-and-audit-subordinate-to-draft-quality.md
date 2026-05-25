# Strategy And Audit Subordinate To Draft Quality

Status: done
Type: AFK

## Parent

docs/testing/manual-QA-result/latest-application-ui-handoff.md

## What to build

Polish strategy and claim audit presentation so they support draft review instead of competing with it. Once a draft exists, application strategy should be collapsed or summarized by default, repeated gap guidance should be deduplicated, Danish strategy wording should be corrected, and claim audit success counts should not imply the draft is ready when draft quality still needs revision.

The slice should keep audit and strategy transparent while making draft readiness the source of truth for whether export can proceed.

## Acceptance criteria

- [x] Application strategy is collapsed or compactly summarized by default once a generated draft exists.
- [x] Repeated gap guidance entries are deduplicated in the strategy display.
- [x] Danish strategy guidance uses correct wording for learning-interest gaps.
- [x] When draft quality is `NeedsRevision`, claim audit success counts are visually subordinate to the draft quality blocker.
- [x] Claim audit includes clear copy that supported claims do not mean the draft is ready to send when draft quality needs revision.
- [x] Export-blocked messaging remains consistent between the workflow summary, draft quality section, claim audit summary, and export panel.

## Blocked by

- docs/planning/v2/issues/42-prepared-application-final-review-first-layout.md
