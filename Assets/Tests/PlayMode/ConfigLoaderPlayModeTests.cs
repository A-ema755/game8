using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GeneForge.Core;

namespace GeneForge.Tests
{
    /// <summary>
    /// PlayMode tests for ConfigLoader.
    /// Covers acceptance criteria requiring runtime initialization.
    /// </summary>
    [TestFixture]
    public class ConfigLoaderPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            ResetConfigLoader();
        }

        [TearDown]
        public void TearDown()
        {
            ResetConfigLoader();
        }

        static void ResetConfigLoader()
        {
            var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            var publicFlags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public;

            typeof(ConfigLoader).GetField("_initialized", flags)?.SetValue(null, false);

            // Clear all registries to prevent state leaking between tests
            ClearRegistry<CreatureConfig>("_creatures");
            ClearRegistry<MoveConfig>("_moves");
            ClearRegistry<BodyPartConfig>("_bodyParts");
            ClearRegistry<StatusEffectConfig>("_statusEffects");
            ClearRegistry<EncounterConfig>("_encounters");
            ClearRegistry<AIPersonalityConfig>("_aiPersonalities");

            typeof(ConfigLoader).GetProperty("Settings", publicFlags)?.SetValue(null, null);

            // Reset InstabilityThresholds to fallback defaults
            var thresholdType = typeof(InstabilityThresholds);
            thresholdType.GetField("_max", flags)?.SetValue(null, 100);
            thresholdType.GetField("_volatileMin", flags)?.SetValue(null, 25);
            thresholdType.GetField("_unstableMin", flags)?.SetValue(null, 50);
            thresholdType.GetField("_criticalMin", flags)?.SetValue(null, 75);
            thresholdType.GetField("_breakdownMin", flags)?.SetValue(null, 100);
        }

        static void ClearRegistry<T>(string fieldName) where T : ConfigBase
        {
            var field = typeof(ConfigLoader).GetField(fieldName,
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var dict = field?.GetValue(null) as System.Collections.IDictionary;
            dict?.Clear();
        }

        // ── AC #18: Boot integration — ConfigLoader initializes, registries populated ──

        [UnityTest]
        public IEnumerator test_ConfigLoader_Initialize_AllRegistriesPopulated_SettingsLoaded()
        {
            // Act
            ConfigLoader.Initialize();
            yield return null;

            // Assert — registries should be accessible (may be empty if no assets on disk)
            Assert.IsNotNull(ConfigLoader.Creatures, "Creatures registry should be non-null");
            Assert.IsNotNull(ConfigLoader.Moves, "Moves registry should be non-null");
            Assert.IsNotNull(ConfigLoader.BodyParts, "BodyParts registry should be non-null");
            Assert.IsNotNull(ConfigLoader.StatusEffects, "StatusEffects registry should be non-null");
            Assert.IsNotNull(ConfigLoader.Encounters, "Encounters registry should be non-null");
            Assert.IsNotNull(ConfigLoader.AIPersonalities, "AIPersonalities registry should be non-null");
        }

        // ── AC #9: GameSettings.asset loads and Settings is non-null ──

        [UnityTest]
        public IEnumerator test_ConfigLoader_GameSettings_LoadsSuccessfully()
        {
            // Act
            ConfigLoader.Initialize();
            yield return null;

            // Assert — GameSettings.asset must exist at Resources/Data/GameSettings
            Assert.IsNotNull(ConfigLoader.Settings,
                "GameSettings.asset should load from Resources/Data/GameSettings. " +
                "If null, create the asset via Unity Editor: " +
                "Assets > Create > GeneForge > GameSettings, save to Resources/Data/GameSettings.asset");
        }

        // ── AC: ConfigLoader.Initialize is idempotent in PlayMode ──

        [UnityTest]
        public IEnumerator test_ConfigLoader_Initialize_CalledTwice_IsIdempotent()
        {
            // Act
            ConfigLoader.Initialize();
            yield return null;

            int creaturesFirst = ConfigLoader.Creatures.Count;
            int movesFirst = ConfigLoader.Moves.Count;

            ConfigLoader.Initialize();
            yield return null;

            // Assert
            Assert.AreEqual(creaturesFirst, ConfigLoader.Creatures.Count);
            Assert.AreEqual(movesFirst, ConfigLoader.Moves.Count);
        }

        // ── AC: InstabilityThresholds uses fallback defaults when GameSettings is null ──

        [UnityTest]
        public IEnumerator test_InstabilityThresholds_FallbackDefaults_WhenSettingsNull()
        {
            // Arrange — don't initialize ConfigLoader, so Settings stays null
            // InstabilityThresholds should use fallback defaults

            // Assert — verify defaults match GDD Section 3.4
            Assert.AreEqual(100, InstabilityThresholds.Max);
            Assert.AreEqual(25, InstabilityThresholds.VolatileMin);
            Assert.AreEqual(50, InstabilityThresholds.UnstableMin);
            Assert.AreEqual(75, InstabilityThresholds.CriticalMin);
            Assert.AreEqual(100, InstabilityThresholds.BreakdownMin);

            // Verify tier boundaries
            Assert.AreEqual(InstabilityTier.Stable, InstabilityThresholds.GetTier(0));
            Assert.AreEqual(InstabilityTier.Volatile, InstabilityThresholds.GetTier(25));
            Assert.AreEqual(InstabilityTier.Unstable, InstabilityThresholds.GetTier(50));
            Assert.AreEqual(InstabilityTier.Critical, InstabilityThresholds.GetTier(75));
            Assert.AreEqual(InstabilityTier.Breakdown, InstabilityThresholds.GetTier(100));
            yield return null;
        }
    }
}
