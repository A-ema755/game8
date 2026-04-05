# Gene Forge — Complete Workflow Guide

This guide provides a comprehensive framework for developing Gene Forge using a multi-agent system within Claude Code. Gene Forge is a tactical creature-collection RPG built in Unity 6 URP with isometric 3D visuals, DNA engineering mechanics, and a turn-based grid combat system.

---

## Core Structure

The workflow spans 10 phases from ideation through post-launch operations. Each phase has specific tools, agents, and deliverables. The system emphasizes design-first development: write design documents before code, validate designs through review processes, and use specialized agents for different domains.

---

## Key Workflow Stages

### Phase 0 — Setup

- Install Claude Code and configure the Unity 6 URP engine via `/setup-engine`
- Verify hooks are working and the Unity MCP bridge is live
- Confirm the isometric camera rig and grid system are in place before feature work begins

### Phases 1–2 — Design

- Start with `/brainstorm` for concept generation around new creature types, DNA traits, or biome mechanics
- Create structured Game Design Documents (GDDs) for each system — 53 system GDDs already completed for Gene Forge
- Define game pillars: **Genetic Architect**, **Tactical Grid Mastery**, **Discovery Through Play**, **Living World**
- Map all required systems using `/map-systems` before any implementation begins

### Phase 3 — Validation

- Use `/prototype` to test risky mechanics before full production
- Key Gene Forge areas that warrant prototyping: DNA splicing edge cases, type-chart interactions, height-variable grid targeting, instability threshold behavior

### Phases 4–5 — Production

- Work in sprints using `/sprint-plan`
- Implement features with appropriate specialists:
  - Creature combat systems → gameplay-programmer
  - DNA alteration logic and instability → systems-programmer
  - Type chart and damage formula → balance-designer
  - Isometric grid and A* pathfinding → technical-programmer
  - Creature art and animations → visual-specialist
- Enforce code quality through `/code-review` before merging

### Phases 6–7 — Quality

- Write tests alongside implementation (unit tests for DNA formulas, type effectiveness lookups, damage clamping)
- Use `/perf-profile` for optimization — isometric rendering and grid evaluation are primary hotspots
- Run `/team-polish` for final creature feel, UI feedback, and audio cues

### Phases 8–9 — Release

- Use `/launch-checklist` for comprehensive validation
- Coordinate release via `/team-release`
- Handle post-launch hotfixes for balance issues (DNA exploits, type-chart edge cases, overpowered body part combos)

---

## Agent Selection Reference

| Task | Agent | Escalation |
|---|---|---|
| New creature concept | game-designer | creative-director |
| DNA alteration system design | systems-designer | technical-director |
| Type chart tuning | balance-designer | creative-director |
| Combat damage formula | gameplay-programmer | technical-director |
| Grid pathfinding | technical-programmer | technical-director |
| Isometric rendering | graphics-programmer | technical-director |
| Creature GDD | game-designer | creative-director |
| UI / HUD design (UI Toolkit) | ui-designer | creative-director |
| Save system | systems-programmer | technical-director |
| Audio / SFX | audio-specialist | creative-director |

Design decisions escalate to creative or technical directors. Use `/team-*` skills (combat, narrative, UI, etc.) for coordinated cross-discipline work rather than managing multiple agents manually.

---

## Gene Forge System Map

The following systems are in scope. All have completed GDDs in `design/gdd/`.

**Core Loop Systems**
- Creature collection and roster management (Party System, Pokedex)
- DNA alteration engine (splicing, body parts, instability management)
- Turn-based grid combat (height-variable isometric, flanking, terrain synergy)
- Type chart (8 MVP types, effectiveness matrix, STAB 1.5x)
- Move system (25 MVP moves, move customization via DNA)
- Capture system (gene traps, catch rate formula, catch predictor)

**World Systems**
- Campaign map (5 habitat zones, branching paths, encounter nodes)
- Living ecosystem (predator/prey, migration, conservation scoring)
- Wild encounter logic (wild, trainer, nest, trophy, horde types)
- Research stations (DNA alteration, healing, storage, upgrades)

**Progression Systems**
- Institute rank (ethics-based ranking, content gating)
- Creature leveling and XP (cubic formula, stat growth curves)
- Pokedex completion (4-tier progressive discovery)
- Research Points economy (earned from battles, Pokedex, DNA discoveries)

**Technical Systems**
- Isometric grid (A* pathfinding, height 0–4, flanking arcs, line-of-sight)
- Save / load (creature state, DNA state, world state — JSON via JsonUtility)
- Unity 6 URP rendering (GPU instanced tiles, creature highlights)
- AI decision system (scoring-based, trainer personality profiles)

---

## Enforcement Mechanisms

Rules files enforce standards by file path:

- `Assets/Scripts/Gameplay/` — no hardcoded damage multipliers; all values from ScriptableObjects
- `Assets/Scripts/Creatures/` — creature stats come from `CreatureConfig` ScriptableObjects; no ad-hoc stat dictionaries
- `Assets/Scripts/Core/` — grid coordinates use `Vector2Int`; never raw floats for tile addressing
- `Assets/Scripts/AI/` — AI scoring weights must be data-driven via `AIPersonalityConfig` ScriptableObjects

Hooks validate commits, check ScriptableObject references, and prevent hardcoded values. This creates guardrails preventing common development pitfalls.

---

## Design-First Rule

Every feature in Gene Forge follows this order:

1. **GDD exists** in `design/gdd/` before any code is written
2. **Architecture proposed** and approved before implementation begins
3. **Tests written** alongside or before production code
4. **Code review** passes before merge
5. **Balance pass** on any system touching damage, DNA instability, type effectiveness, or capture rates

Ad-hoc development that skips the GDD step is the most common source of rework. The emphasis throughout is on structure, documentation, and deliberate decision-making.

---

## Gene Forge Pillars (Quick Reference)

These pillars drive all design decisions. When options conflict, use this hierarchy:

1. **Genetic Architect** — your creatures are your creations. DNA alteration is the core expression mechanic, not evolution.
2. **Tactical Grid Mastery** — positioning, terrain, type synergy, and creature combos win battles — not just stat checks.
3. **Discovery Through Play** — the Pokedex reveals information progressively. You learn by fighting, capturing, and experimenting — not by reading menus.
4. **Living World** — habitats, weather, day/night, and rival trainers that adapt to you create a world that feels reactive.

When evaluating a design option, ask: does this serve at least one pillar? Does it contradict another?

---

## Session Startup Checklist

Before beginning any work session:

- [ ] Read the relevant GDD in `design/gdd/`
- [ ] Check `production/` for active sprint tasks
- [ ] Review ADRs in `.claude/docs/technical-preferences.md` for active architecture decisions
- [ ] Confirm Unity project compiles with zero errors
- [ ] Identify which Gene Forge system you are touching and its dependencies (see `design/gdd/systems-index.md`)
