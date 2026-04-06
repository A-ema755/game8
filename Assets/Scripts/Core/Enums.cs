namespace GeneForge.Core
{
    /// <summary>
    /// Primary genome type for creatures and moves.
    /// 14 types in 3 tiers: Standard (8), Extended (4), Apex (2).
    /// </summary>
    public enum CreatureType
    {
        None = 0,
        Thermal = 1,
        Aqua = 2,
        Organic = 3,
        Bioelectric = 4,
        Cryo = 5,
        Mineral = 6,
        Toxic = 7,
        Neural = 8,
        Ferro = 9,
        Kinetic = 10,
        Aero = 11,
        Sonic = 12,
        Ark = 13,
        Blight = 14
    }

    /// <summary>
    /// Determines stat pairing and grid behavior for damaging moves.
    /// Replaces the old Physical/Special/Status category system.
    /// </summary>
    public enum DamageForm
    {
        None = 0,
        Physical = 1,
        Energy = 2,
        Bio = 3
    }

    /// <summary>Body slot positions where parts can be equipped. Each archetype exposes a subset.</summary>
    public enum BodySlot
    {
        Head = 0,
        Back = 1,
        LeftArm = 2,
        RightArm = 3,
        Tail = 4,
        Legs = 5,
        Hide = 6,
        BodyUpper = 7,
        BodyLower = 8,
        Wings = 9,
        Talons = 10,
        CoreA = 11,
        CoreB = 12,
        CoreC = 13,
        Appendage = 14
    }

    /// <summary>Category grouping for body parts (used for synergy set detection).</summary>
    public enum PartCategory
    {
        Offensive = 0,
        Defensive = 1,
        Utility = 2,
        Aura = 3
    }

    /// <summary>Drop and encounter rarity for creatures and parts.</summary>
    public enum Rarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    /// <summary>Broad body shape archetype — determines available BodySlots.</summary>
    public enum BodyArchetype
    {
        Bipedal = 0,
        Quadruped = 1,
        Serpentine = 2,
        Avian = 3,
        Amorphous = 4
    }

    /// <summary>Instability tier for UI display and triggering instability events.</summary>
    public enum InstabilityTier
    {
        Stable = 0,
        Volatile = 1,
        Unstable = 2,
        Critical = 3,
        Breakdown = 4
    }

    /// <summary>
    /// Instability tier boundary helper. Threshold values are data-driven via
    /// GameSettings.asset (see Section 3.4). Values are cached on first access
    /// after ConfigLoader initializes to avoid per-call null checks in hot paths.
    /// Fallback defaults are used if GameSettings is unavailable (EditMode tests).
    /// </summary>
    public static class InstabilityThresholds
    {
        const int DefaultMax = 100;
        const int DefaultVolatileMin = 25;
        const int DefaultUnstableMin = 50;
        const int DefaultCriticalMin = 75;
        const int DefaultBreakdownMin = 100;

        static int _max = DefaultMax;
        static int _volatileMin = DefaultVolatileMin;
        static int _unstableMin = DefaultUnstableMin;
        static int _criticalMin = DefaultCriticalMin;
        static int _breakdownMin = DefaultBreakdownMin;

        public static int Max => _max;
        public static int VolatileMin => _volatileMin;
        public static int UnstableMin => _unstableMin;
        public static int CriticalMin => _criticalMin;
        public static int BreakdownMin => _breakdownMin;

        /// <summary>
        /// Called by ConfigLoader.Initialize() after GameSettings is loaded.
        /// Caches threshold values for zero-cost access in hot paths.
        /// </summary>
        public static void CacheFromSettings(GameSettings settings)
        {
            if (settings == null) return;
            _max = settings.InstabilityMax;
            _volatileMin = settings.InstabilityVolatileMin;
            _unstableMin = settings.InstabilityUnstableMin;
            _criticalMin = settings.InstabilityCriticalMin;
            _breakdownMin = settings.InstabilityBreakdownMin;
        }

        /// <summary>Returns the InstabilityTier for a given instability value (0-100).</summary>
        public static InstabilityTier GetTier(int instability)
        {
            if (instability >= _breakdownMin) return InstabilityTier.Breakdown;
            if (instability >= _criticalMin) return InstabilityTier.Critical;
            if (instability >= _unstableMin) return InstabilityTier.Unstable;
            if (instability >= _volatileMin) return InstabilityTier.Volatile;
            return InstabilityTier.Stable;
        }
    }

    /// <summary>Target pattern for move application.</summary>
    public enum TargetType
    {
        Single = 0,
        Adjacent = 1,
        AoE = 2,
        Self = 3,
        AllAllies = 4,
        SingleAlly = 5,
        Line = 6
    }

    /// <summary>Current state of a status effect on a creature.</summary>
    public enum StatusEffect
    {
        None = 0,
        Burn = 1,
        Freeze = 2,
        Paralysis = 3,
        Poison = 4,
        Sleep = 5,
        Confusion = 6,
        Taunt = 7,
        Stealth = 8
    }

    /// <summary>Broad behavior archetype for wild creature AI.</summary>
    public enum AIPersonalityType
    {
        Predator = 0,
        Territorial = 1,
        Defensive = 2,
        Berserker = 3,
        Support = 4,
        Trainer = 5
    }

    /// <summary>Terrain tile type — affects terrain synergy and move interactions.</summary>
    public enum TerrainType
    {
        Neutral = 0,
        Thermal = 1,
        Aqua = 2,
        Organic = 3,
        Cryo = 4,
        Mineral = 5,
        Kinetic = 6,
        Neural = 7,
        Toxic = 8,
        Hazard = 9,
        Difficult = 10,
        Elevated = 11
    }

    /// <summary>Research tier for Pokedex entry completeness.</summary>
    public enum PokedexTier
    {
        Unseen = 0,
        Silhouette = 1,
        BasicProfile = 2,
        FullProfile = 3,
        Complete = 4
    }

    /// <summary>XP growth rate curve identifier.</summary>
    public enum GrowthCurve
    {
        Fast = 0,
        Medium = 1,
        Slow = 2,
        Erratic = 3
    }

    /// <summary>Personality behavioral trait equipped on a creature.</summary>
    public enum PersonalityTrait
    {
        None = 0,
        Aggressive = 1,
        Cautious = 2,
        Loyal = 3,
        Feral = 4,
        Curious = 5,
        Territorial = 6
    }
}
