# Systems Index

All 53 systems organized by dependency layer. **MVP requires 27 systems** (marked with *).

> **Root creative reference:** All system GDDs must cite [`game-pillars.md`](game-pillars.md) when justifying a design decision. Before designing any system, read the pillar it primarily implements (see Dependencies section of `game-pillars.md`).

## Foundation Layer (No Dependencies)

| # | System | MVP | Status | Description |
|---|--------|-----|--------|-------------|
| 1 | Data Configuration Pipeline* | Yes | **Implemented** | ScriptableObjects, enums, ConfigLoader |
| 2 | Grid / Tile System* | Yes | **Implemented** | Isometric 3D grid, height, A*, flanking, line-of-sight (LoS owned here, not Terrain System) |
| 3 | Game State Manager* | Yes | **Implemented** | Scene state machine, transitions |
| 4 | Save/Load System* | Yes | Draft | JSON persistence for all state |
| 5 | Type Chart System* | Yes | **Implemented** | 14 genome types (3 tiers), 36 SE relationships, STAB, no immunities |

## Core Layer (Depends on Foundation)

| # | System | MVP | Status | Description |
|---|--------|-----|--------|-------------|
| 6 | Creature Database* | Yes | **Implemented** | Species configs: types, base stats, move pools, catch rates |
| 7 | Move Database* | Yes | **Implemented** | Move configs: genome type, damage form, power, accuracy, PP, effects, targeting |
| 8 | Turn Manager* | Yes | Draft | Phase-based combat sequencing, events |
| 9 | Damage & Health System* | Yes | Draft | Combat math with 3 damage forms (Physical/Energy/Bio), type multipliers, terrain synergy, height bonus |
| 10 | Creature Instance* | Yes | **Implemented** | Runtime creature state: HP, level, XP, moves, DNA mods |
| 11 | Terrain System | No | Draft | Tile effects, type synergy tiles, LoS |
| 39 | Terrain Alteration System | No | Draft | Select creatures alter tiles mid-battle (scorch, freeze, grow, flood) |
| 40 | Threat / Aggro System* | Yes | Draft | Threat scoring, taunt, stealth, aggro manipulation |

## Feature Layer (Depends on Core)

| # | System | MVP | Status | Description |
|---|--------|-----|--------|-------------|
| 12 | Capture System* | Yes | Draft | Gene traps, catch rate formula, catch predictor UI |
| 13 | DNA Alteration System* | Yes | Draft | Stat boosts, perk grafts, instability, mutation risk |
| 34 | Body Part System* | Yes | Draft | Modular body slots, part blueprints, part conflicts, synergy sets |
| 35 | Color & Pattern System | No | Draft | Type-driven color, donor patterns, camouflage, scars |
| 36 | Creature Personality System | No | Draft | Behavioral DNA traits, visual personality indicators |
| 37 | Procedural Animation System | No | Draft | Part-driven animation blending, size-based playback |
| 38 | Sound Mutation System | No | Draft | DNA-driven vocalization layering |
| 14 | Party System* | Yes | Draft | Active party, storage, swap, field abilities |
| 15 | Leveling / XP System* | Yes | Draft | XP gain, stat growth curves, move learning |
| 16 | AI Decision System* | Yes | Draft | Scoring-based enemy AI, trainer personalities |
| 17 | Encounter System* | Yes | Draft | Wild, trainer, nest, trophy, horde encounter types |
| 18 | Pokedex System* | Yes | Draft | Progressive discovery, research tiers, lore entries |
| 19 | Creature Affinity | No | Draft | Bond levels, affinity perks, combo move unlocks |
| 20 | Combo Move System | No | Draft | Adjacent creature fusion attacks, type pairing |

## World Layer (Depends on Feature)

| # | System | MVP | Status | Description |
|---|--------|-----|--------|-------------|
| 21 | Campaign Map* | Yes | Draft | Habitat zones, branching paths, encounter nodes |
| 22 | Rival Trainer System* | Yes | Draft | MVP scope: 1 type-counter adaptation per encounter, no story arcs. Full: adaptive AI, recurring encounters, story arcs |
| 23 | Weather System | No | Draft | Per-region weather zones, type effectiveness shifts |
| 24 | Day/Night Cycle | No | Draft | Time-based spawns, type effectiveness shifts |
| 25 | Nesting System | No | Draft | Egg discovery, randomized innate DNA traits |
| 41 | Living Ecosystem* | Yes | Draft | Creature behavior on map, predator/prey, migration, conservation |
| 42 | Environmental Puzzle System | No | Draft | Field ability puzzles on campaign map |
| 43 | DNA Vault System | No | Draft | Ancient ruins, Forbidden Mods, vault guardians |
| 44 | Fossil System | No | Draft | Fossilized DNA, extinct creature resurrection, ancient parts |
| 45 | Creature Call System | No | Draft | Learned vocalizations to lure, scare, or trigger events |

## Presentation Layer (Depends on Feature)

| # | System | MVP | Status | Description |
|---|--------|-----|--------|-------------|
| 26 | Combat UI* | Yes | Draft | Grid display, creature info, move selection, capture UI |
| 27 | Combat Feedback* | Yes | Draft | Damage popups, type effectiveness callouts, animations |
| 28 | Party Management UI* | Yes | Draft | Party screen, creature details, DNA alteration interface |

## Infrastructure Layer

| # | System | MVP | Status | Description |
|---|--------|-----|--------|-------------|
| 29 | Settings System* | Yes | Draft | Player preferences, accessibility, pause menu |
| 30 | UI Shell* | Yes | Draft | Main menu, scene transitions, loading screens |

## Polish Layer

| # | System | MVP | Status | Description |
|---|--------|-----|--------|-------------|
| 31 | Audio System | No | Draft | SFX, music, dynamic combat music |
| 32 | VFX System | No | Draft | Particles, ability effects, DNA mutation visuals |
| 33 | Tutorial System | No | Draft | Progressive onboarding through play |
| 46 | Black Market System | No | Draft | Underground trader, ethical consequences, rotating inventory |
| 47 | Station Upgrade System* | Yes | Draft | 5-level research station progression |
| 48 | Creature Arena | No | Draft | Endgame battle tower, themed floors, rule modifiers |
| 49 | Expedition System | No | Draft | Send idle creatures on real-time autonomous missions |
| 50 | Institute Rank System* | Yes | Draft | Ethics-based ranking, content gating, grant funding |
| 51 | Move Customization System* | Yes | Draft | DNA-infused move variants, part-based moves, move mastery |
| 52 | Battle Scar System | No | Draft | Permanent visible scars from near-death, veteran prestige |
| 53 | Permadeath Mode | No | Draft | Optional Nuzlocke with memorial wall, posthumous DNA extraction |

## Post-MVP Systems (not indexed)

- Async PvP (upload party, fight AI-controlled teams)
- DNA Trading (share mods/materials between players)
- Creature Photography (snapshot mechanic for Pokedex)
- Breeding (offspring with blended DNA)
- Shiny/Variant system (rare models with innate DNA)
- Field Abilities (out-of-combat creature utility on campaign map)
- Research Station crafting (combine raw DNA materials)
- Auto-battle / Speed modes
- Size & Proportion mods (growth/compact genes, limb extension)
- Skeletal Morphs (stance shift, extra limbs, head split — post-MVP)
- Creature Fusion (permanent merge of two creatures into Chimera)
- Creature Memory system (veteran, nemesis, bonded, trauma, breakthrough)

## Implementation Priority

1. Data Configuration Pipeline (enums, ScriptableObjects, ConfigLoader)
2. Type Chart System (effectiveness matrix)
3. Game State Manager (scene flow)
4. Creature Database (species configs)
5. Move Database (move configs)
6. Grid / Tile System (isometric 3D, height, A*)
7. Creature Instance (runtime state)
8. Turn Manager (phase sequencing)
9. Damage & Health System (type-aware combat math)
10. Leveling / XP System (stat growth, move learning)
11. AI Decision System (enemy creature AI)
12. Capture System (gene traps, catch formula)
13. Party System (active party, storage)
14. Encounter System (wild, trainer, trophy configs)
15. DNA Alteration System (mods, instability, risk)
16. Pokedex System (progressive discovery)
17. Campaign Map (habitat zones, paths)
18. Combat UI + Feedback
19. Party Management UI
20. Save/Load System (all state persistence)
21. Settings System
22. UI Shell

## Scope Tiers

| Tier | Creatures | Moves | Types | Zones | Content |
|------|-----------|-------|-------|-------|---------|
| **MVP** | 20-30 | 40-60 | 6-8 | 1 | Core loop: explore, fight, capture, modify |
| **Vertical Slice** | 50-60 | 80-100 | 10-12 | 2-3 | + Weather, day/night, combos, affinity, rivals |
| **Alpha** | 80-100 | 120-150 | 14-16 | 4-5 | + Nesting, shiny variants, trophy gauntlet |
| **Full Vision** | 150+ | 200+ | 16-18 | 6+ | + PvP, trading, breeding, photography |
