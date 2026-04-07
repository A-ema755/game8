using System.Collections.Generic;
using GeneForge.Core;
using GeneForge.Creatures;
using UnityEngine;

namespace GeneForge.Combat
{
    /// <summary>
    /// Pure math capture-rate calculations.
    /// Implements GDD capture-system.md §3–4.
    /// </summary>
    public static class CaptureCalculator
    {
        // ── Tuning Constants (GDD §7) ────────────────────────────────────
        private const float StatusBonusNone = 1.0f;
        private const float StatusBonusPoisonBurn = 1.2f;
        private const float StatusBonusParalysisFreeze = 1.5f;
        private const float StatusBonusSleep = 2.5f;
        private const float CatchRateMaxRaw = 255f;

        /// <summary>
        /// Returns catch probability in [0, 1].
        /// Formula: (config.CatchRate * trapModifier * hpFactor * statusBonus) / 255
        /// </summary>
        public static float CalculateCatchRate(
            CreatureConfig config, int currentHp, int maxHp,
            float trapModifier, float statusBonus)
        {
            if (config.CatchRate == 0)
                return 0f;

            float hpFactor = (3f * maxHp - 2f * currentHp) / (3f * maxHp);
            float rawRate = (config.CatchRate * trapModifier * hpFactor * statusBonus) / CatchRateMaxRaw;
            return Mathf.Clamp01(rawRate);
        }

        /// <summary>
        /// Returns highest applicable status bonus from active effects.
        /// None=1.0, Poison/Burn=1.2, Paralysis/Freeze=1.5, Sleep=2.5.
        /// Multiple effects: highest wins only (GDD §5).
        /// </summary>
        public static float GetStatusBonus(IReadOnlyList<StatusEffect> activeEffects)
        {
            float highest = StatusBonusNone;

            for (int i = 0; i < activeEffects.Count; i++)
            {
                float bonus = activeEffects[i] switch
                {
                    StatusEffect.Poison => StatusBonusPoisonBurn,
                    StatusEffect.Burn => StatusBonusPoisonBurn,
                    StatusEffect.Paralysis => StatusBonusParalysisFreeze,
                    StatusEffect.Freeze => StatusBonusParalysisFreeze,
                    StatusEffect.Sleep => StatusBonusSleep,
                    _ => StatusBonusNone,
                };

                if (bonus > highest)
                    highest = bonus;
            }

            return highest;
        }

        /// <summary>
        /// Resolves a single capture attempt. Inject RNG for testability.
        /// </summary>
        public static bool AttemptCapture(float catchRate, System.Random rng)
        {
            if (catchRate <= 0f)
                return false;
            if (catchRate >= 1f)
                return true;

            return rng.NextDouble() < catchRate;
        }

        /// <summary>
        /// Returns effective trap modifier for Specialist Gene Traps.
        /// 2.0x when creature primary type matches targetType, 1.0x otherwise.
        /// </summary>
        public static float GetSpecialistModifier(
            CreatureType targetType, CreatureType creaturePrimaryType)
        {
            return targetType == creaturePrimaryType ? 2.0f : 1.0f;
        }
    }
}
