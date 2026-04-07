# Implementation Prompt: Test ScriptableObject Data

## Agent

Invoke **game-designer** agent followed by **gameplay-programmer** agent.

Alternatively use `/team-combat Test Data Creation` with:
- **game-designer** — Select creatures, moves, and stats from GDD rosters that exercise all combat paths
- **gameplay-programmer** — Create ScriptableObject `.asset` files via Unity MCP tools
- **qa-tester** — Verify data loads correctly through ConfigLoader

> **Note**: This is data authoring, not code. The game-designer picks which creatures/moves cover the widest test surface. The gameplay-programmer creates the assets. Use Unity MCP tools (`unity_asset_create_prefab`, `unity_execute_code`) to create ScriptableObjects directly in the editor.

## Objective

Create a **minimal test data set** of ScriptableObject assets — enough creatures, moves, and an encounter config to run a full combat scenario. Without these `.asset` files, ConfigLoader returns null and no combat can initialize.

## Authoritative Design Source

- `design/gdd/creature-database.md` — Full creature roster with base stats, types, move pools, catch rates
- `design/gdd/move-database.md` — Full move list with power, accuracy, PP, effects, damage forms
- `design/gdd/encounter-system.md` — EncounterConfig fields and encounter type rules

## What Already Exists

### ScriptableObject classes (already implemented):
- `Assets/Scripts/Creatures/CreatureConfig.cs` — Species blueprint
- `Assets/Scripts/Creatures/MoveConfig.cs` — Move blueprint with MoveEffect list
- `Assets/Scripts/Core/EncounterConfig.cs` — Encounter blueprint
- `Assets/Scripts/Core/AIPersonalityConfig.cs` — AI personality presets
- `Assets/Scripts/Core/GameSettings.cs` — Global tuning values
- `Assets/Scripts/Core/ConfigLoader.cs` — Loads from `Resources/Data/` subfolders

### Target directories (from ConfigLoader):
- `Resources/Data/Creatures/` — CreatureConfig assets
- `Resources/Data/Moves/` — MoveConfig assets
- `Resources/Data/Encounters/` — EncounterConfig assets
- `Resources/Data/AIPersonalities/` — AIPersonalityConfig assets
- `Resources/Data/GameSettings.asset` — Singleton settings

### What's in the creature-database GDD:
- 20 MVP creatures across 8 genome types
- Each creature has primary type, base stats (HP/ATK/DEF/SPD/ACC), growth curve, move pool, catch rate

### What's in the move-database GDD:
- 45 MVP moves across 14 genome types and 3 damage forms
- Each move: type, form, power, accuracy, PP, priority, target type, range, effects

## Scope — What to Create

### Minimum viable combat data set:

#### 1. GameSettings.asset
One `GameSettings` ScriptableObject with MVP tuning values:
- `movementDivisor`: 20
- `sleepDuration`: 3
- `freezeDuration`: 2
- `confusionDuration`: 3
- `tauntDuration`: 2

#### 2. Three CreatureConfig assets (covers 3 types, all damage forms)
Pick from GDD creature-database.md — need type triangle coverage:

| Asset | Species | Primary Type | Notable |
|-------|---------|-------------|---------|
| `Emberfox.asset` | emberfox | Thermal | Physical attacker, tests STAB |
| `Tidecrab.asset` | tidecrab | Aqua | Energy attacker, tests type advantage vs Thermal |
| `Thornvine.asset` | thornvine | Flora | Bio attacker, tests third damage form |

Set base stats, growth curves, move pools, and catch rates from GDD values.

#### 3. Six MoveConfig assets (covers 3 damage forms + 1 status + 1 multi-effect)

| Asset | Move | Type | Form | Power | Acc | PP | Effects |
|-------|------|------|------|-------|-----|-----|---------|
| `FlameClaw.asset` | flame-claw | Thermal | Physical | 60 | 95 | 20 | — |
| `TidalPulse.asset` | tidal-pulse | Aqua | Energy | 65 | 90 | 15 | — |
| `SporeCloud.asset` | spore-cloud | Flora | Bio | 55 | 100 | 15 | ApplyStatus(Poison, 30%) |
| `ThunderStrike.asset` | thunder-strike | Volt | Energy | 80 | 85 | 10 | ApplyStatus(Paralysis, 20%) |
| `HealingMist.asset` | healing-mist | Flora | None | 0 | 0 | 10 | Drain(50%) self-heal |
| `IronGuard.asset` | iron-guard | Mineral | None | 0 | 0 | 15 | StatStage(DEF, +1) |

#### 4. One AIPersonalityConfig asset
| Asset | Personality | Weights |
|-------|------------|---------|
| `Balanced.asset` | balanced | Equal weights across all scoring dimensions |

#### 5. One EncounterConfig asset
| Asset | Type | Grid | Enemies |
|-------|------|------|---------|
| `TestWildEncounter.asset` | Wild | 8×6 flat grid | 2× Emberfox (level 5, 8) |

Set `captureAllowed: true`, `retreatAllowed: true`, player start tiles at (1,1) and (1,3), enemy spawns at (6,1) and (6,3).

## Method

Use Unity MCP tools to create assets:
1. `unity_execute_code` to create ScriptableObject instances and set fields
2. Or create via `unity_asset_create_prefab` if applicable
3. Verify with `unity_asset_list` that assets appear in correct folders
4. Test with `unity_execute_code` calling `ConfigLoader.Reinitialize()` then `ConfigLoader.GetCreature("emberfox")` to verify loading

Alternatively, create assets manually in the Unity Editor and verify via MCP.

## Constraints

- All values from GDD — no invented stats
- Assets in `Assets/Resources/Data/` subfolders (ConfigLoader requirement)
- IDs use kebab-case (e.g., `emberfox`, `flame-claw`, `test-wild-encounter`)
- Display names use Title Case (e.g., "Emberfox", "Flame Claw")
- Follow collaboration protocol: Question → Options → Decision → Draft → Approval

## Verification

- `ConfigLoader.Reinitialize()` completes without errors
- `ConfigLoader.GetCreature("emberfox")` returns non-null with correct stats
- `ConfigLoader.GetMove("flame-claw")` returns non-null with correct power/type
- `ConfigLoader.GetEncounter("test-wild-encounter")` returns non-null with correct grid size

## Branch

Create branch `feature/Test-ScriptableObjects` from `main`.
