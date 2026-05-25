# Strategy-Led Creative Draft Generation

Status: done
Type: AFK

## Parent

docs/planning/v2/v2.2-milestone-3-prd.md

## What to build

Revise draft generation so the final cover letter is led by the persisted application strategy and carefully scoped candidate fit brief context. The draft should choose 2-3 coherent story angles, write in the selected language with natural prose, and use approved evidence and approved job-local custom facts as the proof context for concrete claims.

The slice should preserve the existing guided generate-and-audit action and export behavior while making the generated cover letter less mechanical and less checklist-like.

## Acceptance criteria

- [x] Draft generation input includes the latest application strategy, approved evidence, approved custom facts, unmatched requirements, gap decisions, and selected candidate fit brief narrative context.
- [x] Draft generation does not receive the raw full approved profile fact set unless a documented edge case requires it.
- [x] The draft prompt tells the provider to follow 2-3 strategy-led story angles instead of covering every requirement.
- [x] Candidate fit brief content is framed as writing context, not standalone proof for concrete claims.
- [x] Approved evidence and approved custom facts remain the explicit proof context for concrete experience claims.
- [x] Tests cover strategy and selected fit brief context reaching draft generation without the raw full approved profile fact set.

## Blocked by

None - can start immediately
