# Gene Forge — Collaborative Session Examples

This directory contains realistic examples of agent interaction patterns for Gene Forge development. Each example demonstrates how to collaborate effectively with AI agents during design, implementation, and strategic decision sessions.

---

## Example Index

| File | Session Type | System | Key Pattern |
|---|---|---|---|
| `session-design-crafting-system.md` | Design | DNA Recombination | Options-first design dialogue |
| `session-implement-combat-damage.md` | Implementation | Combat Damage Formula | Spec-gap identification before coding |
| `session-scope-crisis-decision.md` | Strategic | Demo Scope Cut | Decision framing with ADR output |
| `reverse-document-workflow-example.md` | Reverse Doc | Gene Expression System | Code-to-GDD reconstruction |

---

## Universal Collaborative Pattern

All examples follow this four-step workflow:

### 1. Understand Before Acting
Agents read the relevant GDD and existing code before proposing anything. For Gene Forge this means checking `design/gdd/` for the system spec and `Assets/Scripts/` for current implementation state. Agents ask clarifying questions before presenting options.

### 2. Present Options with Reasoning
Two to four approaches are offered with explicit pros/cons and alignment to Gene Forge's four design pillars:
- **Genetic Architect** — does this support creature customization through DNA alteration?
- **Tactical Grid Mastery** — does this reward positioning, terrain, and type synergy?
- **Discovery Through Play** — does the player learn by doing, not by reading menus?
- **Living World** — does this make the world feel reactive and alive?

### 3. Iterate on Drafts
Work is shown incrementally. Edge cases are flagged before they reach implementation (e.g. "what happens when a creature has two conflicting dominant genes?"). The GDD section is approved before moving to the next.

### 4. Approval and Completion
Agents request explicit permission before writing files. No file is created until the user confirms the draft is correct. This boundary prevents GDD drift from implementation reality.

---

## Core Principles

**Agents function as consultants, not autonomous developers.**

- Users maintain creative control over creature design, DNA rules, type chart values, and world lore
- Agents present expert guidance and flag risks but do not make final calls
- Agents avoid autonomous execution on anything that touches game balance or creature identity
- Feedback is incorporated without defensiveness — the design improves through dialogue

---

## What These Examples Are Not

These are not tutorials on how to use Unity or write C#. They are examples of *how to have a productive conversation* with an AI agent during game development. The patterns apply whether you are designing a new DNA trait, debugging a pathfinding edge case, or deciding whether to cut a feature before a playtest.

---

## Adding New Examples

When you have a session that demonstrates a useful pattern, save it here with the naming convention:

```
session-<verb>-<system-name>.md
```

Examples:
- `session-design-type-chart-expansion.md`
- `session-debug-dna-instability.md`
- `session-balance-dna-recombination-cost.md`
- `session-implement-grid-height-targeting.md`
