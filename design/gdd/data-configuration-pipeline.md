# Data Configuration Pipeline

## 1. Overview

The Data Configuration Pipeline is the foundation layer for all game data in Gene Forge. It defines every enum used across systems, establishes the ScriptableObject schema for creature, move, body part, and encounter configs, and provides a singleton ConfigLoader that pre-loads all data assets into typed dictionaries at startup. All gameplay values live in ScriptableObjects under `Assets/Resources/Data/`; no constants are hardcoded in logic scripts. This system must be initialized before any other system runs.

## 2. Player Fantasy

The player never interacts with this system directly — there is no "data loading" screen or config menu. However, several enums defined here drive player-visible feedback: `InstabilityTier` controls the instability meter UI and disobey warnings, `PokedexTier` gates the progressive information disclosure that serves Pillar 3 (Discovery Through Play), and `Rarity` colors creature and part acquisition moments. The system's purpose is to ensure that every creature, move, and part feel like a hand-crafted entry in a living research database — each with a unique ID, rich metadata, and consistent structure that downstream systems can rely on without fragile string matching or magic numbers.

## 3. Detailed Rules

### 3.1 Enum Definitions

All enums live in `Assets/Scripts/Core/Enums.cs`.

```csharp
namespace GeneForge.Core
{
    /// <summary>
    /// Primary genome type for creatures and moves.
    /// 14 types in 3 tiers: Standard (8), Extended (4), Apex (2).
    /// </summary>
    public enum CreatureType
    {
        None = 0,
        Thermal = 1,
        Aqua = 2,
        Organic = 3,
        Bioelectric = 4,
        Cryo = 5,
        Mineral = 6,
        Toxic = 7,
        Neural = 8,
        Ferro = 9,
        Kinetic = 10,
        Aero = 11,
        Sonic = 12,
        Ark = 13,
        Blight = 14
    }

    /// <summary>
    /// Determines stat pairing and grid behavior for damaging moves.
    /// Replaces the old Physical/Special/Status category system.
    /// </summary>
    public enum DamageForm
    {
        None = 0,       // Status moves — no damage form
        Physical = 1,   // ATK vs DEF, melee range 1-2, blocked by walls/cover
        Energy = 2,     // ATK vs SPD, ranged 3-5, requires LoS, cover reduces 50%
        Bio = 3         // ACC vs DEF, mid-range 2-3, ignores cover, no height bonus
    }

    /// <summary>Body slot positions where parts can be equipped.</summary>
    public enum BodySlot
    {
        Head = 0,
        Back = 1,
        Arms = 2,
        Tail = 3,
        Legs = 4,
        Torso = 5,
        Aura = 6        // Non-physical overlay slot (glow, aura effects)
    }

    /// <summary>Category grouping for body parts (used for synergy set detection).</summary>
    public enum PartCategory
    {
        Offensive = 0,   // Claws, fangs, horns, stingers
        Defensive = 1,   // Shells, scales, carapace, plating
        Mobility = 2,    // Wings, fins, treads, springs
        Utility = 3,     // Glands, sensors, roots, generators
        Cosmetic = 4     // Auras, color patches, glow marks
    }

    /// <summary>Drop and encounter rarity for creatures and parts.</summary>
    public enum Rarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    /// <summary>Broad body shape archetype — determines available BodySlots.</summary>
    public enum BodyArchetype
    {
        Bipedal = 0,      // Head, Back, Arms (x2), Tail, Legs, Torso, Aura
        Quadruped = 1,    // Head, Back, Legs (x4), Tail, Torso, Aura
        Serpentine = 2,   // Head, Back (x3), Tail, Aura
        Avian = 3,        // Head, Back (wings), Arms (talons), Tail, Legs, Aura
        Amorphous = 4     // Torso (x3), Aura (x2) — no fixed slots
    }

    /// <summary>Instability tier for UI display and triggering instability events.</summary>
    public enum InstabilityTier
    {
        Stable = 0,
        Volatile = 1,
        Unstable = 2,
        Critical = 3,
        Breakdown = 4
    }

    /// <summary>
    /// Instability tier boundary helper. Threshold values are data-driven via
    /// GameSettings.asset (see Section 3.4). Values are cached on first access
    /// after ConfigLoader initializes to avoid per-call null checks in hot paths.
    /// Fallback defaults are used if GameSettings is unavailable (EditMode tests).
    /// </summary>
    public static class InstabilityThresholds
    {
        // Fallback defaults — used when ConfigLoader.Settings is null
        const int DefaultMax = 100;
        const int DefaultVolatileMin = 25;
        const int DefaultUnstableMin = 50;
        const int DefaultCriticalMin = 75;
        const int DefaultBreakdownMin = 100;

        // Cached values — set once during Initialize(), read many times in hot paths
        static int _max = DefaultMax;
        static int _volatileMin = DefaultVolatileMin;
        static int _unstableMin = DefaultUnstableMin;
        static int _criticalMin = DefaultCriticalMin;
        static int _breakdownMin = DefaultBreakdownMin;

        public static int Max => _max;
        public static int VolatileMin => _volatileMin;
        public static int UnstableMin => _unstableMin;
        public static int CriticalMin => _criticalMin;
        public static int BreakdownMin => _breakdownMin;

        /// <summary>
        /// Called by ConfigLoader.Initialize() after GameSettings is loaded.
        /// Caches threshold values for zero-cost access in hot paths.
        /// </summary>
        public static void CacheFromSettings(GameSettings settings)
        {
            if (settings == null) return; // Keep fallback defaults
            _max = settings.InstabilityMax;
            _volatileMin = settings.InstabilityVolatileMin;
            _unstableMin = settings.InstabilityUnstableMin;
            _criticalMin = settings.InstabilityCriticalMin;
            _breakdownMin = settings.InstabilityBreakdownMin;
        }

        /// <summary>Returns the InstabilityTier for a given instability value (0-100).</summary>
        public static InstabilityTier GetTier(int instability)
        {
            if (instability >= _breakdownMin) return InstabilityTier.Breakdown;
            if (instability >= _criticalMin) return InstabilityTier.Critical;
            if (instability >= _unstableMin) return InstabilityTier.Unstable;
            if (instability >= _volatileMin) return InstabilityTier.Volatile;
            return InstabilityTier.Stable;
        }
    }

    /// <summary>Target pattern for move application.</summary>
    public enum TargetType
    {
        Single = 0,       // One enemy
        Adjacent = 1,     // All enemies adjacent to caster
        AoE = 2,          // All enemies on grid
        Self = 3,         // Caster only
        AllAllies = 4,    // All friendly creatures
        SingleAlly = 5,   // One friendly creature
        Line = 6          // All creatures in a line from caster
    }

    /// <summary>Current state of a status effect on a creature.</summary>
    public enum StatusEffect
    {
        None = 0,
        Burn = 1,         // -1/16 max HP per round, halves Physical ATK
        Freeze = 2,       // Cannot act; thaws on fire hit or after 2-4 rounds
        Paralysis = 3,    // 25% chance to skip turn; halves SPD
        Poison = 4,       // -1/8 max HP per round
        Sleep = 5,        // Cannot act for 2-5 rounds
        Confusion = 6,    // 33% chance to hit self for 40 base power
        Taunt = 7,        // Forces use of offensive moves only
        Stealth = 8       // Reduces threat score to near zero
    }

    /// <summary>Broad behavior archetype for wild creature AI.</summary>
    public enum AIPersonalityType
    {
        Predator = 0,     // Prefers lowest-HP target
        Territorial = 1,  // Prefers closest target
        Defensive = 2,    // Retreats when below 30% HP
        Berserker = 3,    // Always attacks highest-ATK threat
        Support = 4,      // Prioritizes buffing or healing allies
        Trainer = 5       // Uses scored decision-making per trainer config
    }

    /// <summary>Terrain tile type — affects terrain synergy and move interactions.</summary>
    public enum TerrainType
    {
        Neutral = 0,
        Thermal = 1,      // Lava — synergy with Thermal type
        Aqua = 2,         // Water — synergy with Aqua type
        Organic = 3,      // Forest — synergy with Organic type
        Cryo = 4,         // Ice — synergy with Cryo type
        Mineral = 5,      // Rock — synergy with Mineral type
        Kinetic = 6,      // Sand — synergy with Kinetic type
        Neural = 7,       // Crystal — synergy with Neural type
        Toxic = 8,        // Toxic — synergy with Toxic type
        Hazard = 9,       // Generic damage tile (set by abilities)
        Difficult = 10,   // Costs +1 movement
        Elevated = 11     // Height advantage source tile
    }

    /// <summary>Research tier for Pokedex entry completeness.</summary>
    public enum PokedexTier
    {
        Unseen = 0,
        Silhouette = 1,   // Spotted in wild
        BasicProfile = 2, // Fought once
        FullProfile = 3,  // Captured
        Complete = 4      // 10+ battles + all DNA recipes discovered
    }

    /// <summary>XP growth rate curve identifier.</summary>
    public enum GrowthCurve
    {
        Fast = 0,         // Reaches Lv50 at ~500k XP
        Medium = 1,       // Reaches Lv50 at ~750k XP
        Slow = 2,         // Reaches Lv50 at ~1M XP
        Erratic = 3       // Nonlinear: fast early, slow late
    }

    /// <summary>Personality behavioral trait equipped on a creature.</summary>
    public enum PersonalityTrait
    {
        None = 0,
        Aggressive = 1,
        Cautious = 2,
        Loyal = 3,
        Feral = 4,
        Curious = 5,
        Territorial = 6
    }
}
```

### 3.2 ScriptableObject Base

All *collection-based* config ScriptableObjects inherit from `ConfigBase`. `GameSettings` (Section 3.4) inherits directly from `ScriptableObject` as it is a singleton, not a registry entry.

```csharp
namespace GeneForge.Core
{
    /// <summary>Base class for all Gene Forge config ScriptableObjects.</summary>
    public abstract class ConfigBase : ScriptableObject
    {
        [SerializeField] string id;          // kebab-case unique identifier
        [SerializeField] string displayName; // Human-readable name

        public string Id => id;
        public string DisplayName => displayName;
    }
}
```

### 3.3 ConfigLoader

`ConfigLoader` is a MonoBehaviour on the persistent Boot scene object. It loads all `Resources/Data/` assets at startup and caches them in static typed dictionaries.

```csharp
namespace GeneForge.Core
{
    /// <summary>
    /// Singleton loader for all ScriptableObject config data.
    /// Must be initialized before any system queries creature, move, or part data.
    /// Singleton pattern approved under ADR-003 (see .claude/docs/technical-preferences.md).
    /// </summary>
    public class ConfigLoader : MonoBehaviour
    {
        public static ConfigLoader Instance { get; private set; }

        // Typed registries — read-only after initialization
        public static IReadOnlyDictionary<string, CreatureConfig> Creatures => _creatures;
        public static IReadOnlyDictionary<string, MoveConfig> Moves => _moves;
        public static IReadOnlyDictionary<string, BodyPartConfig> BodyParts => _bodyParts;
        public static IReadOnlyDictionary<string, StatusEffectConfig> StatusEffects => _statusEffects;
        public static IReadOnlyDictionary<string, EncounterConfig> Encounters => _encounters;
        public static IReadOnlyDictionary<string, AIPersonalityConfig> AIPersonalities => _aiPersonalities;

        static readonly Dictionary<string, CreatureConfig> _creatures = new();
        static readonly Dictionary<string, MoveConfig> _moves = new();
        static readonly Dictionary<string, BodyPartConfig> _bodyParts = new();
        static readonly Dictionary<string, StatusEffectConfig> _statusEffects = new();
        static readonly Dictionary<string, EncounterConfig> _encounters = new();
        static readonly Dictionary<string, AIPersonalityConfig> _aiPersonalities = new();

        static bool _initialized;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        public static void Initialize()
        {
            if (_initialized) return;
            LoadAll<CreatureConfig>("Data/Creatures", _creatures);
            LoadAll<MoveConfig>("Data/Moves", _moves);
            LoadAll<BodyPartConfig>("Data/BodyParts", _bodyParts);
            LoadAll<StatusEffectConfig>("Data/StatusEffects", _statusEffects);
            LoadAll<EncounterConfig>("Data/Encounters", _encounters);
            LoadAll<AIPersonalityConfig>("Data/AIPersonalities", _aiPersonalities);
            // GameSettings singleton loaded here — see Section 3.4 for code and schema
            _initialized = true;
        }

        static void LoadAll<T>(string resourcePath, Dictionary<string, T> registry)
            where T : ConfigBase
        {
            var assets = Resources.LoadAll<T>(resourcePath);
            foreach (var asset in assets)
            {
                if (string.IsNullOrEmpty(asset.Id))
                {
                    Debug.LogError($"[ConfigLoader] {typeof(T).Name} at {resourcePath} has empty ID.");
                    continue;
                }
                if (!registry.TryAdd(asset.Id, asset))
                    Debug.LogWarning($"[ConfigLoader] Duplicate ID '{asset.Id}' in {typeof(T).Name}.");
            }
            Debug.Log($"[ConfigLoader] Loaded {registry.Count} {typeof(T).Name} assets.");
        }

        /// <summary>
        /// Generic lookup — internal/test use. Prefer typed methods below.
        /// </summary>
        static T Get<T>(IReadOnlyDictionary<string, T> registry, string id)
            where T : ConfigBase
        {
            if (registry.TryGetValue(id, out var config)) return config;
            Debug.LogError($"[ConfigLoader] Config not found: '{id}' in {typeof(T).Name} registry.");
            return null;
        }

        // Typed convenience methods — prevent wrong-registry bugs at call sites
        public static CreatureConfig GetCreature(string id) => Get(_creatures, id);
        public static MoveConfig GetMove(string id) => Get(_moves, id);
        public static BodyPartConfig GetBodyPart(string id) => Get(_bodyParts, id);
        public static StatusEffectConfig GetStatusEffect(string id) => Get(_statusEffects, id);
        public static EncounterConfig GetEncounter(string id) => Get(_encounters, id);
        public static AIPersonalityConfig GetAIPersonality(string id) => Get(_aiPersonalities, id);
    }
}
```

### 3.4 GameSettings Singleton

`GameSettings.asset` is a single ScriptableObject at `Assets/Resources/Data/GameSettings.asset`. Unlike collection-based configs, it is loaded as a singleton via `Resources.Load` and exposed as a static property on ConfigLoader:

```csharp
        // Singleton settings — not a collection, loaded separately
        public static GameSettings Settings { get; private set; }

        // Inside Initialize():
        Settings = Resources.Load<GameSettings>("Data/GameSettings");
        if (Settings == null)
            Debug.LogError("[ConfigLoader] GameSettings.asset not found at Data/GameSettings.");
```

Other systems access it via `ConfigLoader.Settings.PropertyName`.

#### GameSettings Field Schema

```csharp
namespace GeneForge.Core
{
    /// <summary>
    /// Global tuning values that span multiple systems.
    /// One instance: Assets/Resources/Data/GameSettings.asset
    /// </summary>
    [CreateAssetMenu(menuName = "GeneForge/GameSettings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Capture")]
        [SerializeField] float baseCaptureRate = 0.3f;        // Base probability before modifiers
        [SerializeField] float hpWeightMultiplier = 1.5f;     // How much low HP improves capture
        [SerializeField] float statusCaptureBonus = 0.15f;    // Flat bonus per active status effect
        [SerializeField] int maxCaptureAttempts = 3;           // Per encounter cap

        [Header("XP & Leveling")]
        [SerializeField] int maxLevel = 50;                    // Level cap for all creatures
        [SerializeField] float trainerXpMultiplier = 1.5f;     // XP bonus for trainer battles vs wild
        [SerializeField] float xpShareRatio = 0.5f;            // XP share for non-participating party members

        [Header("Combat")]
        [SerializeField] int minDamage = 1;                    // Floor on all damage after calculation
        [SerializeField] float stabMultiplier = 1.5f;          // Same-Type Attack Bonus
        [SerializeField] float critMultiplier = 1.5f;          // Critical hit damage multiplier
        [SerializeField] float critBaseChance = 0.0625f;       // 1/16 base crit rate
        [SerializeField] float heightBonusPerLevel = 0.1f;     // +10% damage per height level above target
        [SerializeField] float flankingBonus = 0.25f;          // +25% damage when flanking

        [Header("DNA & Instability")]
        [SerializeField] int instabilityMax = 100;             // Upper bound for instability meter
        [SerializeField] int instabilityVolatileMin = 25;      // Stable → Volatile boundary
        [SerializeField] int instabilityUnstableMin = 50;      // Volatile → Unstable boundary
        [SerializeField] int instabilityCriticalMin = 75;      // Unstable → Critical boundary
        [SerializeField] int instabilityBreakdownMin = 100;    // Critical → Breakdown boundary
        [SerializeField] float instabilityDecayPerRest = 5f;   // Points recovered per research station rest
        [SerializeField] float disobeyBaseChance = 0.1f;       // Base disobey chance (scaled per tier by DNA Alteration System)
        [SerializeField] float breakthroughChance = 0.05f;     // Chance of positive instability event

        [Header("Ecosystem")]
        [SerializeField] int conservationBonusThreshold = 80;  // Min conservation score for rare spawns
        [SerializeField] float migrationCycleHours = 2f;       // Real-time hours per migration phase

        // Public read-only accessors
        public float BaseCaptureRate => baseCaptureRate;
        public float HpWeightMultiplier => hpWeightMultiplier;
        public float StatusCaptureBonus => statusCaptureBonus;
        public int MaxCaptureAttempts => maxCaptureAttempts;
        public int MaxLevel => maxLevel;
        public float TrainerXpMultiplier => trainerXpMultiplier;
        public float XpShareRatio => xpShareRatio;
        public int MinDamage => minDamage;
        public float StabMultiplier => stabMultiplier;
        public float CritMultiplier => critMultiplier;
        public float CritBaseChance => critBaseChance;
        public float HeightBonusPerLevel => heightBonusPerLevel;
        public float FlankingBonus => flankingBonus;
        public int InstabilityMax => instabilityMax;
        public int InstabilityVolatileMin => instabilityVolatileMin;
        public int InstabilityUnstableMin => instabilityUnstableMin;
        public int InstabilityCriticalMin => instabilityCriticalMin;
        public int InstabilityBreakdownMin => instabilityBreakdownMin;
        public float InstabilityDecayPerRest => instabilityDecayPerRest;
        public float DisobeyBaseChance => disobeyBaseChance;
        public float BreakthroughChance => breakthroughChance;
        public int ConservationBonusThreshold => conservationBonusThreshold;
        public float MigrationCycleHours => migrationCycleHours;
    }
}
```

**Consumed by**: Damage & Health System (minDamage, stabMultiplier, critMultiplier, heightBonusPerLevel, flankingBonus), Capture System (baseCaptureRate, hpWeightMultiplier, statusCaptureBonus, maxCaptureAttempts), Leveling System (maxLevel, trainerXpMultiplier, xpShareRatio), DNA Alteration System (instabilityDecayPerRest, disobeyBaseChance, breakthroughChance), Living Ecosystem (conservationBonusThreshold, migrationCycleHours).

**Source-of-truth rule**: Default values for combat multipliers are sourced from `damage-health-system.md` Section 4. Default values for capture rates are sourced from `capture-system.md`. If values diverge between GameSettings defaults and the authoritative GDD, the system-specific GDD is authoritative; update GameSettings defaults to match.

**Post-MVP note**: If multiple designers need to tune different headers simultaneously, `GameSettings` may be split into domain-specific SOs (e.g., `CombatSettings.asset`, `DnaSettings.asset`, `EcosystemSettings.asset`) to avoid merge contention. For MVP with a single designer, one asset is sufficient.

### 3.5 Config Lookup Failure Policy

`ConfigLoader.Get<T>()` returns `null` when a config ID is not found. Callers must handle this consistently:

| Caller Context | Expected Behavior | Rationale |
|---------------|-------------------|-----------|
| Combat-critical lookup (creature, move, body part) | Log error, skip the action, do not crash | A missing config must not freeze combat; the player can continue the fight |
| AI personality or encounter lookup | Log error, fall back to a default config ID defined by the consuming system's GDD | AI must always have a valid behavior; the consuming system is responsible for guaranteeing its fallback asset exists |
| UI / cosmetic lookup (display name, icon) | Log warning, display placeholder text `"[MISSING]"` | Non-blocking; visual glitch is preferable to a crash |
| EditMode test | Assert not null — test should fail explicitly | Missing test data is a test authoring bug, not a runtime concern |

**Rule**: No caller may silently ignore a `null` return. Every `Get<T>()` call site must either null-check and handle, or wrap in a test assertion. Code review should flag unguarded `Get<T>()` calls.

### 3.6 EditMode Test Support

`ConfigLoader.Initialize()` is a static method and can be called safely from EditMode tests without instantiating the MonoBehaviour or loading a scene. Test assets placed in `Assets/Resources/Data/` (or a test-specific `Resources/TestData/` folder) will be loaded normally.

For designer iteration, an editor-only reinitialize method allows reloading all configs without restarting Play mode:

```csharp
#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: clears all registries and reloads from disk.
        /// Call from a custom editor menu or Inspector button.
        /// </summary>
        public static void Reinitialize()
        {
            _creatures.Clear();
            _moves.Clear();
            _bodyParts.Clear();
            _statusEffects.Clear();
            _encounters.Clear();
            _aiPersonalities.Clear();
            _initialized = false;
            Initialize();
            Debug.Log("[ConfigLoader] Reinitialized all config registries.");
        }
#endif
```

### 3.7 Asset Placement Rules

- All `CreatureConfig` assets: `Assets/Resources/Data/Creatures/`
- All `MoveConfig` assets: `Assets/Resources/Data/Moves/`
- All `BodyPartConfig` assets: `Assets/Resources/Data/BodyParts/`
- All `StatusEffectConfig` assets: `Assets/Resources/Data/StatusEffects/`
- All `EncounterConfig` assets: `Assets/Resources/Data/Encounters/`
- All `AIPersonalityConfig` assets: `Assets/Resources/Data/AIPersonalities/`
- Asset file names match the `id` field in PascalCase (e.g., `Emberfox.asset` has id `emberfox`)

### 3.8 ID Format

All config IDs use kebab-case: lowercase words separated by hyphens. Examples:
- Creatures: `emberfox`, `thorn-slug`, `voltfin`
- Moves: `flame-claw`, `aqua-bolt`, `root-bind`
- Parts: `fire-glands`, `crystal-shell`, `feathered-wings`

### 3.9 Initialization Order

The Boot scene initializes systems in this order:
1. `ConfigLoader.Initialize()` — all data loaded into memory
2. `TypeChart.Initialize()` — static matrix built from enum counts
3. `GameStateManager` — begins state machine, transitions to MainMenu

No other system may query `ConfigLoader` registries before step 1 completes.

**MVP asset budget estimate**: ~30 creatures, ~25 moves, ~20 body parts, ~8 status effects, ~15 encounters, ~6 AI personalities (~104 ScriptableObjects total). Synchronous `Resources.LoadAll` across 6 directories occurs during the Boot scene splash screen, so it is exempt from the 16.67ms per-frame budget. If asset count exceeds ~500, profile boot time and consider staggered loading or early Addressables migration.

### 3.10 Post-MVP: Addressables Migration

Per ADR-001, MVP uses `Resources.Load` for simplicity. Post-MVP, ConfigLoader will migrate to Addressables v2 for async loading, reducing startup memory pressure and enabling downloadable content updates. This migration will require:
- Converting `Initialize()` to an async method (e.g., `async UniTask InitializeAsync()`)
- Replacing `Resources.LoadAll<T>` with `Addressables.LoadAssetsAsync<T>`
- Adding loading progress callbacks for splash/loading screens
- Updating all callers to await initialization before querying registries
- The `Reinitialize()` editor method will need to release Addressable handles before reloading

## 4. Formulas

No mathematical formulas in this system. All computation is dictionary lookup O(1).

## 5. Edge Cases

| Situation | Behavior |
|-----------|----------|
| Duplicate config ID | Log warning, first-loaded asset wins, second is discarded |
| Empty ID field on asset | Log error, asset skipped, not registered |
| Config queried before `Initialize()` | Returns null, logs error with config ID and type name |
| `Resources.LoadAll` returns empty | Log warning, system continues with empty registry |
| Enum value added without updating type chart | TypeChart logs dimension mismatch warning at init |
| Asset deleted from Resources while game running | Registry retains stale reference; reloading requires restart |
| `GameSettings.asset` missing from Resources | Log error, `Settings` remains null; `InstabilityThresholds` and other consumers fall back to hardcoded defaults |
| `Reinitialize()` called during Play mode | All registries cleared and reloaded; downstream systems holding cached references remain valid (ScriptableObject references don't change on reload) |

## 6. Dependencies

| Dependency | Direction | Notes |
|------------|-----------|-------|
| Unity `Resources` API | External | All assets must be under `Resources/Data/` |
| `CreatureConfig` | Downstream | Defined in creature-database.md |
| `MoveConfig` | Downstream | Defined in move-database.md |
| `BodyPartConfig` | Downstream | Defined in body-part-system.md |
| `StatusEffectConfig` | Downstream | Defined in status-effect-system.md |
| `EncounterConfig` | Downstream | Defined in encounter-system.md |
| `AIPersonalityConfig` | Downstream | Defined in ai-decision-system.md |
| `GameSettings` | Internal | Singleton ScriptableObject for global tuning values |
| `TypeChart` | Downstream | Consumes `CreatureType` enum count |
| `GameStateManager` | Downstream | Must not start until ConfigLoader is done |

## 7. Tuning Knobs

| Parameter | Location | Default | Notes |
|-----------|----------|---------|-------|
| `ResourceBasePath` | `ConfigLoader.cs` constant | `"Data/"` | Root path under Resources/ |
| `CreaturesPath` | `ConfigLoader.cs` constant | `"Data/Creatures"` | Subfolder for creature assets |
| `MovesPath` | `ConfigLoader.cs` constant | `"Data/Moves"` | Subfolder for move assets |
| `BodyPartsPath` | `ConfigLoader.cs` constant | `"Data/BodyParts"` | Subfolder for part assets |
| `EncountersPath` | `ConfigLoader.cs` constant | `"Data/Encounters"` | Subfolder for encounter assets |
| `AIPersonalitiesPath` | `ConfigLoader.cs` constant | `"Data/AIPersonalities"` | Subfolder for AI personality assets |
| `instabilityMax` | `GameSettings.asset` | `100` | Upper bound for instability meter |
| `instabilityVolatileMin` | `GameSettings.asset` | `25` | Tier boundary: Stable → Volatile |
| `instabilityUnstableMin` | `GameSettings.asset` | `50` | Tier boundary: Volatile → Unstable |
| `instabilityCriticalMin` | `GameSettings.asset` | `75` | Tier boundary: Unstable → Critical |
| `instabilityBreakdownMin` | `GameSettings.asset` | `100` | Tier boundary: Critical → Breakdown |

## 8. Acceptance Criteria

- [ ] `Enums.cs` compiles with zero errors and all enums listed in Section 3.1 present
- [ ] `ConfigLoader.Initialize()` completes before first `GameStateManager` transition
- [ ] All `CreatureConfig` assets in `Resources/Data/Creatures/` load without error logs
- [ ] All `MoveConfig` assets in `Resources/Data/Moves/` load without error logs
- [ ] All `BodyPartConfig` assets in `Resources/Data/BodyParts/` load without error logs
- [ ] All `StatusEffectConfig` assets in `Resources/Data/StatusEffects/` load without error logs
- [ ] All `EncounterConfig` assets in `Resources/Data/Encounters/` load without error logs
- [ ] All `AIPersonalityConfig` assets in `Resources/Data/AIPersonalities/` load without error logs
- [ ] `GameSettings.asset` loads successfully and `ConfigLoader.Settings` is non-null
- [ ] Duplicate ID triggers exactly one warning log and second asset is not in registry
- [ ] Empty ID asset triggers exactly one error log and is not in registry
- [ ] `ConfigLoader.Get<T>()` returns null and logs error for unknown ID
- [ ] EditMode test: load test assets, verify registry count matches asset count
- [ ] EditMode test: duplicate ID test confirms first-loaded wins
- [ ] EditMode test: missing `GameSettings.asset` triggers error log and `ConfigLoader.Settings` is null
- [ ] EditMode test: `InstabilityThresholds.GetTier()` returns correct tiers using fallback defaults when `GameSettings` is null
- [ ] EditMode test: after `Reinitialize()`, registry contents match a fresh `Initialize()` call
- [ ] PlayMode test: Boot scene loads, `ConfigLoader.Instance` is non-null, all registries populated, `GameStateManager` transitions to MainMenu without errors
- [ ] No hardcoded string IDs outside config asset definition files
