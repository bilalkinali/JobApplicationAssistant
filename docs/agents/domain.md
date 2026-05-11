# Domain Docs

How engineering skills should consume this repo's planning and domain documentation.

## Before Exploring, Read These

- `docs/planning/v2/v2-specification.md` for the current V2 direction.
- `docs/planning/v1/v1-specification.md` for the completed V1 specification.
- `docs/planning/v1/v1-prd.md` for the completed V1 PRD, when relevant.
- Other files under `docs/planning/v1` or `docs/planning/v2` that touch the area being discussed.

If any of these files do not exist, proceed silently.

## Layout

This repo uses `docs/planning` as the planning and domain documentation location.

```text
docs/
  planning/
    v1/
      v1-specification.md
      v1-prd.md
      issues/
    v2/
      v2-specification.md
```

## Vocabulary

When output names a domain concept, prefer the terms already used in the relevant version folder under `docs/planning`. If a concept is unclear or overloaded, call it out and ask for clarification before encoding it into issues or implementation plans.
