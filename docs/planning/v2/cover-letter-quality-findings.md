# Cover Letter Quality Findings

## Context

The generated Danish cover letter for the Vejle Kommune AI-builder role is noticeably better than earlier drafts, but it is still not good enough to send without substantial manual editing.

The core product goal is not just to produce a syntactically valid cover letter. It should produce a credible, restrained, role-specific application that a candidate would actually consider using.

## Main Findings

### Tone is too eager

The draft repeats high-enthusiasm phrasing:

- "med stor entusiasme"
- "Jeg er meget entusiastisk"

For Danish job applications, this reads exaggerated and generic. The tone should be professional, motivated, and concrete rather than excited.

Preferred direction:

- Use calm motivation.
- Avoid repeated enthusiasm language.
- Show interest through fit, evidence, and understanding of the role.

Example direction:

> Jeg søger stillingen som AI-builder, fordi den kombinerer praktisk softwareudvikling, eksperimentering med AI-løsninger og arbejdet med at gøre nye teknologier anvendelige i en organisation.

### Danish language quality is uneven

Several phrases are awkward or incorrect:

- "skalerbart selskab"
- "ulige krav"
- "dette baggrund"
- "LM-løsninger"
- "Jeg er meget entusiastisk om muligheden"

This may be partly model-related, but the application should still guard against obviously unnatural Danish.

### The letter is too generic

The draft often states broad qualifications without making a strong role-specific argument. It repeatedly says the candidate is interested in new technologies, but does not consistently connect that interest to Vejle Kommune's concrete needs.

Weak pattern:

- Candidate has skill.
- Skill is generally useful.
- Therefore candidate fits role.

Better pattern:

- Role has a concrete need.
- Candidate has specific evidence related to that need.
- Candidate can contribute in a precise way.

### Evidence selection is too permissive

The letter includes points that do not clearly strengthen the application. For example, fluency in several languages is framed as evidence of understanding new technologies, which feels logically weak.

The generator should choose the strongest 3-4 arguments and leave weaker profile facts out unless they directly support the role.

### The structure repeats itself

The draft has multiple paragraphs that restate:

- interest in new technologies
- willingness to learn
- ability to contribute
- background in development

This creates length without increasing persuasiveness.

### Some claims are overconfident or unsupported

Phrases like "jeg er sikker på, at jeg kan bidrage værdifuldt" can sound inflated unless backed by concrete evidence. The tone should avoid certainty where a grounded, professional statement would be stronger.

## Product Improvement Direction

### Test a stronger model as a benchmark

Run the same input through a stronger model once before making prompt changes. This will help separate model-quality limitations from product/prompt limitations.

If a stronger model produces a clearly usable Danish letter, model quality is a significant bottleneck. If it still produces generic or overexcited text, the generation workflow needs stronger constraints.

### Tighten the draft generation contract

The draft prompt should explicitly require:

- Danish professional tone.
- No exaggerated enthusiasm phrases.
- No repeated motivation statements.
- No unsupported claims.
- No weak logical bridges.
- No paragraph unless it connects candidate evidence to a job requirement.
- Prefer concrete examples over personality statements.
- Use only the strongest evidence.

Potential banned or discouraged phrases:

- "med stor entusiasme"
- "meget entusiastisk"
- "passioneret"
- "jeg er sikker på"
- generic "nye teknologier" repetition unless tied to the role

### Add a critique and rewrite pass

The app may need a second quality pass after draft generation.

Suggested reviewer instruction:

> Review this as a Danish hiring manager. Remove exaggerated enthusiasm, repeated points, weak reasoning, awkward Danish, and unsupported claims. Rewrite it into a concise, credible application letter that connects the candidate's strongest evidence to the role.

This pass should optimize for sendability, not creativity.

### Improve evidence ranking

The generation workflow should rank evidence by contribution to the target role before drafting.

Useful ranking questions:

- Does this fact directly match a job requirement?
- Does it prove technical ability, domain fit, collaboration ability, or learning ability?
- Is the link obvious to a hiring manager?
- Would removing this fact make the application weaker?

Facts with weak answers should be omitted from the cover letter, even if they remain useful elsewhere in the application workspace.

## Quality Bar

A generated cover letter should be considered acceptable only if:

- It sounds natural in Danish.
- It avoids exaggerated enthusiasm.
- It has a clear role-specific argument.
- Every paragraph earns its place.
- Candidate claims are grounded in approved evidence.
- The letter can plausibly be sent after light editing, not complete rewriting.

