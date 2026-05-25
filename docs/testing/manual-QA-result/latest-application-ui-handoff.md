# Latest application UI handoff

Manual QA target: running app at `http://localhost:5173/`

Application reviewed: latest `Vejle Kommune` / `AI-builder`, updated 26 May.

## Summary

The latest application detail page is too cluttered for the user's current task. The page exposes the full preparation and evidence workflow before the final draft problem, even though the application is already prepared and the immediate blocker is draft quality. The current task should be obvious: fix or regenerate the Danish draft before export.

## Findings

### Current action is contradictory

- The workflow header says `Next action: Copy or export`.
- The status badges also show `Export blocked`.
- The export panel later correctly says draft quality issues must be resolved before exporting.

Expected correction:

- When draft quality is `NeedsRevision`, the primary next action should be to revise/regenerate the draft, not copy/export.
- The top workflow summary should point directly to the draft quality section.

### Final review appears too late

- The draft quality block, cover letter editor, export block, and claim audit are near the bottom of a long page.
- The user must scroll past application metadata, provider status, job analysis, fit brief, evidence matching, gap decisions, custom facts, and application strategy before reaching the actual blocker.

Expected correction:

- For an application with an existing draft, show a compact final-review section near the top.
- Put draft quality, regenerate/save actions, export state, and claim audit summary before completed preparation details.

### Completed workflow sections are over-expanded

- Job analysis, candidate fit brief, evidence matching, gap decisions, application strategy, and claims to avoid are all expanded by default.
- Much of this is historical context after a draft already exists.

Expected correction:

- Collapse completed sections by default.
- Keep one current-action section expanded.
- Offer `Preparation details`, `Evidence details`, and `Strategy details` as disclosure sections.

### Evidence gap approval has no visible feedback

- In unmatched requirements, clicking `Approve` on matched evidence adds the item to the approved list at the bottom.
- The button does not disable.
- The card does not show an approved state.
- There is no toast, inline confirmation, count update near the clicked card, or scroll/focus movement.
- The user only knows something happened by scrolling to the bottom and discovering it in the approved evidence list.

Expected correction:

- After approval, show immediate local feedback on the clicked card, such as `Approved`, disabled button, or `Remove from approved`.
- Update the nearby approved evidence count immediately.
- Avoid requiring the user to scroll to the bottom to confirm the action.

### Unmatched requirement cards are too noisy

- Every gap shows decision buttons and full custom-fact fields.
- Repeated fields include custom fact title, summary, technologies, allowed claims, and `Add job-local fact`.
- This repeats for many preferred skills and responsibilities, creating a long form-heavy page.

Expected correction:

- Hide custom fact fields until the user chooses a `Covered by custom fact` or `Add custom fact` action.
- For saved learning-interest gaps, show compact summary cards instead of the full editor.
- Consider a table/list style for gaps with one expanded item at a time.

### Saved gap decision list is better than editable gap cards

- The compact saved gap list is much easier to scan than the editable unmatched requirements cards.
- It communicates the review decision without exposing unnecessary controls.

Expected correction:

- After evidence review is saved, default to the compact saved gap list.
- Keep the editable gap cards behind an `Edit evidence review` action.

### Application strategy is verbose and repetitive

- The application strategy section appears before the draft quality section.
- `Gap guidance` repeats `Nævn som et læringsejendom.` many times.
- `Claims to avoid` is long and visually competes with the current draft.

Expected correction:

- Collapse strategy by default once a draft exists.
- Deduplicate repeated gap guidance.
- Fix the Danish wording in strategy guidance.
- Keep `Claims to avoid` available, but summarize it by category unless expanded.

### Claim audit can create false confidence

- Claim audit shows `4 supported, 0 unsupported, 2 needs review`.
- This looks reassuring even though draft quality correctly says `NeedsRevision` and the Danish is awkward.

Expected correction:

- When draft quality is `NeedsRevision`, visually subordinate claim audit success.
- Show a clear message that claim support does not mean the draft is ready to send.

### Scroll behavior feels awkward

- Long textareas and columns can trap attention and make top-to-bottom navigation feel uneven.
- The page has multiple long vertical regions with forms and repeated cards.

Expected correction:

- Reduce textarea height for already-saved job posting text, or collapse it into a preview.
- Avoid long repeated editable cards in the default review state.
- Keep the current task visible without deep scrolling.

## Suggested target layout

For a prepared application with a generated draft:

1. Application summary and status
2. Current blocker / next action
3. Draft quality and regenerate/save controls
4. Cover letter and short motivation editor
5. Export state
6. Claim audit summary
7. Collapsed preparation/evidence/strategy details

## Acceptance notes

- A user should understand within the first viewport why export is blocked.
- A user should not need to scroll through evidence matching to fix a generated draft.
- Approving evidence should produce immediate visible feedback at the clicked location.
- Completed workflow context should remain accessible, but not dominate the default page.
