# OpenAI-Compatible Evidence Matching and Prepare

Status: done
Type: AFK

## Parent

docs/planning/v2/lm-studio-openai-compatible-provider-prd.md

## What to build

Make evidence matching and the V2 prepare orchestration work through the `OpenAiCompatible` provider. The provider should use OpenAI-compatible chat completions for evidence matching, parse strict assistant JSON into the existing evidence match and unmatched requirement result shape, and let `POST /api/applications/{id}/prepare` run job analysis plus evidence matching through LM Studio rather than Fake AI.

The completed slice should prove the main preparation workflow can reach the evidence review checkpoint with the configured local real provider.

## Acceptance criteria

- [x] Evidence matching calls the configured OpenAI-compatible chat completions endpoint using the configured model.
- [x] The evidence matching prompt requests strict JSON output compatible with existing parsing and validation.
- [x] Valid assistant JSON is parsed into existing evidence match and unmatched requirement results.
- [x] Duplicate, conflicting, malformed, or incomplete evidence matching output is rejected by existing validation rules.
- [x] Existing repair-attempt behavior is preserved for invalid evidence matching JSON.
- [x] `POST /api/applications/{id}/prepare` uses `OpenAiCompatible` for both job analysis and evidence matching when configured.
- [x] A successful prepare call persists job signals, evidence matches, unmatched requirements, preparation status, and the evidence review checkpoint using the real provider path.
- [x] Failed evidence matching or prepare calls fail gracefully without persisting partial prepared state.
- [x] Raw payloads are stored or logged for failed evidence matching or prepare provider calls when `StoreRawPayloads` is enabled.
- [x] Backend tests prove evidence matching works with `OpenAiCompatible` and returns valid parsed JSON.
- [x] Backend tests prove prepare works with `OpenAiCompatible`, not Fake AI.
- [x] Backend tests cover invalid JSON, provider API error, timeout, unreachable endpoint, and partial-state preservation for this path.

## Blocked by

- docs/planning/v2/issues/14-openai-compatible-job-analysis-json-path.md
