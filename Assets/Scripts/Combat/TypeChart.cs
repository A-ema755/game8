using GeneForge.Core; // CreatureType enum + ConfigLoader both live in GeneForge.Core
using UnityEngine;

namespace GeneForge.Combat
{
    /// <summary>
    /// Static type effectiveness matrix for 14 genome types.
    /// Initialized once at startup. Thread-safe after initialization.
    /// ADR-007: static array, not ScriptableObject.
    /// </summary>
    public static class TypeChart
    {
        // Dimensions: [attackingTypeIndex, defendingTypeIndex]
        // Index 0 = None (neutral), indices 1-14 = Genome types
        private static float[,] _matrix;
        private static bool _initialized;

        private const int TypeCount = 15; // 0=None, 1-14=Genome types

        public static void Initialize()
        {
            if (_initialized) return;

            _matrix = new float[TypeCount, TypeCount];

            // Default all to 1.0 (neutral)
            for (int a = 0; a < TypeCount; a++)
            for (int d = 0; d < TypeCount; d++)
                _matrix[a, d] = 1.0f;

            // -- Self-resist: every type resists itself ----------------------
            for (int i = 1; i < TypeCount; i++)
                _matrix[i, i] = 0.5f;

            // -- Core Triangle 1: Elemental ----------------------------------
            Set(CreatureType.Thermal, CreatureType.Organic,     2.0f); // #1
            Set(CreatureType.Organic, CreatureType.Aqua,        2.0f); // #2
            Set(CreatureType.Aqua,    CreatureType.Thermal,     2.0f); // #3

            // -- Core Triangle 2: Physical -----------------------------------
            Set(CreatureType.Mineral,     CreatureType.Bioelectric, 2.0f); // #4
            Set(CreatureType.Bioelectric, CreatureType.Aqua,        2.0f); // #5
            Set(CreatureType.Aqua,        CreatureType.Mineral,     2.0f); // #6

            // -- Core Triangle 3: Mental -------------------------------------
            Set(CreatureType.Neural,  CreatureType.Kinetic, 2.0f); // #7
            Set(CreatureType.Kinetic, CreatureType.Ferro,   2.0f); // #8
            Set(CreatureType.Ferro,   CreatureType.Neural,  2.0f); // #9

            // -- Core Triangle 4: Atmospheric --------------------------------
            Set(CreatureType.Cryo, CreatureType.Aero,  2.0f); // #10
            Set(CreatureType.Aero, CreatureType.Sonic,  2.0f); // #11
            Set(CreatureType.Sonic, CreatureType.Cryo,  2.0f); // #12

            // -- Cross-Links -------------------------------------------------
            Set(CreatureType.Thermal,     CreatureType.Cryo,     2.0f); // #13
            Set(CreatureType.Thermal,     CreatureType.Ferro,    2.0f); // #14
            Set(CreatureType.Cryo,        CreatureType.Organic,  2.0f); // #15
            Set(CreatureType.Cryo,        CreatureType.Kinetic,  2.0f); // #16
            Set(CreatureType.Mineral,     CreatureType.Toxic,    2.0f); // #17
            Set(CreatureType.Mineral,     CreatureType.Sonic,    2.0f); // #18
            Set(CreatureType.Mineral,     CreatureType.Thermal,  2.0f); // #19
            Set(CreatureType.Toxic,       CreatureType.Organic,  2.0f); // #20
            Set(CreatureType.Toxic,       CreatureType.Ferro,    2.0f); // #21
            Set(CreatureType.Neural,      CreatureType.Toxic,    2.0f); // #22
            Set(CreatureType.Ferro,       CreatureType.Cryo,     2.0f); // #23
            Set(CreatureType.Kinetic,     CreatureType.Mineral,  2.0f); // #24
            Set(CreatureType.Bioelectric, CreatureType.Aero,     2.0f); // #25
            Set(CreatureType.Aero,        CreatureType.Thermal,  2.0f); // #26
            Set(CreatureType.Organic,     CreatureType.Mineral,  2.0f); // #27

            // -- Apex --------------------------------------------------------
            Set(CreatureType.Ark,     CreatureType.Blight,      2.0f); // #28
            Set(CreatureType.Ark,     CreatureType.Toxic,       2.0f); // #29
            Set(CreatureType.Ark,     CreatureType.Kinetic,     2.0f); // #30
            Set(CreatureType.Blight,  CreatureType.Ark,         2.0f); // #31
            Set(CreatureType.Blight,  CreatureType.Bioelectric, 2.0f); // #32
            Set(CreatureType.Blight,  CreatureType.Neural,      2.0f); // #33
            Set(CreatureType.Thermal, CreatureType.Ark,         2.0f); // #34
            Set(CreatureType.Organic, CreatureType.Blight,      2.0f); // #35
            Set(CreatureType.Sonic,   CreatureType.Neural,      2.0f); // #36

            // -- Additional Resistances (0.5x) -------------------------------
            // Thermal resists: Organic, Cryo, Ferro
            Set(CreatureType.Organic, CreatureType.Thermal, 0.5f);
            Set(CreatureType.Cryo,    CreatureType.Thermal, 0.5f);
            Set(CreatureType.Ferro,   CreatureType.Thermal, 0.5f);

            // Aqua resists: Thermal, Cryo
            Set(CreatureType.Thermal, CreatureType.Aqua, 0.5f);
            Set(CreatureType.Cryo,    CreatureType.Aqua, 0.5f);

            // Organic resists: Aqua, Bioelectric, Mineral
            Set(CreatureType.Aqua,        CreatureType.Organic, 0.5f);
            Set(CreatureType.Bioelectric, CreatureType.Organic, 0.5f);
            Set(CreatureType.Mineral,     CreatureType.Organic, 0.5f);

            // Bioelectric resists: Aero, Ferro
            Set(CreatureType.Aero,  CreatureType.Bioelectric, 0.5f);
            Set(CreatureType.Ferro, CreatureType.Bioelectric, 0.5f);

            // Cryo resists: Aqua
            Set(CreatureType.Aqua, CreatureType.Cryo, 0.5f);

            // Mineral resists: Thermal, Bioelectric, Toxic, Sonic
            Set(CreatureType.Thermal,     CreatureType.Mineral, 0.5f);
            Set(CreatureType.Bioelectric, CreatureType.Mineral, 0.5f);
            Set(CreatureType.Toxic,       CreatureType.Mineral, 0.5f);
            Set(CreatureType.Sonic,       CreatureType.Mineral, 0.5f);

            // Toxic resists: Organic
            Set(CreatureType.Organic, CreatureType.Toxic, 0.5f);

            // Neural resists: Toxic
            Set(CreatureType.Toxic, CreatureType.Neural, 0.5f);

            // Ferro resists: Cryo, Organic, Neural, Sonic, Aero
            Set(CreatureType.Cryo,    CreatureType.Ferro, 0.5f);
            Set(CreatureType.Organic, CreatureType.Ferro, 0.5f);
            Set(CreatureType.Neural,  CreatureType.Ferro, 0.5f);
            Set(CreatureType.Sonic,   CreatureType.Ferro, 0.5f);
            Set(CreatureType.Aero,    CreatureType.Ferro, 0.5f);

            // Kinetic resists: Sonic
            Set(CreatureType.Sonic, CreatureType.Kinetic, 0.5f);

            // Aero resists: Organic, Kinetic
            Set(CreatureType.Organic, CreatureType.Aero, 0.5f);
            Set(CreatureType.Kinetic, CreatureType.Aero, 0.5f);

            // Sonic resists: Kinetic
            Set(CreatureType.Kinetic, CreatureType.Sonic, 0.5f);

            // Ark resists: Toxic, Neural, Cryo, Sonic, Mineral
            Set(CreatureType.Toxic,   CreatureType.Ark, 0.5f);
            Set(CreatureType.Neural,  CreatureType.Ark, 0.5f);
            Set(CreatureType.Cryo,    CreatureType.Ark, 0.5f);
            Set(CreatureType.Sonic,   CreatureType.Ark, 0.5f);
            Set(CreatureType.Mineral, CreatureType.Ark, 0.5f);

            // Blight resists: Thermal, Bioelectric, Toxic, Kinetic
            Set(CreatureType.Thermal,     CreatureType.Blight, 0.5f);
            Set(CreatureType.Bioelectric, CreatureType.Blight, 0.5f);
            Set(CreatureType.Toxic,       CreatureType.Blight, 0.5f);
            Set(CreatureType.Kinetic,     CreatureType.Blight, 0.5f);

            ValidateMatrix();
            CacheSettings();
            _initialized = true;
        }

        private static void Set(CreatureType atk, CreatureType def, float value)
            => _matrix[(int)atk, (int)def] = value;

        /// <summary>
        /// Returns the combined type effectiveness multiplier for an attack.
        /// If target has two types, both multipliers are multiplied together.
        /// </summary>
        /// <param name="attackType">The genome type of the move.</param>
        /// <param name="primaryType">The target's primary genome type.</param>
        /// <param name="secondaryType">The target's secondary genome type (None if single-type).</param>
        public static float GetMultiplier(
            CreatureType attackType,
            CreatureType primaryType,
            CreatureType secondaryType = CreatureType.None)
        {
            if (!_initialized)
            {
                Debug.LogError("[TypeChart] Not initialized. Call TypeChart.Initialize() first.");
                return 1.0f;
            }

            float mult = _matrix[(int)attackType, (int)primaryType];

            if (secondaryType != CreatureType.None)
                mult *= _matrix[(int)attackType, (int)secondaryType];

            return mult;
        }

        /// <summary>
        /// Returns the effectiveness label for UI display.
        /// Uses the combined multiplier from GetMultiplier.
        /// No Immune label -- minimum multiplier is 0.25x (dual resist).
        /// </summary>
        public static EffectivenessLabel GetLabel(float multiplier)
        {
            if (multiplier < 1f)    return EffectivenessLabel.Resisted;
            if (multiplier > 1f)    return EffectivenessLabel.SuperEffective;
            return EffectivenessLabel.Neutral;
        }

        /// <summary>
        /// Calculate STAB multiplier.
        /// Returns StabMultiplier if move type matches either creature type; 1.0 otherwise.
        /// Also applies to types granted via DNA type infusion (see DNA Alteration System).
        /// </summary>
        public static float GetStab(
            CreatureType moveType,
            CreatureType creaturePrimaryType,
            CreatureType creatureSecondaryType = CreatureType.None)
        {
            if (moveType == CreatureType.None) return 1.0f;

            bool hasStab = moveType == creaturePrimaryType
                        || (creatureSecondaryType != CreatureType.None
                            && moveType == creatureSecondaryType);
            return hasStab ? _stabMultiplier : 1.0f;
        }

        // -- Cached multipliers -- loaded from GameSettings during Initialize() --
        // Fallback defaults used when GameSettings is unavailable (EditMode tests)
        private static float _stabMultiplier = 1.5f;
        private static float _superEffectiveMultiplier = 2.0f;
        private static float _resistedMultiplier = 0.5f;

        public static float StabMultiplier => _stabMultiplier;
        public static float SuperEffectiveMultiplier => _superEffectiveMultiplier;
        public static float ResistedMultiplier => _resistedMultiplier;

        /// <summary>
        /// Validates matrix integrity after population. Logs warnings for:
        /// - Unexpected number of SE entries (expected: 36)
        /// - Any 0.0 entries (no immunities allowed)
        /// - Missing self-resistances
        /// </summary>
        private static void ValidateMatrix()
        {
            int seCount = 0, zeroCount = 0, invalidCount = 0;
            for (int a = 1; a < TypeCount; a++)
            {
                for (int d = 1; d < TypeCount; d++)
                {
                    float v = _matrix[a, d];
                    if (v == 2.0f) seCount++;
                    if (v == 0.0f) zeroCount++;
                    if (v != 0.5f && v != 1.0f && v != 2.0f)
                        invalidCount++; // Only 0.5, 1.0, 2.0 are valid (Section 3.2)
                }
                if (_matrix[a, a] != 0.5f)
                    Debug.LogWarning($"[TypeChart] Type {(CreatureType)a} does not resist itself.");
            }
            if (seCount != 36)
                Debug.LogWarning($"[TypeChart] Expected 36 SE entries, found {seCount}.");
            if (zeroCount > 0)
                Debug.LogError($"[TypeChart] Found {zeroCount} immunity (0.0) entries -- no immunities allowed.");
            if (invalidCount > 0)
                Debug.LogError($"[TypeChart] Found {invalidCount} entries not in {{0.5, 1.0, 2.0}} -- only three values allowed.");
        }

        /// <summary>
        /// Called at end of Initialize() to load tunable values from GameSettings.
        /// Source of truth for STAB is GameSettings.asset (see data-configuration-pipeline.md).
        /// </summary>
        private static void CacheSettings()
        {
            if (ConfigLoader.Settings == null) return; // Keep fallback defaults
            _stabMultiplier = ConfigLoader.Settings.StabMultiplier;
            // SuperEffective and Resisted are baked into the matrix at init time;
            // these cached values are reference constants for UI and balance tools.
        }
    }

    /// <summary>
    /// UI display label for type effectiveness results.
    /// Defined here (not in Core/Enums.cs) because it is exclusively consumed by
    /// TypeChart.GetLabel() and Combat UI -- it has no cross-system meaning.
    /// </summary>
    public enum EffectivenessLabel
    {
        Resisted         = 0,
        Neutral          = 1,
        SuperEffective   = 2
    }
}
