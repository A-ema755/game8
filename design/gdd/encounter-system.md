# Encounter System

## 1. Overview

The Encounter System defines and initializes all combat scenarios in Gene Forge. Every battle is spawned from an `EncounterConfig` ScriptableObject that specifies the encounter type, grid dimensions, terrain layout, height map, participating creatures with their AI personalities, allowed actions (capture, retreat), and reward pool. Five encounter types exist: Wild, Trainer, Nest, Trophy, and Horde. The MVP ships 8–10 encounters for the Verdant Basin zone. The system reads the config, builds the battle grid, spawns creatures at their designated tiles, and hands control to the Turn Manager.

## 2. Player Fantasy

Every encounter feels intentional. Walking into a Nest encounter with waves crashing against your flank is a different tension than facing a lone Trophy creature on a wide open plateau. The terrain layout printed into each config means the designer controls the tactical story: a narrow chokepoint that rewards area moves, an elevated promontory that the enemy already controls, a water tile in the center that helps your Water creature if you can reach it. The encounter is the level design.

## 3. Detailed Rules

### 3.1 Encounter Types

| Type | Capture Allowed | Player Retreat | Creature Count | Waves | Notes |
|------|----------------|---------------|----------------|-------|-------|
| Wild | Yes | Yes (costs turn) | 1–3 | No | Standard exploration combat |
| Trainer | No | No | 1–6 | No | NPC trainer with named team |
| Nest | No | No | Waves of 3–6 | Yes (3 waves) | Defend eggs; failure = no egg reward |
| Trophy | No | No | 1 | No | Superboss; unique DNA drop |
| Horde | No | Yes | 6–12 | No | Many weak creatures; bulk XP/materials |

Capture is blocked in non-Wild encounters by setting `captureAllowed = false` on `EncounterConfig`. The Combat UI hides the Throw Trap action when this flag is false.

Player retreat in Wild encounters: the retreating creature skips its turn; the full party flees at the start of the next round. Retreat fails (50% chance, tunable) if any enemy has used a move with the `preventsRetreat` flag this turn.

### 3.2 EncounterConfig ScriptableObject

```csharp
[CreateAssetMenu(menuName = "GeneForge/EncounterConfig")]
public class EncounterConfig : ScriptableObject
{
    [Header("Identity")]
    public string id;                              // kebab-case, e.g. "vb-wild-001"
    public string displayName;
    public EncounterType encounterType;

    [Header("Grid")]
    public Vector2Int gridDimensions;              // e.g. (8, 6)
    public int[,] heightMap;                       // Tile height values (0–3)
    public TileType[,] tileLayout;                 // Terrain types per tile
    public Vector2Int[] playerStartTiles;          // Where player creatures spawn
    public Vector2Int[] enemyStartTiles;           // Where enemy creatures spawn

    [Header("Creatures")]
    public List<EncounterCreatureEntry> enemies;
    public bool captureAllowed;
    public bool retreatAllowed;

    [Header("Waves (Nest/Horde only)")]
    public int totalWaves;
    public List<WaveConfig> waves;                 // Null for non-wave encounters

    [Header("Rewards")]
    public EncounterRewardConfig rewards;

    [Header("Special Conditions")]
    public string preEncounterDialogueId;          // Optional: NPC dialogue before battle
    public string postEncounterDialogueId;         // Optional: NPC dialogue after win
    public List<string> specialConditionFlags;     // e.g. "night-only", "rain-required"
}

[System.Serializable]
public class EncounterCreatureEntry
{
    public string speciesId;
    public int level;
    public string personalityConfigId;             // References AIPersonalityConfig asset
    public List<string> overrideMoves;             // Empty = use default level-up moves
    public Vector2Int spawnTile;
    public bool isBoss;                            // Blocks capture; shows special health bar
}

[System.Serializable]
public class WaveConfig
{
    public int waveIndex;
    public List<EncounterCreatureEntry> creatures;
    public int spawnDelayTurns;                    // Turns after wave start before spawning
}

[System.Serializable]
public class EncounterRewardConfig
{
    public int rpBase;                             // Base Research Points awarded on win
    public List<string> guaranteedMaterialIds;     // Always dropped
    public List<LootTableEntry> randomMaterials;   // Rolled per win
    public string uniqueDnaRecipeId;               // Trophy only; null otherwise
    public bool grantsEgg;                         // Nest only
}
```

### 3.3 Grid Initialization

On encounter start:
1. Load `EncounterConfig` by ID from `Resources/Data/Encounters/`.
2. Instantiate grid tiles per `gridDimensions`, applying `heightMap` and `tileLayout`.
3. Spawn player creatures at `playerStartTiles` in party slot order.
4. Spawn enemy creatures at their `spawnTile` positions.
5. Pass `BattleState` to the Turn Manager to begin.

Height map values (0–3):
- 0: Ground level
- 1: Elevated (low)
- 2: Elevated (high)
- 3: Cliff/unreachable ledge (impassable; creatures can be knocked onto it with knockback moves)

### 3.4 Wild Encounter Spawning

Wild encounters on the campaign map are not all pre-authored configs. The Campaign Map uses a `WildEncounterTable` per zone that procedurally selects:
- 1–3 creatures from the zone's spawn list (weighted by rarity).
- A random grid template from the zone's `WildGridTemplates` pool (pre-authored grids, randomly selected).
- Creature levels scaled to the zone's `encounterLevelRange`.

The constructed encounter is functionally identical to an authored `EncounterConfig` — it just has no `preEncounterDialogueId` and uses the zone's default reward scaling.

### 3.5 Nest Encounter Rules

- The grid contains 1–3 Nest objects (indestructible tiles with an egg state).
- Waves spawn at the edges of the grid on turns 1, 4, and 7 (configurable via `spawnDelayTurns`).
- If any Nest tile is reached by an enemy creature and that creature spends a full turn adjacent to it uncontested, the egg is destroyed.
- Win condition: survive all waves with at least 1 egg intact.
- Reward: `grantsEgg = true` — player receives a creature egg with randomized innate DNA traits.

### 3.6 Trophy Encounter Rules

- Single enemy creature marked `isBoss = true`.
- Boss creature has a full-width health bar UI (not the standard compact bar).
- Boss creature cannot be captured.
- On defeat: `uniqueDnaRecipeId` is always granted. This recipe cannot be found anywhere else.
- Trophy encounters do not appear in the Campaign Map until a prerequisite flag is set (e.g. "cleared zone's final trainer battle").

### 3.7 Horde Encounter Rules

- 6–12 low-level creatures spawn simultaneously.
- Creatures spawn in clusters with the same `speciesId` groups.
- Win condition: defeat all creatures on the field.
- Player can retreat (costs all remaining turns for that round).
- Rewards scale with number of creatures defeated, not just win/loss.

### 3.8 MVP Verdant Basin Encounters

| ID | Type | Name | Grid | Enemy | Notes |
|----|------|------|------|-------|-------|
| `vb-wild-001` | Wild | Forest Patrol | 6×5 | 2x Psysprout (L3) | Tutorial encounter |
| `vb-wild-002` | Wild | Riverside Ambush | 7×5 | Voltfin (L4), Mosshell (L3) | Water tile center |
| `vb-wild-003` | Wild | Elevated Den | 8×6 | 3x Thornslug (L5) | Height 2 enemy start |
| `vb-wild-004` | Wild | Grassland Herd | 7×5 | 3x Psysprout (L6) | Open terrain |
| `vb-wild-005` | Wild | Bog Stalker | 6×6 | Shadowmite (L7) | Dark tile hazards |
| `vb-trainer-001` | Trainer | Rival: First Meeting | 8×6 | Rival team L5–6 | Story dialogue |
| `vb-nest-001` | Nest | Thornslug Nest | 8×7 | 3 waves, Thornslugs | Egg reward |
| `vb-horde-001` | Horde | Psysprout Swarm | 9×6 | 10x Psysprout (L4–5) | Bulk XP |
| `vb-trophy-001` | Trophy | Ancient Coalbear | 10×8 | Coalbear (L15) | Unique DNA recipe |
| `vb-trainer-002` | Trainer | Rival: Rematch | 8×6 | Rival team L9–11 | Post-nest story beat |

## 4. Formulas

### Wild Encounter Level Scaling

```
enemyLevel = Random.Range(zoneConfig.encounterLevelMin, zoneConfig.encounterLevelMax + 1)
```

### Horde Reward Scaling

```
rpAwarded = rpBase + Floor(rpBase * defeatedCount / totalCount)
materialDrops = Floor(guaranteedMaterialCount + randomMaterialCount * (defeatedCount / totalCount))
```

### Retreat Success Chance

```
retreatSuccessChance = 1.0 - (enemyCount * 0.15)   // -15% per enemy with preventsRetreat
retreatSuccessChance = Clamp(retreatSuccessChance, 0.1, 1.0)   // floor 10%
```

Base retreat always succeeds if no enemy has used `preventsRetreat` this round.

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Player party has only 1 creature when entering a Trainer encounter | Allowed; player fights with one creature |
| All player start tiles are blocked by terrain error in config | Log error; fall back to grid center tiles |
| Nest egg destroyed before wave 3 | Encounter continues but `grantsEgg` flag set to false; player still earns RP and materials for fighting |
| All nest eggs destroyed | Encounter ends immediately as a loss (no partial reward) if `requireAllEggsToWin = true`, otherwise win with reduced reward |
| Trophy creature's `uniqueDnaRecipeId` is already known by the player | Recipe is still granted (duplicate); player sees "Recipe Already Known" notification |
| Horde encounter: player retreats after defeating 4 of 10 | Partial rewards calculated via formula; partial retreat is allowed |
| Wild encounter table has no valid creatures for the current time of day | Falls back to the zone's `fallbackSpecies` list; never returns an empty encounter |
| `EncounterConfig` references a species ID not in the Creature Database | Log error at encounter load time; substitute a default creature; do not crash |
| Player enters a Trophy encounter without meeting the prerequisite flag | Campaign Map should prevent access; if somehow reached, encounter loads normally (no failsafe needed in encounter system itself) |
| Wave spawn tile is occupied by a player creature | Wave creature spawns at the nearest available empty tile |

## 6. Dependencies

| System | Dependency Type | Notes |
|--------|----------------|-------|
| Grid / Tile System | Write | Builds grid from `heightMap` and `tileLayout` |
| Creature Database | Read | Spawns enemy creatures by `speciesId` |
| Turn Manager | Write | Hands `BattleState` to Turn Manager on init |
| AI Decision System | Read | Enemy creatures use `personalityConfigId` |
| Party System | Read | Player start tiles populated from active party |
| Capture System | Read | `captureAllowed` flag consumed by Combat UI |
| Leveling / XP System | Write | Awards XP from `EncounterRewardConfig` after battle |
| Pokedex System | Write | Encounter with new species triggers Pokedex update |
| Campaign Map | Read | Selects encounter by ID based on player position |
| Save/Load System | Read/Write | Encounter completion flags persisted |
| Combat UI | Read | Reads encounter type for UI mode (capture UI, boss bar, etc.) |

## 7. Tuning Knobs

| Parameter | Default | Range | Notes |
|-----------|---------|-------|-------|
| Wild encounter creature count | 1–3 | 1–4 | Per zone table |
| `encounterLevelMin/Max` (Verdant Basin) | 3–8 | Zone-configurable | |
| `retreatBasePenalty` | 0.15 | 0.0–0.3 | Per-enemy reduction to retreat chance |
| `retreatSuccessFloor` | 0.10 | 0.05–0.5 | Minimum retreat success chance |
| Nest egg destroy delay (turns adjacent) | 1 | 1–3 | Higher = more forgiving |
| Nest wave spawn turns | 1, 4, 7 | Configurable per wave | |
| `rpBase` (Wild, standard) | 80 | 20–500 | Per-config |
| `rpBase` (Trainer) | 200 | 100–1000 | Per-config |
| `rpBase` (Trophy) | 500 | 300–2000 | Per-config |
| `requireAllEggsToWin` | false | bool | If true, losing any egg = loss |
| Horde partial reward scaling | Linear | Replaceable | See formula |

## 8. Acceptance Criteria

- [ ] All 5 encounter types load from `EncounterConfig` without errors.
- [ ] Grid tiles are instantiated with correct height and terrain type from `heightMap` and `tileLayout`.
- [ ] Player creatures spawn at `playerStartTiles` in party slot order.
- [ ] `captureAllowed = false` hides the Throw Trap action in the Combat UI.
- [ ] Trainer encounters cannot have the Throw Trap action regardless of any other state.
- [ ] Nest encounters spawn waves on the correct turns; egg destruction logic fires correctly.
- [ ] Trophy encounters display the full-width boss health bar.
- [ ] Trophy `uniqueDnaRecipeId` is granted on win; duplicate notification shown if already known.
- [ ] Horde partial reward formula correctly scales RP and material drops to creatures defeated.
- [ ] Retreat attempt in Wild encounters resolves with correct probability; `preventsRetreat` flag reduces success.
- [ ] All 10 MVP Verdant Basin encounters load without errors in Play Mode.
- [ ] Wild encounter level scaling draws from `encounterLevelRange` correctly.
- [ ] Encounter completion flags are saved and prevent replaying one-time encounters (Trainer, Trophy).
- [ ] Missing `speciesId` in config logs an error and substitutes a fallback without crashing.
