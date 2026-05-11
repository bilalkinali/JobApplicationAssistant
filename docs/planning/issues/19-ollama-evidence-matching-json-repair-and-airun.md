# Ollama Evidence Matching JSON Repair and AiRun Tracking

Status: ready-for-agent
Type: AFK

## Parent

docs/planning/milestone-5-prd.md

## What to build

Add Ollama evidence matching behind the existing evidence matching workflow action. When Ollama is configured, the user should be able to match analyzed job signals against approved profile facts, see matched evidence and unmatched requirements, and then continue using the existing evidence review flow.

The operation should use a backend-owned prompt, require strict JSON, validate that matches only reference approved facts and existing job signals, attempt one repair pass for malformed or structurally invalid output, avoid mutating workflow state on unrepaired invalid output, and create minimal `AiRun` records.

This slice should not add Ollama draft generation, claim audit, export, custom fact normalization, or provider settings editing.

## Acceptance criteria

- [ ] Ollama evidence matching is available through the existing evidence matching workflow action when Ollama is configured.
- [ ] The evidence matching prompt is stored as a backend markdown file.
- [ ] The Ollama request asks for strict JSON matching the evidence matching contract.
- [ ] Matching uses only approved profile facts as possible evidence.
- [ ] Valid Ollama matching stores evidence matches and unmatched requirements on the application session.
- [ ] Match output validation rejects references to draft or archived profile facts.
- [ ] Match output validation rejects references to unknown profile facts.
- [ ] Match output validation rejects references to unknown job signals.
- [ ] Evidence matching remains blocked with a plain validation error when no approved profile facts exist.
- [ ] Malformed JSON receives exactly one repair attempt.
- [ ] Structurally invalid JSON receives exactly one repair attempt.
- [ ] Application workflow state is not mutated when output remains invalid after repair.
- [ ] `AiRun` records success, repaired success, unavailable provider failure, and invalid output failure.
- [ ] Backend tests cover valid matching, invalid evidence references, unavailable provider, repair behavior, unchanged state on failure, and `AiRun` records.
- [ ] No Ollama draft generation, claim audit, export, custom fact normalization, or provider settings editing is introduced.

## Blocked by

- docs/planning/issues/18-ollama-job-analysis-json-repair-and-airun.md
