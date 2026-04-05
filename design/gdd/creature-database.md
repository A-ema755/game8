# Creature Database

## 1. Overview

The Creature Database defines the `CreatureConfig` ScriptableObject schema — the immutable blueprint for every species in Gene Forge. Each config captures the species' elemental types, base stats, learnable move pool, body archetype and available slots, signature part, catch rate, XP growth curve, habitat zones, and rarity. The database is loaded at startup by `ConfigLoader` and never modified at runtime. Runtime creature state (HP, level, DNA mods, etc.) lives in `CreatureInstance`, not here. Eight MVP creatures are defined below as reference examples spanning the Verdant Basin starting zone.

## 2. Player Fantasy

Flipping through the creature database feels like reading a field researcher's journal. Each species has a distinct ecological niche, a visual identity implied by its archetype and type, and mechanical hooks that make it interesting to build around. The player immediately understands why a Grass/Water dual-type slug with a slow growth curve and high DEF is a different strategic choice from a fast, fragile Fire creature with an aggressive signature part.

## 3. Detailed Rules

### 3.1 CreatureConfig ScriptableObject

```csharp
namespace GeneForge.Creatures
{
    /// <summary>
    /// Immutable species blueprint. One asset per species in Resources/Data/Creatures/.
    /// Do not modify at runtime — all runtime state lives in CreatureInstance.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCreature", menuName = "GeneForge/Creature Config")]
    public class CreatureConfig : ConfigBase
    {
        [Header("Identity")]
        [SerializeField] CreatureType primaryType;
        [SerializeField] CreatureType secondaryType;   // None if single-type
        [SerializeField] Rarity rarity;
        [SerializeField] BodyArchetype bodyArchetype;

        [Header("Base Stats")]
        [SerializeField] BaseStats baseStats;

        [Header("Move Pool")]
        [SerializeField] List<LevelMoveEntry> movePool; // Moves learnable by level

        [Header("Body")]
        [SerializeField] List<BodySlot> availableSlots;  // Determined by archetype
        [SerializeField] string signaturePartId;          // Part config ID; strongest on this species

        [Header("Progression")]
        [SerializeField] GrowthCurve growthCurve;
        [SerializeField] int baseXpYield;       // XP given to opponent on defeat
        [SerializeField] int catchRate;         // 0-255; higher = easier to catch

        [Header("World")]
        [SerializeField] List<string> habitatZoneIds;   // Zones where this species spawns
        [SerializeField] CreatureType terrainSynergyType; // Type of terrain tile for synergy bonus

        // ── Properties ──────────────────────────────────────────────────
        public CreatureType PrimaryType      => primaryType;
        public CreatureType SecondaryType    => secondaryType;
        public Rarity Rarity                 => rarity;
        public BodyArchetype BodyArchetype   => bodyArchetype;
        public BaseStats BaseStats           => baseStats;
        public IReadOnlyList<LevelMoveEntry> MovePool => movePool;
        public IReadOnlyList<BodySlot> AvailableSlots => availableSlots;
        public string SignaturePartId        => signaturePartId;
        public GrowthCurve GrowthCurve       => growthCurve;
        public int BaseXpYield               => baseXpYield;
        public int CatchRate                 => catchRate;
        public IReadOnlyList<string> HabitatZoneIds => habitatZoneIds;
        public CreatureType TerrainSynergyType => terrainSynergyType;

        public bool IsDualType => secondaryType != CreatureType.None;
    }

    /// <summary>Base stat block for a species. These are the Lv1 base values.</summary>
    [Serializable]
    public class BaseStats
    {
        [SerializeField] int hp;    // Base max HP
        [SerializeField] int atk;   // Physical attack
        [SerializeField] int def;   // Physical defense
        [SerializeField] int spd;   // Speed (initiative, move priority)
        [SerializeField] int acc;   // Accuracy modifier (base 100)

        public int HP  => hp;
        public int ATK => atk;
        public int DEF => def;
        public int SPD => spd;
        public int ACC => acc;
    }

    /// <summary>A move learnable at a specific level threshold.</summary>
    [Serializable]
    public class LevelMoveEntry
    {
        [SerializeField] int level;
        [SerializeField] string moveId;

        public int Level   => level;
        public string MoveId => moveId;
    }
}
```

### 3.2 Archetype → Available Slots

| Archetype | Available BodySlots |
|-----------|-------------------|
| Bipedal | Head, Back, Arms, Tail, Legs, Torso, Aura |
| Quadruped | Head, Back, Tail, Legs, Torso, Aura |
| Serpentine | Head, Back, Tail, Aura |
| Avian | Head, Back, Arms, Tail, Legs, Aura |
| Amorphous | Torso, Aura (multiple allowed) |

### 3.3 Stat Scaling

Base stats are Lv1 values. Actual stats at a given level are computed in `CreatureInstance` using the growth formula defined in the Creature Instance GDD. Typical Lv1 ranges:

| Stat | Low | Average | High |
|------|-----|---------|------|
| HP | 35 | 55 | 80 |
| ATK | 20 | 40 | 65 |
| DEF | 15 | 35 | 60 |
| SPD | 15 | 40 | 70 |
| ACC | 90 | 100 | 110 |

### 3.4 Catch Rate Scale

| Catch Rate | Label | Approximate base catch % at 50% HP |
|-----------|-------|-------------------------------------|
| 200-255 | Common | ~60% |
| 100-199 | Uncommon | ~30% |
| 45-99 | Rare | ~12% |
| 10-44 | Epic | ~3% |
| 3-9 | Legendary | <1% |

### 3.5 MVP Creature Roster (8 Examples)

---

#### 1. Emberfox
- **ID:** `emberfox`
- **Types:** Fire
- **Rarity:** Common
- **Archetype:** Bipedal
- **Base Stats:** HP 45, ATK 60, DEF 30, SPD 65, ACC 100
- **Growth Curve:** Fast
- **Catch Rate:** 180
- **Base XP Yield:** 65
- **Signature Part:** `flame-tail` — Fire-type tail part; flame-whip counter-attack
- **Habitat:** Verdant Basin (meadow fringe)
- **Terrain Synergy:** Fire
- **Move Pool:**

| Level | Move ID |
|-------|---------|
| 1 | `scratch` |
| 1 | `ember` |
| 4 | `tail-whip` |
| 8 | `flame-claw` |
| 12 | `agility` |
| 18 | `inferno-dash` |
| 24 | `fire-fang` |

---

#### 2. Thornslug
- **ID:** `thorn-slug`
- **Types:** Grass / Dark (dual-type, MVP-safe — Poison type is post-MVP)
- **Rarity:** Common
- **Archetype:** Serpentine
- **Base Stats:** HP 70, ATK 30, DEF 65, SPD 20, ACC 95
- **Growth Curve:** Slow
- **Catch Rate:** 190
- **Base XP Yield:** 55
- **Signature Part:** `toxic-spine` — Poison back part; contacts apply Poison on hit
- **Habitat:** Verdant Basin (forest floor)
- **Terrain Synergy:** Grass
- **Move Pool:**

| Level | Move ID |
|-------|---------|
| 1 | `vine-lash` |
| 1 | `acid-spit` |
| 5 | `root-bind` |
| 10 | `poison-spore` |
| 15 | `coil` |
| 20 | `toxic-bloom` |

---

#### 3. Voltfin
- **ID:** `voltfin`
- **Types:** Electric / Water (dual-type)
- **Rarity:** Uncommon
- **Archetype:** Serpentine
- **Base Stats:** HP 50, ATK 55, DEF 35, SPD 70, ACC 100
- **Growth Curve:** Medium
- **Catch Rate:** 120
- **Base XP Yield:** 80
- **Signature Part:** `shock-fin` — Electric back part; boosts SPD on kill
- **Habitat:** Verdant Basin (river channels)
- **Terrain Synergy:** Water
- **Move Pool:**

| Level | Move ID |
|-------|---------|
| 1 | `water-pulse` |
| 1 | `spark` |
| 6 | `aqua-bolt` |
| 11 | `thunder-fang` |
| 17 | `eel-rush` |
| 23 | `discharge` |

---

#### 4. Mosshell
- **ID:** `mosshell`
- **Types:** Grass / Rock (dual-type)
- **Rarity:** Common
- **Archetype:** Quadruped
- **Base Stats:** HP 80, ATK 35, DEF 75, SPD 15, ACC 90
- **Growth Curve:** Slow
- **Catch Rate:** 170
- **Base XP Yield:** 60
- **Signature Part:** `stone-carapace` — Rock torso part; reduces incoming Physical damage by 15%
- **Habitat:** Verdant Basin (rocky outcrops)
- **Terrain Synergy:** Rock
- **Move Pool:**

| Level | Move ID |
|-------|---------|
| 1 | `tackle` |
| 1 | `vine-lash` |
| 4 | `harden` |
| 9 | `rock-throw` |
| 14 | `spore-cloud` |
| 19 | `boulder-slam` |
| 25 | `living-fortress` |

---

#### 5. Glacipede
- **ID:** `glacipede`
- **Types:** Ice
- **Rarity:** Uncommon
- **Archetype:** Serpentine
- **Base Stats:** HP 55, ATK 50, DEF 45, SPD 50, ACC 100
- **Growth Curve:** Medium
- **Catch Rate:** 130
- **Base XP Yield:** 75
- **Signature Part:** `frost-fangs` — Ice head part; attacks have 20% chance to inflict Freeze
- **Habitat:** Verdant Basin (shaded caverns, damp areas)
- **Terrain Synergy:** Ice
- **Move Pool:**

| Level | Move ID |
|-------|---------|
| 1 | `ice-shard` |
| 1 | `bite` |
| 5 | `frost-breath` |
| 10 | `coil` |
| 16 | `blizzard-fang` |
| 22 | `cryo-crush` |

---

#### 6. Shadowmite
- **ID:** `shadowmite`
- **Types:** Dark
- **Rarity:** Uncommon
- **Archetype:** Amorphous
- **Base Stats:** HP 45, ATK 55, DEF 25, SPD 75, ACC 110
- **Growth Curve:** Fast
- **Catch Rate:** 110
- **Base XP Yield:** 85
- **Signature Part:** `void-aura` — Dark aura part; reduces threat score by 50% while active
- **Habitat:** Verdant Basin (cave systems, night encounters only — post-MVP)
- **Terrain Synergy:** Dark
- **Move Pool:**

| Level | Move ID |
|-------|---------|
| 1 | `feint-attack` |
| 1 | `smokescreen` |
| 4 | `shadow-claw` |
| 9 | `taunt` |
| 14 | `night-slash` |
| 20 | `shadow-rush` |

---

#### 7. Psysprout
- **ID:** `psysprout`
- **Types:** Psychic / Grass (dual-type)
- **Rarity:** Rare
- **Archetype:** Bipedal
- **Base Stats:** HP 50, ATK 40, DEF 40, SPD 55, ACC 105
- **Growth Curve:** Medium
- **Catch Rate:** 75
- **Base XP Yield:** 95
- **Signature Part:** `psi-bloom` — Psychic head part; Status moves have +1 priority when equipped
- **Habitat:** Verdant Basin (ancient grove, restricted area)
- **Terrain Synergy:** Grass
- **Move Pool:**

| Level | Move ID |
|-------|---------|
| 1 | `confusion` |
| 1 | `absorb` |
| 6 | `spore-cloud` |
| 12 | `psybeam` |
| 18 | `calm-mind` |
| 24 | `psi-bloom-burst` |

---

#### 8. Coalbear
- **ID:** `coalbear`
- **Types:** Fire / Rock (dual-type)
- **Rarity:** Rare
- **Archetype:** Quadruped
- **Base Stats:** HP 75, ATK 70, DEF 55, SPD 30, ACC 95
- **Growth Curve:** Medium
- **Catch Rate:** 65
- **Base XP Yield:** 110
- **Signature Part:** `magma-claws` — Fire arms part; Physical attacks gain Fire type on top of base type
- **Habitat:** Verdant Basin (volcanic vents, late-zone area)
- **Terrain Synergy:** Fire
- **Move Pool:**

| Level | Move ID |
|-------|---------|
| 1 | `scratch` |
| 1 | `ember` |
| 5 | `rock-throw` |
| 10 | `lava-slam` |
| 15 | `smash` |
| 21 | `magma-crash` |
| 28 | `eruption-claw` |

---

## 4. Formulas

All stat scaling formulas are defined in `creature-instance.md`. The database only stores base values.

| Value | Formula / Notes |
|-------|----------------|
| Catch rate range | 0–255 (higher = easier) |
| Base XP yield range | 50–200 for MVP creatures |
| Stat ranges (Lv1) | HP: 35–80, ATK: 20–70, DEF: 15–75, SPD: 15–75, ACC: 90–110 |

## 5. Edge Cases

| Situation | Behavior |
|-----------|----------|
| `secondaryType` == `primaryType` | Validation error logged in Editor; treat as single-type |
| `movePool` has duplicate level entries | Both entries kept; creature learns both moves at that level |
| `signaturePartId` refers to missing part | Log warning at load; signature part effect simply not applied |
| `availableSlots` list is empty | Creature cannot equip any parts; log warning |
| `catchRate` = 0 | Creature is uncatchable (used for bosses/trainers) |
| `habitatZoneIds` is empty | Creature never spawns naturally; only available through special encounters |
| `baseXpYield` = 0 | No XP awarded on defeat (used for sparring/tutorial creatures) |
| `growthCurve` = Erratic with level > 50 | Handled in leveling system; database stores enum only |

## 6. Dependencies

| Dependency | Direction | Notes |
|------------|-----------|-------|
| `ConfigBase` | Inbound | Provides `id` and `displayName` |
| `Enums.cs` | Inbound | `CreatureType`, `Rarity`, `BodyArchetype`, `BodySlot`, `GrowthCurve` |
| `ConfigLoader` | Inbound | Loads all `CreatureConfig` assets at startup |
| `MoveConfig` | Outbound | Move pool references move IDs validated by ConfigLoader |
| `BodyPartConfig` | Outbound | `signaturePartId` references part config |
| `CreatureInstance` | Outbound | Runtime state factory takes `CreatureConfig` as blueprint |
| `Encounter System` | Outbound | Uses `habitatZoneIds`, `catchRate`, `rarity` for encounter generation |

## 7. Tuning Knobs

| Parameter | Location | Default | Notes |
|-----------|----------|---------|-------|
| Max moves in move pool | Design guideline | ~10–15 per species | No hard cap; UI shows 4 active |
| Max active moves per creature | `CreatureInstance` | 4 | Defined in creature-instance.md |
| Stat cap at Lv1 | Design guideline | HP≤80, others≤70 | Enforced by design, not code |
| Catch rate for uncatchable | Convention | 0 | Used for bosses, trainers |
| Terrain synergy bonus | `GameSettings` SO | 1.2× | Applied in damage system |
| STAB multiplier | `TypeChart` const | 1.5× | See type-chart-system.md |

## 8. Acceptance Criteria

- [ ] All 8 MVP creatures load from `Resources/Data/Creatures/` without errors
- [ ] Each creature has at least one move at level 1 in its move pool
- [ ] All move IDs in move pools exist in the Move Database
- [ ] `signaturePartId` fields reference valid `BodyPartConfig` IDs
- [ ] `IsDualType` returns true for dual-type creatures and false for single-type
- [ ] No two creatures share the same `id`
- [ ] `availableSlots` is non-empty for all 8 creatures
- [ ] `catchRate` is within 0–255 for all entries
- [ ] EditMode test: load all `CreatureConfig` assets, assert count >= 8
- [ ] EditMode test: `Emberfox.primaryType == CreatureType.Fire`
- [ ] EditMode test: `Thornslug.IsDualType == true`
- [ ] EditMode test: `Mosshell` move pool contains `boulder-slam` at level 19
