# Ollama Draft Generation and Claim Audit JSON Repair

Status: done
Type: AFK

## Parent

docs/planning/milestone-5-prd.md

## What to build

Add Ollama support for the final two trust workflow operations: draft generation and claim audit. When Ollama is configured, the user should be able to generate one latest-state cover letter and one short motivation text from approved evidence, then manually run claim audit against the current draft.

Both operations should use backend-owned prompts, strict JSON response contracts, structural validation, one repair attempt, unchanged workflow state on unrepaired invalid output, and minimal `AiRun` tracking. Draft generation must stay grounded in approved evidence and honest unmatched requirement context. Claim audit must classify claims as supported, unsupported, or needs-review and reference approved evidence where possible.

This slice should not add export, streaming generation, draft history, provider settings editing, or custom fact normalization.

## Acceptance criteria

- [x] Ollama draft generation is available through the existing draft generation workflow action when Ollama is configured.
- [x] The draft generation prompt is stored as a backend markdown file.
- [x] The Ollama draft generation request asks for strict JSON containing cover letter text and short motivation text.
- [x] Valid Ollama draft generation persists one latest-state `GeneratedDraft`.
- [x] Draft generation remains blocked with a plain validation error when there is no job posting text.
- [x] Draft generation remains blocked with a plain validation error when there is no approved evidence.
- [x] Draft generation output validation rejects missing or empty draft text.
- [x] Ollama claim audit is available through the existing claim audit workflow action when Ollama is configured.
- [x] The claim audit prompt is stored as a backend markdown file.
- [x] The Ollama claim audit request asks for strict JSON containing supported, unsupported, and needs-review claim results.
- [x] Valid Ollama claim audit stores structured audit results on the current generated draft.
- [x] Claim audit remains blocked with a plain validation error when no generated draft exists.
- [x] Claim audit output validation rejects unknown claim statuses and unknown evidence references.
- [x] Malformed JSON receives exactly one repair attempt for both operations.
- [x] Structurally invalid JSON receives exactly one repair attempt for both operations.
- [x] Existing draft and audit state are not mutated when output remains invalid after repair.
- [x] `AiRun` records success, repaired success, unavailable provider failure, and invalid output failure for both operations.
- [x] Backend tests cover valid generation, valid audit, unavailable provider, repair behavior, unchanged state on failure, and `AiRun` records.
- [x] No export, streaming generation, draft history, provider settings editing, or custom fact normalization is introduced.

## Blocked by

- docs/planning/issues/19-ollama-evidence-matching-json-repair-and-airun.md
