# OpenAI-Compatible Draft Generation and Claim Audit

Status: done
Type: AFK

## Parent

docs/planning/v2/lm-studio-openai-compatible-provider-prd.md

## What to build

Make draft generation and claim audit work through the `OpenAiCompatible` provider after evidence review is ready. The provider should use OpenAI-compatible chat completions for draft generation and claim audit, parse strict assistant JSON into the existing result shapes, and preserve the V2 trust boundary around approved evidence, gap decisions, audit status, and stable draft state.

The completed slice should prove that the real local provider can generate cover letter text, short motivation text, and current audit feedback without relying on Fake AI.

## Acceptance criteria

- [x] Draft generation calls the configured OpenAI-compatible chat completions endpoint using the configured model.
- [x] Claim audit calls the configured OpenAI-compatible chat completions endpoint using the configured model.
- [x] Draft generation and claim audit prompts request strict JSON output compatible with existing parsing and validation.
- [x] Valid draft generation JSON persists the generated cover letter and short motivation as the current draft.
- [x] Valid claim audit JSON persists current audit state against the generated draft.
- [x] Existing validation rejects malformed drafts, empty draft fields, malformed audit JSON, unsupported audit statuses, and audit evidence references that are not allowed.
- [x] Existing repair-attempt behavior is preserved for invalid draft generation and claim audit JSON.
- [x] Generation or audit failures preserve the latest stable draft and workflow state according to existing V2 rules.
- [x] Raw payloads are stored or logged for failed generation and audit calls when `StoreRawPayloads` is enabled.
- [x] Backend tests prove draft generation works with `OpenAiCompatible` after evidence review is ready.
- [x] Backend tests prove claim audit works with `OpenAiCompatible` and persists current audit state.
- [x] Backend tests cover invalid JSON, structurally invalid JSON, model/API errors, timeout, unreachable endpoint, and stable draft preservation for generation and audit.

## Blocked by

- docs/planning/v2/issues/15-openai-compatible-evidence-matching-and-prepare.md
