# Profile Readiness and Setup Warnings

Status: done
Type: AFK

## Parent

docs/planning/milestone-2-prd.md

## What to build

Add lightweight readiness indicators that help the user understand whether the app has enough manual profile evidence for later generation work. The app should warn when contact information or approved profile facts are missing, without blocking unrelated manual CRUD work.

This slice should not implement generation blocking or AI provider checks.

## Acceptance criteria

- [x] The backend or frontend can determine whether the profile has required contact fields.
- [x] The backend or frontend can determine whether at least one approved profile fact exists.
- [x] The Home workbench shows profile readiness in a concise way.
- [x] The Profile area warns when there are no approved profile facts.
- [x] Warnings do not block editing profile information, editing profile facts, or editing applications.
- [x] Warning language stays plain and actionable.

## Blocked by

- docs/planning/issues/04-profile-fact-crud.md
