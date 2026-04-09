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
        private readonly CombatSettings _settings;
        private readonly System.Random _rng;

        /// <summary>Cached default settings for static Estimate path (AI callers).</summary>
        private static CombatSettings _defaultSettings;

        /// <summary>
        /// Create a DamageCalculator reading tuning knobs from CombatSettings.
        /// Pass a seeded System.Random for deterministic tests.
        /// </summary>
        public DamageCalculator(CombatSettings settings, System.Random rng = null)
        {
            _settings = settings;
            _rng = rng ?? new System.Random();
        }

        /// <inheritdoc/>
        public int Calculate(MoveConfig move, CreatureInstance attacker, CreatureInstance target, GridSystem grid)
        {
            if (!move.IsDamaging)
                return 0;

            float baseDamage = ComputeBase(move, attacker, target, grid, _settings);

            float critMult = RollCritical(move) ? _settings.CritMultiplier : 1f;
            float variance = _settings.VarianceMin + (float)_rng.NextDouble() * _settings.VarianceRange;

            return Mathf.Max(_settings.MinDamage, (int)(baseDamage * critMult * variance));
        }

        /// <summary>
        /// Estimate damage without randomness (for AI decision-making).
        /// Uses fixed variance (default 0.925 midpoint). No critical hit.
        /// Pass CombatSettings explicitly or uses cached defaults.
        /// </summary>
        public static int Estimate(MoveConfig move, CreatureInstance attacker, CreatureInstance target, GridSystem grid, CombatSettings settings = null)
        {
            if (!move.IsDamaging)
                return 0;

            settings ??= GetDefaultSettings();
            float baseDamage = ComputeBase(move, attacker, target, grid, settings);

            return Mathf.Max(settings.MinDamage, (int)(baseDamage * settings.EstimateVariance));
        }

        /// <summary>
        /// Returns a cached default CombatSettings instance for static callers.
        /// </summary>
        private static CombatSettings GetDefaultSettings()
        {
            if (_defaultSettings == null)
                _defaultSettings = ScriptableObject.CreateInstance<CombatSettings>();
            return _defaultSettings;
        }

        /// <summary>
        /// Shared damage pipeline: base formula × STAB × type × terrain × height × cover.
        /// Returns pre-variance, pre-crit damage as float.
        /// </summary>
        private static float ComputeBase(MoveConfig move, CreatureInstance attacker, CreatureInstance target, GridSystem grid, CombatSettings settings)
        {
            GetFormStatPairing(move.Form, attacker, target, out int offStat, out int defStat);

            float levelCoeff = (2f * attacker.Level / 5f) + 2f;
            float statRatio = (float)offStat / Mathf.Max(1, defStat);
            float baseDamage = (levelCoeff * move.Power * statRatio / settings.StatDivisor) + settings.BaseDamageFloor;

            float stabMult = TypeChart.GetStab(
                move.GenomeType, attacker.Config.PrimaryType, attacker.ActiveSecondaryType);

            float typeEffectMult = TypeChart.GetMultiplier(
                move.GenomeType, target.Config.PrimaryType, target.ActiveSecondaryType);

            var attackerTile = grid.GetTile(attacker.GridPosition);
            var defenderTile = grid.GetTile(target.GridPosition);

            float terrainMult = GetTerrainSynergyMultiplier(attackerTile, attacker, defenderTile, target, settings);
            float heightMult = GetFormHeightBonus(move.Form, attackerTile, defenderTile, settings);
            float coverMult = GetCoverMultiplier(move.Form, defenderTile, settings);

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
        private static float GetFormHeightBonus(DamageForm form, TileData attackerTile, TileData defenderTile, CombatSettings settings)
        {
            if (form == DamageForm.Bio)
                return 1f;

            if (attackerTile == null || defenderTile == null)
                return 1f;

            int heightDiff = attackerTile.Height - defenderTile.Height;
            if (heightDiff <= 0)
                return 1f;

            return Mathf.Min(settings.HeightBonusCap, 1f + (heightDiff * settings.HeightBonusPerLevel));
        }

        /// <summary>
        /// Terrain synergy multiplier. Attacker on matching terrain: ×1.2.
        /// Defender on matching terrain: ×0.8. Both stack multiplicatively.
        /// </summary>
        private static float GetTerrainSynergyMultiplier(
            TileData attackerTile, CreatureInstance attacker,
            TileData defenderTile, CreatureInstance defender,
            CombatSettings settings)
        {
            float mult = 1f;

            if (attackerTile != null
                && TypeChart.TerrainMatchesCreatureType(attackerTile.Terrain, attacker.Config.TerrainSynergyType))
                mult *= settings.AttackerTerrainSynergy;

            if (defenderTile != null
                && TypeChart.TerrainMatchesCreatureType(defenderTile.Terrain, defender.Config.TerrainSynergyType))
                mult *= settings.DefenderTerrainSynergy;

            return mult;
        }

        /// <summary>
        /// Cover multiplier. Energy form: ×0.5 if defender tile provides cover.
        /// Bio ignores cover. Physical blocked at targeting phase (not here).
        /// </summary>
        private static float GetCoverMultiplier(DamageForm form, TileData defenderTile, CombatSettings settings)
        {
            if (form == DamageForm.Energy && defenderTile != null && defenderTile.ProvidesCover)
                return settings.CoverReduction;

            return 1f;
        }

        /// <summary>
        /// Roll for critical hit. Base: 6.25% (1/16). HighCrit effect: 12.5% (1/8).
        /// </summary>
        private bool RollCritical(MoveConfig move)
        {
            float critChance = _settings.CritBaseChance;

            foreach (var effect in move.Effects)
            {
                if (effect.EffectType == MoveEffectType.HighCrit)
                {
                    critChance = _settings.CritHighChance;
                    break;
                }
            }

            return _rng.NextDouble() < critChance;
        }
    }
}
