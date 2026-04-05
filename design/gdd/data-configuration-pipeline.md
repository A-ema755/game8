# Data Configuration Pipeline

## 1. Overview

The Data Configuration Pipeline is the foundation layer for all game data in Gene Forge. It defines every enum used across systems, establishes the ScriptableObject schema for creature, move, body part, and encounter configs, and provides a singleton ConfigLoader that pre-loads all data assets into typed dictionaries at startup. All gameplay values live in ScriptableObjects under `Assets/Resources/Data/`; no constants are hardcoded in logic scripts. This system must be initialized before any other system runs.

## 2. Player Fantasy

The player never sees this system directly. Its purpose is to ensure that every creature, move, and part feel like a hand-crafted entry in a living research database — each with a unique ID, rich metadata, and consistent structure that downstream systems can rely on without fragile string matching or magic numbers.

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
        Stable = 0,       // 0-24: no effects
        Volatile = 1,     // 25-49: minor stat swings possible
        Unstable = 2,     // 50-74: disobey chance, random bonuses
        Critical = 3,     // 75-99: frequent disobey, possible ally attack
        Breakdown = 4     // 100: guaranteed disobey each turn, visual breakdown
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

All config ScriptableObjects inherit from `ConfigBase`:

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
    /// </summary>
    public class ConfigLoader : MonoBehaviour
    {
        public static ConfigLoader Instance { get; private set; }

        // Typed registries — read-only after initialization
        public static IReadOnlyDictionary<string, CreatureConfig> Creatures => _creatures;
        public static IReadOnlyDictionary<string, MoveConfig> Moves => _moves;
        public static IReadOnlyDictionary<string, BodyPartConfig> BodyParts => _bodyParts;
        public static IReadOnlyDictionary<string, StatusEffectConfig> StatusEffects => _statusEffects;

        static readonly Dictionary<string, CreatureConfig> _creatures = new();
        static readonly Dictionary<string, MoveConfig> _moves = new();
        static readonly Dictionary<string, BodyPartConfig> _bodyParts = new();
        static readonly Dictionary<string, StatusEffectConfig> _statusEffects = new();

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
        /// Safe lookup with error logging. Returns null if not found.
        /// </summary>
        public static T Get<T>(IReadOnlyDictionary<string, T> registry, string id)
            where T : ConfigBase
        {
            if (registry.TryGetValue(id, out var config)) return config;
            Debug.LogError($"[ConfigLoader] Config not found: '{id}' in {typeof(T).Name} registry.");
            return null;
        }
    }
}
```

### 3.4 Asset Placement Rules

- All `CreatureConfig` assets: `Assets/Resources/Data/Creatures/`
- All `MoveConfig` assets: `Assets/Resources/Data/Moves/`
- All `BodyPartConfig` assets: `Assets/Resources/Data/BodyParts/`
- All `StatusEffectConfig` assets: `Assets/Resources/Data/StatusEffects/`
- Asset file names match the `id` field in PascalCase (e.g., `Emberfox.asset` has id `emberfox`)

### 3.5 ID Format

All config IDs use kebab-case: lowercase words separated by hyphens. Examples:
- Creatures: `emberfox`, `thorn-slug`, `voltfin`
- Moves: `flame-claw`, `aqua-bolt`, `root-bind`
- Parts: `fire-glands`, `crystal-shell`, `feathered-wings`

### 3.6 Initialization Order

The Boot scene initializes systems in this order:
1. `ConfigLoader.Initialize()` — all data loaded into memory
2. `TypeChart.Initialize()` — static matrix built from enum counts
3. `GameStateManager` — begins state machine, transitions to MainMenu

No other system may query `ConfigLoader` registries before step 1 completes.

## 4. Formulas

No mathematical formulas in this system. All computation is dictionary lookup O(1).

## 5. Edge Cases

| Situation | Behavior |
|-----------|----------|
| Duplicate config ID | Log warning, first-loaded asset wins, second is discarded |
| Empty ID field on asset | Log error, asset skipped, not registered |
| Config queried before `Initialize()` | Returns null, logs error with caller info |
| `Resources.LoadAll` returns empty | Log warning, system continues with empty registry |
| Enum value added without updating type chart | TypeChart logs dimension mismatch warning at init |
| Asset deleted from Resources while game running | Registry retains stale reference; reloading requires restart |

## 6. Dependencies

| Dependency | Direction | Notes |
|------------|-----------|-------|
| Unity `Resources` API | External | All assets must be under `Resources/Data/` |
| `CreatureConfig` | Downstream | Defined in creature-database.md |
| `MoveConfig` | Downstream | Defined in move-database.md |
| `BodyPartConfig` | Downstream | Defined in body-part-system.md |
| `StatusEffectConfig` | Downstream | Defined per status effect data |
| `TypeChart` | Downstream | Consumes `CreatureType` enum count |
| `GameStateManager` | Downstream | Must not start until ConfigLoader is done |

## 7. Tuning Knobs

| Parameter | Location | Default | Notes |
|-----------|----------|---------|-------|
| `ResourceBasePath` | `ConfigLoader.cs` constant | `"Data/"` | Root path under Resources/ |
| `CreaturesPath` | `ConfigLoader.cs` constant | `"Data/Creatures"` | Subfolder for creature assets |
| `MovesPath` | `ConfigLoader.cs` constant | `"Data/Moves"` | Subfolder for move assets |
| `BodyPartsPath` | `ConfigLoader.cs` constant | `"Data/BodyParts"` | Subfolder for part assets |
| `MaxInstability` | `Enums.cs` constant | `100` | Upper bound for instability meter |
| `InstabilityVolatileThreshold` | `Enums.cs` | `25` | Tier boundary: Stable → Volatile |
| `InstabilityUnstableThreshold` | `Enums.cs` | `50` | Tier boundary: Volatile → Unstable |
| `InstabilityCriticalThreshold` | `Enums.cs` | `75` | Tier boundary: Unstable → Critical |
| `InstabilityBreakdownThreshold` | `Enums.cs` | `100` | Tier boundary: Critical → Breakdown |

## 8. Acceptance Criteria

- [ ] `Enums.cs` compiles with zero errors and all enums listed in Section 3.1 present
- [ ] `ConfigLoader.Initialize()` completes before first `GameStateManager` transition
- [ ] All `CreatureConfig` assets in `Resources/Data/Creatures/` load without error logs
- [ ] All `MoveConfig` assets in `Resources/Data/Moves/` load without error logs
- [ ] Duplicate ID triggers exactly one warning log and second asset is not in registry
- [ ] Empty ID asset triggers exactly one error log and is not in registry
- [ ] `ConfigLoader.Get<T>()` returns null and logs error for unknown ID
- [ ] EditMode test: load test assets, verify registry count matches asset count
- [ ] EditMode test: duplicate ID test confirms first-loaded wins
- [ ] No hardcoded string IDs outside config asset definition files
