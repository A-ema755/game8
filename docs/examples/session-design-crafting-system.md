# Example Session: Designing the DNA Recombination System

**Session Type:** Design
**Agent:** game-designer
**System:** DNA Recombination (Gene Forge core loop)
**GDD Reference:** `design/gdd/dna-recombination.md`

---

## Context

The player collects genetic material from wild creatures and uses it to engineer new traits onto their roster. This session walks through the collaborative design of how DNA recombination works mechanically — what choices the player makes, what risks exist, and how outcomes are determined.

---

## Session Transcript

**User:**
```
I need to design how DNA recombination works. The player should be able to combine
genes from two creatures to try to get a better outcome. Not sure if it should be
deterministic or have randomness. Help me think through it.
```

**Game-Designer:**
```
Before I propose options, a few clarifying questions:

1. **Discovery method:** Does the player see the outcome before committing, or is
   there an element of surprise? (Affects risk/reward feel significantly.)

2. **Failure states:** Can recombination produce a worse result — weaker stats,
   lost traits, corrupted genes? Or is failure simply "no improvement"?

3. **Progression gating:** Is recombination available from the start, or does it
   unlock through the gene research tree? Are higher-tier recombinations locked
   behind player rank?

4. **Reference feel:** Are you thinking more Pokémon breeding (planned outcomes),
   Darkest Dungeon afflictions (unexpected results), or Slay the Spire card upgrades
   (controlled improvement with variance)?

5. **Scope:** Is this an MVP mechanic or a deep system with many interaction
   layers?
```

**User:**
```
Good questions. I want surprise — the player shouldn't see the exact outcome.
Failure should be possible but rare. It unlocks early, but higher-tier genes need
research unlocks. Feel is closer to Slay the Spire — mostly controlled but with
variance that creates stories.
```

**Game-Designer:**
```
That gives me a clear direction. Here are three distinct design options aligned
with your stated feel and Gene Forge's pillars:

---

OPTION A: Probability Bands
- Each gene pair has known success/partial/failure bands (e.g. 70% / 20% / 10%)
- Player sees the bands before committing
- Outcome within each band is hidden (which specific trait emerges)
- Higher research tier narrows failure band, expands success band
- PROS: Transparent risk, player can make informed decisions
- CONS: Can feel mechanical/calculated, reduces surprise
- PILLAR ALIGNMENT: Strong Discovery Through Play (read the odds); weak Genetic Architect
  (traits feel statistical rather than emergent)

---

OPTION B: Dominant / Recessive Expression
- Every gene is tagged Dominant or Recessive
- Recombining two creatures always expresses the Dominant gene
- Recessive genes go into a hidden "latent" pool — can surface on future
  recombinations or DNA alteration
- Rare "mutation" chance (5–10%) produces an unexpected third gene from the
  creature's latent pool
- PROS: Creates emergent creature histories, rewards long-term gene tracking
- CONS: More complex to communicate to the player, latent pool needs clear UI
- PILLAR ALIGNMENT: Very strong Genetic Architect and Tactical Grid Mastery; moderate
  Discovery Through Play

---

OPTION C: Recombination Dice (Slot System)
- DNA has 3–5 gene slots; recombination "rolls" each slot independently
- Each slot has: inherit from Parent A / inherit from Parent B / mutate (new gene)
- Mutation chance scales with gene tier and player research level
- Failed slot = slot inherits a weaker version of the gene
- PROS: Visual clarity, each slot tells its own story, scales naturally with
  content expansion
- CONS: Can feel like a slot machine rather than biology, failure on multiple slots
  frustrating
- PILLAR ALIGNMENT: Strong Living World (every recombination is a reveal event);
  moderate Discovery Through Play

---

My recommendation: **Option B** with the latent pool as Gene Forge's defining
mechanic. The dominant/recessive system gives creatures biological identity, and
the latent pool creates a "hidden history" that rewards players who track lineages.
The mutation rate gives you the Slay the Spire variance without making outcomes
feel arbitrary.

Shall I draft the GDD section for Option B?
```

**User:**
```
Let's go with B but I want to add something: if you try to combine two creatures
with the same Dominant gene, you should get a "reinforced" version — stronger than
normal, but no chance of mutation. Rewards deliberate breeding programs.
```

**Game-Designer:**
```
That's a strong addition — it creates a meaningful choice between:
- Diverse recombination (variance, mutation chance, unexpected outcomes)
- Homogeneous recombination (predictable power boost, no surprises)

This maps well to creature collection meta: some players will chase mutations for
novel builds, others will optimize known-good genes. Both are valid strategies.

One edge case to flag: what happens when both parents have the same Dominant gene
but *different* Recessive genes? The Dominant expresses reinforced — but which
Recessive genes enter the latent pool? Options:

1. Both Recessives enter the latent pool (doubles mutation potential on future
   recombinations)
2. Only one Recessive enters — the player chooses which to keep
3. Recessives merge: if compatible, they form a "compound recessive" with unique
   expression rules

Which feels right for Gene Forge's level of complexity?
```

**User:**
```
Option 1 — both Recessives enter the latent pool. Keep it simple to track but
reward the long-term payoff.
```

**Game-Designer:**
```
Confirmed. Here is the complete Gene Interaction Rules section for the GDD:

---

## Gene Interaction Rules (Draft)

### Dominant Expression
When two creatures are recombined, the resulting creature expresses the Dominant
gene from each occupied slot. If both parents carry the same Dominant gene in a
slot, the result is a Reinforced expression of that gene (+20% effectiveness,
no mutation chance on that slot).

### Recessive Accumulation
All Recessive genes from both parents enter the offspring's Latent Gene Pool.
Latent genes are not expressed but can:
- Surface during DNA Alteration (each DNA alteration tier has a latent expression roll)
- Be revealed through the Gene Scan ability (unlocked via research)
- Trigger spontaneous mutation on future recombinations (base 8% per latent gene)

### Mutation Events
During any recombination, each slot has a base mutation chance equal to:
  `(number of latent genes in offspring pool) × 2% + research_mutation_bonus`
A mutation event replaces the expressed gene on that slot with a new gene drawn
from the offspring's latent pool. The previous expressed gene becomes Recessive.

### Failure State
Recombination failure occurs when a slot produces an incompatible gene pair with
no dominant resolution. Failure rate base: 5%. On failure, that slot carries a
Weakened Gene (half effectiveness) until the creature is recombined again.
Research investment reduces failure rate; it cannot be reduced to 0%.

### Reinforced Genes
Reinforced genes cannot mutate. They can only return to normal expression if a
future recombination introduces a different Dominant gene to that slot. Reinforced
status is visible on the creature's gene card.

---

Acceptance criteria:
- Player can identify at a glance whether a gene is Dominant, Recessive, or Latent
- Reinforced state is visually distinct from normal Dominant expression
- Mutation events are surfaced as a narrative moment ("unexpected expression
  detected"), not a silent stat change
- A player can intentionally construct a 3-generation breeding program and achieve
  a predicted Reinforced outcome in at least 80% of attempts

**Shall I write this to design/gdd/dna-recombination.md?**
```

**User:**
```
Yes, write it.
```

**Game-Designer:**
```
Written to design/gdd/dna-recombination.md

Next steps to consider:
- Design the Gene Scan UI (how does the player inspect the Latent Pool?)
- Define the full Weakened Gene recovery path
- Create the research tree nodes that reduce failure rate and boost mutation chance
- Write acceptance test scenarios for the 3-generation breeding program case
- Run /balance-check once initial gene values are in config
```

---

## Key Observations

1. **Agent asked clarifying questions first** — "feel" and "failure state" shaped the entire option set
2. **Three options with pillar alignment** — user could see which pillars each approach serves
3. **User modification surfaced a deeper design** — the reinforced gene rule created a meaningful strategic dichotomy
4. **Edge case proactively flagged** — the same-dominant-gene + different-recessive case was caught in design, not implementation
5. **Acceptance criteria tied to player behavior** — not "recombination works" but "player can execute a 3-generation plan"
6. **File written only after explicit approval** — no autonomous file creation
