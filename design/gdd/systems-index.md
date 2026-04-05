# Systems Index

All 53 systems organized by dependency layer. **MVP requires 27 systems** (marked with *).

> **Root creative reference:** All system GDDs must cite [`game-pillars.md`](game-pillars.md) when justifying a design decision. Before designing any system, read the pillar it primarily implements (see Dependencies section of `game-pillars.md`).

## Foundation Layer (No Dependencies)

| # | System | MVP | Description |
|---|--------|-----|-------------|
| 1 | Data Configuration Pipeline* | Yes | ScriptableObjects, enums, ConfigLoader |
| 2 | Grid / Tile System* | Yes | Isometric 3D grid, height, A*, flanking, line-of-sight (LoS owned here, not Terrain System) |
| 3 | Game State Manager* | Yes | Scene state machine, transitions |
| 4 | Save/Load System* | Yes | JSON persistence for all state |
| 5 | Type Chart System* | Yes | Type effectiveness matrix, STAB |

## Core Layer (Depends on Foundation)

| # | System | MVP | Description |
|---|--------|-----|-------------|
| 6 | Creature Database* | Yes | Species configs: types, base stats, move pools, catch rates |
| 7 | Move Database* | Yes | Move configs: type, power, accuracy, PP, effects, targeting |
| 8 | Turn Manager* | Yes | Phase-based combat sequencing, events |
| 9 | Damage & Health System* | Yes | Combat math with type multipliers, terrain synergy, height bonus |
| 10 | Creature Instance* | Yes | Runtime creature state: HP, level, XP, moves, DNA mods |
| 11 | Terrain System | No | Tile effects, type synergy tiles, LoS |
| 39 | Terrain Alteration System | No | Select creatures alter tiles mid-battle (scorch, freeze, grow, flood) |
| 40 | Threat / Aggro System* | Yes | Threat scoring, taunt, stealth, aggro manipulation |

## Feature Layer (Depends on Core)

| # | System | MVP | Description |
|---|--------|-----|-------------|
| 12 | Capture System* | Yes | Gene traps, catch rate formula, catch predictor UI |
| 13 | DNA Alteration System* | Yes | Stat boosts, perk grafts, instability, mutation risk |
| 34 | Body Part System* | Yes | Modular body slots, part blueprints, part conflicts, synergy sets |
| 35 | Color & Pattern System | No | Type-driven color, donor patterns, camouflage, scars |
| 36 | Creature Personality System | No | Behavioral DNA traits, visual personality indicators |
| 37 | Procedural Animation System | No | Part-driven animation blending, size-based playback |
| 38 | Sound Mutation System | No | DNA-driven vocalization layering |
| 14 | Party System* | Yes | Active party, storage, swap, field abilities |
| 15 | Leveling / XP System* | Yes | XP gain, stat growth curves, move learning |
| 16 | AI Decision System* | Yes | Scoring-based enemy AI, trainer personalities |
| 17 | Encounter System* | Yes | Wild, trainer, nest, trophy, horde encounter types |
| 18 | Pokedex System* | Yes | Progressive discovery, research tiers, lore entries |
| 19 | Creature Affinity | No | Bond levels, affinity perks, combo move unlocks |
| 20 | Combo Move System | No | Adjacent creature fusion attacks, type pairing |

## World Layer (Depends on Feature)

| # | System | MVP | Description |
|---|--------|-----|-------------|
| 21 | Campaign Map* | Yes | Habitat zones, branching paths, encounter nodes |
| 22 | Rival Trainer System* | Yes | MVP scope: 1 type-counter adaptation per encounter, no story arcs. Full: adaptive AI, recurring encounters, story arcs |
| 23 | Weather System | No | Per-region weather zones, type effectiveness shifts |
| 24 | Day/Night Cycle | No | Time-based spawns, type effectiveness shifts |
| 25 | Nesting System | No | Egg discovery, randomized innate DNA traits |
| 41 | Living Ecosystem* | Yes | Creature behavior on map, predator/prey, migration, conservation |
| 42 | Environmental Puzzle System | No | Field ability puzzles on campaign map |
| 43 | DNA Vault System | No | Ancient ruins, Forbidden Mods, vault guardians |
| 44 | Fossil System | No | Fossilized DNA, extinct creature resurrection, ancient parts |
| 45 | Creature Call System | No | Learned vocalizations to lure, scare, or trigger events |

## Presentation Layer (Depends on Feature)

| # | System | MVP | Description |
|---|--------|-----|-------------|
| 26 | Combat UI* | Yes | Grid display, creature info, move selection, capture UI |
| 27 | Combat Feedback* | Yes | Damage popups, type effectiveness callouts, animations |
| 28 | Party Management UI* | Yes | Party screen, creature details, DNA alteration interface |

## Infrastructure Layer

| # | System | MVP | Description |
|---|--------|-----|-------------|
| 29 | Settings System* | Yes | Player preferences, accessibility, pause menu |
| 30 | UI Shell* | Yes | Main menu, scene transitions, loading screens |

## Polish Layer

| # | System | MVP | Description |
|---|--------|-----|-------------|
| 31 | Audio System | No | SFX, music, dynamic combat music |
| 32 | VFX System | No | Particles, ability effects, DNA mutation visuals |
| 33 | Tutorial System | No | Progressive onboarding through play |
| 46 | Black Market System | No | Underground trader, ethical consequences, rotating inventory |
| 47 | Station Upgrade System* | Yes | 5-level research station progression |
| 48 | Creature Arena | No | Endgame battle tower, themed floors, rule modifiers |
| 49 | Expedition System | No | Send idle creatures on real-time autonomous missions |
| 50 | Institute Rank System* | Yes | Ethics-based ranking, content gating, grant funding |
| 51 | Move Customization System* | Yes | DNA-infused move variants, part-based moves, move mastery |
| 52 | Battle Scar System | No | Permanent visible scars from near-death, veteran prestige |
| 53 | Permadeath Mode | No | Optional Nuzlocke with memorial wall, posthumous DNA extraction |

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
