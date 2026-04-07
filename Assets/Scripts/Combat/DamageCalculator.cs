using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.Combat
{
    /// <summary>
    /// Calculates exact or estimated damage for a move hit.
    /// Implements IDamageCalculator for TurnManager injection.
    /// RNG injected via constructor for testability.
    /// Implements GDD damage-health-system.md §3.3.
    /// </summary>
    public class DamageCalculator : IDamageCalculator
    {
        // ── Tuning Constants (GDD §7) ────────────────────────────────────
        private const float StabMultiplier = 1.5f;       // unused here — lives in TypeChart, listed for reference
        private const float CritMultiplier = 1.5f;
        private const float BaseCritChance = 0.0625f;    // 1/16
        private const float HighCritChance = 0.125f;     // 1/8
        private const float VarianceMin = 0.85f;
        private const float VarianceRange = 0.15f;       // max = VarianceMin + VarianceRange = 1.0
        private const float EstimateVariance = 0.925f;   // midpoint of 0.85–1.0
        private const float HeightBonusPerLevel = 0.1f;
        private const float HeightBonusCap = 2.0f;
        private const float AttackerTerrainSynergy = 1.2f;
        private const float DefenderTerrainSynergy = 0.8f;
        private const float CoverReduction = 0.5f;
        private const float StatDivisor = 50f;
        private const float BaseDamageFloor = 2f;
        private const int MinDamage = 1;

        private readonly System.Random _rng;

        /// <summary>
        /// Create a DamageCalculator with optional seeded RNG.
        /// Pass a seeded System.Random for deterministic tests.
        /// </summary>
        public DamageCalculator(System.Random rng = null)
        {
            _rng = rng ?? new System.Random();
        }

        /// <inheritdoc/>
        public int Calculate(MoveConfig move, CreatureInstance attacker, CreatureInstance target, GridSystem grid)
        {
            if (!move.IsDamaging)
                return 0;

            float baseDamage = ComputeBase(move, attacker, target, grid);

            float critMult = RollCritical(move) ? CritMultiplier : 1f;
            float variance = VarianceMin + (float)_rng.NextDouble() * VarianceRange;

            return Mathf.Max(MinDamage, (int)(baseDamage * critMult * variance));
        }

        /// <summary>
        /// Estimate damage without randomness (for AI decision-making).
        /// Uses fixed variance of 0.925 (midpoint). No critical hit.
        /// </summary>
        public static int Estimate(MoveConfig move, CreatureInstance attacker, CreatureInstance target, GridSystem grid)
        {
            if (!move.IsDamaging)
                return 0;

            float baseDamage = ComputeBase(move, attacker, target, grid);

            return Mathf.Max(MinDamage, (int)(baseDamage * EstimateVariance));
        }

        /// <summary>
        /// Shared damage pipeline: base formula × STAB × type × terrain × height × cover.
        /// Returns pre-variance, pre-crit damage as float.
        /// </summary>
        private static float ComputeBase(MoveConfig move, CreatureInstance attacker, CreatureInstance target, GridSystem grid)
        {
            GetFormStatPairing(move.Form, attacker, target, out int offStat, out int defStat);

            float levelCoeff = (2f * attacker.Level / 5f) + 2f;
            float statRatio = (float)offStat / Mathf.Max(1, defStat);
            float baseDamage = (levelCoeff * move.Power * statRatio / StatDivisor) + BaseDamageFloor;

            float stabMult = TypeChart.GetStab(
                move.GenomeType, attacker.Config.PrimaryType, attacker.ActiveSecondaryType);

            float typeEffectMult = TypeChart.GetMultiplier(
                move.GenomeType, target.Config.PrimaryType, target.ActiveSecondaryType);

            var attackerTile = grid.GetTile(attacker.GridPosition);
            var defenderTile = grid.GetTile(target.GridPosition);

            float terrainMult = GetTerrainSynergyMultiplier(attackerTile, attacker, defenderTile, target);
            float heightMult = GetFormHeightBonus(move.Form, attackerTile, defenderTile);
            float coverMult = GetCoverMultiplier(move.Form, defenderTile);

            return baseDamage * stabMult * typeEffectMult * terrainMult * heightMult * coverMult;
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
        /// Height bonus based on damage form.
        /// Physical/Energy: +10% per height level above defender, cap 2.0×.
        /// Bio: no height bonus.
        /// </summary>
        private static float GetFormHeightBonus(DamageForm form, TileData attackerTile, TileData defenderTile)
        {
            if (form == DamageForm.Bio)
                return 1f;

            if (attackerTile == null || defenderTile == null)
                return 1f;

            int heightDiff = attackerTile.Height - defenderTile.Height;
            if (heightDiff <= 0)
                return 1f;

            return Mathf.Min(HeightBonusCap, 1f + (heightDiff * HeightBonusPerLevel));
        }

        /// <summary>
        /// Terrain synergy multiplier. Attacker on matching terrain: ×1.2.
        /// Defender on matching terrain: ×0.8. Both stack multiplicatively.
        /// </summary>
        private static float GetTerrainSynergyMultiplier(
            TileData attackerTile, CreatureInstance attacker,
            TileData defenderTile, CreatureInstance defender)
        {
            float mult = 1f;

            if (attackerTile != null
                && TypeChart.TerrainMatchesCreatureType(attackerTile.Terrain, attacker.Config.TerrainSynergyType))
                mult *= AttackerTerrainSynergy;

            if (defenderTile != null
                && TypeChart.TerrainMatchesCreatureType(defenderTile.Terrain, defender.Config.TerrainSynergyType))
                mult *= DefenderTerrainSynergy;

            return mult;
        }

        /// <summary>
        /// Cover multiplier. Energy form: ×0.5 if defender tile provides cover.
        /// Bio ignores cover. Physical blocked at targeting phase (not here).
        /// </summary>
        private static float GetCoverMultiplier(DamageForm form, TileData defenderTile)
        {
            if (form == DamageForm.Energy && defenderTile != null && defenderTile.ProvidesCover)
                return CoverReduction;

            return 1f;
        }

        /// <summary>
        /// Roll for critical hit. Base: 6.25% (1/16). HighCrit effect: 12.5% (1/8).
        /// </summary>
        private bool RollCritical(MoveConfig move)
        {
            float critChance = BaseCritChance;

            foreach (var effect in move.Effects)
            {
                if (effect.EffectType == MoveEffectType.HighCrit)
                {
                    critChance = HighCritChance;
                    break;
                }
            }

            return _rng.NextDouble() < critChance;
        }
    }
}
