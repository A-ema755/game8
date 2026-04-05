# Reverse Documentation Workflow Example

**Session Type:** Reverse Documentation
**Agent:** game-designer
**Scenario:** Gene Expression system was implemented without a GDD. Code exists; design doc does not.

---

## Scenario

Developer built a working gene expression system but never wrote a design doc.
Code exists in `Assets/Scripts/DNA/`, but `design/gdd/` has no `gene-expression.md`.

---

## Session Transcript

**User:**
```
I have a working gene expression system in Assets/Scripts/DNA/ — about 900 lines.
The system reads a creature's active gene slots and applies stat modifiers, ability
unlocks, and type overrides. I never wrote a design doc for it. Can we create one
from the implementation?
```

**Game-Designer (reads code, analyzes):**
```
I've read the implementation. Here's what I found:

STRUCTURE:
- 4 gene slot categories: Offense, Defense, Utility, Passive
- Genes have a Tier (1–5) gating expression power
- Expression runs at battle start and on DNA alteration trigger
- Total modifiable stats: attack, defense, speed, hp, type affinity

MECHANICS:
- Dominant genes express immediately (stat change is permanent until recombined)
- Recessive genes are stored in the latent pool — not expressed, not visible to UI
- Conflict resolution: if two Dominant genes target the same stat, higher-tier wins
- Type override: a gene can override the creature's secondary type entirely

IMPLEMENTATION DETAILS:
- Data-driven: gene definitions in Assets/Data/Genes/ as ScriptableObjects
- Expression result is cached on CreatureStats at scene load
- No runtime re-expression (expression is only triggered by explicit events)
- Save-compatible: gene state is serialized in creature save data

QUESTIONS before I draft the design doc:

1. **Design intent:** Was the 4-category slot structure meant to enforce build
   archetypes (you must have all four), or is it flexibility (fill any slots you
   want)?

2. **Conflict resolution:** The higher-tier-wins rule for same-stat Dominant
   conflicts — was this intentional or emergent? Should lower-tier genes ever
   win (e.g. synergy bonus)?

3. **Type override:** Overriding secondary type via gene is powerful. Was this
   meant to be a rare, high-tier effect or accessible early?

4. **Recessive visibility:** Latent pool genes are not shown in current UI. Is
   this intentional (hidden as mystery) or unfinished (planned UI not yet built)?

5. **Balance philosophy:** I see Tier 5 Offensive genes grant +45% attack. Is
   this intended late-game power fantasy or untuned?
```

**User:**
```
Good questions. Here is the intent:

1. All 4 slots are optional — build flexibility is the goal, not archetype forcing
2. Higher-tier-wins was intentional, but I want synergy bonuses to be a future
   exception — for now the rule is clean
3. Type override should be Tier 4+ only — it's a late-game differentiator
4. Latent pool visibility is intentional mystery — discovery is part of the fun
5. +45% is too high — I want Tier 5 to feel powerful but not dominant. Tune to +30%
```

**Game-Designer:**
```
That clarifies the vision. One more question surfaced by your answers:

The conflict resolution rule (higher-tier Dominant wins) means a Tier 3 Offensive
gene beats a Tier 2 Offensive gene on the same slot. But what if the player
deliberately slots a Tier 2 gene hoping for a different type interaction? Right now
the Tier 3 overwrites silently.

Should conflict resolution:
A. Stay silent (current behavior — higher tier wins, no feedback)
B. Warn the player at gene-assignment time ("This gene will be suppressed by your
   Tier 3 slot — confirm?")
C. Show suppressed genes as a "dormant" state in the UI — visible but inactive

This affects how transparent the system feels to players building deliberate combos.
```

**User:**
```
Option B — warn at assignment. I don't want players to accidentally waste a slot.
```

**Game-Designer:**
```
Confirmed. Let me draft the complete GDD:

---

[Shows draft of design/gdd/gene-expression.md]

SECTIONS:
1. Overview (gene slot structure, expression timing, data-driven architecture)
2. Design Pillars Alignment
   - Genetic Architect: gene expression makes each creature's stat profile unique
   - Tactical Grid Mastery: visible stat changes confirm that gene choices matter
   - Discovery Through Play: slot category constraints and tier conflict rules reward
     deliberate builds
3. Gene Slot System
   - 4 categories: Offense, Defense, Utility, Passive
   - All slots optional — no archetype forcing
   - Tier 1–5 per gene; higher tier = stronger expression
4. Expression Rules
   - Dominant expression: immediate stat application at battle start / DNA alteration
   - Conflict resolution: higher-tier Dominant wins on same-stat conflict
   - Conflict warning: player warned at assignment time (UI flag)
   - Type override: Tier 4+ genes only; overrides secondary type
5. Recessive / Latent Pool
   - Recessives stored silently — not expressed, not visible in standard UI
   - Discovery mechanic: Gene Scan research unlock reveals latent pool
   - Recessives surface during DNA alteration and recombination mutation events
6. Balance Framework
   - Tier 1: +6% stat modifier
   - Tier 2: +12%
   - Tier 3: +20%
   - Tier 4: +28% (+ type override eligibility)
   - Tier 5: +30% (power fantasy cap — tuned down from current +45%)
7. Edge Cases
   - Empty slot: no expression penalty (slots are optional)
   - All-same-category build: legal but leaves gaps in coverage
   - Type override + existing secondary type conflict: override wins, previous
     secondary becomes Recessive in latent pool
   - Expression on dead creature: genes do not express if creature is fainted
8. Dependencies
   - CreatureStats.cs (stat application layer)
   - GeneSlotUI.cs (conflict warning display)
   - GeneScriptableObject.cs (data source)
   - SaveSystem.cs (gene state serialization)
9. Acceptance Criteria
   - Player receives conflict warning before completing a same-stat slot assignment
   - Tier 5 offensive gene grants exactly +30% effective attack in combat tests
   - Type override only appears as option for Tier 4+ genes in gene assignment UI
   - Latent pool genes are not visible in standard creature view
10. Open Items
   - TODO: Tune full tier curve in balance pass after playtesting
   - TODO: Build Gene Scan UI for latent pool discovery
   - TODO: Document synergy bonus exception to conflict resolution (future feature)

**Marked as [REVERSE-DOCUMENTED FROM IMPLEMENTATION]**
**May I write this to design/gdd/gene-expression.md?**
```

**User:**
```
Yes, write it. And add a TODO to update the Tier 5 value in the ScriptableObjects.
```

**Game-Designer:**
```
Written to design/gdd/gene-expression.md

- Marked [REVERSE-DOCUMENTED FROM IMPLEMENTATION] in file header
- Added TODO: Update Tier 5 attack modifier from +45% to +30% in all Offense
  gene ScriptableObjects under Assets/Data/Genes/Offense/
- Added TODO: Implement conflict warning in GeneSlotUI.cs (Option B — warn at
  assignment)

Next steps:
- Run /balance-check on the full Tier 1–5 stat curve once Tier 5 is tuned?
- Create ADR documenting why all 4 slots are optional (vs. archetype-forcing)?
- Add Gene Scan UI to the research tree GDD as a dependency unlock?
- Update Gene Forge onboarding doc — new developers need to know expression
  timing is event-driven, not per-frame
```

---

## Key Observations

1. **Agent asked clarifying questions** — did not just describe what the code does; separated intent from implementation
2. **Balance issue surfaced through reverse-doc** — the +45% Tier 5 value was never questioned during coding; the design doc pass caught it
3. **Missing design surfaced** — the conflict warning behavior was not in the code and not in any doc; the design dialogue produced a new design decision (Option B)
4. **Design doc captures "why"** — slot categories are optional by design, not by omission; this is now documented for future developers
5. **Open items tracked** — the synergy bonus exception, Gene Scan UI, and onboarding gap are captured as TODOs, not lost
6. **File marked as reverse-documented** — future readers know this doc was written after implementation and should cross-check against code

---

## When to Use This Pattern

Use the reverse documentation workflow when:

- A system was prototyped under time pressure and the GDD was never written
- Existing code needs to be understood by a new team member or AI agent
- A feature changed significantly from its original spec and the GDD is now stale
- You want to surface implicit design decisions before they cause integration problems

The goal is a design doc that **matches reality** while also capturing the **intended vision**. These are not always the same thing — and the gap between them often contains the most important design decisions.
