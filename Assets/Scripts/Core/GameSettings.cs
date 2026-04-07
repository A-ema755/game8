using UnityEngine;

namespace GeneForge.Core
{
    /// <summary>
    /// Global tuning values that span multiple systems.
    /// One instance: Assets/Resources/Data/GameSettings.asset
    /// </summary>
    [CreateAssetMenu(menuName = "GeneForge/GameSettings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Capture")]
        [SerializeField] float baseCaptureRate = 0.3f;
        [SerializeField] float hpWeightMultiplier = 1.5f;
        [SerializeField] float statusCaptureBonus = 0.15f;
        [SerializeField] int maxCaptureAttempts = 3;

        [Header("XP & Leveling")]
        [SerializeField] int maxLevel = 50;
        [SerializeField] float trainerXpMultiplier = 1.5f;
        [SerializeField] float xpShareRatio = 0.5f;

        [Header("Combat")]
        [SerializeField] int minDamage = 1;
        [SerializeField] float stabMultiplier = 1.5f;
        [SerializeField] float critMultiplier = 1.5f;
        [SerializeField] float critBaseChance = 0.0625f;
        [SerializeField] float heightBonusPerLevel = 0.1f;
        [SerializeField] float flankingBonus = 0.25f;

        [Header("DNA & Instability")]
        [SerializeField] int instabilityMax = 100;
        [SerializeField] int instabilityVolatileMin = 25;
        [SerializeField] int instabilityUnstableMin = 50;
        [SerializeField] int instabilityCriticalMin = 75;
        [SerializeField] int instabilityBreakdownMin = 100;
        [SerializeField] float instabilityDecayPerRest = 5f;
        [SerializeField] float disobeyBaseChance = 0.1f;
        [SerializeField] float breakthroughChance = 0.05f;

        [Header("Grid")]
        [SerializeField] int fallDamageBase = 10;
        [SerializeField] int fallDamageMinDelta = 3;

        [Header("Ecosystem")]
        [SerializeField] int conservationBonusThreshold = 80;
        [SerializeField] float migrationCycleHours = 2f;

        public float BaseCaptureRate => baseCaptureRate;
        public float HpWeightMultiplier => hpWeightMultiplier;
        public float StatusCaptureBonus => statusCaptureBonus;
        public int MaxCaptureAttempts => maxCaptureAttempts;
        public int MaxLevel => maxLevel;
        public float TrainerXpMultiplier => trainerXpMultiplier;
        public float XpShareRatio => xpShareRatio;
        public int MinDamage => minDamage;
        public float StabMultiplier => stabMultiplier;
        public float CritMultiplier => critMultiplier;
        public float CritBaseChance => critBaseChance;
        public float HeightBonusPerLevel => heightBonusPerLevel;
        public float FlankingBonus => flankingBonus;
        public int InstabilityMax => instabilityMax;
        public int InstabilityVolatileMin => instabilityVolatileMin;
        public int InstabilityUnstableMin => instabilityUnstableMin;
        public int InstabilityCriticalMin => instabilityCriticalMin;
        public int InstabilityBreakdownMin => instabilityBreakdownMin;
        public float InstabilityDecayPerRest => instabilityDecayPerRest;
        public float DisobeyBaseChance => disobeyBaseChance;
        public float BreakthroughChance => breakthroughChance;
        public int FallDamageBase => fallDamageBase;
        public int FallDamageMinDelta => fallDamageMinDelta;
        public int ConservationBonusThreshold => conservationBonusThreshold;
        public float MigrationCycleHours => migrationCycleHours;
    }
}
