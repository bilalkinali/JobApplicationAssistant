# Collapse Completed Preparation Context

Status: done
Type: AFK

## Parent

docs/testing/manual-QA-result/latest-application-ui-handoff.md

## What to build

Collapse completed workflow context by default once an application has a generated draft. Job analysis, candidate fit brief, evidence matching, gap decisions, application strategy, and claims-to-avoid details should be grouped behind compact disclosure sections so the user can scan or reopen them without scrolling through every historical detail.

The slice should make the review page calmer without removing information that is still needed for trust, debugging, or manual review.

## Acceptance criteria

- [x] Prepared applications with a generated draft default completed job analysis, fit brief, evidence, gap decision, and strategy sections to collapsed or compact summaries.
- [x] The page exposes clear `Preparation details`, `Evidence details`, and `Strategy details` disclosure areas or equivalent grouped controls.
- [x] Each collapsed area includes enough summary information for the user to decide whether to expand it.
- [x] Claims to avoid remain available but are summarized or grouped until expanded.
- [x] The default state avoids long repeated editable cards before the draft review area.
- [x] Applications that still need preparation or evidence review keep the relevant active section expanded.

## Blocked by

- docs/planning/v2/issues/42-prepared-application-final-review-first-layout.md
