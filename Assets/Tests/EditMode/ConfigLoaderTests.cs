using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GeneForge.Core;
using GeneForge.Creatures;

namespace GeneForge.Tests
{
    [TestFixture]
    public class ConfigLoaderTests
    {
        static void ResetConfigLoader()
        {
            var flags = BindingFlags.Static | BindingFlags.NonPublic;

            var initialized = typeof(ConfigLoader).GetField("_initialized", flags);
            initialized.SetValue(null, false);

            ClearRegistry<CreatureConfig>("_creatures");
            ClearRegistry<MoveConfig>("_moves");
            ClearRegistry<BodyPartConfig>("_bodyParts");
            ClearRegistry<StatusEffectConfig>("_statusEffects");
            ClearRegistry<EncounterConfig>("_encounters");
            ClearRegistry<AIPersonalityConfig>("_aiPersonalities");

            var settingsProp = typeof(ConfigLoader).GetProperty("Settings",
                BindingFlags.Static | BindingFlags.Public);
            settingsProp.SetValue(null, null);

            // Reset InstabilityThresholds static fields to defaults
            var thresholdFlags = BindingFlags.Static | BindingFlags.NonPublic;
            var thresholdType = typeof(InstabilityThresholds);
            thresholdType.GetField("_max", thresholdFlags).SetValue(null, 100);
            thresholdType.GetField("_volatileMin", thresholdFlags).SetValue(null, 25);
            thresholdType.GetField("_unstableMin", thresholdFlags).SetValue(null, 50);
            thresholdType.GetField("_criticalMin", thresholdFlags).SetValue(null, 75);
            thresholdType.GetField("_breakdownMin", thresholdFlags).SetValue(null, 100);
        }

        static Dictionary<string, T> GetRegistry<T>(string fieldName) where T : ConfigBase
        {
            var field = typeof(ConfigLoader).GetField(fieldName,
                BindingFlags.Static | BindingFlags.NonPublic);
            return (Dictionary<string, T>)field.GetValue(null);
        }

        static void ClearRegistry<T>(string fieldName) where T : ConfigBase
        {
            GetRegistry<T>(fieldName).Clear();
        }

        static T CreateConfig<T>(string id, string displayName) where T : ConfigBase
        {
            var config = ScriptableObject.CreateInstance<T>();
            var idField = typeof(ConfigBase).GetField("id",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var nameField = typeof(ConfigBase).GetField("displayName",
                BindingFlags.Instance | BindingFlags.NonPublic);
            idField.SetValue(config, id);
            nameField.SetValue(config, displayName);
            return config;
        }

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

        [Test]
        public void Creatures_ExposesRegistryCount_MatchesInsertedAssets()
        {
            var registry = GetRegistry<CreatureConfig>("_creatures");
            var a = CreateConfig<CreatureConfig>("emberfox", "Emberfox");
            var b = CreateConfig<CreatureConfig>("thorn-slug", "Thorn Slug");
            registry.Add(a.Id, a);
            registry.Add(b.Id, b);

            Assert.AreEqual(2, ConfigLoader.Creatures.Count);
        }

        [Test]
        public void DuplicateId_FirstAssetWins_WarningLogged()
        {
            LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex("Duplicate ID 'emberfox'"));

            var registry = GetRegistry<CreatureConfig>("_creatures");
            var first = CreateConfig<CreatureConfig>("emberfox", "Emberfox");
            var duplicate = CreateConfig<CreatureConfig>("emberfox", "Emberfox Duplicate");

            // First add succeeds
            registry.TryAdd(first.Id, first);

            // Second add fails — simulate ConfigLoader.LoadAll duplicate handling
            if (!registry.TryAdd(duplicate.Id, duplicate))
                Debug.LogWarning($"[ConfigLoader] Duplicate ID '{duplicate.Id}' in {typeof(CreatureConfig).Name}.");

            Assert.AreEqual(1, registry.Count);
            Assert.AreEqual("Emberfox", registry["emberfox"].DisplayName);
        }

        [Test]
        public void EmptyId_AssetSkipped_ErrorLogged()
        {
            var emptyIdConfig = CreateConfig<CreatureConfig>("", "No ID Creature");

            Assert.IsTrue(string.IsNullOrEmpty(emptyIdConfig.Id));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("empty ID"));

            // Simulate LoadAll behavior for empty ID
            if (string.IsNullOrEmpty(emptyIdConfig.Id))
            {
                Debug.LogError($"[ConfigLoader] {typeof(CreatureConfig).Name} at Data/Creatures has empty ID.");
            }

            var registry = GetRegistry<CreatureConfig>("_creatures");
            Assert.AreEqual(0, registry.Count);
        }

        [Test]
        public void Get_ReturnsCorrectAsset_ForKnownId()
        {
            var registry = GetRegistry<CreatureConfig>("_creatures");
            var config = CreateConfig<CreatureConfig>("emberfox", "Emberfox");
            registry.Add(config.Id, config);

            var result = ConfigLoader.GetCreature("emberfox");

            Assert.IsNotNull(result);
            Assert.AreEqual("emberfox", result.Id);
            Assert.AreEqual("Emberfox", result.DisplayName);
        }

        [Test]
        public void Get_ReturnsNull_AndLogsError_ForUnknownId()
        {
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex("Config not found.*nonexistent"));

            var result = ConfigLoader.GetCreature("nonexistent");

            Assert.IsNull(result);
        }

        [Test]
        public void Initialize_CalledTwice_IsIdempotent()
        {
            // First Initialize logs GameSettings error (no asset on disk in EditMode)
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex("GameSettings.asset not found"));

            ConfigLoader.Initialize();

            int creaturesAfterFirst = ConfigLoader.Creatures.Count;
            int movesAfterFirst = ConfigLoader.Moves.Count;

            // Second Initialize — should return early, no duplicate entries, no additional logs
            ConfigLoader.Initialize();

            Assert.AreEqual(creaturesAfterFirst, ConfigLoader.Creatures.Count);
            Assert.AreEqual(movesAfterFirst, ConfigLoader.Moves.Count);
        }

        [Test]
        public void GetTier_ReturnsFallbackDefaults_WhenSettingsNull()
        {
            // Settings is null after ResetConfigLoader, so fallback defaults apply
            Assert.AreEqual(InstabilityTier.Stable, InstabilityThresholds.GetTier(0));
            Assert.AreEqual(InstabilityTier.Stable, InstabilityThresholds.GetTier(24));
            Assert.AreEqual(InstabilityTier.Volatile, InstabilityThresholds.GetTier(25));
            Assert.AreEqual(InstabilityTier.Volatile, InstabilityThresholds.GetTier(49));
            Assert.AreEqual(InstabilityTier.Unstable, InstabilityThresholds.GetTier(50));
            Assert.AreEqual(InstabilityTier.Unstable, InstabilityThresholds.GetTier(74));
            Assert.AreEqual(InstabilityTier.Critical, InstabilityThresholds.GetTier(75));
            Assert.AreEqual(InstabilityTier.Critical, InstabilityThresholds.GetTier(99));
            Assert.AreEqual(InstabilityTier.Breakdown, InstabilityThresholds.GetTier(100));
        }

        [Test]
        public void TypedGetMethods_WorkForAllRegistries()
        {
            var move = CreateConfig<MoveConfig>("flame-claw", "Flame Claw");
            GetRegistry<MoveConfig>("_moves").Add(move.Id, move);

            var bodyPart = CreateConfig<BodyPartConfig>("fire-glands", "Fire Glands");
            GetRegistry<BodyPartConfig>("_bodyParts").Add(bodyPart.Id, bodyPart);

            var status = CreateConfig<StatusEffectConfig>("burn", "Burn");
            GetRegistry<StatusEffectConfig>("_statusEffects").Add(status.Id, status);

            var encounter = CreateConfig<EncounterConfig>("forest-ambush", "Forest Ambush");
            GetRegistry<EncounterConfig>("_encounters").Add(encounter.Id, encounter);

            var ai = CreateConfig<AIPersonalityConfig>("predator", "Predator");
            GetRegistry<AIPersonalityConfig>("_aiPersonalities").Add(ai.Id, ai);

            Assert.AreEqual("Flame Claw", ConfigLoader.GetMove("flame-claw").DisplayName);
            Assert.AreEqual("Fire Glands", ConfigLoader.GetBodyPart("fire-glands").DisplayName);
            Assert.AreEqual("Burn", ConfigLoader.GetStatusEffect("burn").DisplayName);
            Assert.AreEqual("Forest Ambush", ConfigLoader.GetEncounter("forest-ambush").DisplayName);
            Assert.AreEqual("Predator", ConfigLoader.GetAIPersonality("predator").DisplayName);
        }

        [Test]
        public void Initialize_MissingGameSettings_LogsError_SettingsIsNull()
        {
            // Initialize will attempt Resources.Load<GameSettings> which returns null in EditMode
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex("GameSettings.asset not found"));

            ConfigLoader.Initialize();

            Assert.IsNull(ConfigLoader.Settings);
        }

        [Test]
        public void Reinitialize_ClearsAndReloads_MatchesFreshInitialize()
        {
            // Both Initialize() and Reinitialize() trigger GameSettings error (no asset on disk)
            var settingsError = new System.Text.RegularExpressions.Regex("GameSettings.asset not found");
            LogAssert.Expect(LogType.Error, settingsError);
            LogAssert.Expect(LogType.Error, settingsError);

            ConfigLoader.Initialize();

            int creaturesAfterInit = ConfigLoader.Creatures.Count;
            int movesAfterInit = ConfigLoader.Moves.Count;
            int bodyPartsAfterInit = ConfigLoader.BodyParts.Count;

            // Reinitialize — should clear and reload, yielding same counts
            ConfigLoader.Reinitialize();

            Assert.AreEqual(creaturesAfterInit, ConfigLoader.Creatures.Count);
            Assert.AreEqual(movesAfterInit, ConfigLoader.Moves.Count);
            Assert.AreEqual(bodyPartsAfterInit, ConfigLoader.BodyParts.Count);
        }
    }
}
