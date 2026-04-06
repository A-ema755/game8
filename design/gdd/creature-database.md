# Creature Database

## 1. Overview

The Creature Database defines the `CreatureConfig` ScriptableObject schema — the immutable blueprint for every species in Gene Forge. Each config captures the species' genome types (from the 14-type system), base stats, learnable move pool, body archetype and available slots, **default body parts** (which determine starting damage form access), signature part, catch rate, XP growth curve, habitat zones, and rarity. The database is loaded at startup by `ConfigLoader` and never modified at runtime. Runtime creature state (HP, level, DNA mods, etc.) lives in `CreatureInstance`, not here. Fourteen MVP creatures are defined below as reference examples spanning the Verdant Basin starting zone.

## 2. Player Fantasy

Flipping through the creature database feels like reading a field researcher's journal. Each species has a distinct ecological niche, a visual identity implied by its archetype and type, and mechanical hooks that make it interesting to build around. The player immediately understands why an Organic/Toxic dual-type slug with a slow growth curve and high DEF is a different strategic choice from a fast, fragile Thermal creature with an aggressive signature part. **Default body parts tell you at a glance what forms a species can use** — a creature born with Claws and Glands has both Physical and Energy options, while one with only Spore Pods is a Bio specialist.

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
        [SerializeField] List<string> defaultPartIds;     // Body parts equipped at capture/creation (determines starting form access)
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
        public IReadOnlyList<string> DefaultPartIds => defaultPartIds;
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
| --- | --- |
| Bipedal | Head, Back, LeftArm, RightArm, Tail, Legs |
| Quadruped | Head, Back, Tail, Legs, Hide |
| Serpentine | Head, BodyUpper, BodyLower, Tail |
| Avian | Head, Wings, Tail, Talons |
| Amorphous | CoreA, CoreB, CoreC, Appendage |

### 3.3 Stat Scaling

Base stats are Lv1 values. Actual stats at a given level are computed in `CreatureInstance` using the growth formula defined in the Creature Instance GDD. Typical Lv1 ranges:

| Stat | Low | Average | High |
| --- | --- | --- | --- |
| HP | 35 | 55 | 80 |
| ATK | 20 | 45 | 70 |
| DEF | 15 | 40 | 80 |
| SPD | 15 | 45 | 80 |
| ACC | 90 | 100 | 110 |

### 3.3.1 Base Stat Total (BST) Targets

BST = HP + ATK + DEF + SPD + ACC. Target ranges per rarity tier:

| Rarity | BST Range | Notes |
| --- | --- | --- |
| Common | 270–310 | Widely available; solid but not exceptional |
| Uncommon | 290–320 | Slightly stronger or more specialized |
| Rare | 300–340 | Noticeable stat advantage or extreme specialization |
| Epic | 310–350 | Powerful; hard to find |
| Legendary | 330–380 | Peak stats; unique encounters only |

Ranges overlap intentionally — rarity reflects availability, not raw power. A well-built Common can compete with an Uncommon.

### 3.4 Catch Rate Scale

| Catch Rate | Label | Approximate base catch % at 50% HP |
| --- | --- | --- |
| 200-255 | Common | ~60% |
| 100-199 | Uncommon | ~30% |
| 45-99 | Rare | ~12% |
| 10-44 | Epic | ~3% |
| 3-9 | Legendary | <1% |

### 3.5 MVP Creature Roster (14 Examples)

---

#### 1. Emberfox

* **ID:** `emberfox`
* **Types:** Thermal
* **Rarity:** Common
* **Archetype:** Bipedal
* **Base Stats:** HP 45, ATK 60, DEF 30, SPD 65, ACC 100
* **Growth Curve:** Fast
* **Catch Rate:** 180
* **Base XP Yield:** 65
* **Default Parts:** `claws-rending` (Physical), `glands-thermal` (Energy)
* **Starting Form Access:** Physical, Energy
* **Signature Part:** `flame-tail` — Thermal tail part; flame-whip counter-attack (Physical)
* **Habitat:** Verdant Basin (meadow fringe)
* **Terrain Synergy:** Thermal
* **Move Pool:**

| Level | Move ID |
| --- | --- |
| 1 | `scratch` |
| 1 | `ember` |
| 8 | `flame-claw` |
| 12 | `agility` |
| 18 | `inferno-dash` |



---

#### 2. Thornslug

* **ID:** `thorn-slug`
* **Types:** Organic / Toxic (dual-type)
* **Rarity:** Common
* **Archetype:** Serpentine
* **Base Stats:** HP 70, ATK 30, DEF 65, SPD 20, ACC 95
* **Growth Curve:** Slow
* **Catch Rate:** 190
* **Base XP Yield:** 55
* **Default Parts:** `stinger-venom` (Bio), `spore-pods` (Bio)
* **Starting Form Access:** Bio
* **Signature Part:** `toxic-spine` — Toxic back part; contacts apply Poison on hit
* **Habitat:** Verdant Basin (forest floor)
* **Terrain Synergy:** Organic
* **Move Pool:**

| Level | Move ID |
| --- | --- |
| 1 | `vine-lash` |
| 1 | `toxic-spore` |
| 5 | `root-bind` |
| 10 | `leech-sting` |
| 20 | `spore-cloud` |



---

#### 3. Voltfin

* **ID:** `voltfin`
* **Types:** Bioelectric / Aqua (dual-type)
* **Rarity:** Uncommon
* **Archetype:** Serpentine
* **Base Stats:** HP 50, ATK 55, DEF 35, SPD 70, ACC 100
* **Growth Curve:** Medium
* **Catch Rate:** 120
* **Base XP Yield:** 80
* **Default Parts:** `fangs-serrated` (Physical), `core-bioelectric` (Energy)
* **Starting Form Access:** Physical, Energy
* **Signature Part:** `shock-fin` — Bioelectric back part; boosts SPD on kill (Energy)
* **Habitat:** Verdant Basin (river channels)
* **Terrain Synergy:** Aqua
* **Move Pool:**

| Level | Move ID |
| --- | --- |
| 1 | `water-pulse` |
| 1 | `spark` |
| 6 | `aqua-bolt` |
| 12 | `feint-attack` |
| 17 | `discharge` |



---

#### 4. Mosshell

* **ID:** `mosshell`
* **Types:** Organic / Mineral (dual-type)
* **Rarity:** Common
* **Archetype:** Quadruped
* **Base Stats:** HP 80, ATK 35, DEF 75, SPD 15, ACC 90
* **Growth Curve:** Slow
* **Catch Rate:** 170
* **Base XP Yield:** 60
* **Default Parts:** `horns-bone` (Physical), `spore-pods` (Bio)
* **Starting Form Access:** Physical, Bio
* **Signature Part:** `stone-carapace` — Mineral torso part; reduces incoming Physical damage by 15%
* **Habitat:** Verdant Basin (rocky outcrops)
* **Terrain Synergy:** Mineral
* **Move Pool:**

| Level | Move ID |
| --- | --- |
| 1 | `tackle` |
| 1 | `vine-lash` |
| 4 | `harden` |
| 9 | `rock-throw` |
| 14 | `spore-cloud` |
| 19 | `boulder-slam` |



---

#### 5. Glacipede

* **ID:** `glacipede`
* **Types:** Cryo
* **Rarity:** Uncommon
* **Archetype:** Serpentine
* **Base Stats:** HP 55, ATK 50, DEF 45, SPD 50, ACC 100
* **Growth Curve:** Medium
* **Catch Rate:** 130
* **Base XP Yield:** 75
* **Default Parts:** `fangs-serrated` (Physical)
* **Starting Form Access:** Physical
* **Signature Part:** `frost-fangs` — Cryo head part; attacks have 20% chance to inflict Freeze (Physical)
* **Habitat:** Verdant Basin (shaded caverns, damp areas)
* **Terrain Synergy:** Cryo
* **Move Pool:**

| Level | Move ID |
| --- | --- |
| 1 | `ice-shard` |
| 1 | `ferro-bite` |
| 1 | `tackle` |
| 5 | `frost-breath` |
| 10 | `harden` |



---

#### 6. Shadowmite

* **ID:** `shadowmite`
* **Types:** Neural
* **Rarity:** Uncommon
* **Archetype:** Amorphous
* **Base Stats:** HP 45, ATK 55, DEF 25, SPD 75, ACC 110
* **Growth Curve:** Fast
* **Catch Rate:** 110
* **Base XP Yield:** 85
* **Default Parts:** `fangs-serrated` (Physical), `tendrils-neural` (Bio)
* **Starting Form Access:** Physical, Bio
* **Signature Part:** `void-aura` — Neural aura part; reduces threat score by 50% while active
* **Habitat:** Verdant Basin (cave systems, night encounters only — post-MVP)
* **Terrain Synergy:** Neural
* **Move Pool:**

| Level | Move ID |
| --- | --- |
| 1 | `feint-attack` |
| 4 | `neural-claw` |
| 9 | `taunt` |
| 14 | `mind-beam` |



---

#### 7. Psysprout

* **ID:** `psysprout`
* **Types:** Neural / Organic (dual-type)
* **Rarity:** Rare
* **Archetype:** Bipedal
* **Base Stats:** HP 55, ATK 45, DEF 40, SPD 55, ACC 105
* **Growth Curve:** Medium
* **Catch Rate:** 75
* **Base XP Yield:** 95
* **Default Parts:** `tendrils-neural` (Bio), `core-neural` (Energy)
* **Starting Form Access:** Bio, Energy
* **Signature Part:** `psi-bloom` — Neural head part; Status moves have +1 priority when equipped
* **Habitat:** Verdant Basin (ancient grove, restricted area)
* **Terrain Synergy:** Organic
* **Move Pool:**

| Level | Move ID |
| --- | --- |
| 1 | `mind-beam` |
| 6 | `spore-cloud` |
| 9 | `leech-sting` |
| 12 | `toxic-spore` |
| 15 | `root-bind` |



---

#### 8. Coalbear

* **ID:** `coalbear`
* **Types:** Thermal / Mineral (dual-type)
* **Rarity:** Rare
* **Archetype:** Quadruped
* **Base Stats:** HP 75, ATK 70, DEF 55, SPD 30, ACC 95
* **Growth Curve:** Medium
* **Catch Rate:** 65
* **Base XP Yield:** 110
* **Default Parts:** `claws-rending` (Physical), `glands-thermal` (Energy)
* **Starting Form Access:** Physical, Energy
* **Signature Part:** `magma-claws` — Thermal arms part; Physical attacks gain Thermal type on top of base type
* **Habitat:** Verdant Basin (volcanic vents, late-zone area)
* **Terrain Synergy:** Thermal
* **Move Pool:**

| Level | Move ID |
| --- | --- |
| 1 | `scratch` |
| 1 | `ember` |
| 5 | `rock-throw` |
| 10 | `flame-claw` |
| 16 | `boulder-slam` |



---

#### 9. Ferrovex

* **ID:** `ferrovex`
* **Types:** Ferro / Mineral (dual-type)
* **Rarity:** Uncommon
* **Archetype:** Quadruped
* **Base Stats:** HP 65, ATK 40, DEF 80, SPD 20, ACC 90
* **Growth Curve:** Slow
* **Catch Rate:** 115
* **Base XP Yield:** 90
* **Default Parts:** `plating-ferro` (Physical), `horns-bone` (Physical)
* **Starting Form Access:** Physical
* **Signature Part:** `ore-carapace` — Ferro torso; reduces all incoming Physical damage by 20%
* **Habitat:** Verdant Basin (ore caves, late zone)
* **Terrain Synergy:** Mineral
* **Move Pool:**

| Level | Move ID |
| --- | --- |
| 1 | `iron-bash` |
| 1 | `harden` |
| 6 | `rock-throw` |
| 12 | `metal-press` |
| 18 | `siege-slam` |



---

#### 10. Galewhip

* **ID:** `galewhip`
* **Types:** Aero / Sonic (dual-type)
* **Rarity:** Common
* **Archetype:** Serpentine
* **Base Stats:** HP 45, ATK 55, DEF 25, SPD 80, ACC 100
* **Growth Curve:** Fast
* **Catch Rate:** 160
* **Base XP Yield:** 70
* **Default Parts:** `wings-aero` (Physical), `emitter-resonance` (Energy)
* **Starting Form Access:** Physical, Energy
* **Signature Part:** `gale-tail` — Aero tail; user always moves first on the turn it is equipped and active
* **Habitat:** Verdant Basin (open meadows, cliff updrafts)
* **Terrain Synergy:** Aero
* **Move Pool:**

| Level | Move ID |
| --- | --- |
| 1 | `wind-slash` |
| 1 | `sonic-pulse` |
| 5 | `gust` |
| 10 | `screech` |
| 16 | `cyclone-strike` |



---

#### 11. Quarrok

* **ID:** `quarrok`
* **Types:** Kinetic / Mineral (dual-type)
* **Rarity:** Common
* **Archetype:** Bipedal
* **Base Stats:** HP 60, ATK 70, DEF 50, SPD 35, ACC 90
* **Growth Curve:** Medium
* **Catch Rate:** 155
* **Base XP Yield:** 75
* **Default Parts:** `fists-impact` (Physical), `horns-bone` (Physical)
* **Starting Form Access:** Physical
* **Signature Part:** `seismic-fists` — Kinetic arms; Physical attacks from height 2+ gain +15% damage
* **Habitat:** Verdant Basin (boulder fields, elevated ridges)
* **Terrain Synergy:** Kinetic
* **Move Pool:**

| Level | Move ID |
| --- | --- |
| 1 | `tackle` |
| 1 | `rock-throw` |
| 4 | `power-strike` |
| 9 | `boulder-slam` |
| 15 | `seismic-smash` |



---

#### 12. Corrovex

* **ID:** `corrovex`
* **Types:** Ferro / Toxic (dual-type)
* **Rarity:** Uncommon
* **Archetype:** Serpentine
* **Base Stats:** HP 55, ATK 65, DEF 45, SPD 55, ACC 100
* **Growth Curve:** Medium
* **Catch Rate:** 120
* **Base XP Yield:** 85
* **Default Parts:** `fangs-serrated` (Physical), `glands-toxic` (Bio)
* **Starting Form Access:** Physical, Bio
* **Signature Part:** `acid-scales` — Ferro back; contact attacks apply a stacking DEF reduction (Physical)
* **Habitat:** Verdant Basin (industrial ruins, toxic pools — mid zone)
* **Terrain Synergy:** Toxic
* **Move Pool:**

| Level | Move ID |
| --- | --- |
| 1 | `ferro-bite` |
| 1 | `toxic-spore` |
| 7 | `acid-spray` |
| 13 | `corrode` |
| 19 | `rust-lash` |



---

#### 13. Arkveil

* **ID:** `arkveil`
* **Types:** Ark / Aqua (dual-type)
* **Rarity:** Epic
* **Archetype:** Amorphous
* **Base Stats:** HP 75, ATK 40, DEF 70, SPD 45, ACC 100
* **Growth Curve:** Slow
* **Catch Rate:** 15
* **Base XP Yield:** 140
* **Default Parts:** `crystal-lattice` (Energy), `tendrils-neural` (Bio)
* **Starting Form Access:** Energy, Bio
* **Signature Part:** `purity-aura` — Ark aura; reduces DNA instability by 5 at end of each combat
* **Habitat:** Verdant Basin (sealed underground grotto — post-rival unlock)
* **Terrain Synergy:** Ark
* **Move Pool:**

| Level | Move ID |
| --- | --- |
| 1 | `purify` |
| 1 | `water-pulse` |
| 8 | `aqua-bolt` |
| 14 | `stasis-field` |
| 22 | `genetic-lock` |



---

#### 14. Blighthowl

* **ID:** `blighthowl`
* **Types:** Blight / Organic (dual-type)
* **Rarity:** Epic
* **Archetype:** Quadruped
* **Base Stats:** HP 55, ATK 70, DEF 30, SPD 65, ACC 95
* **Growth Curve:** Fast
* **Catch Rate:** 20
* **Base XP Yield:** 130
* **Default Parts:** `claws-rending` (Physical), `glands-blight` (Bio)
* **Starting Form Access:** Physical, Bio
* **Signature Part:** `corruption-maw` — Blight head; attacks have 25% chance to apply a random negative status to the target
* **Habitat:** Verdant Basin (overgrown ruins — night encounters only)
* **Terrain Synergy:** Blight
* **Move Pool:**

| Level | Move ID |
| --- | --- |
| 1 | `vine-lash` |
| 1 | `blight-claw` |
| 6 | `corrupt` |
| 12 | `entropic-howl` |
| 20 | `genetic-collapse` |



---

## 4. Formulas

All stat scaling formulas are defined in `creature-instance.md`. The database only stores base values.

| Value | Formula / Notes |
| --- | --- |
| Catch rate range | 0–255 (higher = easier) |
| Base XP yield range | 50–200 for MVP creatures |
| Stat ranges (Lv1) | HP: 35–80, ATK: 20–70, DEF: 15–80, SPD: 15–80, ACC: 90–110 |

## 5. Edge Cases

| Situation | Behavior |
| --- | --- |
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
| --- | --- | --- |
| `ConfigBase` | Inbound | Provides `id` and `displayName` |
| `Enums.cs` | Inbound | `CreatureType` (14 genome types), `DamageForm`, `Rarity`, `BodyArchetype`, `BodySlot`, `GrowthCurve` |
| `ConfigLoader` | Inbound | Loads all `CreatureConfig` assets at startup |
| `MoveConfig` | Outbound | Move pool references move IDs validated by ConfigLoader |
| `BodyPartConfig` | Outbound | `signaturePartId` and `defaultPartIds` reference part configs; form access derived from default parts |
| `CreatureInstance` | Outbound | Runtime state factory takes `CreatureConfig` as blueprint |
| `Encounter System` | Outbound | Uses `habitatZoneIds`, `catchRate`, `rarity` for encounter generation |

## 7. Tuning Knobs

| Parameter | Location | Default | Notes |
| --- | --- | --- | --- |
| Max moves in move pool | Design guideline | ~10–15 per species | No hard cap; UI shows 4 active |
| Max active moves per creature | `CreatureInstance` | 4 | Defined in creature-instance.md |
| Stat cap at Lv1 | Design guideline | HP≤80, others≤70 | Enforced by design, not code |
| Catch rate for uncatchable | Convention | 0 | Used for bosses, trainers |
| Terrain synergy bonus | `GameSettings` SO | 1.2× | Applied in damage system |
| STAB multiplier | `TypeChart` const | 1.5× | See type-chart-system.md |

## 8. Acceptance Criteria

- [ ] All 14 MVP creatures load from `Resources/Data/Creatures/` without errors
- [ ] Each creature has at least one move at level 1 in its move pool
- [ ] All move IDs in move pools exist in the Move Database
- [ ] `signaturePartId` fields reference valid `BodyPartConfig` IDs
- [ ] `IsDualType` returns true for dual-type creatures and false for single-type
- [ ] No two creatures share the same `id`
- [ ] `availableSlots` is non-empty for all 14 creatures
- [ ] `catchRate` is within 0–255 for all entries
- [ ] EditMode test: load all `CreatureConfig` assets, assert count >= 14
- [ ] Every creature has at least one entry in `defaultPartIds`
- [ ] Default parts determine correct starting form access per creature
- [ ] EditMode test: `Emberfox.primaryType == CreatureType.Thermal`
- [ ] EditMode test: `Thornslug.IsDualType == true`
- [ ] EditMode test: `Mosshell` move pool contains `boulder-slam` at level 19
- [ ] EditMode test: at least 9 of 14 CreatureType enum values are represented as a primary type in the roster (Aqua, Toxic, Mineral, Kinetic, Sonic deferred to roster expansion)
- [ ] Every creature's level-1 moves use only damage forms granted by its default body parts
- [ ] Signature part BodyPartConfig assets exist for all 14 creatures (blocked until body-part-system catalog expansion)