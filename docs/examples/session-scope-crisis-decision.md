# Example Session: Scope Crisis Decision

**Session Type:** Strategic Decision
**Agent:** creative-director
**Situation:** Demo deadline in 3 weeks; DNA Recombination system is behind schedule
**Deliverables:** Architecture Decision Record (ADR), updated GDD scope markers, demo script

---

## Context

Gene Forge has an investor demo in 3 weeks. The DNA Recombination system — one of the game's two defining pillars — is at 40% implementation. The combat system is polished. The question is whether to ship a simplified recombination system, cut it from the demo, or slip the deadline.

---

## Session Transcript

**User:**
```
We have an investor demo in 3 weeks. The DNA recombination system is behind — 40%
done. Combat is polished and working great. I'm not sure whether to simplify
recombination, cut it from the demo, or try to push the deadline. What do you
think?
```

**Creative-Director:**
```
Before I can give you a useful recommendation, I need to understand the constraints
better:

1. **Deadline flexibility:** Is the demo date fixed (investor travel, group event)
   or is a 1–2 week slip genuinely possible without consequence?

2. **Minimum viable recombination:** If you simplified to the absolute core — say,
   one-click gene swap with no latent pool or mutation — would it still represent
   the pillar, or would it misrepresent the system?

3. **Investor context:** Is this investor familiar with the tactical RPG / creature
   collection space? Or are they a generalist who needs to see the differentiator
   explained explicitly?

4. **Team capacity:** Is the 40% gap a scope problem (system is larger than
   estimated) or a time problem (system is right-sized but got deprioritized)?
```

**User:**
```
Date is fixed — investor is flying in from out of town. Simplified recombination
would still show the concept but would look rough. The investor is a generalist —
they've funded mobile games but not this genre. Team capacity issue: recombination
got deprioritized when we fixed the grid pathfinding bugs last sprint.
```

**Creative-Director:**
```
That context is critical. Here is my evaluation framework for this decision:

PRIORITY HIERARCHY (for this specific situation):
1. Investor Confidence — project survival depends on this demo
2. Pillar Visibility — showing both pillars (combat + DNA) vs. showing one well
3. Schedule Integrity — shipping something polished vs. something rough
4. Polish Quality — feel and presentation

---

OPTION A: Push to complete full recombination by demo
- Requires the team to work at high intensity for 3 weeks
- Risk: CRITICAL — 60% of a complex system in 3 weeks is aggressive
- If it doesn't land: investor sees an unfinished, buggy feature on a feature
  that was supposed to differentiate the game
- Pillar Visibility: High if it works; catastrophic if it doesn't
- Recommendation: Only viable if the team has genuine confidence, not optimism

---

OPTION B: Ship simplified recombination — "Gene Splice Preview"
- Implement: one-click gene slot swap, visual before/after stat change, one
  mutation event per demo (scripted or high-probability trigger)
- Cut: latent pool UI, multi-generation tracking, failure states
- Frame it to the investor as "the core loop — the full system ships in Alpha"
- Risk: Investor may ask about depth — be prepared to describe the full system
- Pillar Visibility: Moderate — shows the concept, not the full expression
- Schedule Integrity: High — 3 weeks is achievable for this scope
- Recommendation: Strong option IF combat is genuinely polished

---

OPTION C: Cut recombination from the demo entirely
- Show combat only — frame as "tactical creature combat with a rich type system"
- Risk: CRITICAL for a generalist investor — Gene Forge becomes "another tactical
  RPG." DNA engineering is the differentiator. Without it, the pitch loses its hook
- The investor's question will be "how is this different from existing games in
  the category?" and the answer becomes harder without a live demo of recombination
- Pillar Visibility: Low — the defining pillar is absent
- Recommendation: Only if Option B is genuinely not achievable

---

MY RECOMMENDATION: Option B

A generalist investor needs to see the differentiator in motion. "Watch me take a
gene from this creature and splice it onto that one — the stats change immediately"
is a 30-second demo beat that communicates the entire concept. You do not need the
latent pool for that beat to land.

Polished combat + a working gene splice beats polished combat alone. Historical
precedent: Hades showed early access with one biome and simplified builds — the
concept was clear even without the full system.

But this is your call. You know this investor and I don't.
```

**User:**
```
Option B makes sense. Let's go with that. Can you help me document this decision
and frame the demo?
```

**Creative-Director:**
```
Producing three artifacts:

---

ARCHITECTURE DECISION RECORD — ADR-012

Title: Demo Scope Reduction — DNA Recombination System
Date: 2026-04-04
Status: Accepted
Decider: [Developer]

Context:
Investor demo in 3 weeks. DNA Recombination system at 40% implementation due to
grid pathfinding bug sprint deprioritization. Demo date is fixed.

Decision:
Ship "Gene Splice Preview" for demo — simplified recombination showing gene slot
swap with stat feedback. Full system (latent pool, mutation events, failure states,
multi-generation tracking) deferred to Alpha milestone.

Consequences:
- Demo shows concept but not full depth
- Investor pitch must describe full system verbally to cover the gap
- Full recombination resumes as Sprint 1 priority after demo
- GDD sections for latent pool, mutation, and failure marked [POST-DEMO]

Validation Criteria:
- Demo can execute a complete gene splice in under 60 seconds
- Stat change is visually immediate and readable
- At least one mutation event triggers during the demo (scripted or high-prob)
- Investor can describe the gene concept back to us after seeing it

---

GDD SCOPE MARKERS (apply to design/gdd/dna-recombination.md):

Sections to mark [DEMO SCOPE]:
- Gene slot swap mechanic
- Before/after stat display
- Single mutation trigger

Sections to mark [POST-DEMO — Alpha]:
- Latent Gene Pool system
- Multi-generation tracking UI
- Failure state and Weakened Gene recovery
- Gene Scan research unlock

---

DEMO SCRIPT — DNA Recombination Beat

Setup: "Gene Forge isn't just about winning fights — it's about engineering your
team. Let me show you."

Action: Select Creature A (Fire/Physical attacker). Open Gene Lab. Drag a Water
gene from Creature B into Creature A's secondary slot.

Beat 1: "The stats update immediately — watch the type coverage chart change."
[Show type chart updating — Fire/Water now covers both weaknesses.]

Beat 2: "And sometimes — not always — something unexpected happens."
[Trigger the scripted mutation event. A third gene appears.]

Close: "In the full game, players track gene lineages across generations. We're
shipping this system in Alpha."

Duration target: 45–60 seconds.

---

All three artifacts are ready to write. Shall I update design/gdd/dna-recombination.md
with the scope markers and create docs/decisions/ADR-012-demo-scope.md?
```

**User:**
```
Yes, write both.
```

**Creative-Director:**
```
Written:
- docs/decisions/ADR-012-demo-scope.md
- design/gdd/dna-recombination.md updated with [DEMO SCOPE] and [POST-DEMO] markers

Recommended next steps:
- Brief the team on the simplified scope — confirm 3-week estimate is achievable
- Build the scripted mutation trigger first (it's the demo's emotional peak)
- Rehearse the demo script twice before the investor meeting
- After demo: Sprint 1 resumes full recombination — latent pool and mutation events
```

---

## Key Observations

1. **Context gathered before any recommendation** — deadline flexibility and investor background changed the analysis entirely
2. **Decision hierarchy made explicit** — "Investor Confidence (project survival)" ranked highest for this specific situation
3. **All three options received honest risk assessment** — Option C's CRITICAL risk was stated plainly, not softened
4. **Recommendation tied to precedent** — Hades early access framing gave the user a reference point for presenting incomplete systems positively
5. **Three concrete artifacts produced** — ADR, GDD scope markers, and demo script are actionable outputs, not just advice
6. **Final authority deferred** — "But this is your call" preserved developer agency on a project-survival decision

---

## Pattern: Scope Reduction as Strategic Prioritization

This session demonstrates that cutting scope is not failure — it is risk management aligned with survival-critical objectives. The key discipline is:

- Document the decision with full context (ADR)
- Mark the GDD clearly so the cut is not forgotten
- Define exactly what ships and what defers (no ambiguity)
- Build the demo narrative around what works, not around what's missing
