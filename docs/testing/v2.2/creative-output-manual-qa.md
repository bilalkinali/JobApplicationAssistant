# V2.2 Creative Output Manual QA

Date created: 2026-05-25

Use this checklist to validate that V2.2 draft generation has improved the original product complaint: app-generated drafts should be richer, more specific, and closer to a direct-chat baseline while still staying auditable.

Run each scenario twice with the same model:

1. In the app, using the normal prepare, evidence review, draft generation, and claim audit flow.
2. In direct chat, using the same CV/profile source, job posting, and model, without the app's intermediate evidence and strategy constraints.

Record enough detail that another tester can reproduce the comparison.

## Run Record

- Date:
- Tester:
- Provider:
- Model:
- Provider endpoint:
- App backend URL:
- App frontend URL:
- Scenario:
- Input fixture or source:
- Application id:
- Company:
- Role:
- Direct-chat prompt/source used:
- App draft quality notes:
- Direct-chat baseline quality notes:
- Claim audit outcome:
- Final verdict:

## Scenario 1: Vejle Kommune AI Builder

Use the Vejle Kommune AI-builder job scenario that exposed the V2.2 complaint.

### Setup

- [ ] Use the same provider and model in the app and direct chat.
- [ ] Use the same CV/profile source in both runs.
- [ ] Use the same Vejle Kommune AI-builder job posting in both runs.
- [ ] Import or approve enough profile facts to cover technical, education, business, customer, communication, language, and location context when present in the source.
- [ ] Prepare the application in the app and review the candidate fit brief, evidence quality, gap decisions, application strategy, generated draft, short motivation, and claim audit.

### App Output Checklist

- [ ] Cover letter uses 2-3 coherent story angles instead of listing accepted skills.
- [ ] Draft uses broader background where supported: AI tooling, local model or AI workflow experience, integrations, education, business or customer experience, communication, technical architecture, language, and location.
- [ ] Vejle Kommune context is reflected specifically where the job posting supports it.
- [ ] Transferable competencies are phrased as relevant strengths, not inflated direct experience claims.
- [ ] Gap decisions are respected; learning-interest gaps use cautious wording and ignored gaps stay quiet.
- [ ] The same evidence item is not repeated as the main proof for unrelated claims.
- [ ] Phrases from the job posting are not copied mechanically or overused.
- [ ] Short motivation is a distinct concise pitch, not a compressed duplicate of the cover letter.
- [ ] Concrete claims can be traced to approved evidence or approved custom facts.
- [ ] Unsupported or over-broad claims are flagged by claim audit.

### Direct-Chat Baseline Comparison

- [ ] Baseline was generated with the same model and same CV/profile plus job source.
- [ ] App output is meaningfully closer to the baseline's richness than the original narrow V2.2 complaint output.
- [ ] App output preserves more auditability than the direct-chat baseline by avoiding unsupported concrete claims.
- [ ] Any area where direct chat is still clearly stronger is recorded with examples.
- [ ] Any area where the app is safer but less persuasive is recorded with the likely cause.

## Scenario 2: Milestone 2 Backend Platform Regression

Use the Milestone 2 real-provider QA scenario as a regression comparison. The known source is documented in `docs/planning/v2/milestone-2-real-provider-qa-handoff.md`.

### Setup

- [ ] Use provider `OpenAiCompatible` or the current real provider under test.
- [ ] Use the same model for app and direct chat; the Milestone 2 handoff used `qwen2.5-coder-14b-instruct`.
- [ ] Use the Backend Platform Engineer application source from the Milestone 2 QA handoff or an equivalent saved fixture if one has been promoted.
- [ ] Include the preferred Kubernetes and cloud-native deployment gaps from the Milestone 2 regression source when applicable.
- [ ] Prepare the application, approve non-weak evidence, save gap decisions, generate the draft, and run claim audit.

### Regression Checklist

- [ ] App still completes prepare, evidence review, draft generation, and claim audit with the real provider.
- [ ] Application strategy has 2-3 primary angles and visible claims to avoid when gaps or cautious evidence exist.
- [ ] Backend-platform draft remains technically specific without claiming unsupported Kubernetes or cloud-native deployment experience.
- [ ] Learning-interest wording is cautious when Kubernetes or cloud-native deployment are gaps.
- [ ] Evidence is not stretched from one broad profile fact across unrelated backend-platform requirements.
- [ ] Short motivation is distinct from the cover letter and appropriate for a backend-platform role.
- [ ] Claim audit remains current after generation and flags unsupported concrete claims if introduced.
- [ ] Direct-chat baseline is compared for specificity, structure, persuasive quality, and audit risk.

## Quality Rating

Use these ratings for both scenarios.

- Richness: `poor`, `acceptable`, `strong`
- Specificity to role: `poor`, `acceptable`, `strong`
- Broader background usage: `missing`, `partial`, `strong`
- Repetition level: `high`, `moderate`, `low`
- Phrase copying risk: `high`, `moderate`, `low`
- Short motivation distinctness: `duplicate`, `partly distinct`, `distinct`
- Auditability: `unsafe`, `mostly safe`, `safe`
- Direct-chat comparison: `worse`, `closer but weaker`, `comparable`, `better`

## Pass Criteria

The run passes when:

- [ ] Both scenarios produce app drafts rated at least `acceptable` for richness and specificity.
- [ ] The Vejle Kommune draft uses at least four relevant broader-background categories where supported by the source.
- [ ] The Milestone 2 backend-platform draft does not regress the real-provider flow or unsupported-gap behavior.
- [ ] Repeated evidence and phrase copying are no worse than `moderate`.
- [ ] Short motivation is `distinct` or `partly distinct`.
- [ ] Claim audit is current and concrete claims are supported by approved evidence or approved custom facts.
- [ ] Direct-chat comparison shows the app is closer to baseline richness while retaining stronger audit boundaries.
