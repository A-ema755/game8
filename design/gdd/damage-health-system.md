# Damage & Health System

## 1. Overview

The Damage & Health System governs how creatures take and sustain damage during combat. Every damaging move has both a **genome type** (determining type effectiveness) and a **damage form** (Physical, Energy, or Bio) that determines stat pairings, grid range, and terrain interactions. A unified damage formula accounts for attacker stats, defender stats, move properties, damage form, type effectiveness, terrain synergy, height advantage, and random variance. Critical hits can double the final damage. The system applies status effects from moves (Burn, Poison), manages HP drain and recoil, and detects faints. `DamageCalculator` is a pure C# class providing both exact damage calculation for hit resolution and an estimate function for AI decision-making (without random variance).

## 2. Player Fantasy

Damage feels earned and predictable. A powerful move against a weak defender deals huge numbers, making the player feel their team is strong. A resisted move against a bulky tank does minimal damage, teaching type matchups and positioning. **Choosing the right damage form adds a second layer of tactical mastery** — a Physical strike rewards closing distance and high ground, an Energy beam rewards line-of-sight positioning, and a Bio attack bypasses cover to punish turtling defenders. Terrain synergy rewards thoughtful positioning — standing on your type's terrain makes you hit harder. Critical hits arrive with excitement but don't feel random — the system is transparent enough that skilled players can play around odds.

## 3. Detailed Rules

### 3.1 Damage Forms

```csharp
namespace GeneForge.Combat
{
    /// <summary>
    /// Determines the physical mechanism of a move, affecting stat pairing,
    /// range, and terrain interaction. Every damaging move has exactly one form.
    /// </summary>
    public enum DamageForm
    {
        None,       // Status moves — no damage form
        Physical,   // Body contact (claws, fangs, slam) — ATK vs DEF
        Energy,     // Elemental emission (beam, bolt, ray) — ATK vs SPD
        Bio         // Biological invasion (spores, venom, parasites) — ACC vs DEF
    }
}
```

| Form | Mechanism | Grid Range | Stat Pairing | Terrain Interaction |
|------|-----------|------------|-------------|-------------------|
| **Physical** | Body contact (claws, fangs, slam) | 1–2 tiles (melee) | ATK vs DEF | Blocked by walls, +height bonus |
| **Energy** | Elemental emission (beam, bolt, ray) | 3–5 tiles (ranged) | ATK vs SPD | Requires line-of-sight, +height bonus |
| **Bio** | Biological invasion (spores, venom, parasites) | 2–3 tiles (mid-range) | ACC vs DEF | Ignores cover, no height bonus |

- Every damaging move has exactly one genome type AND one damage form
- Status moves use `DamageForm.None`
- Body parts determine which forms a creature can use (see body-part-system.md)

### 3.2 MoveConfig (Updated)

```csharp
/// <summary>
/// ScriptableObject defining a move's static data.
/// Genome type determines type effectiveness; damage form determines stat pairing and grid behavior.
/// </summary>
[CreateAssetMenu(fileName = "NewMove", menuName = "GeneForge/MoveConfig")]
public class MoveConfig : ScriptableObject
{
    [SerializeField] string id;
    [SerializeField] string displayName;
    [SerializeField] CreatureType genomeType;   // Type effectiveness
    [SerializeField] DamageForm form;            // Stat pairing + terrain rules
    [SerializeField] int power;
    [SerializeField] int accuracy;
    [SerializeField] int pp;
    [SerializeField] int range;                  // Default from form, overridable
    [SerializeField] MoveEffect[] effects;

    public string Id => id;
    public string DisplayName => displayName;
    public CreatureType GenomeType => genomeType;
    public DamageForm Form => form;
    public int Power => power;
    public int Accuracy => accuracy;
    public int PP => pp;
    public int Range => range;
    public bool IsDamaging => power > 0 && form != DamageForm.None;
    public MoveEffect[] Effects => effects;
}
```

### 3.3 Damage Formula

```csharp
namespace GeneForge.Combat
{
    /// <summary>
    /// Calculates exact or estimated damage for a move hit.
    /// Pure C# — no dependencies on MonoBehaviour or game state.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Calculate exact damage for a move, accounting for all multipliers and randomness.
        /// Used during actual hit resolution in combat.
        /// </summary>
        public static int Calculate(
            MoveConfig move,
            CreatureInstance attacker,
            CreatureInstance defender,
            GridSystem grid,
            System.Random rng = null)
        {
            if (!move.IsDamaging)
                return 0;

            rng = rng ?? new System.Random();

            // Base damage formula:
            // damage = ((2*level/5+2) * power * (offStat/defStat) / 50 + 2) * STAB * typeEffect * terrainSynergy * heightBonus * critical * random(0.85,1.0)
            // Minimum: 1

            int level = attacker.Level;
            int power = move.Power;

            // Form-based stat pairing
            int offStat, defStat;
            GetFormStatPairing(move.Form, attacker, defender, out offStat, out defStat);

            // Level-based coefficient
            float levelCoeff = (2f * level / 5f) + 2f;

            // Offensive/Defensive stat ratio
            float statRatio = (float)offStat / Mathf.Max(1, defStat);

            // Base formula (before multipliers)
            float baseDamage = (levelCoeff * power * statRatio / 50f) + 2f;

            // STAB (Same Type Attack Bonus) = 1.5x if move genome type matches attacker's primary or secondary type
            float stabMult = TypeChart.GetStab(move.GenomeType, attacker);

            // Type effectiveness (resist/neutral/super-effective)
            float typeEffectMult = TypeChart.GetMultiplier(move.GenomeType, defender.Config.PrimaryType)
                                  * (defender.Config.IsDualType
                                      ? TypeChart.GetMultiplier(move.GenomeType, defender.Config.SecondaryType)
                                      : 1f);

            // Terrain synergy: if defender is on a tile matching their terrain synergy type, they take 20% less damage
            // OR if attacker is on a matching tile, they deal 20% more damage
            float terrainSynergyMult = 1f;
            var defenderTile = grid.GetTile(defender.GridPosition);
            var attackerTile = grid.GetTile(attacker.GridPosition);

            if (defenderTile != null && defenderTile.Terrain == defender.Config.TerrainSynergyType)
                terrainSynergyMult *= 0.8f; // Defender resists
            if (attackerTile != null && attackerTile.Terrain == attacker.Config.TerrainSynergyType)
                terrainSynergyMult *= 1.2f; // Attacker amplifies

            // Height bonus — form-dependent
            float heightBonus = GetFormHeightBonus(move.Form, attackerTile, defenderTile);

            // Critical hit multiplier (1.5x) — 6.25% base, 12.5% with HighCrit effect
            float critMult = 1f;
            bool isCritical = RollCritical(move, attacker, rng);
            if (isCritical)
                critMult = 1.5f;

            // Random variance: 0.85x to 1.0x
            float variance = 0.85f + (float)rng.NextDouble() * 0.15f;

            // Final calculation
            float finalDamage = baseDamage * stabMult * typeEffectMult * terrainSynergyMult * heightBonus * critMult * variance;

            return Mathf.Max(1, (int)finalDamage);
        }

        /// <summary>
        /// Estimate damage without randomness (for AI decision-making).
        /// Uses fixed variance of 0.925 (midpoint of 0.85-1.0).
        /// </summary>
        public static int Estimate(
            MoveConfig move,
            CreatureInstance attacker,
            CreatureInstance defender,
            GridSystem grid)
        {
            if (!move.IsDamaging)
                return 0;

            int level = attacker.Level;
            int power = move.Power;

            int offStat, defStat;
            GetFormStatPairing(move.Form, attacker, defender, out offStat, out defStat);

            float levelCoeff = (2f * level / 5f) + 2f;
            float statRatio = (float)offStat / Mathf.Max(1, defStat);
            float baseDamage = (levelCoeff * power * statRatio / 50f) + 2f;

            float stabMult = TypeChart.GetStab(move.GenomeType, attacker);

            float typeEffectMult = TypeChart.GetMultiplier(move.GenomeType, defender.Config.PrimaryType)
                                  * (defender.Config.IsDualType
                                      ? TypeChart.GetMultiplier(move.GenomeType, defender.Config.SecondaryType)
                                      : 1f);

            float terrainSynergyMult = 1f;
            var defenderTile = grid.GetTile(defender.GridPosition);
            var attackerTile = grid.GetTile(attacker.GridPosition);

            if (defenderTile != null && defenderTile.Terrain == defender.Config.TerrainSynergyType)
                terrainSynergyMult *= 0.8f;
            if (attackerTile != null && attackerTile.Terrain == attacker.Config.TerrainSynergyType)
                terrainSynergyMult *= 1.2f;

            float heightBonus = GetFormHeightBonus(move.Form, attackerTile, defenderTile);

            // No critical hit in estimate; no random variance
            float finalDamage = baseDamage * stabMult * typeEffectMult * terrainSynergyMult * heightBonus * 0.925f;

            return Mathf.Max(1, (int)finalDamage);
        }

        /// <summary>
        /// Select offensive and defensive stats based on damage form.
        /// Physical: ATK vs DEF. Energy: ATK vs SPD. Bio: ACC vs DEF.
        /// </summary>
        private static void GetFormStatPairing(
            DamageForm form,
            CreatureInstance attacker,
            CreatureInstance defender,
            out int offStat,
            out int defStat)
        {
            switch (form)
            {
                case DamageForm.Energy:
                    offStat = attacker.ComputedStats.ATK;
                    defStat = defender.ComputedStats.SPD;
                    break;
                case DamageForm.Bio:
                    offStat = attacker.ComputedStats.ACC;
                    defStat = defender.ComputedStats.DEF;
                    break;
                case DamageForm.Physical:
                default:
                    offStat = attacker.ComputedStats.ATK;
                    defStat = defender.ComputedStats.DEF;
                    break;
            }
        }

        /// <summary>
        /// Calculate height bonus based on damage form.
        /// Physical/Energy: +10% per height level above defender (cap 2.0x).
        /// Bio: no height bonus (spores/parasites don't care about elevation).
        /// </summary>
        private static float GetFormHeightBonus(DamageForm form, GridTile attackerTile, GridTile defenderTile)
        {
            if (form == DamageForm.Bio)
                return 1f; // Bio ignores height

            int heightDiff = attackerTile.Height - defenderTile.Height;
            if (heightDiff <= 0)
                return 1f; // No penalty for being lower

            return Mathf.Min(2f, 1f + (heightDiff * 0.1f)); // Cap at 2.0x
        }

        /// <summary>
        /// Determine if a hit is critical.
        /// Base: 6.25% (1/16). HighCrit effect: 12.5% (1/8).
        /// </summary>
        private static bool RollCritical(MoveConfig move, CreatureInstance attacker, System.Random rng)
        {
            float critChance = 0.0625f; // 1/16

            foreach (var effect in move.Effects)
            {
                if (effect.EffectType == MoveEffectType.HighCrit)
                    critChance = 0.125f; // 1/8
            }

            return rng.NextDouble() < critChance;
        }
    }
}
```

### 3.4 Health Management

```csharp
public class CreatureInstance
{
    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }

    /// <summary>Take damage and check for faint. Public for TurnManager.</summary>
    public void TakeDamage(int damage)
    {
        CurrentHP = Mathf.Max(0, CurrentHP - damage);
        if (CurrentHP == 0)
            Faint();
    }

    /// <summary>Heal HP up to max. Used by drain/recover moves and between battles.</summary>
    public void Heal(int amount)
    {
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
    }

    /// <summary>Heal to full (research station, battle reset).</summary>
    public void HealFull()
    {
        CurrentHP = MaxHP;
    }

    /// <summary>Mark creature as fainted. Used by TurnManager.</summary>
    public void Faint()
    {
        CurrentHP = 0;
        IsFainted = true;
    }

    /// <summary>Revive creature at specified HP. Used after permadeath check (post-MVP).</summary>
    public void Revive(int hp = 1)
    {
        IsFainted = false;
        CurrentHP = Mathf.Min(hp, MaxHP);
    }

    public bool IsFainted { get; private set; } = false;
}
```

### 3.5 Recoil and Drain Effects

Moves with `Recoil` or `Drain` effects are resolved in `TurnManager.ExecuteMoveAsync`:

```csharp
// After damage is applied to target:
foreach (var effect in action.Move.Effects)
{
    if (effect.EffectType == MoveEffectType.Recoil)
    {
        int recoilDamage = (int)(damage * (effect.Magnitude / 100f));
        action.Actor.TakeDamage(recoilDamage);
    }
    else if (effect.EffectType == MoveEffectType.Drain)
    {
        int healAmount = (int)(damage * (effect.Magnitude / 100f));
        action.Actor.Heal(healAmount);
    }
}
```

### 3.6 Damage Form Terrain Interactions

| Form | Walls | Cover (partial) | Line-of-Sight | Height Bonus |
|------|-------|-----------------|---------------|-------------|
| Physical | Blocked (cannot target through) | Blocked (cannot target through) | Not required | +10% per level above |
| Energy | Blocked | Reduces damage by 50% | Required | +10% per level above |
| Bio | Passes through | Ignores cover entirely | Not required | None |

- **Physical**: Melee attacks require adjacency (1–2 tiles). Walls and cover block targeting entirely. High ground provides +10% per height level.
- **Energy**: Ranged attacks at 3–5 tiles. Requires unbroken line-of-sight from attacker to target. Cover reduces Energy damage by 50% (half-cover). Height advantage provides +10% per level.
- **Bio**: Mid-range at 2–3 tiles. Spores, parasites, and venom bypass physical barriers — cover provides no protection. Height is irrelevant (biological agents drift/spread regardless of elevation).

## 4. Formulas

| Formula | Expression | Notes |
|---------|-----------|-------|
| Base damage | `((2×level/5+2) × power × (offStat/defStat) / 50 + 2)` | offStat/defStat determined by form |
| Stat pairing (Physical) | offStat = ATK, defStat = DEF | Melee combat |
| Stat pairing (Energy) | offStat = ATK, defStat = SPD | Fast creatures dodge beams |
| Stat pairing (Bio) | offStat = ACC, defStat = DEF | Precision-based biological attack |
| STAB (Primary) | `×1.5` | If move genome type == creature primary type |
| STAB (Secondary) | `×1.5` | If move genome type == creature secondary type |
| STAB (Infused) | `×1.5` | If move genome type == infused type (via DNA alteration) |
| Type effectiveness | `TypeChart[move.genomeType][defender.type]` | 2.0x (super), 1.0x (neutral), 0.5x (resist) |
| Dual-type effectiveness | Product of both types | Max 4.0x, min 0.25x |
| Terrain synergy (defender) | `×0.8` | If defender on matching terrain |
| Terrain synergy (attacker) | `×1.2` | If attacker on matching terrain |
| Height bonus (Physical/Energy) | `1.0 + (heightDiff × 0.1)` | +10% per height level above defender, cap 2.0× |
| Height bonus (Bio) | `1.0` | No height bonus for Bio form |
| Cover reduction (Energy) | `×0.5` | When target has partial cover |
| Critical hit damage | `×1.5` | Applied before variance |
| Random variance | `×(0.85 to 1.0)` | Uniform random; 0.925 for estimates |
| Recoil damage | `floor(damage × recoilFraction)` | User takes % of damage dealt |
| Drain heal | `floor(damage × drainFraction)` | User heals % of damage dealt |
| Minimum damage | 1 | All damage ≥ 1 HP |

### Example Calculations

**Physical move**: Flame Claw (Thermal, Physical, Power 65)
- Level 10 attacker, 25 ATK vs 15 DEF, STAB (Thermal creature)
- Base: `((2×10/5+2) × 65 × (25/15) / 50 + 2)` = `(6 × 65 × 1.67 / 50 + 2)` = `(650.1/50 + 2)` = 15.0
- × STAB 1.5 × Neutral 1.0 × No terrain × Height+1 (1.1) × No crit × Variance 0.925
- = 15.0 × 1.5 × 1.1 × 0.925 ≈ **22 damage**

**Energy move**: Spark Arc (Bioelectric, Energy, Power 55)
- Level 10 attacker, 25 ATK vs 20 SPD (defender), no STAB
- Base: `((6) × 55 × (25/20) / 50 + 2)` = `(6 × 55 × 1.25 / 50 + 2)` = `(412.5/50 + 2)` = 10.25
- × No STAB × SE 2.0 (vs Aqua) × No terrain × No height × No crit × Variance 0.925
- = 10.25 × 2.0 × 0.925 ≈ **18 damage**

**Bio move**: Toxic Spore (Toxic, Bio, Power 45)
- Level 10 attacker, 22 ACC vs 15 DEF, STAB (Toxic creature), target behind cover
- Base: `((6) × 45 × (22/15) / 50 + 2)` = `(6 × 45 × 1.47 / 50 + 2)` = `(396.9/50 + 2)` = 9.94
- × STAB 1.5 × SE 2.0 (vs Organic) × No terrain × No height (Bio) × No crit × Variance 0.925
- Cover irrelevant (Bio ignores cover)
- = 9.94 × 1.5 × 2.0 × 0.925 ≈ **27 damage**

## 5. Edge Cases

| Situation | Behavior |
|-----------|----------|
| DEF or SPD stat is 0 | Treated as 1 (no division by zero) |
| ACC stat is 0 (Bio form) | Treated as 1 (Bio moves deal minimum damage) |
| Move genome type is `None` | No STAB applied; no type effectiveness applied |
| Move form is `None` | `IsDamaging` returns false; damage function returns 0 (status move) |
| Dual-type super-effective on both | Multiply: 2.0 × 2.0 = 4.0× damage |
| Dual-type: one weak, one resistant | Multiply: 2.0 × 0.5 = 1.0× (cancels out) |
| Dual-type: both resistant | Multiply: 0.5 × 0.5 = 0.25× damage |
| Height difference negative (defender higher) | Height bonus = 1.0 (no penalty for Physical/Energy) |
| Height bonus cap | `1.0 + (10 × 0.1)` = 2.0× maximum for Physical/Energy forms |
| Bio move from height advantage | Height bonus = 1.0 (Bio ignores height) |
| Energy move with no LoS | Move cannot target — blocked at targeting phase, not damage phase |
| Physical move at range 3+ | Move cannot target — blocked at targeting phase |
| Energy move + target behind cover | Damage × 0.5 (cover penalty applied after all other multipliers) |
| Bio move + target behind cover | Cover ignored entirely — full damage |
| Physical move + wall between | Move cannot target — blocked at targeting phase |
| Attacker on synergy tile, defender on synergy tile | Multiply: 1.2 × 0.8 = 0.96× |
| Creature faints from recoil | Faint occurs, round continues to end-of-round effects |
| Creature faints from Burn/Poison DoT | Handled separately in `StatusEffectProcessor` at RoundStart |
| Critical hit + super-effective | Both multiplied: 1.5 × 2.0 = 3.0× |
| AI estimates with form it can't use | Move is filtered out before scoring — creature must have body part access to the form |

## 6. Dependencies

| Dependency | Direction | Notes |
|------------|-----------|-------|
| `MoveConfig` | Inbound | Power, genome type, damage form, effects read for calculation |
| `CreatureInstance` | Inbound | Level, stats (ATK, DEF, SPD, ACC), position read; TakeDamage/Heal called |
| `CreatureConfig` | Inbound | Primary/secondary type, terrain synergy type read |
| `TypeChart` | Outbound | Type effectiveness lookup (14 genome types) |
| `GridSystem` | Inbound | Tile height, terrain type, line-of-sight, cover status read via GetTile |
| `TurnManager` | Outbound | Called per move execution; damage result published via CreatureActed event |
| `StatusEffectProcessor` | Inbound | Recoil/drain applied after move resolution |
| `BodyPartSystem` | Inbound | Determines which damage forms a creature can access |

## 7. Tuning Knobs

| Parameter | Location | Default | Safe Range | Affects |
|-----------|----------|---------|------------|---------|
| Level coefficient | `DamageCalculator.Calculate` | `(2×level/5+2)` | — | Damage scaling per level |
| Stat divisor | Formula | 50 | 30–80 | Average damage output |
| STAB multiplier | `TypeChart.GetStab` | 1.5× | 1.2–1.8 | Reward for type matching |
| Terrain synergy amplify | `DamageCalculator` | 1.2× | 1.1–1.3 | Attacker on home terrain |
| Terrain synergy reduce | `DamageCalculator` | 0.8× | 0.7–0.9 | Defender on home terrain |
| Height bonus per level | `DamageCalculator` | +0.1 (10%) | 0.05–0.15 | Height advantage value |
| Height bonus cap | `GetFormHeightBonus` | 2.0× | 1.5–3.0 | Max height advantage |
| Cover reduction (Energy) | `DamageCalculator` | 0.5× | 0.3–0.7 | How much cover helps vs Energy |
| Critical hit base chance | `RollCritical` | 6.25% (1/16) | 3–10% | Crit frequency |
| Critical hit HighCrit chance | `RollCritical` | 12.5% (1/8) | 8–20% | HighCrit move frequency |
| Critical multiplier | Formula | 1.5× | 1.25–2.0 | Crit damage spike |
| Variance range | Formula | 0.85–1.0 | 0.80–1.0 | Damage unpredictability |
| Minimum damage | Formula | 1 HP | 1 | Floor for all calculations |
| Physical range default | MoveConfig | 1–2 tiles | 1–2 | Melee reach |
| Energy range default | MoveConfig | 3–5 tiles | 2–6 | Ranged reach |
| Bio range default | MoveConfig | 2–3 tiles | 1–4 | Mid-range reach |

## 8. Acceptance Criteria

- [ ] `DamageForm` enum exists with values: None, Physical, Energy, Bio
- [ ] `MoveConfig` includes `GenomeType` and `Form` fields
- [ ] `Calculate()` with Physical form uses ATK vs DEF
- [ ] `Calculate()` with Energy form uses ATK vs SPD
- [ ] `Calculate()` with Bio form uses ACC vs DEF
- [ ] `Calculate()` with Bio form returns same damage regardless of height difference
- [ ] `Calculate()` with Physical/Energy form at +3 height returns 1.3× damage
- [ ] `Calculate()` caps height bonus at 2.0× for Physical/Energy forms
- [ ] `Calculate()` with STAB returns 1.5× damage vs non-STAB
- [ ] `Calculate()` with super-effective returns 2.0× damage
- [ ] `Calculate()` with resistant returns 0.5× damage
- [ ] `Calculate()` with attacker on synergy terrain returns 1.2× damage
- [ ] `Calculate()` with defender on synergy terrain returns 0.8× damage
- [ ] Energy move targeting through cover applies 0.5× cover reduction
- [ ] Bio move targeting through cover deals full damage (no reduction)
- [ ] Physical move cannot target through walls (blocked at targeting phase)
- [ ] Energy move cannot target without line-of-sight (blocked at targeting phase)
- [ ] `Estimate()` returns same result as `Calculate()` with no random variance and no crit
- [ ] `TakeDamage(100)` on creature with 50 HP faints creature
- [ ] `Heal(10)` on creature with 50/100 HP sets HP to 60
- [ ] `HealFull()` on damaged creature sets HP to MaxHP
- [ ] Recoil effect applies 25% of damage dealt back to user
- [ ] Drain effect heals user 50% of damage dealt
- [ ] Critical hit rolls at expected rate (~6.25% base)
- [ ] Minimum damage of 1 is enforced even with terrible type matchup
- [ ] EditMode test: deterministic damage with seeded RNG is reproducible across all 3 forms
- [ ] PlayMode test: actual damage in combat matches `Calculate()` result for same inputs
