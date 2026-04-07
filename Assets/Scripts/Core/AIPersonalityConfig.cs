using UnityEngine;

namespace GeneForge.Core
{
    /// <summary>
    /// Scoring weights, behavioral biases, and thresholds for AI decision-making.
    /// One asset per personality archetype in Resources/Data/AIPersonalities/.
    /// Implements GDD ai-decision-system.md §3.3.
    /// </summary>
    [CreateAssetMenu(menuName = "GeneForge/AIPersonalityConfig")]
    public class AIPersonalityConfig : ConfigBase
    {
        // ── Scoring Weights (should sum to ~8 for normalization) ──────────

        [Header("Scoring Weights")]
        [SerializeField, Range(0f, 3f)] float weightDamage = 1.0f;
        [SerializeField, Range(0f, 3f)] float weightKill = 1.2f;
        [SerializeField, Range(0f, 3f)] float weightThreat = 0.8f;
        [SerializeField, Range(0f, 3f)] float weightPosition = 0.7f;
        [SerializeField, Range(0f, 3f)] float weightTerrain = 0.5f;
        [SerializeField, Range(0f, 3f)] float weightSelfPreservation = 0.8f;
        [SerializeField, Range(0f, 3f)] float weightGenomeMatch = 1.0f;
        [SerializeField, Range(0f, 3f)] float weightFormTactic = 0.8f;

        // ── Behavioral Biases ────────────────────────────────────────────

        [Header("Behavioral Biases")]
        [SerializeField, Range(0f, 1f)] float aggressionBias = 0.6f;
        [SerializeField, Range(0f, 1f)] float focusFireBias = 0.5f;
        [SerializeField, Range(0f, 1f)] float abilityPreference = 0.5f;
        [SerializeField, Range(0f, 1f)] float randomnessFactor = 0.1f;

        // ── Thresholds ───────────────────────────────────────────────────

        [Header("Thresholds")]
        [SerializeField, Range(0f, 1f)] float lowHpThreshold = 0.30f;
        [SerializeField, Range(0f, 1f)] float retreatHpThreshold = 0.15f;

        // ── Public Properties ────────────────────────────────────────────

        /// <summary>Weight for expected damage scoring dimension.</summary>
        public float WeightDamage => weightDamage;

        /// <summary>Weight for kill potential scoring dimension.</summary>
        public float WeightKill => weightKill;

        /// <summary>Weight for threat target scoring dimension.</summary>
        public float WeightThreat => weightThreat;

        /// <summary>Weight for positional advantage scoring dimension.</summary>
        public float WeightPosition => weightPosition;

        /// <summary>Weight for terrain synergy scoring dimension.</summary>
        public float WeightTerrain => weightTerrain;

        /// <summary>Weight for self-preservation scoring dimension.</summary>
        public float WeightSelfPreservation => weightSelfPreservation;

        /// <summary>Weight for genome type matchup scoring dimension.</summary>
        public float WeightGenomeMatch => weightGenomeMatch;

        /// <summary>Weight for damage form tactical advantage scoring dimension.</summary>
        public float WeightFormTactic => weightFormTactic;

        /// <summary>Bias toward approaching opponents (1.0 = always approach, 0.0 = always retreat).</summary>
        public float AggressionBias => aggressionBias;

        /// <summary>Bias toward targeting the same enemy across turns (post-MVP tracking).</summary>
        public float FocusFireBias => focusFireBias;

        /// <summary>Preference for buff/heal moves over direct attacks (1.0 = always buff first).</summary>
        public float AbilityPreference => abilityPreference;

        /// <summary>Random jitter magnitude added to final scores for unpredictability.</summary>
        public float RandomnessFactor => randomnessFactor;

        /// <summary>HP fraction below which self-preservation scoring activates.</summary>
        public float LowHpThreshold => lowHpThreshold;

        /// <summary>HP fraction below which creature always tries to retreat.</summary>
        public float RetreatHpThreshold => retreatHpThreshold;
    }
}
