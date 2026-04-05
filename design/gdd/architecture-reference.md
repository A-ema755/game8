# Architecture Reference

## 1. Overview

This document serves as a navigation guide and design reference for the Gene Forge project structure, naming conventions, design principles, and key architectural decisions. It is not a system implementation document but rather a living index of conventions that all contributors follow. This reference is authoritative for project governance and consistency; disagreements about architecture are resolved against this document.

## 2. Player Fantasy

This section does not apply to an architecture reference document. The player fantasy is encoded in individual system GDDs.

## 3. Detailed Rules

### Project Structure

Gene Forge is organized into the following directory hierarchy:

```
C:\Users\gueva\game8\
├── Assets/                          # Unity project assets
│   ├── Scripts/
│   │   ├── Core/                    # Foundation systems (no gameplay dependency)
│   │   │   ├── GameStateManager.cs
│   │   │   ├── SaveLoadManager.cs
│   │   │   ├── ConfigLoader.cs
│   │   │   ├── EventSystem.cs
│   │   │   └── [other core systems]
│   │   ├── Gameplay/                # Gameplay logic (creatures, combat, grid)
│   │   │   ├── Grid/
│   │   │   │   ├── GridSystem.cs
│   │   │   │   ├── TileData.cs
│   │   │   │   ├── PathfindingService.cs
│   │   │   │   └── [grid-related]
│   │   │   ├── Creatures/
│   │   │   │   ├── CreatureInstance.cs
│   │   │   │   ├── CreatureView.cs
│   │   │   │   ├── DNAModifier.cs
│   │   │   │   └── [creature-related]
│   │   │   ├── Combat/
│   │   │   │   ├── CombatController.cs
│   │   │   │   ├── TurnManager.cs
│   │   │   │   ├── DamageCalculator.cs
│   │   │   │   ├── CombatFeedbackManager.cs
│   │   │   │   └── [combat-related]
│   │   │   ├── Capture/
│   │   │   │   ├── CaptureSystem.cs
│   │   │   │   ├── GeneTrappedCreature.cs
│   │   │   │   └── [capture-related]
│   │   │   └── [other gameplay]
│   │   ├── AI/                      # AI decision systems
│   │   │   ├── AIDecisionSystem.cs
│   │   │   ├── AIPersonality.cs
│   │   │   ├── ThreatEvaluator.cs
│   │   │   └── [AI-related]
│   │   ├── UI/                      # UI controllers and logic
│   │   │   ├── MainMenuController.cs
│   │   │   ├── CombatUIController.cs
│   │   │   ├── PartyManagementUIController.cs
│   │   │   ├── SettingsUIController.cs
│   │   │   └── [UI-related]
│   │   └── World/                   # World and map systems
│   │       ├── CampaignMapController.cs
│   │       ├── EcosystemManager.cs
│   │       ├── EncounterSpawner.cs
│   │       └── [world-related]
│   ├── Resources/
│   │   └── Data/                    # All ScriptableObject config assets
│   │       ├── Creatures/           # CreatureConfig instances
│   │       │   ├── Emberfox.asset
│   │       │   ├── Aqualing.asset
│   │       │   └── [creature configs]
│   │       ├── Moves/               # MoveConfig instances
│   │       │   ├── Flameclaw.asset
│   │       │   ├── Aquablast.asset
│   │       │   └── [move configs]
│   │       ├── BodyParts/           # BodyPartConfig instances
│   │       │   ├── GlandsThermal.asset
│   │       │   ├── CryoShell.asset
│   │       │   └── [part configs]
│   │       ├── Encounters/          # EncounterConfig instances
│   │       │   ├── VB_WildForest.asset
│   │       │   ├── VB_TrainerBattle.asset
│   │       │   └── [encounter configs]
│   │       ├── AIPersonalities/     # AIPersonalityConfig instances
│   │       │   ├── Aggressive.asset
│   │       │   ├── Cautious.asset
│   │       │   └── [personality configs]
│   │       ├── StatusEffects/       # StatusEffectConfig instances
│   │       │   ├── Burn.asset
│   │       │   ├── Poison.asset
│   │       │   └── [status configs]
│   │       ├── MapZones/            # MapZoneConfig instances (post-MVP)
│   │       │   ├── VerdantBasin.asset
│   │       │   └── [zone configs]
│   │       ├── GameSettings.asset   # Global game config
│   │       ├── TypeColors.asset     # Type color palette (colorblind modes)
│   │       ├── TypeChart.asset      # Type effectiveness matrix
│   │       └── [other configs]
│   ├── Scenes/
│   │   ├── Boot.unity               # Single-frame entry point
│   │   ├── MainMenu.unity
│   │   ├── Combat.unity             # Isometric 3D grid + creatures
│   │   ├── CampaignMap.unity        # 2D node-based map screen
│   │   ├── PartyManagement.unity    # Creature detail/forge screen
│   │   └── [other scenes]
│   ├── Prefabs/
│   │   ├── Creatures/               # Creature 3D models + components
│   │   │   ├── CreaturePrefab.prefab
│   │   │   └── [creature prefabs]
│   │   ├── Grid/                    # Grid tiles and highlighting
│   │   │   ├── GridTile.prefab
│   │   │   ├── TileHighlight.prefab
│   │   │   └── [grid-related]
│   │   ├── UI/                      # Reusable UI component prefabs
│   │   │   ├── MoveButton.prefab
│   │   │   ├── CreatureCard.prefab
│   │   │   └── [UI prefabs]
│   │   └── [other prefabs]
│   ├── Art/
│   │   ├── Models/                  # 3D meshes (creatures, tiles, props)
│   │   │   ├── Creatures/
│   │   │   ├── BodyParts/
│   │   │   ├── Tiles/
│   │   │   └── [models]
│   │   ├── Materials/               # Material instances for tiles, creatures, UI
│   │   │   ├── TileMaterials/
│   │   │   ├── CreatureMaterials/
│   │   │   └── [materials]
│   │   ├── Textures/
│   │   │   ├── Creature/
│   │   │   ├── Terrain/
│   │   │   └── [textures]
│   │   └── Animations/              # Creature animations (post-MVP)
│   ├── Audio/
│   │   ├── Music/
│   │   ├── SFX/
│   │   │   ├── Combat/
│   │   │   ├── UI/
│   │   │   ├── Creatures/
│   │   │   └── [sfx]
│   │   └── [audio assets]
│   ├── VFX/                         # Particle systems and effects
│   │   ├── Combat/
│   │   ├── Capture/
│   │   ├── Status/
│   │   └── [vfx]
│   ├── Shaders/                     # Custom shaders and shader graphs
│   │   ├── Isometric.shader
│   │   ├── CreatureMutation.shader
│   │   └── [shaders]
│   └── Settings/                    # URP asset, quality profiles
│       └── UniversalRenderer-HighQuality.asset
├── design/                          # Game design documents
│   └── gdd/                         # Game Design Documents
│       ├── game-concept.md          # Vision and pillars
│       ├── systems-index.md         # System dependency map
│       ├── [27 MVP system GDDs]
│       ├── [26 post-MVP system GDDs]
│       └── architecture-reference.md (this file)
├── docs/                            # Technical documentation
│   ├── architecture/                # Architecture decision records (ADRs)
│   │   ├── ADR-001-scriptableobjects-config.md
│   │   ├── ADR-002-plain-csharp-logic.md
│   │   ├── ADR-003-singleton-gamestate.md
│   │   ├── ADR-004-csharp-events.md
│   │   ├── ADR-005-json-saves.md
│   │   ├── ADR-006-modular-creature-parts.md
│   │   ├── ADR-007-type-chart-static.md
│   │   └── [additional ADRs]
│   ├── engine-reference/            # Engine API references
│   │   └── unity/
│   │       ├── VERSION.md           # Unity 6 version pinning
│   │       ├── URP-4.3-API.md       # URP rendering pipeline API
│   │       └── [engine docs]
│   └── [other documentation]
├── tests/                           # Test suites (EditMode + PlayMode)
│   ├── EditMode/
│   │   ├── TypeChartTests.cs
│   │   ├── DamageCalculatorTests.cs
│   │   ├── GridPathfindingTests.cs
│   │   ├── CaptureRateTests.cs
│   │   └── [unit tests]
│   └── PlayMode/
│       ├── CombatScenarioTests.cs
│       ├── LevelingTests.cs
│       └── [integration tests]
├── tools/                           # Build and pipeline tools
│   ├── ConfigGenerator.cs           # Generate configs from data sheets
│   ├── SpriteAtlasBaker.cs          # Bake sprite atlases
│   └── [tools]
├── prototypes/                      # Throwaway experimentation (isolated)
│   ├── IsometricCameraTest/
│   ├── DNAVisualsTest/
│   └── [prototypes]
├── production/                      # Production management
│   ├── session-state/               # Ephemeral session snapshots (gitignored)
│   │   └── active.md
│   └── session-logs/                # Audit trail (gitignored)
└── .claude/                         # Claude Code agent config
    └── docs/                        # Agent guides, standards, coordination
        ├── directory-structure.md
        ├── technical-preferences.md
        ├── coding-standards.md
        ├── coordination-rules.md
        └── context-management.md
```

### Naming Conventions

| Category | Convention | Examples | Notes |
|----------|-----------|----------|-------|
| **Classes** | PascalCase | `CreatureInstance`, `GridSystem`, `DamageCalculator` | Public and private both use PascalCase |
| **Public Methods** | PascalCase | `GetReachableTiles()`, `ApplyDamage()`, `CaptureSurvival()` | No "Get" prefix for properties |
| **Private Fields** | _camelCase | `_moveSpeed`, `_currentHealth`, `_turnOrder` | Always use underscore prefix |
| **Serialized Fields** | camelCase | `[SerializeField] int maxHp` | No underscore (exposed in Inspector) |
| **Properties** | PascalCase | `public int MaxHP { get; set; }` | Use properties over public fields |
| **Events** | PascalCase | `RoundStarted`, `CreatureCaptured`, `DNAModApplied` | Event handlers: `On[Event]` |
| **Enums** | PascalCase | `CreatureType.Thermal`, `DamageForm.Energy`, `NodeState.Available` | Values are PascalCase |
| **Constants** | PascalCase | `MinimumDamage`, `MaxPartySize`, `BaseExpYield` | Readonly static fields use PascalCase |
| **Files** | PascalCase.cs | `CreatureInstance.cs`, `GridSystem.cs`, `DamageCalculator.cs` | Match class name |
| **Scenes** | PascalCase.unity | `Combat.unity`, `MainMenu.unity`, `CampaignMap.unity` | Domain-specific naming |
| **ScriptableObjects** | PascalCase.asset | `Emberfox.asset`, `Flameclaw.asset`, `StandardTrap.asset` | Represent game entities |
| **Config IDs** | kebab-case | `emberfox`, `flame-claw`, `standard-gene-trap` | Used in code for lookups |
| **Folders** | kebab-case | `Scripts/gameplay`, `Resources/Data/creatures`, `Assets/art` | Lowercase with hyphens |

### Design Principles

#### 1. Data-Driven Configuration
All gameplay values live in ScriptableObjects (under `Resources/Data/`), not hardcoded in scripts. Examples:
- Creature base stats: `CreatureConfig` asset
- Move properties: `MoveConfig` asset
- Type effectiveness matrix: `TypeChart` asset (exception: static 2D array for performance)
- Encounter compositions: `EncounterConfig` asset
- UI colors and layout: ScriptableObject or public fields on UI controllers

**Rationale**: Non-programmers (designers, balancers) can adjust values without recompiling. Easy to version multiple balance patches.

#### 2. Separation of Logic and View
- **Logic**: Pure C# classes with no MonoBehaviour dependency (e.g., `DamageCalculator`, `GridPathfinding`, `TypeChart`)
- **View**: MonoBehaviours that consume logic and render results (e.g., `CreatureView`, `CombatUIController`)
- **Data**: Immutable configs and state holders (e.g., `CreatureInstance` wraps logic + state)

**Rationale**: Logic is unit-testable. Views can be swapped (console vs. 3D rendering) without touching logic.

#### 3. Event-Based Decoupling
Systems communicate via C# events, not direct method calls. Examples:
```csharp
public static event System.Action<CreatureInstance> CreatureCaptured;
public static event System.Action<DamageEvent> DamageDealt;
public static event System.Action<TurnPhase> TurnPhaseChanged;
```

**Rationale**: Loose coupling allows systems to be developed and tested independently. Adding new feedback (UI, audio, analytics) doesn't require touching combat logic.

#### 4. Dependency Injection (No Service Locators)
Where systems need dependencies, pass them as constructor parameters or serialized references, not `FindObjectOfType` or global singletons (except GameStateManager).

**Rationale**: Dependencies are explicit. Testability is maximized.

#### 5. Minimal Singletons
Only one singleton is allowed: `GameStateManager` (DontDestroyOnLoad). All other systems use injected references or event-driven communication.

**Rationale**: Reduces hidden global state. Easier to reason about object lifetimes.

#### 6. Type-Safe Creature Data
`CreatureInstance` is the runtime state holder. It contains:
- Immutable reference to `CreatureConfig` (the species template)
- Mutable state: current HP, level, XP, move PP, DNA mods, personality, party ID
- Methods: `TakeDamage()`, `LearnMove()`, `ApplyDNAMod()`, `CalculateStat()`

**Rationale**: Creatures are first-class objects. Their state is always consistent.

#### 7. Grid-Based Movement
The grid system (`GridSystem.cs`) is the source of truth for creature positions and movement. It handles:
- Isometric coordinate conversion (world → grid)
- Pathfinding (A* with height costs)
- Tile state (occupancy, terrain effects)

**Rationale**: Combat is deterministic and locally consistent. No physics engine needed.

### Key Architectural Decisions

| ADR | Title | Decision | Rationale |
|-----|-------|----------|-----------|
| ADR-001 | Config as ScriptableObjects | All gameplay configs are ScriptableObjects, not JSON or hardcoded | Designer-friendly, version-controlled, zero deserialization overhead |
| ADR-002 | Pure Logic in Plain C# | Combat math, pathfinding, type effectiveness calculated in non-MonoBehaviour classes | Testable without a running scene; fast execution |
| ADR-003 | One Singleton: GameStateManager | Only GameStateManager is a global singleton (DontDestroyOnLoad) | Scene management centralized; avoids hidden global state elsewhere |
| ADR-004 | Events over UnityEvents | C# `event System.Action<T>` for inter-system communication, not UnityEvent | Type-safe, zero serialization overhead, familiar to C# developers |
| ADR-005 | JSON Saves via JsonUtility | Player saves serialized with Unity's JsonUtility, not binary or custom | Human-readable, easy to debug, no third-party dependencies |
| ADR-006 | Modular Creature Parts | Body parts are ScriptableObjects equippable to creature slots; not baked into models | Enables visible mutations without per-combination art; easy to balance |
| ADR-007 | Type Chart as Static 2D Array | Type effectiveness matrix is a static int[16, 16] array, not a ScriptableObject | Type checks happen every turn; static array is fastest (no lookup, no GC) |

### Dependency Hierarchy

Layers are organized by dependency. Lower layers depend on nothing above them:

```
┌──────────────────────────────────────┐
│ Presentation Layer                   │
│ (Combat UI, Party UI, Main Menu)     │
└────────────────┬─────────────────────┘
                 ↓
┌──────────────────────────────────────┐
│ Feature Layer                        │
│ (Capture, Party System, Pokedex,     │
│  DNA Alteration, Leveling)           │
└────────────────┬─────────────────────┘
                 ↓
┌──────────────────────────────────────┐
│ Gameplay Layer                       │
│ (Combat, Grid, Creatures, AI,        │
│  Encounters, Encounters)             │
└────────────────┬─────────────────────┘
                 ↓
┌──────────────────────────────────────┐
│ Core Layer                           │
│ (Game State Manager, Config Loader,  │
│  Save/Load, Events, Type Chart)      │
└──────────────────────────────────────┘
```

### Performance Budgets

| System | Budget | Notes |
|--------|--------|-------|
| **Frame Rate** | 60 FPS minimum | 16.67ms frame budget |
| **Combat Turn Resolution** | < 100ms | Instant for async operations (state changes), < 100ms for single attack resolution |
| **Grid Pathfinding** | < 50ms | A* with height; limit to 16x16 tile grid for MVP |
| **Type Effectiveness Lookup** | < 1ms | Static 2D array access |
| **Damage Calculation** | < 1ms | Per-hit formula is simple (no RNG per hit in combat log) |
| **Memory (Creatures)** | < 1 MB per active creature | All instances + metadata for 4-6 party creatures + 1-3 enemies |

## 4. Formulas

### Type Effectiveness Calculation

```
damageMultiplier = typeChart[attackerType, defenderType]
finalDamage = baseDamage * damageMultiplier * stab * terrain * weather * ...
```

Where `typeChart` is a static int[16, 16] array:
- 0 = immune (0x)
- 1 = not very effective (0.5x)
- 2 = neutral (1x)
- 3 = super effective (2x)

Example:
```csharp
// Thermal vs. Organic
float effectiveness = TypeChart.GetMultiplier(CreatureType.Thermal, CreatureType.Organic);
// Returns 3 (super effective, 2x multiplier)
```

### Save File Structure

Saves are JSON with this top-level schema:

```json
{
  "metadata": {
    "version": "1.0.0",
    "saveSlot": 0,
    "timestamp": "2026-04-04T14:30:00Z",
    "researcherName": "Alice",
    "playtimeSeconds": 3600
  },
  "gameSettings": {
    "difficulty": "Normal",
    "permadeathEnabled": false
  },
  "playerState": {
    "currentZoneId": "verdant-basin",
    "currentNodeId": "vb-05-trainer",
    "partyIds": ["creature-001", "creature-002", "creature-003"],
    "partyHPs": [45, 60, 38],
    "totalRP": 1250,
    "totalMoney": 500
  },
  "creatures": {
    "creature-001": {
      "speciesId": "emberfox",
      "level": 12,
      "currentHP": 45,
      "maxHP": 65,
      "experience": 850,
      "moveIds": ["flame-claw", "ember", "rest", "agility"],
      "movePPs": [8, 12, 8, 10],
      "dnaModIds": ["stat-boost-attack", "fire-affinity"],
      "instabilityValue": 15,
      "personalityTraitId": null,
      "affinity": 2,
      "scarIds": [],
      "captureLocationId": "vb-02-forest",
      "captureTimestamp": "2026-04-04T13:00:00Z"
    }
  },
  "pokedex": {
    "emberfox": { "researchTier": 2, "captureCount": 1, "battleCount": 5 },
    "aqualing": { "researchTier": 1, "captureCount": 0, "battleCount": 3 }
  },
  "mapState": {
    "verdant-basin": {
      "visitedNodes": ["vb-01-entry", "vb-02-forest", "vb-03-brook", "vb-05-trainer"],
      "completedEncounters": ["vb-02-forest", "vb-03-brook", "vb-05-trainer"],
      "ecosystemState": {
        "populations": {
          "emberfox": 45,
          "aqualing": 55,
          "bugling": 38
        },
        "conservationScore": 62
      }
    }
  }
}
```

## 5. Edge Cases

1. **Cross-Domain Save Compatibility**: If a save was created on a previous codebase version, the SaveLoadManager validates the `metadata.version` field. If older, it applies migration rules (e.g., if a creature type is no longer valid, remap it to a new type). If newer, it warns the player and prevents load.

2. **Config Lookup Failures**: If a creature ID or move ID referenced in a save doesn't exist in the current config set, SaveLoadManager logs a warning and skips that entry rather than failing the entire load. This allows balance patches to remove creatures without corrupting saves.

3. **Circular Event Dependencies**: If Event A triggers Event B, which triggers Event A, the system will spam events. Prevent this by documenting event flow in code comments and using guards like `if (!isProcessingEvent)`.

4. **UI Threading**: All Unity UI updates must happen on the main thread. Async operations (config loading, save file I/O) are queued and applied via `coroutines` or `async/await` patterns that marshal to main thread.

5. **Creature With No Valid Moves**: If a creature somehow has no valid moves (corrupted save or removed move), it defaults to a basic attack move (damage-only, no status) to prevent soft-locks.

## 6. Dependencies

This document depends on:
- **Game Concept** (`game-concept.md`) — defines vision, pillars, and core loop
- **Systems Index** (`systems-index.md`) — lists all 53 systems and dependencies
- **Individual System GDDs** — each system has its own detailed design doc with formulas and edge cases
- **Technical Preferences** (`.claude/docs/technical-preferences.md`) — naming conventions and allowed libraries
- **Coding Standards** (`.claude/docs/coding-standards.md`) — XML doc comments, test requirements, forbidden patterns

This document is referenced by:
- **All C# scripts** — must adhere to naming conventions and design principles
- **All configuration creators** — must follow the ScriptableObject patterns
- **All new system designers** — must follow the GDD template with 8 sections

## 7. Tuning Knobs

All tuning values are stored in ScriptableObjects. Key config assets:

| Asset | Path | Tunable Values |
|-------|------|-----------------|
| GameSettings | `Resources/Data/GameSettings.asset` | Master volume, VFX quality, combat speed multiplier, UI scale |
| CreatureConfig | `Resources/Data/Creatures/[SpeciesName].asset` | Base stats, move pool, catch rate, growth curve |
| MoveConfig | `Resources/Data/Moves/[MoveName].asset` | Power, accuracy, PP, type, category, effect |
| EncounterConfig | `Resources/Data/Encounters/[EncounterName].asset` | Creature pool, party composition, RP reward, DNA drop rates |
| TypeChart | `Resources/Data/TypeChart.asset` | Type effectiveness matrix (16x16) |
| MapZoneConfig | `Resources/Data/MapZones/[ZoneName].asset` | Node connectivity, recommended levels, difficulty scaling |

Script constants (rarely tuned, can be in code):
- `GridSystem.GRID_SIZE = 16` (tile width/height in units)
- `CombatController.TURN_TIMEOUT_SECONDS = 30` (max seconds for a player turn)
- `DamageCalculator.CRITICAL_MULTIPLIER = 1.5f` (crit damage scaling)
- `EcosystemManager.PREDATION_CYCLE_INTERVAL = 3` (encounters between predation checks)

## 8. Acceptance Criteria

1. **Project loads and compiles**: Unity Editor opens the project without errors. All scripts compile (no syntax errors, no missing references).

2. **Directory structure matches specification**: All folders exist. All core scripts are in their designated paths (Scripts/Core, Scripts/Gameplay, etc.). All config assets are in Resources/Data/ with correct subfolder organization.

3. **Naming conventions are applied consistently**: A random sample of 10 files shows correct class naming (PascalCase), field naming (_camelCase for private, camelCase for serialized), and method naming (PascalCase). No mixed conventions.

4. **Core layer systems are isolated**: GameStateManager, ConfigLoader, SaveLoadManager, EventSystem, and TypeChart have no dependencies on gameplay or presentation layers. They can be unit-tested in isolation.

5. **Events are the communication backbone**: Inter-system communication uses C# events, not method calls. A grep for `FindObjectOfType` (excluding singleton lookup) returns zero results.

6. **ScriptableObjects are the config source**: A random sample of gameplay values (creature HP, move power, type effectiveness) are read from ScriptableObjects at runtime, not hardcoded. No hardcoded balance numbers in gameplay code.

7. **One save/load cycle works**: Starting a new game, playing for 5 minutes (traveling map, fighting one encounter), saving, quitting, loading — the save file restores all state correctly (party, RP, map progress, Pokedex).

8. **All GDDs are internally consistent**: The 9 MVP GDDs (campaign-map, living-ecosystem, station-upgrade, combat-ui, combat-feedback, party-management-ui, settings, ui-shell, architecture-reference) cross-reference each other correctly and share consistent terminology, numbering, and structure (8 sections each).

9. **Documentation reflects code, not speculation**: A random check of 3 formulas in GDDs (e.g., damage multiplier, conservation score, level scaling) matches the actual code implementation.

10. **Agent coordination rules are enforced**: Changes to files outside an agent's designated directory require explicit approval. No unilateral cross-domain changes.
