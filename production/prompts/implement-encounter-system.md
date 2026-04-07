# Implementation Prompt: Encounter System (#17)

## Agent

Invoke **team-combat** skill: `/team-combat Encounter System`

Team composition for this feature:
- **game-designer** — Verify GDD completeness, confirm encounter type rules and edge cases
- **gameplay-programmer** — Implement EncounterConfig, EncounterManager, and grid initialization
- **qa-tester** — Write acceptance tests from GDD criteria

> **Note**: This system is config + initialization logic. Skip Phases 3b–3d (AI, technical-artist, sound-designer) — proceed directly from implementation to testing.

## Objective

Implement the **Encounter System** — battle initialization from `EncounterConfig` ScriptableObjects. This is system #17 in the systems index, a Feature Layer system that sets up all combat scenarios by building grids, spawning creatures, and handing control to the Turn Manager.

## Authoritative Design Source

`design/gdd/encounter-system.md` — all rules, encounter types, grid initialization, edge cases, and acceptance criteria live there. Do NOT deviate from the GDD without explicit approval.

## What Already Exists

### Completed systems this builds on:
- `Assets/Scripts/Core/Enums.cs` — `EncounterType` enum (Wild, Trainer, Nest, Trophy, Horde)
- `Assets/Scripts/Core/ConfigStubs.cs` — `EncounterConfig : ConfigBase` stub (replace with full implementation)
- `Assets/Scripts/Core/ConfigLoader.cs` — already has `GetEncounter(string id)` method, loads from `Resources/Data/Encounters/`
- `Assets/Scripts/Combat/TurnManager.cs` — constructor takes `EncounterType encounterType`, ready to receive battle state
- `Assets/Scripts/Gameplay/Grid/GridSystem.cs` — grid creation and tile management
- `Assets/Scripts/Gameplay/Grid/TileData.cs` — tile data (height, terrain type)
- `Assets/Scripts/Creatures/CreatureInstance.cs` — `Create(config, level)` factory, `SetGridPosition()`
- `Assets/Scripts/Creatures/CreatureConfig.cs` — species configs loaded by ConfigLoader
- `Assets/Scripts/Gameplay/PartyState.cs` — player party management
- `Assets/Scripts/Combat/Enums/ActionType.cs` — includes `Flee` action type
- `Assets/Scripts/Combat/Enums/CombatResult.cs` — Victory, Defeat, Fled, Draw

### Assembly layout:
- `Assets/Scripts/GeneForge.Core.asmdef` (GUID: `a2365be9`) — Core namespace (covers Core/, Creatures/, Gameplay/)
- `Assets/Scripts/Combat/GeneForge.Combat.asmdef` (GUID: `ac121ad6`) — Combat namespace, references Core
- `Assets/Scripts/Gameplay/Grid/GeneForge.Grid.asmdef` — Grid namespace, references Core
- `Assets/Tests/EditMode/GeneForge.Tests.EditMode.asmdef` — references Core + Combat

### Key architectural decisions:
- **ADR-001**: ScriptableObjects for all config data
- **ADR-002**: Pure C# classes for logic systems (no MonoBehaviour)
- **ADR-004**: C# events for system decoupling
- **ADR-008**: Domain namespaces with assembly definitions

## Scope — What to Implement

### MVP scope (implement these):

#### 1. `Assets/Scripts/Core/EncounterConfig.cs` (replaces stub in ConfigStubs.cs)

**ScriptableObject** in `GeneForge.Core` namespace. Replace the stub in `ConfigStubs.cs`.

**Fields (from GDD §3.2):**
```csharp
[CreateAssetMenu(menuName = "GeneForge/EncounterConfig")]
public class EncounterConfig : ConfigBase
{
    // Identity
    [SerializeField] string displayName;
    [SerializeField] EncounterType encounterType;

    // Grid
    [SerializeField] Vector2Int gridDimensions;       // e.g. (8, 6)
    [SerializeField] int[] heightMapFlat;              // Flat array (Unity can't serialize 2D)
    [SerializeField] TerrainType[] tileLayoutFlat;     // Flat array
    [SerializeField] Vector2Int[] playerStartTiles;
    [SerializeField] Vector2Int[] enemyStartTiles;

    // Creatures
    [SerializeField] List<EncounterCreatureEntry> enemies;
    [SerializeField] bool captureAllowed;
    [SerializeField] bool retreatAllowed;

    // Rewards
    [SerializeField] int rpBase;

    // Properties with public getters
}
```

**Supporting serializable types:**
```csharp
[System.Serializable]
public class EncounterCreatureEntry
{
    public string speciesId;
    public int level;
    public string personalityConfigId;
    public List<string> overrideMoves;
    public Vector2Int spawnTile;
    public bool isBoss;
}
```

> **Note**: Wave configs (Nest/Horde), reward loot tables, dialogue IDs, and special condition flags are **post-MVP**. Include the fields as stubs but don't implement wave spawning logic.

#### 2. `Assets/Scripts/Combat/EncounterManager.cs`

**Plain C# class** in `GeneForge.Combat` namespace. Orchestrates encounter initialization.

**Methods:**
```csharp
/// Initializes a battle from an EncounterConfig.
/// Builds grid, spawns creatures, returns a populated BattleContext.
public BattleContext InitializeEncounter(EncounterConfig config, PartyState playerParty)

/// Spawns enemy creatures from config entries onto the grid.
/// Returns list of CreatureInstances placed at their spawn tiles.
private List<CreatureInstance> SpawnEnemies(EncounterConfig config, GridSystem grid)

/// Places player creatures at start tiles in party slot order.
private void PlacePlayerCreatures(PartyState party, Vector2Int[] startTiles, GridSystem grid)

/// Validates config integrity (species exist, tiles in bounds, etc.).
/// Logs errors for invalid entries, substitutes fallbacks.
public List<string> ValidateConfig(EncounterConfig config)
```

#### 3. `Assets/Scripts/Combat/BattleContext.cs`

**Data class** holding everything the TurnManager needs to start combat.

```csharp
public class BattleContext
{
    public EncounterConfig Config { get; }
    public GridSystem Grid { get; }
    public List<CreatureInstance> PlayerCreatures { get; }
    public List<CreatureInstance> EnemyCreatures { get; }
    public EncounterType EncounterType { get; }
    public bool CaptureAllowed { get; }
    public bool RetreatAllowed { get; }
}
```

#### 4. `Assets/Tests/EditMode/EncounterManagerTests.cs`

**Required tests (~15):**
- Config loads without errors for each encounter type (Wild, Trainer, Nest, Trophy, Horde)
- Grid dimensions match config
- Player creatures spawn at correct start tiles in party order
- Enemy creatures spawn at correct tiles with correct levels
- CaptureAllowed=false is propagated to BattleContext
- RetreatAllowed=false is propagated for Trainer encounters
- Invalid speciesId logs error and substitutes fallback
- Start tile out of grid bounds logs error and falls back to center
- Enemy count matches config entries
- Boss flag propagated correctly on EncounterCreatureEntry
- Empty enemy list handled gracefully
- ValidateConfig catches missing species IDs
- ValidateConfig catches out-of-bounds spawn tiles

### Out of scope (skip these):
- Wave spawning logic (Nest/Horde waves — post-MVP complexity)
- Wild encounter procedural generation from zone tables
- Retreat success probability (implemented in TurnManager already)
- Reward distribution (depends on Leveling/XP, Pokedex — not implemented)
- Pre/post encounter dialogue
- Encounter completion persistence (depends on Save/Load)
- Actual ScriptableObject asset creation (config assets created later)

## Constraints

- **EncounterConfig**: ScriptableObject extending `ConfigBase`
- **EncounterManager**: Pure C# class, no MonoBehaviour
- **Namespace**: `GeneForge.Core` for config, `GeneForge.Combat` for manager
- **No hardcoded gameplay values** — all values from config
- **XML doc comments** on all public API
- Follow existing code patterns from `DamageCalculator.cs` and `CaptureCalculator.cs`
- Use reflection-based test helpers from `CreatureInstanceTests.cs` pattern
- Remove the `EncounterConfig` stub from `ConfigStubs.cs` after creating the real class

## Collaboration Protocol

Follow Question -> Options -> Decision -> Draft -> Approval:
1. Ask before creating any file
2. Show draft or summary before writing
3. Get explicit approval for the full changeset
4. No commits without user instruction

## Branch

Create branch `feature/Encounter-System` from `main`.
