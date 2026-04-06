# Move Database

## 1. Overview

The Move Database defines the `MoveConfig` ScriptableObject schema — the immutable blueprint for every move in Gene Forge. Each config captures the move's **genome type** (one of 14 types for type effectiveness), **damage form** (Physical, Energy, or Bio — determining stat pairing, range, and terrain interaction), power, accuracy, PP, targeting pattern, priority, and a list of on-hit or on-use effects. The database is loaded at startup by `ConfigLoader`. Forty-five MVP moves are defined below, spanning the 14 genome types and all three damage forms, forming the starting pool for the creature roster defined in `creature-database.md`.

## 2. Player Fantasy

Every move feels distinct. A high-power, low-accuracy move is a gamble with a payoff. A status move that roots an enemy to the tile feels like a tactical decision, not a wasted turn. AoE moves reshape the board. **Choosing between a Physical claw strike, an Energy beam, or a Bio spore attack feels like picking the right tool for the terrain** — not just the right type matchup. The move list reads like a toolkit, not a stat sheet — each entry evokes an image of the creature using it.

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
        [Header("Genome Type & Damage Form")]
        [SerializeField] CreatureType genomeType;   // Type effectiveness (14 types + None)
        [SerializeField] DamageForm form;            // Physical, Energy, Bio, or None (status)

        [Header("Stats")]
        [SerializeField] int power;        // Base power; 0 for Status moves
        [SerializeField] int accuracy;     // 0-100; 0 = always hits (e.g. Feint Attack)
        [SerializeField] int pp;           // Uses per battle (Power Points)
        [SerializeField] int priority;     // Higher priority moves go first regardless of speed

        [Header("Targeting")]
        [SerializeField] TargetType targetType;
        [SerializeField] int range;        // Chebyshev distance; defaults from form if unset

        [Header("Effects")]
        [SerializeField] List<MoveEffect> effects;

        // ── Properties ───────────────────────────────────────────────────
        public CreatureType GenomeType     => genomeType;
        public DamageForm Form             => form;
        public int Power                   => power;
        public int Accuracy                => accuracy;
        public int PP                      => pp;
        public int Priority                => priority;
        public TargetType TargetType       => targetType;
        public int Range                   => range;
        public IReadOnlyList<MoveEffect> Effects => effects;

        public bool IsDamaging             => form != DamageForm.None && power > 0;
        public bool AlwaysHits             => accuracy == 0;
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
        IgnoreDefense = 5,       // Damage ignores target's DEF/SPD
        MultiHit = 6,            // Hits 2-5 times; magnitude = max hits
        HighCrit = 7,            // Increased critical hit chance
        Flinch = 8,              // Target loses next action (chance-based)
        TerrainCreate = 9,       // Creates terrain tile at target location
        PriorityNext = 10        // User's next move gains +1 priority
    }
}
```

### 3.2 Damage Form Defaults and Targeting

Every damaging move has a genome type AND a damage form. Default targeting per form:

| Form | Default Range | Default Target | Stat Pairing | Notes |
|------|--------------|---------------|-------------|-------|
| Physical | 1–2 tiles | Single adjacent | ATK vs DEF | Blocked by walls and cover |
| Energy | 3–5 tiles | Single at range | ATK vs SPD | Requires LoS, cover reduces by 50% |
| Bio | 2–3 tiles | Single mid-range | ACC vs DEF | Ignores cover, no height bonus |
| None | — | — | — | Status moves; no damage form |

Individual moves can override the default range (e.g., a Physical charge move with range 3). See `damage-health-system.md` for full form interaction rules.

### 3.3 Move Learning and Body Part Access

A creature can only **use** moves of a damage form it has body part access to. Form access is derived from equipped body parts (see `body-part-system.md`):
- Physical form ← Jaws, Claws, Horns, Tail weapons
- Energy form ← Glands, Vents, Core Organ
- Bio form ← Spore Pods, Stingers, Tendrils

A creature without the appropriate body part has moves of that form **suspended** (greyed out) until the part is equipped. Move learning screens filter by available forms.

### 3.4 PP System

PP is tracked per move slot on `CreatureInstance`, not on `MoveConfig`. `MoveConfig.PP` is the max PP value copied to the instance at time of learning. PP refills fully after each battle (MVP simplification; in-battle PP management is post-MVP).

### 3.5 Priority System

Priority values determine action order within a turn phase, overriding speed:

| Priority | Examples |
|----------|---------|
| +2 | Quick Strike, Protective Shell |
| +1 | Status moves with PriorityNext effect, Ice Shard |
| 0 | All standard moves |
| -1 | Heavy charge moves |

Same-priority moves resolve by SPD stat, then by random tiebreak.

### 3.6 Accuracy Resolution

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

### 3.7 MVP Move List (45 Moves)

---

#### 1. Scratch
- **ID:** `scratch`
- **Genome Type:** None | **Form:** Physical
- **Power:** 40 | **Accuracy:** 100 | **PP:** 35 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** None
- **Notes:** Basic first move for most Bipedal creatures. Typeless — no STAB, no type effectiveness.

---

#### 2. Tackle
- **ID:** `tackle`
- **Genome Type:** None | **Form:** Physical
- **Power:** 40 | **Accuracy:** 100 | **PP:** 35 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** None
- **Notes:** Basic first move for Quadruped and heavier archetypes. Typeless.

---

#### 3. Ember
- **ID:** `ember`
- **Genome Type:** Thermal | **Form:** Energy
- **Power:** 40 | **Accuracy:** 100 | **PP:** 25 | **Priority:** 0
- **Target:** Single | **Range:** 3
- **Effects:** `ApplyStatus(Burn, 10% chance)`
- **Notes:** Standard early Thermal ranged move; small burn chance introduces status pressure. Requires Energy-form body part (Glands/Vents).

---

#### 4. Flame Claw
- **ID:** `flame-claw`
- **Genome Type:** Thermal | **Form:** Physical
- **Power:** 65 | **Accuracy:** 95 | **PP:** 15 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** `ApplyStatus(Burn, 20% chance)`
- **Notes:** Mid-tier Thermal physical; higher burn rate rewards melee positioning. Requires Physical-form body part (Claws).

---

#### 5. Inferno Dash
- **ID:** `inferno-dash`
- **Genome Type:** Thermal | **Form:** Physical
- **Power:** 80 | **Accuracy:** 90 | **PP:** 10 | **Priority:** 0
- **Target:** Single | **Range:** 3
- **Effects:** `Recoil(25%)`, `ForcedMove(1 tile away, target)`
- **Notes:** Emberfox signature-style move. Dashes to target, deals damage, pushes target back 1 tile, user takes 25% recoil. Override range (3) for a Physical move.

---

#### 6. Vine Lash
- **ID:** `vine-lash`
- **Genome Type:** Organic | **Form:** Physical
- **Power:** 45 | **Accuracy:** 100 | **PP:** 25 | **Priority:** 0
- **Target:** Single | **Range:** 2
- **Effects:** None
- **Notes:** Reliable early Organic physical with slightly extended range.

---

#### 7. Root Bind
- **ID:** `root-bind`
- **Genome Type:** Organic | **Form:** None (Status)
- **Power:** 0 | **Accuracy:** 85 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 2
- **Effects:** `ApplyStatus(Paralysis, 100%)`, `TerrainCreate(Difficult at target tile)`
- **Notes:** Immobilizes target and creates difficult terrain at their tile. Repositioning counter.

---

#### 8. Spore Cloud
- **ID:** `spore-cloud`
- **Genome Type:** Organic | **Form:** Bio
- **Power:** 0 | **Accuracy:** 90 | **PP:** 15 | **Priority:** 0
- **Target:** AoE | **Range:** 2
- **Effects:** `ApplyStatus(Sleep, 100%)` — hits all enemies within range 2
- **Notes:** High-impact AoE sleep via Bio form (bypasses cover). Short range forces commitment. Requires Bio-form body part (Spore Pods).

---

#### 9. Toxic Spore
- **ID:** `toxic-spore`
- **Genome Type:** Toxic | **Form:** Bio
- **Power:** 45 | **Accuracy:** 90 | **PP:** 15 | **Priority:** 0
- **Target:** Adjacent | **Range:** 2
- **Effects:** `ApplyStatus(Poison, 100%)` on all adjacent enemies
- **Notes:** Thornslug's area poison tool via Bio form. Ignores cover. Rewards surrounding enemies.

---

#### 10. Water Pulse
- **ID:** `water-pulse`
- **Genome Type:** Aqua | **Form:** Energy
- **Power:** 60 | **Accuracy:** 100 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 4
- **Effects:** `ApplyStatus(Confusion, 20% chance)`
- **Notes:** Long-range Aqua Energy move with confusion chance. Requires LoS.

---

#### 11. Aqua Bolt
- **ID:** `aqua-bolt`
- **Genome Type:** Aqua | **Form:** Energy
- **Power:** 80 | **Accuracy:** 90 | **PP:** 10 | **Priority:** 0
- **Target:** Line | **Range:** 5
- **Effects:** None
- **Notes:** Fires through a line of tiles hitting all creatures in the path. Powerful but narrow. Requires LoS along the line.

---

#### 12. Spark
- **ID:** `spark`
- **Genome Type:** Bioelectric | **Form:** Physical
- **Power:** 65 | **Accuracy:** 100 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** `ApplyStatus(Paralysis, 30% chance)`
- **Notes:** Contact Bioelectric move. Paralysis chance strong on fast attackers like Voltfin.

---

#### 13. Discharge
- **ID:** `discharge`
- **Genome Type:** Bioelectric | **Form:** Energy
- **Power:** 80 | **Accuracy:** 100 | **PP:** 15 | **Priority:** 0
- **Target:** Adjacent | **Range:** 1
- **Effects:** `ApplyStatus(Paralysis, 30% chance)` on all adjacent
- **Notes:** Full radial AoE Energy discharge hitting all adjacent creatures (including allies — use carefully). Override range (1) for an Energy move.

---

#### 14. Ice Shard
- **ID:** `ice-shard`
- **Genome Type:** Cryo | **Form:** Physical
- **Power:** 40 | **Accuracy:** 100 | **PP:** 30 | **Priority:** +1
- **Target:** Single | **Range:** 2
- **Effects:** None
- **Notes:** High priority Cryo physical. Useful for finishing off faster enemies before they act.

---

#### 15. Frost Breath
- **ID:** `frost-breath`
- **Genome Type:** Cryo | **Form:** Energy
- **Power:** 60 | **Accuracy:** 90 | **PP:** 15 | **Priority:** 0
- **Target:** Single | **Range:** 3
- **Effects:** `ApplyStatus(Freeze, 10% chance)`
- **Notes:** Mid-range Cryo Energy beam. Freeze is rare but devastating — skips target's next turn.

---

#### 16. Rock Throw
- **ID:** `rock-throw`
- **Genome Type:** Mineral | **Form:** Physical
- **Power:** 50 | **Accuracy:** 90 | **PP:** 15 | **Priority:** 0
- **Target:** Single | **Range:** 3
- **Effects:** None
- **Notes:** Ranged Mineral physical (thrown projectile). Override range (3). Works on Mosshell and Coalbear at low levels.

---

#### 17. Boulder Slam
- **ID:** `boulder-slam`
- **Genome Type:** Mineral | **Form:** Physical
- **Power:** 100 | **Accuracy:** 75 | **PP:** 10 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** `ForcedMove(2 tiles away, target)`, `Flinch(50% chance)`
- **Notes:** High-risk, high-reward melee. Pushes target 2 tiles; can knock off elevated ground. Mosshell's signature damage tool.

---

#### 18. Neural Claw
- **ID:** `neural-claw`
- **Genome Type:** Neural | **Form:** Physical
- **Power:** 70 | **Accuracy:** 100 | **PP:** 15 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** `HighCrit`
- **Notes:** Reliable Neural physical with boosted critical hit rate. Core offensive tool for Shadowmite. (Renamed from Shadow Claw.)

---

#### 19. Mind Beam
- **ID:** `mind-beam`
- **Genome Type:** Neural | **Form:** Energy
- **Power:** 65 | **Accuracy:** 100 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 4
- **Effects:** `ApplyStatus(Confusion, 30% chance)`
- **Notes:** Long-range Neural Energy beam. Confusion chance disrupts enemy action planning. (Replaces Psybeam.)

---

#### 20. Taunt
- **ID:** `taunt`
- **Genome Type:** Neural | **Form:** None (Status)
- **Power:** 0 | **Accuracy:** 100 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 3
- **Effects:** `ApplyStatus(Taunt, 100%)`
- **Notes:** Forces target to use only offensive moves for 3 rounds. Critical aggro manipulation tool.

---

#### 21. Feint Attack
- **ID:** `feint-attack`
- **Genome Type:** Kinetic | **Form:** Physical
- **Power:** 60 | **Accuracy:** 0 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 2
- **Effects:** None
- **Notes:** Always hits (accuracy = 0). Cannot be avoided by evasion boosts. Kinetic type rewards raw physical force.

---

#### 22. Ferro Bite
- **ID:** `ferro-bite`
- **Genome Type:** Ferro | **Form:** Physical
- **Power:** 60 | **Accuracy:** 100 | **PP:** 25 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** `Flinch(30% chance)`
- **Notes:** Bio-metallic fangs crunch the target. Flinch prevents target from acting. (Replaces Bite/Dark.)

---

#### 23. Harden
- **ID:** `harden`
- **Genome Type:** None | **Form:** None (Status)
- **Power:** 0 | **Accuracy:** 0 | **PP:** 30 | **Priority:** 0
- **Target:** Self | **Range:** 0
- **Effects:** `StatStage(DEF, +1, 100%, affectsSelf=true)`
- **Notes:** Always hits self. Raises DEF by 1 stage. Essential for tank creatures like Mosshell.

---

#### 24. Agility
- **ID:** `agility`
- **Genome Type:** None | **Form:** None (Status)
- **Power:** 0 | **Accuracy:** 0 | **PP:** 30 | **Priority:** 0
- **Target:** Self | **Range:** 0
- **Effects:** `StatStage(SPD, +2, 100%, affectsSelf=true)`
- **Notes:** Always hits self. Raises SPD by 2 stages. Emberfox's speed amplifier.

---

#### 25. Leech Sting
- **ID:** `leech-sting`
- **Genome Type:** Toxic | **Form:** Bio
- **Power:** 50 | **Accuracy:** 95 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 2
- **Effects:** `Drain(50%)`
- **Notes:** Bio-form Toxic attack. Stinger injects parasites that drain health. Ignores cover. User heals 50% of damage dealt.

---

#### 26. Iron Bash
- **ID:** `iron-bash`
- **Genome Type:** Ferro | **Form:** Physical
- **Power:** 50 | **Accuracy:** 100 | **PP:** 25 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** `Flinch(20% chance)`
- **Notes:** Reliable Ferro melee. Metallic headbutt with flinch chance. Ferrovex's bread-and-butter.

---

#### 27. Metal Press
- **ID:** `metal-press`
- **Genome Type:** Ferro | **Form:** Physical
- **Power:** 80 | **Accuracy:** 85 | **PP:** 10 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** `StatStage(DEF, -1, 50% chance)`
- **Notes:** Heavy Ferro slam that can crack armor. Lower accuracy compensated by DEF shred.

---

#### 28. Siege Slam
- **ID:** `siege-slam`
- **Genome Type:** Ferro | **Form:** Physical
- **Power:** 100 | **Accuracy:** 80 | **PP:** 5 | **Priority:** -1
- **Target:** Single | **Range:** 1
- **Effects:** `Recoil(20%)`, `ForcedMove(1 tile away, target)`
- **Notes:** Ferrovex's heaviest hit. Low priority, low accuracy, high recoil — but devastating when it lands.

---

#### 29. Wind Slash
- **ID:** `wind-slash`
- **Genome Type:** Aero | **Form:** Physical
- **Power:** 55 | **Accuracy:** 95 | **PP:** 25 | **Priority:** 0
- **Target:** Single | **Range:** 2
- **Effects:** None
- **Notes:** Blade of compressed air. Reliable early Aero physical with slight range extension.

---

#### 30. Sonic Pulse
- **ID:** `sonic-pulse`
- **Genome Type:** Sonic | **Form:** Energy
- **Power:** 50 | **Accuracy:** 100 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 3
- **Effects:** `ApplyStatus(Confusion, 10% chance)`
- **Notes:** Focused sound wave. Standard early Sonic ranged attack with minor confusion.

---

#### 31. Gust
- **ID:** `gust`
- **Genome Type:** Aero | **Form:** Energy
- **Power:** 40 | **Accuracy:** 100 | **PP:** 25 | **Priority:** 0
- **Target:** Single | **Range:** 4
- **Effects:** `ForcedMove(1 tile away, target)`
- **Notes:** Weak but repositions. Long-range Aero Energy blast that pushes target back 1 tile.

---

#### 32. Screech
- **ID:** `screech`
- **Genome Type:** Sonic | **Form:** None (Status)
- **Power:** 0 | **Accuracy:** 85 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 3
- **Effects:** `StatStage(DEF, -2, 100%)`
- **Notes:** Piercing sonic shriek that sharply lowers target's DEF. Sets up physical attackers.

---

#### 33. Cyclone Strike
- **ID:** `cyclone-strike`
- **Genome Type:** Aero | **Form:** Physical
- **Power:** 85 | **Accuracy:** 90 | **PP:** 10 | **Priority:** 0
- **Target:** Adjacent | **Range:** 1
- **Effects:** None
- **Notes:** Spinning wind attack hitting all adjacent enemies. Galewhip's finisher — high power AoE melee.

---

#### 34. Power Strike
- **ID:** `power-strike`
- **Genome Type:** Kinetic | **Form:** Physical
- **Power:** 60 | **Accuracy:** 100 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** None
- **Notes:** Concentrated force blow. Reliable mid-tier Kinetic physical. Quarrok's workhorse move.

---

#### 35. Seismic Smash
- **ID:** `seismic-smash`
- **Genome Type:** Kinetic | **Form:** Physical
- **Power:** 90 | **Accuracy:** 85 | **PP:** 10 | **Priority:** 0
- **Target:** Adjacent | **Range:** 1
- **Effects:** `ForcedMove(1 tile away, all targets)`
- **Notes:** Ground-shaking AoE that knocks all adjacent enemies back 1 tile. Devastating on elevated terrain.

---

#### 36. Acid Spray
- **ID:** `acid-spray`
- **Genome Type:** Toxic | **Form:** Bio
- **Power:** 55 | **Accuracy:** 95 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 2
- **Effects:** `StatStage(DEF, -1, 100%)`
- **Notes:** Corrosive Bio projectile that always lowers DEF. Guaranteed armor shred sets up follow-up attacks.

---

#### 37. Corrode
- **ID:** `corrode`
- **Genome Type:** Toxic | **Form:** Bio
- **Power:** 70 | **Accuracy:** 90 | **PP:** 15 | **Priority:** 0
- **Target:** Single | **Range:** 2
- **Effects:** `ApplyStatus(Poison, 50% chance)`, `StatStage(DEF, -1, 50% chance)`
- **Notes:** Advanced Toxic Bio attack. Chance to poison AND shred DEF. Corrovex's mid-game pressure tool.

---

#### 38. Rust Lash
- **ID:** `rust-lash`
- **Genome Type:** Ferro | **Form:** Physical
- **Power:** 75 | **Accuracy:** 95 | **PP:** 15 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** `StatStage(DEF, -1, 50% chance)`
- **Notes:** Oxidizing metallic strike. Ferro physical with DEF shred chance. Corrovex's hybrid identity move.

---

#### 39. Purify
- **ID:** `purify`
- **Genome Type:** Ark | **Form:** None (Status)
- **Power:** 0 | **Accuracy:** 0 | **PP:** 15 | **Priority:** 0
- **Target:** Self | **Range:** 0
- **Effects:** Removes all status conditions from user
- **Notes:** Always hits self. Ark's signature cleanse. Cures Burn, Freeze, Paralysis, Poison, Sleep, Confusion.

---

#### 40. Stasis Field
- **ID:** `stasis-field`
- **Genome Type:** Ark | **Form:** Energy
- **Power:** 60 | **Accuracy:** 100 | **PP:** 15 | **Priority:** 0
- **Target:** Single | **Range:** 3
- **Effects:** `ApplyStatus(Paralysis, 30% chance)`
- **Notes:** Crystalline energy beam that can lock targets in stasis. Ark's main offensive tool.

---

#### 41. Genetic Lock
- **ID:** `genetic-lock`
- **Genome Type:** Ark | **Form:** None (Status)
- **Power:** 0 | **Accuracy:** 90 | **PP:** 10 | **Priority:** 0
- **Target:** Single | **Range:** 3
- **Effects:** `StatStage(ATK, -2, 100%)`
- **Notes:** Stabilizes target's genome, sharply reducing attack power. Ark's signature debuff — thematically locks down genetic instability.

---

#### 42. Blight Claw
- **ID:** `blight-claw`
- **Genome Type:** Blight | **Form:** Physical
- **Power:** 60 | **Accuracy:** 100 | **PP:** 20 | **Priority:** 0
- **Target:** Single | **Range:** 1
- **Effects:** `ApplyStatus(Poison, 30% chance)`
- **Notes:** Corrupted physical strike. Reliable Blight melee with poison chance. Blighthowl's early Physical option.

---

#### 43. Corrupt
- **ID:** `corrupt`
- **Genome Type:** Blight | **Form:** Bio
- **Power:** 55 | **Accuracy:** 90 | **PP:** 15 | **Priority:** 0
- **Target:** Single | **Range:** 2
- **Effects:** `ApplyStatus(Confusion, 50% chance)`
- **Notes:** Blight Bio attack that destabilizes the target's mind. High confusion rate makes it a control tool.

---

#### 44. Entropic Howl
- **ID:** `entropic-howl`
- **Genome Type:** Blight | **Form:** Bio
- **Power:** 70 | **Accuracy:** 95 | **PP:** 15 | **Priority:** 0
- **Target:** AoE | **Range:** 2
- **Effects:** `StatStage(ATK, -1, 100%, all enemies in range)`
- **Notes:** Wave of entropic corruption that weakens all nearby enemies. Blighthowl's AoE debuff — delivered via Bio form (biological corruption emission).

---

#### 45. Genetic Collapse
- **ID:** `genetic-collapse`
- **Genome Type:** Blight | **Form:** Bio
- **Power:** 90 | **Accuracy:** 85 | **PP:** 5 | **Priority:** 0
- **Target:** Single | **Range:** 2
- **Effects:** `HighCrit`, `ApplyStatus(Poison, 30% chance)`
- **Notes:** Blight's ultimate attack. Triggers cascading genetic failure in the target. Low PP, high risk/reward with boosted crit rate.

---

## 4. Formulas

| Formula | Expression |
|---------|-----------|
| Hit chance | `(accuracy / 100) × accuracyStageMultiplier × (1 / evasionStageMultiplier)` |
| Always-hits condition | `move.accuracy == 0` |
| Stat pairing (Physical) | ATK vs DEF |
| Stat pairing (Energy) | ATK vs SPD |
| Stat pairing (Bio) | ACC vs DEF |
| Critical hit base chance | `6.25%` (1/16); HighCrit effect raises to `12.5%` |
| Critical hit damage multiplier | `1.5×` (applied in DamageCalculator) |
| Multi-hit distribution | Uniform random 2–5 hits; each hit calculated independently |
| Recoil damage | `floor(damageDealt × recoilFraction)` |
| Drain heal | `floor(damageDealt × drainFraction)` |

## 5. Edge Cases

| Situation | Behavior |
|-----------|----------|
| Move used with 0 PP remaining | Move fails; "Out of PP" message shown; creature uses Struggle (10 base power, typeless, Physical form, 25% recoil) |
| Status move targeting already-statused creature | Move fails; "Already has a status condition" message |
| AoE/Adjacent move with no valid targets | Move fails; PP still consumed |
| `accuracy = 0` — does this mean 0% or always hit? | Always hits — `AlwaysHits` property resolves this; `accuracy == 0` is the always-hit sentinel |
| `ForcedMove` pushes target off grid edge | Target placed at nearest valid tile; fall damage applies if height drop ≥ 3 |
| `TerrainCreate` on already-typed terrain tile | Replaces terrain type; previous synergy lost |
| Multi-hit move: target faints on hit 2 of 5 | Remaining hits do not resolve; damage tallied from completed hits |
| `Line` targeting: ally in path | By default allies are not hit; post-MVP: friendly fire toggle |
| Confusion status: creature hits itself | Uses attacker's own ATK vs own DEF with fixed 40 base power, typeless, Physical form |
| Creature lacks body part for move's form | Move is suspended (greyed out); cannot be selected in combat |
| Energy move with no LoS to target | Move cannot target — blocked at targeting phase |
| Bio move through cover | Full damage — Bio ignores cover |
| Physical move through wall | Move cannot target — blocked at targeting phase |

## 6. Dependencies

| Dependency | Direction | Notes |
|------------|-----------|-------|
| `ConfigBase` | Inbound | Provides `id`, `displayName` |
| `Enums.cs` | Inbound | `CreatureType` (14 genome types), `DamageForm`, `TargetType`, `StatusEffect` |
| `ConfigLoader` | Inbound | Loads all `MoveConfig` assets at startup |
| `CreatureDatabase` | Outbound | Move IDs in creature move pools must exist here |
| `DamageCalculator` | Outbound | Reads `power`, `genomeType`, `form`, `effects` per move |
| `TurnManager` | Outbound | Reads `priority`, `targetType`, `range`, `pp` |
| `CreatureInstance` | Outbound | Tracks current PP per learned move slot; form access from body parts |
| `TypeChart` | Outbound | `genomeType` used for effectiveness and STAB lookups |
| `BodyPartSystem` | Outbound | Form access determines which moves can be used |

## 7. Tuning Knobs

| Parameter | Location | Default | Notes |
|-----------|----------|---------|-------|
| Struggle base power | `GameSettings` SO | 10 | Typeless fallback when all PP exhausted |
| Struggle recoil | `GameSettings` SO | 25% | Fraction of damage taken as recoil |
| Struggle form | Design | Physical | Always melee, ATK vs DEF |
| Critical hit base rate | `GameSettings` SO | 0.0625 (6.25%) | 1/16 |
| HighCrit rate | `GameSettings` SO | 0.125 (12.5%) | 1/8 |
| Critical hit multiplier | `GameSettings` SO | 1.5× | Applied in DamageCalculator |
| Max PP per move | Convention | 35 | No hard cap; design guideline |
| Max move range (Physical) | Convention | 2 | Default melee range |
| Max move range (Energy) | Convention | 5 | Beyond 5 = effectively global |
| Max move range (Bio) | Convention | 3 | Mid-range biological delivery |

## 8. Acceptance Criteria

- [ ] All 45 MVP moves load from `Resources/Data/Moves/` without errors
- [ ] No two moves share the same `id`
- [ ] Every damaging move has both a valid `GenomeType` and a valid `Form` (not None)
- [ ] Every status move has `Form = None` and `Power = 0`
- [ ] All move IDs referenced in creature move pools exist in this database
- [ ] `feint-attack` has `accuracy = 0` and `AlwaysHits` returns true
- [ ] `ice-shard` has `priority = 1`
- [ ] `root-bind` effect list contains both `ApplyStatus(Paralysis)` and `TerrainCreate`
- [ ] `harden` has `targetType = Self` and `affectsSelf = true`
- [ ] `discharge` has `targetType = Adjacent`
- [ ] `flame-claw` has `genomeType = Thermal` and `form = Physical`
- [ ] `ember` has `genomeType = Thermal` and `form = Energy`
- [ ] `toxic-spore` has `genomeType = Toxic` and `form = Bio`
- [ ] `leech-sting` has `genomeType = Toxic` and `form = Bio` with `Drain(50%)`
- [ ] Creature without Physical body part has Physical moves greyed out
- [ ] EditMode test: load all `MoveConfig` assets, assert count >= 45
- [ ] EditMode test: `boulder-slam.Power == 100 && boulder-slam.Form == DamageForm.Physical`
- [ ] EditMode test: `spore-cloud.TargetType == TargetType.AoE && spore-cloud.Form == DamageForm.Bio`
