# Damage & Health System

## 1. Overview

The Damage & Health System governs how creatures take and sustain damage during combat. A unified damage formula accounts for attacker stats, defender stats, move properties, type effectiveness, terrain synergy, height advantage, and random variance. Critical hits can double the final damage. The system applies status effects from moves (Burn, Poison), manages HP drain and recoil, and detects faints. `DamageCalculator` is a pure C# class providing both exact damage calculation for hit resolution and an estimate function for AI decision-making (without random variance).

## 2. Player Fantasy

Damage feels earned and predictable. A powerful move against a weak defender deals huge numbers, making the player feel their team is strong. A resisted move against a bulky tank does minimal damage, teaching type matchups and positioning. Terrain synergy rewards thoughtful positioning — standing on your type's terrain makes you hit harder. Critical hits arrive with excitement but don't feel random — the system is transparent enough that skilled players can play around odds.

## 3. Detailed Rules

### 3.1 Damage Formula

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
            // damage = ((2*level/5+2) * power * (ATK/DEF) / 50 + 2) * STAB * typeEffect * terrainSynergy * heightBonus * critical * random(0.85,1.0)
            // Minimum: 1

            int level = attacker.Level;
            int power = move.Power;
            int atk = attacker.ComputedStats.ATK;
            int def = defender.ComputedStats.DEF;

            // Level-based coefficient
            float levelCoeff = (2f * level / 5f) + 2f;

            // ATK/DEF ratio
            float atkDefRatio = (float)atk / Mathf.Max(1, def);

            // Base formula (before multipliers)
            float baseDamage = (levelCoeff * power * atkDefRatio / 50f) + 2f;

            // STAB (Same Type Attack Bonus) = 1.5x if move type matches attacker's primary type
            float stabMult = 1f;
            if (move.Type != CreatureType.None && move.Type == attacker.Config.PrimaryType)
                stabMult = 1.5f;
            else if (move.Type != CreatureType.None && move.Type == attacker.Config.SecondaryType)
                stabMult = 1.5f; // Same STAB for primary and secondary

            // Type effectiveness (weak/resist/neutral)
            float typeEffectMult = TypeChart.GetMultiplier(move.Type, defender.Config.PrimaryType)
                                  * (defender.Config.IsDualType 
                                      ? TypeChart.GetMultiplier(move.Type, defender.Config.SecondaryType) 
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

            // Height advantage: creatures on higher ground deal +10% damage per height level
            float heightBonus = 1f;
            int heightDiff = attackerTile.Height - defenderTile.Height;
            if (heightDiff > 0)
                heightBonus = 1f + (heightDiff * 0.1f);

            // Critical hit multiplier (1.5x) — 6.25% base, 12.5% with HighCrit effect
            float critMult = 1f;
            bool isCritical = RollCritical(move, attacker, rng);
            if (isCritical)
                critMult = 1.5f;

            // Random variance: 0.85x to 1.0x
            float variance = 0.85f + (float)rng.NextDouble() * 0.15f;

            // Final calculation
            float finalDamage = baseDamage * stabMult * typeEffectMult * terrainSynergyMult * heightBonus * critMult * variance;

            // Apply move-specific effects (recoil, drain)
            // Note: recoil/drain handling is separate in TurnManager.ExecuteMoveAsync

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
            int atk = attacker.ComputedStats.ATK;
            int def = defender.ComputedStats.DEF;

            float levelCoeff = (2f * level / 5f) + 2f;
            float atkDefRatio = (float)atk / Mathf.Max(1, def);
            float baseDamage = (levelCoeff * power * atkDefRatio / 50f) + 2f;

            float stabMult = 1f;
            if (move.Type != CreatureType.None && move.Type == attacker.Config.PrimaryType)
                stabMult = 1.5f;
            else if (move.Type != CreatureType.None && move.Type == attacker.Config.SecondaryType)
                stabMult = 1.5f; // Same STAB for primary and secondary

            float typeEffectMult = TypeChart.GetMultiplier(move.Type, defender.Config.PrimaryType)
                                  * (defender.Config.IsDualType 
                                      ? TypeChart.GetMultiplier(move.Type, defender.Config.SecondaryType) 
                                      : 1f);

            float terrainSynergyMult = 1f;
            var defenderTile = grid.GetTile(defender.GridPosition);
            var attackerTile = grid.GetTile(attacker.GridPosition);
            
            if (defenderTile != null && defenderTile.Terrain == defender.Config.TerrainSynergyType)
                terrainSynergyMult *= 0.8f;
            if (attackerTile != null && attackerTile.Terrain == attacker.Config.TerrainSynergyType)
                terrainSynergyMult *= 1.2f;

            float heightBonus = 1f;
            int heightDiff = attackerTile.Height - defenderTile.Height;
            if (heightDiff > 0)
                heightBonus = 1f + (heightDiff * 0.1f);

            // No critical hit in estimate; no random variance
            float finalDamage = baseDamage * stabMult * typeEffectMult * terrainSynergyMult * heightBonus * 0.925f;

            return Mathf.Max(1, (int)finalDamage);
        }

        /// <summary>
        /// Determine if a hit is critical.
        /// Base: 6.25% (1/16). HighCrit effect: 12.5% (1/8).
        /// </summary>
        private static bool RollCritical(MoveConfig move, CreatureInstance attacker, System.Random rng)
        {
            float critChance = 0.0625f; // 1/16

            // Check if move has HighCrit effect
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

### 3.2 Health Management

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

### 3.3 Recoil and Drain Effects

Moves with `Recoil` or `Drain` effects are resolved in `TurnManager.ExecuteMoveAsync`:

```csharp
// After damage is applied to target:
foreach (var effect in action.Move.Effects)
{
    if (effect.EffectType == MoveEffectType.Recoil)
    {
        // User takes recoil damage
        int recoilDamage = (int)(damage * (effect.Magnitude / 100f));
        action.Actor.TakeDamage(recoilDamage);
    }
    else if (effect.EffectType == MoveEffectType.Drain)
    {
        // User heals from damage dealt
        int healAmount = (int)(damage * (effect.Magnitude / 100f));
        action.Actor.Heal(healAmount);
    }
}
```

Example moves:
- `inferno-dash`: Power 80, Recoil 25% → user takes 25% of damage dealt
- `absorb`: Power 20 (Grass Special), Drain 50% → user heals 50% of damage dealt

## 4. Formulas

| Formula | Expression | Notes |
|---------|-----------|-------|
| Base damage | `((2×level/5+2) × power × (ATK/DEF) / 50 + 2)` | Core damage calculation |
| STAB (Primary) | `×1.5` | If move type == creature primary type |
| STAB (Secondary) | `×1.5` | If move type == creature secondary type |
| Type effectiveness | `TypeChart[move.type][defender.type]` | 2.0x (super), 1.0x (neutral), 0.5x (resist) |
| Dual-type effectiveness | Product of both types | Multiply both super/resist chances |
| Terrain synergy (defender) | `×0.8` | If defender on matching terrain |
| Terrain synergy (attacker) | `×1.2` | If attacker on matching terrain |
| Height bonus | `1.0 + (heightDiff × 0.1)` | +10% per height level above defender |
| Critical hit damage | `×1.5` | Applied before variance |
| Random variance | `×(0.85 to 1.0)` | Uniform random; 0.925 for estimates |
| Recoil damage | `floor(damage × recoilFraction)` | User takes % of damage dealt |
| Drain heal | `floor(damage × drainFraction)` | User heals % of damage dealt |
| Minimum damage | 1 | All damage ≥ 1 HP |

## 5. Edge Cases

| Situation | Behavior |
|-----------|----------|
| DEF stat is 0 | Treated as 1 (no division by zero) |
| Move type is `None` | Uses attacker's primary type for STAB only; no type effectiveness applied |
| Dual-type super-effective on both | Multiply: 2.0 × 2.0 = 4.0× damage (rare but possible) |
| Dual-type: one weak, one resistant | Multiply: 2.0 × 0.5 = 1.0× (cancels out) |
| Height difference negative (defender higher) | Height bonus = 1.0 (no penalty; attacker deals normal damage) |
| Height difference > 10 | Height bonus capped at `1.0 + (10 × 0.1)` = 2.0× for balance |
| Attacker on synergy tile, defender on synergy tile | Multiply: 1.2 × 0.8 = 0.96× (benefits mostly cancel) |
| Move power is 0 (Status move) | `IsDamaging` returns false; damage function returns 0 |
| Creature faints from recoil | Faint occurs, round continues to end-of-round effects |
| Creature faints from Burn/Poison DoT | Handled separately in `StatusEffectProcessor` at RoundStart |
| Critical hit + super-effective | Both multipliers applied: 1.5 × 2.0 = 3.0× damage |
| AI estimates damage with opponent at 1 HP | Estimate returns full damage; actual damage may exceed current HP (design intention: AI can over-estimate) |

## 6. Dependencies

| Dependency | Direction | Notes |
|------------|-----------|-------|
| `MoveConfig` | Inbound | Power, type, effects read for calculation |
| `CreatureInstance` | Inbound | Level, stats, position, grid position read; TakeDamage/Heal called |
| `CreatureConfig` | Inbound | Primary/secondary type, terrain synergy type read |
| `TypeChart` | Outbound | Type effectiveness lookup |
| `GridSystem` | Inbound | Tile height, terrain type read via GetTile |
| `TurnManager` | Outbound | Called per move execution; damage result published via CreatureActed event |
| `StatusEffectProcessor` | Inbound | Recoil/drain applied after move resolution |

## 7. Tuning Knobs

| Parameter | Location | Default | Notes |
|-----------|----------|---------|-------|
| Level coefficient | `DamageCalculator.Calculate` | `(2×level/5+2)` | Controls damage scaling per level |
| ATK/DEF divisor | Formula | 50 | Larger = lower average damage |
| STAB primary multiplier | `DamageCalculator` | 1.5× | Reward for type matching |
| STAB secondary multiplier | `DamageCalculator` | 1.5× | Same as primary STAB |
| Terrain synergy amplify | `DamageCalculator` | 1.2× | Attacker on home terrain |
| Terrain synergy reduce | `DamageCalculator` | 0.8× | Defender on home terrain |
| Height bonus per level | `DamageCalculator` | +0.1 (10%) | Per height tier above defender |
| Height bonus cap | Design | 2.0× | Max 10 height levels |
| Critical hit base chance | `DamageCalculator` | 6.25% (1/16) | Standard move crit rate |
| Critical hit with HighCrit | `DamageCalculator` | 12.5% (1/8) | For moves with HighCrit effect |
| Critical multiplier | Formula | 1.5× | Damage multiplier on crit |
| Variance range | Formula | 0.85–1.0 | Random damage range |
| Minimum damage | Formula | 1 HP | Floor for all damage calculations |

## 8. Acceptance Criteria

- [ ] `Calculate()` with level 1 creature, 10 ATK vs 10 DEF, power 40 returns ~4–5 damage
- [ ] `Calculate()` with STAB returns 1.5× damage vs non-STAB
- [ ] `Calculate()` with super-effective returns 2.0× damage
- [ ] `Calculate()` with resistant returns 0.5× damage
- [ ] `Calculate()` with attacker on synergy terrain returns 1.2× damage
- [ ] `Calculate()` with defender on synergy terrain returns 0.8× damage
- [ ] `Calculate()` with attacker 3 height levels higher returns 1.3× damage (1.0 + 0.3)
- [ ] `Calculate()` caps height bonus at 2.0× (10 levels max)
- [ ] `Estimate()` returns same result as `Calculate()` with no random variance
- [ ] `TakeDamage(100)` on creature with 50 HP faints creature
- [ ] `Heal(10)` on creature with 50/100 HP sets HP to 60
- [ ] `HealFull()` on damaged creature sets HP to MaxHP
- [ ] Recoil effect applies 25% of damage dealt back to user
- [ ] Drain effect heals user 50% of damage dealt
- [ ] Critical hit rolls at expected rate (~6.25% base)
- [ ] Minimum damage of 1 is enforced even with terrible type matchup
- [ ] EditMode test: deterministic damage with seeded RNG is reproducible
- [ ] PlayMode test: actual damage in combat matches `Calculate()` result for same inputs
