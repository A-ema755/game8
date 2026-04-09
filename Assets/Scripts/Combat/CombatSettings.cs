using UnityEngine;

namespace GeneForge.Combat
{
    /// <summary>
    /// All tuning knobs owned by the Turn Manager system.
    /// One instance: Assets/Resources/Data/CombatSettings.asset.
    /// Injected into TurnManager via constructor (ADR-001, ADR-003).
    /// Implements GDD Turn Manager §7 — Tuning Knobs.
    /// </summary>
    [CreateAssetMenu(menuName = "GeneForge/CombatSettings")]
    public class CombatSettings : ScriptableObject
    {
        [Header("Initiative")]
        [Tooltip("Multiplier applied to Chebyshev distance when computing initiative score. " +
                 "GDD default 1000. Range 500–5000. Below 500: SPD can override distance. " +
                 "Above 5000: SPD contribution becomes negligible.")]
        [SerializeField] private int initiativeDistanceWeight = 1000;

        [Header("Movement")]
        [Tooltip("Divisor applied to ComputedStats.SPD to determine tiles per turn. " +
                 "GDD default 20. Range 10–40. At 10: high mobility (SPD 60 = 6 tiles). " +
                 "At 40: low mobility (SPD 60 = 1 tile).")]
        [SerializeField] private int movementDivisor = 20;

        [Header("Capture")]
        [Tooltip("Chebyshev distance for Gene Trap throw range. GDD default 1. Range 1–3.")]
        [SerializeField] private int captureRange = 1;

        [Header("Round Limit")]
        [Tooltip("Maximum rounds before combat ends in a Draw. 0 = no limit (MVP default). " +
                 "Range 30–100. Requires CombatResult.Draw implementation (post-MVP).")]
        [SerializeField] private int maxRoundsPerCombat = 0;

        [Header("Flee")]
        [Tooltip("Flee success rate for wild encounters. MVP default 1.0 (always succeeds). " +
                 "Range 0.5–1.0. Post-MVP formula: clamp01(playerAvgSPD / enemyAvgSPD * 0.8).")]
        [SerializeField] [Range(0f, 1f)] private float wildFleeSuccessRate = 1.0f;

        [Header("Burn")]
        [Tooltip("Burn deals floor(maxHP / burnDotDivisor) damage per round (min 1). " +
                 "GDD default 16 (1/16 maxHP). Range 8–32 (1/8 to 1/32 maxHP). " +
                 "At 8 Burn equals Poison severity.")]
        [SerializeField] private int burnDotDivisor = 16;

        [Header("Poison")]
        [Tooltip("Poison deals floor(maxHP / poisonDotDivisor) damage per round (min 1). " +
                 "GDD default 8 (1/8 maxHP). Range 4–16. Above 6 is very punishing.")]
        [SerializeField] private int poisonDotDivisor = 8;

        [Header("Status Durations")]
        [Tooltip("Fixed duration of Sleep in rounds. GDD default 3. Range 2–5.")]
        [SerializeField] private int sleepDuration = 3;

        [Tooltip("Fixed duration of Freeze in rounds. GDD default 2. Range 1–4.")]
        [SerializeField] private int freezeDuration = 2;

        [Tooltip("Fixed duration of Confusion in rounds. GDD default 3. Range 2–5.")]
        [SerializeField] private int confusionDuration = 3;

        [Tooltip("Fixed duration of Taunt in rounds. GDD default 3. Range 2–5.")]
        [SerializeField] private int tauntDuration = 3;

        [Header("Paralysis")]
        [Tooltip("Probability 0.0–1.0 that Paralysis suppresses a creature this round. " +
                 "GDD default 0.25. Range 0.15–0.40.")]
        [SerializeField] [Range(0f, 1f)] private float paralysisSuppressionChance = 0.25f;

        [Header("Confusion")]
        [Tooltip("Probability 0.0–1.0 of confusion self-hit per action step. " +
                 "GDD default 0.33. Range 0.20–0.50.")]
        [SerializeField] [Range(0f, 1f)] private float confusionSelfHitChance = 0.33f;

        [Tooltip("Base power of the synthetic confusion self-hit move. " +
                 "GDD default 40. Range 30–50. Above 60 rivals DoT effects.")]
        [SerializeField] private int confusionSelfHitPower = 40;

        [Header("Damage Calculator — Critical Hits")]
        [Tooltip("Damage multiplier applied on critical hit. GDD default 1.5. Range 1.25–2.0.")]
        [SerializeField] private float critMultiplier = 1.5f;

        [Tooltip("Base critical hit chance (1/16). GDD default 0.0625. Range 0.03–0.15.")]
        [SerializeField] [Range(0f, 1f)] private float critBaseChance = 0.0625f;

        [Tooltip("Elevated critical hit chance for HighCrit moves (1/8). GDD default 0.125.")]
        [SerializeField] [Range(0f, 1f)] private float critHighChance = 0.125f;

        [Header("Damage Calculator — Variance")]
        [Tooltip("Minimum random variance factor. GDD default 0.85. Max damage = varianceMin + varianceRange.")]
        [SerializeField] private float varianceMin = 0.85f;

        [Tooltip("Random variance range added to varianceMin. GDD default 0.15 (so max = 1.0).")]
        [SerializeField] private float varianceRange = 0.15f;

        [Tooltip("Fixed variance used by AI Estimate (midpoint of range). GDD default 0.925.")]
        [SerializeField] private float estimateVariance = 0.925f;

        [Header("Damage Calculator — Terrain & Height")]
        [Tooltip("Height bonus per level above defender. GDD default 0.1 (+10%/level). Range 0.05–0.2.")]
        [SerializeField] private float heightBonusPerLevel = 0.1f;

        [Tooltip("Maximum height bonus multiplier cap. GDD default 2.0.")]
        [SerializeField] private float heightBonusCap = 2.0f;

        [Tooltip("Attacker terrain synergy multiplier. GDD default 1.2.")]
        [SerializeField] private float attackerTerrainSynergy = 1.2f;

        [Tooltip("Defender terrain synergy multiplier. GDD default 0.8.")]
        [SerializeField] private float defenderTerrainSynergy = 0.8f;

        [Header("Damage Calculator — Formula")]
        [Tooltip("Cover damage reduction for Energy form. GDD default 0.5 (50% reduction).")]
        [SerializeField] private float coverReduction = 0.5f;

        [Tooltip("Divisor in base damage formula. GDD default 50.")]
        [SerializeField] private float statDivisor = 50f;

        [Tooltip("Floor added after stat ratio calculation. GDD default 2.")]
        [SerializeField] private float baseDamageFloor = 2f;

        [Tooltip("Absolute minimum damage for any damaging move. GDD default 1.")]
        [SerializeField] private int minDamage = 1;

        // ── Properties ────────────────────────────────────────────────────

        /// <summary>
        /// Multiplier on Chebyshev distance in the initiative score formula.
        /// initiativeScore = (distance × InitiativeDistanceWeight) − SPD.
        /// </summary>
        public int InitiativeDistanceWeight => initiativeDistanceWeight;

        /// <summary>
        /// SPD divisor for movement range. movementRange = max(1, floor(SPD / MovementDivisor)).
        /// </summary>
        public int MovementDivisor => movementDivisor;

        /// <summary>Chebyshev distance for Gene Trap throw range.</summary>
        public int CaptureRange => captureRange;

        /// <summary>
        /// Maximum rounds before combat is a Draw. 0 means no limit (MVP).
        /// </summary>
        public int MaxRoundsPerCombat => maxRoundsPerCombat;

        /// <summary>Wild encounter flee success rate (0.0–1.0). MVP default 1.0.</summary>
        public float WildFleeSuccessRate => wildFleeSuccessRate;

        /// <summary>Divisor for Burn DoT: damage = max(1, floor(maxHP / BurnDotDivisor)).</summary>
        public int BurnDotDivisor => burnDotDivisor;

        /// <summary>Divisor for Poison DoT: damage = max(1, floor(maxHP / PoisonDotDivisor)).</summary>
        public int PoisonDotDivisor => poisonDotDivisor;

        /// <summary>Fixed duration of Sleep in rounds (MVP).</summary>
        public int SleepDuration => sleepDuration;

        /// <summary>Fixed duration of Freeze in rounds (MVP).</summary>
        public int FreezeDuration => freezeDuration;

        /// <summary>Fixed duration of Confusion in rounds (MVP).</summary>
        public int ConfusionDuration => confusionDuration;

        /// <summary>Fixed duration of Taunt in rounds (MVP).</summary>
        public int TauntDuration => tauntDuration;

        /// <summary>Probability that Paralysis suppresses a creature per round (0.0–1.0).</summary>
        public float ParalysisSuppressionChance => paralysisSuppressionChance;

        /// <summary>Probability of confusion self-hit per action step (0.0–1.0).</summary>
        public float ConfusionSelfHitChance => confusionSelfHitChance;

        /// <summary>Base power of the synthetic confusion self-hit move.</summary>
        public int ConfusionSelfHitPower => confusionSelfHitPower;

        // ── Damage Calculator Properties ─────────────────────────────────

        /// <summary>Damage multiplier on critical hit (GDD default 1.5×).</summary>
        public float CritMultiplier => critMultiplier;

        /// <summary>Base critical hit chance (GDD default 1/16 = 0.0625).</summary>
        public float CritBaseChance => critBaseChance;

        /// <summary>Elevated crit chance for HighCrit moves (GDD default 1/8 = 0.125).</summary>
        public float CritHighChance => critHighChance;

        /// <summary>Minimum random variance factor (GDD default 0.85).</summary>
        public float VarianceMin => varianceMin;

        /// <summary>Random variance range (GDD default 0.15, so max = 1.0).</summary>
        public float VarianceRange => varianceRange;

        /// <summary>Fixed variance for AI damage estimation (GDD default 0.925).</summary>
        public float EstimateVariance => estimateVariance;

        /// <summary>Height bonus per level above defender (GDD default 0.1).</summary>
        public float HeightBonusPerLevel => heightBonusPerLevel;

        /// <summary>Maximum height bonus cap (GDD default 2.0×).</summary>
        public float HeightBonusCap => heightBonusCap;

        /// <summary>Attacker terrain synergy multiplier (GDD default 1.2×).</summary>
        public float AttackerTerrainSynergy => attackerTerrainSynergy;

        /// <summary>Defender terrain synergy multiplier (GDD default 0.8×).</summary>
        public float DefenderTerrainSynergy => defenderTerrainSynergy;

        /// <summary>Cover damage reduction for Energy form (GDD default 0.5).</summary>
        public float CoverReduction => coverReduction;

        /// <summary>Divisor in base damage formula (GDD default 50).</summary>
        public float StatDivisor => statDivisor;

        /// <summary>Floor added after stat ratio in damage formula (GDD default 2).</summary>
        public float BaseDamageFloor => baseDamageFloor;

        /// <summary>Absolute minimum damage for damaging moves (GDD default 1).</summary>
        public int MinDamage => minDamage;
    }
}
