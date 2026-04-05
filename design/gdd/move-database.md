# Move Database

## 1. Overview

The Move Database defines the `MoveConfig` ScriptableObject schema — the immutable blueprint for every move in Gene Forge. Each config captures the move's elemental type, category (Physical/Special/Status), power, accuracy, PP, targeting pattern, priority, and a list of on-hit or on-use effects. The database is loaded at startup by `ConfigLoader`. Twenty-five MVP moves are defined below, spanning all 8 elemental types and all move categories, forming the starting pool for the creature roster defined in `creature-database.md`.

## 2. Player Fantasy

Every move feels distinct. A high-power, low-accuracy move is a gamble with a payoff. A status move that roots an enemy to the tile feels like a tactical decision, not a wasted turn. AoE moves reshape the board. The move list reads like a toolkit, not a stat sheet — each entry evokes an image of the creature using it.

## 3. Detailed Rules

### 3.1 MoveConfig ScriptableObject

```csharp
namespace GeneForge.Moves
{
    /// <summary>
    /// Immutable move blueprint. One asset per move in Resources/Data/Moves/.
    /// Do not modify at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMove", menuName = "GeneForge/Move Config")]
    public class MoveConfig : ConfigBase
    {
        [Header("Type & Category")]
        [SerializeField] CreatureType type;
        [SerializeField] MoveCategory category;

        [Header("Stats")]
        [SerializeField] int power;        // Base power; 0 for Status moves
        [SerializeField] int accuracy;     // 0-100; 0 = always hits (e.g. Swift)
        [SerializeField] int pp;           // Uses per battle (Power Points)
        [SerializeField] int priority;     // Higher priority moves go first regardless of speed

        [Header("Targeting")]
        [SerializeField] TargetType targetType;
        [SerializeField] int range;        // Chebyshev distance; 1 = adjacent only, 0 = unlimited

        [Header("Effects")]
        [SerializeField] List<MoveEffect> effects;

        // ── Properties ───────────────────────────────────────────────────
        public CreatureType Type        => type;
        public MoveCategory Category    => category;
        public int Power                => power;
        public int Accuracy             => accuracy;
        public int PP                   => pp;
        public int Priority             => priority;
        public TargetType TargetType    => targetType;
        public int Range                => range;
        public IReadOnlyList<MoveEffect> Effects => effects;

        public bool IsDamaging          => category != MoveCategory.Status && power > 0;
        public bool AlwaysHits          => accuracy == 0;
    }

    /// <summary>A single on-hit or on-use effect attached to a move.</summary>
    [Serializable]
    public class MoveEffect
    {
        [SerializeField] MoveEffectType effectType;
        [SerializeField] float chance;          // 0.0–1.0; 1.0 = guaranteed
        [SerializeField] int magnitude;         // Effect-specific value (e.g. stat stages, damage %)
        [SerializeField] StatusEffect statusToApply; // For status-inflicting effects
        [SerializeField] bool affectsSelf;      // True if effect targets user, not target

        public MoveEffectType EffectType    => effectType;
        public float Chance                 => chance;
        public int Magnitude                => magnitude;
        public StatusEffect StatusToApply   => statusToApply;
        public bool AffectsSelf             => affectsSelf;
    }

    public enum MoveEffectType
    {
        ApplyStatus = 0,         // Inflict a StatusEffect on target
        StatStage = 1,           // Modify a stat by N stages (+/-1 to +/-3)
        Recoil = 2,              // User takes % of damage dealt as recoil
        Drain = 3,               // User heals % of damage dealt
        ForcedMove = 4,          // Push target N tiles in a direction
        IgnoreDefense = 5,       // Damage ignores target's DEF
        MultiHit = 6,            // Hits 2-5 times; magnitude = max hits
        HighCrit = 7,            // Increased critical hit chance
        Flinch = 8,              // Target loses next action (chance-based)
        TerrainCreate = 9,       // Creates terrain tile at target location
        PriorityNext = 10        // User's next move gains +1 priority
    }
}
```

### 3.2 PP System

PP is tracked per move slot on `CreatureInstance`, not on `MoveConfig`. `MoveConfig.PP` is the max PP value copied to the instance at time of learning. PP refills fully after each battle (MVP simplification; in-battle PP management is post-MVP).

### 3.3 Priority System

Priority values determine action order within a turn phase, overriding speed:

| Priority | Examples |
|----------|---------|
| +2 | Quick Strike, Protective Shell |
| +1 | Status moves with PriorityNext effect |
| 0 | All standard moves |
| -1 | Heavy charge moves |

Same-priority moves resolve by SPD stat, then by random tiebreak.

### 3.4 Accuracy Resolution

```csharp
// In TurnManager / MoveResolver
bool HitCheck(MoveConfig move, CreatureInstance attacker, CreatureInstance defender)
{
    if (move.AlwaysHits) return true;   // accuracy == 0
    float hitChance = (move.Accuracy / 100f)
                    * (attacker.AccuracyStage.ToMultiplier())
                    * (1f / defender.EvasionStage.ToMultiplier());
    return Random.value < hitChance;
}
```

### 3.5 MVP Move List (19 Moves)

---

#### 1. Scratch
- **ID:** `scratch`
- **Type:** None (typeless — uses attacker's primary type for STAB)
- **Category:** Physical
- **Power:** 40 | **Accuracy:** 100 | **PP:** 35 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** None
- **Notes:** Basic first move for most Bipedal creatures.

---

#### 2. Tackle
- **ID:** `tackle`
- **Type:** None
- **Category:** Physical
- **Power:** 40 | **Accuracy:** 100 | **PP:** 35 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** None
- **Notes:** Basic first move for Quadruped and heavier archetypes.

---

#### 3. Ember
- **ID:** `ember`
- **Type:** Fire
- **Category:** Special
- **Power:** 40 | **Accuracy:** 100 | **PP:** 25 | **Priority:** 0
- **Target:** Single | **Range:** 3
- **Effects:** `ApplyStatus(Burn, 10% chance)`
- **Notes:** Standard early Fire move; small burn chance introduces status pressure.

---

#### 4. Flame Claw
- **ID:** `flame-claw`
- **Type:** Fire
- **Category:** Physical
- **Power:** 65 | **Accuracy:** 95 | **PP:** 15 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** `ApplyStatus(Burn, 20% chance)`
- **Notes:** Mid-tier Fire physical; higher burn rate rewards melee positioning.

---

#### 5. Inferno Dash
- **ID:** `inferno-dash`
- **Type:** Fire
- **Category:** Physical
- **Power:** 80 | **Accuracy:** 90 | **PP:** 10 | **Priority:** 0
- **Target:** Single | **Range:** 3
- **Effects:** `Recoil(25%)`, `ForcedMove(1 tile away, target)`
- **Notes:** Emberfox signature-style move. Dashes to target, deals damage, pushes target back 1 tile, user takes 25% recoil.

---

#### 6. Vine Lash
- **ID:** `vine-lash`
- **Type:** Grass
- **Category:** Physical
- **Power:** 45 | **Accuracy:** 100 | **PP:** 25 | **Priority:** 0
- **Target:** Single | **Range:** 2
- **Effects:** None
- **Notes:** Reliable early Grass physical with slightly extended range.

---

#### 7. Root Bind
- **ID:** `root-bind`
- **Type:** Grass
- **Category:** Status
- **Power:** 0 | **Accuracy:** 85 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 2
- **Effects:** `ApplyStatus(Paralysis, 100%)`, `TerrainCreate(Difficult at target tile)`
- **Notes:** Immobilizes target and creates difficult terrain at their tile. Repositioning counter.

---

#### 8. Spore Cloud
- **ID:** `spore-cloud`
- **Type:** Grass
- **Category:** Status
- **Power:** 0 | **Accuracy:** 90 | **PP:** 15 | **Priority:** 0
- **Target:** AoE | **Range:** 2
- **Effects:** `ApplyStatus(Sleep, 100%)` — hits all enemies within range 2
- **Notes:** High-impact AoE sleep. Short range forces commitment. Use with positioning.

---

#### 9. Toxic Bloom
- **ID:** `toxic-bloom`
- **Type:** Poison
- **Category:** Status
- **Power:** 0 | **Accuracy:** 90 | **PP:** 15 | **Priority:** 0
- **Target:** Adjacent | **Range:** 1
- **Effects:** `ApplyStatus(Poison, 100%)` on all adjacent enemies
- **Notes:** Thornslug's area poison tool. Rewards surrounding enemies.

---

#### 10. Water Pulse
- **ID:** `water-pulse`
- **Type:** Water
- **Category:** Special
- **Power:** 60 | **Accuracy:** 100 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 4
- **Effects:** `ApplyStatus(Confusion, 20% chance)`
- **Notes:** Long-range Water special with confusion chance. Good for water-terrain setup.

---

#### 11. Aqua Bolt
- **ID:** `aqua-bolt`
- **Type:** Water
- **Category:** Special
- **Power:** 80 | **Accuracy:** 90 | **PP:** 10 | **Priority:** 0
- **Target:** Line | **Range:** 5
- **Effects:** None
- **Notes:** Fires through a line of tiles hitting all creatures in the path. Powerful but narrow.

---

#### 12. Spark
- **ID:** `spark`
- **Type:** Electric
- **Category:** Physical
- **Power:** 65 | **Accuracy:** 100 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** `ApplyStatus(Paralysis, 30% chance)`
- **Notes:** Contact Electric move. Paralysis chance strong on fast attackers like Voltfin.

---

#### 13. Discharge
- **ID:** `discharge`
- **Type:** Electric
- **Category:** Special
- **Power:** 80 | **Accuracy:** 100 | **PP:** 15 | **Priority:** 0
- **Target:** Adjacent | **Range:** 1
- **Effects:** `ApplyStatus(Paralysis, 30% chance)` on all adjacent
- **Notes:** Full radial AoE discharge hitting all adjacent creatures (including allies — use carefully).

---

#### 14. Ice Shard
- **ID:** `ice-shard`
- **Type:** Ice
- **Category:** Physical
- **Power:** 40 | **Accuracy:** 100 | **PP:** 30 | **Priority:** +1
- **Target:** Single | **Range:** 2
- **Effects:** None
- **Notes:** High priority Ice move. Useful for finishing off faster enemies before they act.

---

#### 15. Frost Breath
- **ID:** `frost-breath`
- **Type:** Ice
- **Category:** Special
- **Power:** 60 | **Accuracy:** 90 | **PP:** 15 | **Priority:** 0
- **Target:** Single | **Range:** 3
- **Effects:** `ApplyStatus(Freeze, 10% chance)`
- **Notes:** Mid-range Ice special. Freeze is rare but devastating — skips target's next turn.

---

#### 16. Rock Throw
- **ID:** `rock-throw`
- **Type:** Rock
- **Category:** Physical
- **Power:** 50 | **Accuracy:** 90 | **PP:** 15 | **Priority:** 0
- **Target:** Single | **Range:** 3
- **Effects:** None
- **Notes:** Standard ranged Rock physical. Works on Mosshell and Coalbear at low levels.

---

#### 17. Boulder Slam
- **ID:** `boulder-slam`
- **Type:** Rock
- **Category:** Physical
- **Power:** 100 | **Accuracy:** 75 | **PP:** 10 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** `ForcedMove(2 tiles away, target)`, `Flinch(50% chance)`
- **Notes:** High-risk, high-reward. Pushes target 2 tiles; can knock off elevated ground. Mosshell's signature damage tool.

---

#### 18. Shadow Claw
- **ID:** `shadow-claw`
- **Type:** Dark
- **Category:** Physical
- **Power:** 70 | **Accuracy:** 100 | **PP:** 15 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** `HighCrit`
- **Notes:** Reliable Dark physical with boosted critical hit rate. Core offensive tool for Shadowmite.

---

#### 19. Psybeam
- **ID:** `psybeam`
- **Type:** Psychic
- **Category:** Special
- **Power:** 65 | **Accuracy:** 100 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 4
- **Effects:** `ApplyStatus(Confusion, 30% chance)`
- **Notes:** Long-range Psychic special. Confusion chance disrupts enemy action planning.

---

#### 20. Taunt
- **ID:** `taunt`
- **Type:** Dark
- **Category:** Status
- **Power:** 0 | **Accuracy:** 100 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 3
- **Effects:** `ApplyStatus(Taunt, 100%)`
- **Notes:** Forces target to use only offensive moves for 3 rounds. Critical aggro manipulation tool.

---

#### 21. Feint Attack
- **ID:** `feint-attack`
- **Type:** Dark
- **Category:** Physical
- **Power:** 60 | **Accuracy:** 0 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 2
- **Effects:** None
- **Notes:** Always hits (accuracy = 0). Cannot be avoided by evasion boosts.

---

#### 22. Confusion (Move)
- **ID:** `confusion`
- **Type:** Psychic
- **Category:** Special
- **Power:** 50 | **Accuracy:** 100 | **PP:** 25 | **Priority:** 0
- **Target:** Single | **Range:** 3
- **Effects:** `ApplyStatus(Confusion, 10% chance)`
- **Notes:** Entry-level Psychic special. Confusion chance scales up in later moves.

---

#### 23. Harden
- **ID:** `harden`
- **Type:** None
- **Category:** Status
- **Power:** 0 | **Accuracy:** 0 | **PP:** 30 | **Priority:** 0
- **Target:** Self | **Range:** 0
- **Effects:** `StatStage(DEF, +1, 100%, affectsSelf=true)`
- **Notes:** Always hits self. Raises DEF by 1 stage. Essential for tank creatures like Mosshell.

---

#### 24. Agility
- **ID:** `agility`
- **Type:** None
- **Category:** Status
- **Power:** 0 | **Accuracy:** 0 | **PP:** 30 | **Priority:** 0
- **Target:** Self | **Range:** 0
- **Effects:** `StatStage(SPD, +2, 100%, affectsSelf=true)`
- **Notes:** Always hits self. Raises SPD by 2 stages. Emberfox's speed amplifier.

---

#### 25. Bite
- **ID:** `bite`
- **Type:** Dark
- **Category:** Physical
- **Power:** 60 | **Accuracy:** 100 | **PP:** 25 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** `Flinch(30% chance)`
- **Notes:** Contact Dark physical with flinch. Prevents target from acting that turn.

---

## 4. Formulas

| Formula | Expression |
|---------|-----------|
| Hit chance | `(accuracy / 100) × accuracyStageMultiplier × (1 / evasionStageMultiplier)` |
| Always-hits condition | `move.accuracy == 0` |
| Critical hit base chance | `6.25%` (1/16); HighCrit effect raises to `12.5%` |
| Critical hit damage multiplier | `1.5×` (applied in DamageSystem) |
| Multi-hit distribution | Uniform random 2–5 hits; each hit calculated independently |
| Recoil damage | `floor(damageDealt × recoilFraction)` |
| Drain heal | `floor(damageDealt × drainFraction)` |

## 5. Edge Cases

| Situation | Behavior |
|-----------|----------|
| Move used with 0 PP remaining | Move fails; "Out of PP" message shown; creature uses Struggle (10 base power, typeless, 25% recoil) |
| Status move targeting already-statused creature | Move fails; "Already has a status condition" message |
| AoE/Adjacent move with no valid targets | Move fails; PP still consumed |
| `accuracy = 0` — does this mean 0% or always hit? | Always hits — `AlwaysHits` property resolves this; `accuracy == 0` is the always-hit sentinel |
| `ForcedMove` pushes target off grid edge | Target placed at nearest valid tile; fall damage applies if height drop ≥ 3 |
| `TerrainCreate` on already-typed terrain tile | Replaces terrain type; previous synergy lost |
| Multi-hit move: target faints on hit 2 of 5 | Remaining hits do not resolve; damage tallied from completed hits |
| `Line` targeting: ally in path | By default allies are not hit; post-MVP: friendly fire toggle |
| Confusion status: creature hits itself | Uses attacker's own ATK vs own DEF with fixed 40 base power, typeless |

## 6. Dependencies

| Dependency | Direction | Notes |
|------------|-----------|-------|
| `ConfigBase` | Inbound | Provides `id`, `displayName` |
| `Enums.cs` | Inbound | `CreatureType`, `MoveCategory`, `TargetType`, `StatusEffect` |
| `ConfigLoader` | Inbound | Loads all `MoveConfig` assets at startup |
| `CreatureDatabase` | Outbound | Move IDs in creature move pools must exist here |
| `DamageSystem` | Outbound | Reads `power`, `category`, `type`, `effects` per move |
| `TurnManager` | Outbound | Reads `priority`, `targetType`, `range`, `pp` |
| `CreatureInstance` | Outbound | Tracks current PP per learned move slot |
| `TypeChart` | Outbound | `type` used for effectiveness and STAB lookups |

## 7. Tuning Knobs

| Parameter | Location | Default | Notes |
|-----------|----------|---------|-------|
| Struggle base power | `GameSettings` SO | 10 | Typeless fallback when all PP exhausted |
| Struggle recoil | `GameSettings` SO | 25% | Fraction of damage taken as recoil |
| Critical hit base rate | `GameSettings` SO | 0.0625 (6.25%) | 1/16 |
| HighCrit rate | `GameSettings` SO | 0.125 (12.5%) | 1/8 |
| Critical hit multiplier | `GameSettings` SO | 1.5× | Applied in DamageSystem |
| Max PP per move | Convention | 35 | No hard cap; design guideline |
| Max move range | Convention | 5 | Beyond 5 = effectively global; use AoE instead |

## 8. Acceptance Criteria

- [ ] All 25 MVP moves load from `Resources/Data/Moves/` without errors
- [ ] No two moves share the same `id`
- [ ] All move IDs referenced in creature move pools exist in this database
- [ ] `feint-attack` has `accuracy = 0` and `AlwaysHits` returns true
- [ ] `ice-shard` has `priority = 1`
- [ ] `root-bind` effect list contains both `ApplyStatus(Paralysis)` and `TerrainCreate`
- [ ] `harden` has `targetType = Self` and `affectsSelf = true`
- [ ] `discharge` has `targetType = Adjacent`
- [ ] Status moves have `power = 0`
- [ ] EditMode test: load all `MoveConfig` assets, assert count >= 19
- [ ] EditMode test: `boulder-slam.power == 100`
- [ ] EditMode test: `spore-cloud.targetType == TargetType.AoE`
