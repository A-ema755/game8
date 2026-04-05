---
name: gdd-audit
description: "Multi-role red-team audit of a single GDD. Five reviewers (creative-director, systems-designer, qa-lead, technical-director, plus one domain swap role) surface faults, gaps, contradictions, and pillar drift, then issue a verdict and name the follow-up skill. Run this after /design-review passes, before handing the GDD to programmers."
argument-hint: "[path-to-gdd] [focus: full|balance|testability|pillar-fit] [swap-role]"
user-invocable: true
allowed-tools: Read, Glob, Grep
---

When this skill is invoked:

1. **Parse the arguments**:
   - First argument: path to the target GDD (required). If missing or
     ambiguous, glob `design/gdd/` and ask the user to pick.
   - Second argument: focus — `full` (default), `balance`, `testability`,
     or `pillar-fit`.
   - Third argument: swap reviewer role. If omitted, infer from the GDD's
     domain and confirm with the user before proceeding.

2. **Read the master CLAUDE.md** to understand project context, pillars,
   and standards.

3. **Read the target GDD** in full.

4. **Read the Design Document Standard**:
   - `.claude/rules/design-docs.md` (the 8 required sections, formula
     format, edge case rule)
   - `.claude/docs/templates/game-design-document.md` (canonical GDD shape)

5. **Read each reviewer's agent file** from `.claude/agents/` before that
   reviewer critiques. Each reviewer must stay inside their domain.

6. **Read related design documents** this GDD touches (check `design/gdd/`
   for sister systems referenced or implied).

---

## Pass 1 — Mechanical Conformance

Diff the target GDD against the rule and template. Report only:
- Missing required sections
- Empty or placeholder sections
- Missing edge-case coverage
- Formulas not in standard format

If the document is clean, state so in one line and move on.

## Pass 2 — Role Critique

Each reviewer outputs 2–4 faults from their angle only. No overlap between
reviewers. No praise. Each fault must:
- Cite a specific GDD section, claim, or omission
- Name what it conflicts with (pillar / rule / sister-GDD / coding standard)
- Propose a fix direction, not a full fix
- Have concrete doc evidence — no taste-based faults

**Fixed reviewers (always fire):**

1. **creative-director** — Pillar fit against the game's pillars (read
   `design/gdd/game-pillars.md`). Any anti-pillar triggered? Does this
   belong in this game?

2. **systems-designer** — Internal coherence, unstated assumptions, and
   sister-GDD interfaces. Name every other system this touches and flag
   where the interfaces disagree.

3. **qa-lead** — Testability as written, missing acceptance criteria,
   ambiguity that will breed bugs, regression risk for adjacent systems.

4. **technical-director** — Buildability under the project's engine and
   `.claude/docs/coding-standards.md`. Is an ADR required?

**Swap reviewer (pick one, matched to GDD domain):**

| GDD domain | Swap role |
|---|---|
| Progression / economy / balance | `economy-designer` |
| UI / HUD / player feedback | `ux-designer` |
| AI / enemy / creature behavior | `ai-programmer` |
| Combat moment-to-moment | `gameplay-programmer` |
| Story / world / campaign | `narrative-director` |
| Art / VFX / shader / procedural visuals | `technical-artist` |
| Audio / sonic identity | `sound-designer` |
| Save / network / persistence | `network-programmer` |
| Accessibility | `accessibility-specialist` |
| Live-ops / events | `live-ops-designer` |

## Pass 3 — Synthesis

- **Load-bearing gaps**: faults raised by 2+ reviewers independently.
  These are the real design problems.
- **What passed**: one line per area reviewers did not flag.

7. **Output the review** in this format:
```
## GDD Audit: [Document Title]
Focus: [focus] | Swap reviewer: [role]

### Pass 1 — Mechanical Conformance
[findings or "Clean."]

### Pass 2 — Role Critique

#### creative-director
- **Fault**: [...]
  - Conflicts with: [...]
  - Fix direction: [...]

#### systems-designer
[...]

#### qa-lead
[...]

#### technical-director
[...]

#### [swap-role]
[...]

### Pass 3 — Synthesis

**Load-bearing gaps:**
- [gap raised by 2+ reviewers]

**What passed:**
- [area 1]
- [area 2]

### Verdict: [APPROVED / NEEDS REVISION / MAJOR REVISION NEEDED]
```

8. **Contextual next step recommendations**:
   - If Pass 1 flagged structural issues: suggest `/design-review` first
     before re-running this audit.
   - If math, formulas, or economy was flagged: suggest `/balance-check`.
   - If architecture concerns were raised: suggest `/architecture-decision`
     to document the decision before implementation.
   - If implementation already exists: suggest `/code-review` on the
     relevant source files.
   - If verdict is NEEDS REVISION or MAJOR REVISION NEEDED: do NOT proceed
     to `/gate-check` — fix first, then re-audit.
   - If verdict is APPROVED: the GDD is ready for sign-off per
     `.claude/docs/review-workflow.md` (requires `game-designer` and
     `creative-director` approval).
   - If `design/gdd/systems-index.md` exists and references this GDD:
     suggest the user update the systems index status to match the verdict.

Note: This skill is read-only. It does not write the audit to disk.
If the user wants the audit saved, they will ask and confirm the filepath
per the collaboration protocol in CLAUDE.md.