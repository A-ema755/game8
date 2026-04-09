using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using GeneForge.Core;
using GeneForge.Creatures;
using UnityEngine;

namespace GeneForge.Tests
{
    [TestFixture]
    public class CreatureInstanceTests
    {
        // ── Test Configs ─────────────────────────────────────────────────
        private CreatureConfig _testConfig;
        private MoveConfig _moveA;
        private MoveConfig _moveB;
        private MoveConfig _moveC;
        private MoveConfig _moveD;
        private MoveConfig _moveE;
        private BodyPartConfig _testPart;

        // ── Reflection Helpers ───────────────────────────────────────────

        private static void SetField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {obj.GetType().Name}");
            field.SetValue(obj, value);
        }

        private static void SetStaticField(Type type, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, $"Static field '{fieldName}' not found on {type.Name}");
            field.SetValue(null, value);
        }

        private static object GetStaticField(Type type, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            return field?.GetValue(null);
        }

        private MoveConfig CreateMoveConfig(string id, string name, int pp)
        {
            var move = ScriptableObject.CreateInstance<MoveConfig>();
            SetField(move, "id", id);
            SetField(move, "displayName", name);
            SetField(move, "pp", pp);
            return move;
        }

        private BodyPartConfig CreateBodyPartConfig(string id, DamageForm form)
        {
            var part = ScriptableObject.CreateInstance<BodyPartConfig>();
            SetField(part, "id", id);
            SetField(part, "displayName", id);
            SetField(part, "formAccess", form);
            return part;
        }

        private CreatureConfig CreateCreatureConfig(
            string id, string name,
            BaseStats stats, GrowthCurve curve,
            List<LevelMoveEntry> movePool,
            List<BodySlot> slots,
            CreatureType primary = CreatureType.Thermal,
            CreatureType secondary = CreatureType.None)
        {
            var config = ScriptableObject.CreateInstance<CreatureConfig>();
            SetField(config, "id", id);
            SetField(config, "displayName", name);
            SetField(config, "primaryType", primary);
            SetField(config, "secondaryType", secondary);
            SetField(config, "baseStats", stats);
            SetField(config, "growthCurve", curve);
            SetField(config, "movePool", movePool);
            SetField(config, "availableSlots", slots);
            SetField(config, "defaultPartIds", new List<string>());
            SetField(config, "habitatZoneIds", new List<string>());
            return config;
        }

        private void InjectIntoConfigLoader()
        {
            var movesDict = (Dictionary<string, ConfigBase>)GetStaticField(typeof(ConfigLoader), "_moves");
            movesDict.Clear();
            movesDict["move-a"] = _moveA;
            movesDict["move-b"] = _moveB;
            movesDict["move-c"] = _moveC;
            movesDict["move-d"] = _moveD;
            movesDict["move-e"] = _moveE;

            var partsDict = (Dictionary<string, ConfigBase>)GetStaticField(typeof(ConfigLoader), "_bodyParts");
            partsDict.Clear();
            partsDict["claw-part"] = _testPart;

            SetStaticField(typeof(ConfigLoader), "_initialized", true);
        }

        // ── Setup / Teardown ─────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _moveA = CreateMoveConfig("move-a", "Flame Claw", 10);
            _moveB = CreateMoveConfig("move-b", "Aqua Jet", 8);
            _moveC = CreateMoveConfig("move-c", "Vine Whip", 12);
            _moveD = CreateMoveConfig("move-d", "Shock Bolt", 6);
            _moveE = CreateMoveConfig("move-e", "Ice Shard", 5);

            _testPart = CreateBodyPartConfig("claw-part", DamageForm.Physical);

            var movePool = new List<LevelMoveEntry>
            {
                new LevelMoveEntry(1, "move-a"),
                new LevelMoveEntry(1, "move-b"),
                new LevelMoveEntry(5, "move-c"),
                new LevelMoveEntry(10, "move-d"),
                new LevelMoveEntry(15, "move-e"),
            };

            var slots = new List<BodySlot>
            {
                BodySlot.Head, BodySlot.LeftArm, BodySlot.RightArm, BodySlot.Tail
            };

            _testConfig = CreateCreatureConfig(
                "emberfox", "Emberfox",
                new BaseStats(50, 40, 30, 35, 100),
                GrowthCurve.Medium,
                movePool, slots
            );

            InjectIntoConfigLoader();
        }

        [TearDown]
        public void TearDown()
        {
            var movesDict = (Dictionary<string, ConfigBase>)GetStaticField(typeof(ConfigLoader), "_moves");
            movesDict?.Clear();
            var partsDict = (Dictionary<string, ConfigBase>)GetStaticField(typeof(ConfigLoader), "_bodyParts");
            partsDict?.Clear();
            SetStaticField(typeof(ConfigLoader), "_initialized", false);

            ScriptableObject.DestroyImmediate(_testConfig);
            ScriptableObject.DestroyImmediate(_moveA);
            ScriptableObject.DestroyImmediate(_moveB);
            ScriptableObject.DestroyImmediate(_moveC);
            ScriptableObject.DestroyImmediate(_moveD);
            ScriptableObject.DestroyImmediate(_moveE);
            ScriptableObject.DestroyImmediate(_testPart);
        }

        // ================================================================
        // GDD Acceptance Criteria — #1 through #23
        // ================================================================

        [Test]
        public void test_CreatureInstance_Create_Level1_HasCorrectDefaults()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);

            Assert.AreEqual(1, creature.Level);
            Assert.AreEqual(0, creature.CurrentXP);
            Assert.AreEqual(creature.MaxHP, creature.CurrentHP);
            Assert.AreEqual(2, creature.LearnedMoveIds.Count);
            Assert.AreEqual("move-a", creature.LearnedMoveIds[0]);
            Assert.AreEqual("move-b", creature.LearnedMoveIds[1]);
            Assert.IsFalse(creature.IsFainted);
            Assert.AreEqual(0, creature.Instability);
            Assert.AreEqual(PersonalityTrait.None, creature.Personality);
        }

        [Test]
        public void test_CreatureInstance_Stats_Level1_MatchesFormula()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);

            Assert.AreEqual(51, creature.MaxHP);
            Assert.AreEqual(40, creature.ComputedStats.ATK);
            Assert.AreEqual(30, creature.ComputedStats.DEF);
            Assert.AreEqual(35, creature.ComputedStats.SPD);
            Assert.AreEqual(100, creature.ComputedStats.ACC);
        }

        [Test]
        public void test_CreatureInstance_Stats_Level25_MatchesFormula()
        {
            var creature = CreatureInstance.Create(_testConfig, 25);

            Assert.AreEqual(75, creature.MaxHP);
            Assert.AreEqual(60, creature.ComputedStats.ATK);
            Assert.AreEqual(45, creature.ComputedStats.DEF);
            Assert.AreEqual(52, creature.ComputedStats.SPD);
        }

        [Test]
        public void test_CreatureInstance_Stats_Level50_MatchesFormula()
        {
            var creature = CreatureInstance.Create(_testConfig, 50);

            Assert.AreEqual(100, creature.MaxHP);
            Assert.AreEqual(80, creature.ComputedStats.ATK);
            Assert.AreEqual(60, creature.ComputedStats.DEF);
            Assert.AreEqual(70, creature.ComputedStats.SPD);
        }

        [Test]
        public void test_CreatureInstance_GrowthCurve_ProducesDifferentStats()
        {
            var fastConfig = CreateCreatureConfig(
                "fast-fox", "FastFox",
                new BaseStats(50, 40, 30, 35, 100),
                GrowthCurve.Fast,
                new List<LevelMoveEntry> { new LevelMoveEntry(1, "move-a") },
                new List<BodySlot> { BodySlot.Head });

            var slowConfig = CreateCreatureConfig(
                "slow-fox", "SlowFox",
                new BaseStats(50, 40, 30, 35, 100),
                GrowthCurve.Slow,
                new List<LevelMoveEntry> { new LevelMoveEntry(1, "move-a") },
                new List<BodySlot> { BodySlot.Head });

            var fast = CreatureInstance.Create(fastConfig, 25);
            var slow = CreatureInstance.Create(slowConfig, 25);

            Assert.AreEqual(80, fast.MaxHP);
            Assert.AreEqual(70, slow.MaxHP);
            Assert.Greater(fast.ComputedStats.ATK, slow.ComputedStats.ATK);

            ScriptableObject.DestroyImmediate(fastConfig);
            ScriptableObject.DestroyImmediate(slowConfig);
        }

        [Test]
        public void test_CreatureInstance_Personality_Aggressive_AppliesModifiers()
        {
            var creature = CreatureInstance.Create(_testConfig, 25);
            int baseATK = creature.ComputedStats.ATK;
            int baseDEF = creature.ComputedStats.DEF;

            creature.SetPersonality(PersonalityTrait.Aggressive);

            Assert.AreEqual((int)(Mathf.Max(1, baseATK) * 1.1f), creature.ComputedStats.ATK);
            Assert.AreEqual((int)(Mathf.Max(1, baseDEF) * 0.95f), creature.ComputedStats.DEF);
        }

        [Test]
        public void test_CreatureInstance_Personality_Cautious_AppliesModifiers()
        {
            var creature = CreatureInstance.Create(_testConfig, 25);
            int baseDEF = creature.ComputedStats.DEF;
            int baseSPD = creature.ComputedStats.SPD;

            creature.SetPersonality(PersonalityTrait.Cautious);

            Assert.AreEqual((int)(Mathf.Max(1, baseDEF) * 1.1f), creature.ComputedStats.DEF);
            Assert.AreEqual((int)(Mathf.Max(1, baseSPD) * 0.95f), creature.ComputedStats.SPD);
        }

        [Test]
        public void test_CreatureInstance_AwardXP_TriggersLevelUp()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            int oldMaxHP = creature.MaxHP;

            creature.AwardXP(40);

            Assert.AreEqual(2, creature.Level);
            Assert.GreaterOrEqual(creature.MaxHP, oldMaxHP);
            Assert.AreEqual(creature.MaxHP, creature.CurrentHP);
        }

        [Test]
        public void test_CreatureInstance_AwardXP_MultiLevelUp()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);

            creature.AwardXP(500);

            Assert.Greater(creature.Level, 2);
        }

        [Test]
        public void test_CreatureInstance_AwardXP_LearnsNewMoveAtLevelThreshold()
        {
            var creature = CreatureInstance.Create(_testConfig, 4);
            Assert.IsFalse(Enumerable.Contains(creature.LearnedMoveIds,"move-c"));

            creature.AwardXP(250);

            Assert.GreaterOrEqual(creature.Level, 5);
            Assert.IsTrue(Enumerable.Contains(creature.LearnedMoveIds,"move-c"));
        }

        [Test]
        public void test_CreatureInstance_LearnMove_At4Moves_ReplacesLastSlot()
        {
            var creature = CreatureInstance.Create(_testConfig, 10);
            Assert.AreEqual(4, creature.LearnedMoveIds.Count);

            creature.LearnMove("move-e");

            Assert.AreEqual(4, creature.LearnedMoveIds.Count);
            Assert.IsTrue(Enumerable.Contains(creature.LearnedMoveIds,"move-e"));
        }

        [Test]
        public void test_CreatureInstance_DeductPP_ClampsToZero()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            int initialPP = creature.LearnedMovePP[0];

            for (int i = 0; i <= initialPP + 5; i++)
                creature.DeductPP(0);

            Assert.AreEqual(0, creature.LearnedMovePP[0]);
        }

        [Test]
        public void test_CreatureInstance_RestoreAllPP_ResetsToMax()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.DeductPP(0);
            creature.DeductPP(0);
            creature.DeductPP(1);

            creature.RestoreAllPP();

            Assert.AreEqual(10, creature.LearnedMovePP[0]);
            Assert.AreEqual(8, creature.LearnedMovePP[1]);
        }

        [Test]
        public void test_CreatureInstance_EquipPart_InvalidSlot_ReturnsFalse()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            bool result = creature.EquipPart(BodySlot.Wings, "claw-part");
            Assert.IsFalse(result);
        }

        [Test]
        public void test_CreatureInstance_EquipPart_ValidSlot_ReturnsTrue()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            bool result = creature.EquipPart(BodySlot.Head, "claw-part");

            Assert.IsTrue(result);
            Assert.AreEqual("claw-part", creature.EquippedPartIds[BodySlot.Head]);
        }

        [Test]
        public void test_CreatureInstance_UnequipPart_RemovesPart()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.EquipPart(BodySlot.Head, "claw-part");
            creature.UnequipPart(BodySlot.Head);
            Assert.IsFalse(creature.EquippedPartIds.ContainsKey(BodySlot.Head));
        }

        [Test]
        public void test_CreatureInstance_AvailableForms_ReflectsEquippedParts()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            Assert.AreEqual(0, creature.AvailableForms.Count);

            creature.EquipPart(BodySlot.Head, "claw-part");
            Assert.IsTrue(creature.AvailableForms.Contains(DamageForm.Physical));
        }

        [Test]
        public void test_CreatureInstance_ApplyDNAMod_IncreasesInstability()
        {
            var creature = CreatureInstance.Create(_testConfig, 10);
            int oldATK = creature.ComputedStats.ATK;

            creature.ApplyDNAMod("mod-strength", 10);

            Assert.AreEqual(10, creature.Instability);
            Assert.Greater(creature.ComputedStats.ATK, oldATK);
        }

        [Test]
        public void test_CreatureInstance_ApplyDNAMod_Duplicate_IsNoOp()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.ApplyDNAMod("mod-a", 10);
            creature.ApplyDNAMod("mod-a", 10);

            Assert.AreEqual(10, creature.Instability);
            Assert.AreEqual(1, creature.AppliedDNAMods.Count);
        }

        [Test]
        public void test_CreatureInstance_Instability80_SecondaryTypeBecomesBlight()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            Assert.AreEqual(CreatureType.None, creature.ActiveSecondaryType);

            for (int i = 0; i < 16; i++)
                creature.ApplyDNAMod($"mod-{i}", 5);

            Assert.GreaterOrEqual(creature.Instability, 80);
            Assert.AreEqual(CreatureType.Blight, creature.ActiveSecondaryType);
            Assert.IsTrue(creature.IsDualType);
        }

        [Test]
        public void test_CreatureInstance_InstabilityBelow80_ReturnsConfigSecondaryType()
        {
            var dualConfig = CreateCreatureConfig(
                "dual-fox", "DualFox",
                new BaseStats(50, 40, 30, 35, 100),
                GrowthCurve.Medium,
                new List<LevelMoveEntry> { new LevelMoveEntry(1, "move-a") },
                new List<BodySlot> { BodySlot.Head },
                CreatureType.Thermal, CreatureType.Aqua);

            var creature = CreatureInstance.Create(dualConfig, 1);

            Assert.AreEqual(CreatureType.Aqua, creature.ActiveSecondaryType);
            Assert.IsTrue(creature.IsDualType);

            ScriptableObject.DestroyImmediate(dualConfig);
        }

        [Test]
        public void test_CreatureInstance_Instability_ClampsTo100()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            for (int i = 0; i < 30; i++)
                creature.ApplyDNAMod($"mod-{i}", 10);
            Assert.AreEqual(100, creature.Instability);
        }

        [Test]
        public void test_CreatureInstance_RemoveDNAMod_ClampsToZero()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.ApplyDNAMod("mod-a", 5);
            creature.RemoveDNAMod("mod-a", 20);

            Assert.AreEqual(0, creature.Instability);
            Assert.AreEqual(0, creature.AppliedDNAMods.Count);
        }

        [Test]
        public void test_CreatureInstance_TakeDamage_ReducesHP()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            int maxHP = creature.MaxHP;
            creature.TakeDamage(10);

            Assert.AreEqual(maxHP - 10, creature.CurrentHP);
            Assert.IsFalse(creature.IsFainted);
        }

        [Test]
        public void test_CreatureInstance_TakeDamage_FatalAmount_Faints()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.TakeDamage(creature.MaxHP + 100);

            Assert.AreEqual(0, creature.CurrentHP);
            Assert.IsTrue(creature.IsFainted);
        }

        [Test]
        public void test_CreatureInstance_TakeDamage_Zero_NoOp()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            int hp = creature.CurrentHP;
            creature.TakeDamage(0);

            Assert.AreEqual(hp, creature.CurrentHP);
            Assert.IsFalse(creature.IsFainted);
        }

        [Test]
        public void test_CreatureInstance_Heal_ClampsToMaxHP()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.TakeDamage(10);
            creature.Heal(999);
            Assert.AreEqual(creature.MaxHP, creature.CurrentHP);
        }

        [Test]
        public void test_CreatureInstance_Heal_FaintedCreature_NoOp()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.TakeDamage(creature.MaxHP);
            creature.Heal(50);

            Assert.AreEqual(0, creature.CurrentHP);
            Assert.IsTrue(creature.IsFainted);
        }

        [Test]
        public void test_CreatureInstance_Revive_UnfaintsWithHP()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.TakeDamage(creature.MaxHP);
            creature.Revive(25);

            Assert.IsFalse(creature.IsFainted);
            Assert.AreEqual(25, creature.CurrentHP);
        }

        [Test]
        public void test_CreatureInstance_Revive_ClampsToMaxHP()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.TakeDamage(creature.MaxHP);
            creature.Revive(9999);
            Assert.AreEqual(creature.MaxHP, creature.CurrentHP);
        }

        [Test]
        public void test_CreatureInstance_StatusEffects_AddAndRemove()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.ApplyStatusEffect(StatusEffect.Burn);
            creature.ApplyStatusEffect(StatusEffect.Poison);

            Assert.AreEqual(2, creature.ActiveStatusEffects.Count);
            creature.RemoveStatusEffect(StatusEffect.Burn);

            Assert.AreEqual(1, creature.ActiveStatusEffects.Count);
            Assert.IsFalse(Enumerable.Contains(creature.ActiveStatusEffects,StatusEffect.Burn));
        }

        [Test]
        public void test_CreatureInstance_StatusEffect_DuplicateNotAdded()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.ApplyStatusEffect(StatusEffect.Burn);
            creature.ApplyStatusEffect(StatusEffect.Burn);
            Assert.AreEqual(1, creature.ActiveStatusEffects.Count);
        }

        [Test]
        public void test_CreatureInstance_GridPosition_AndFacing()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.SetGridPosition(new Vector2Int(3, 5));
            creature.SetFacing(Facing.NW);

            Assert.AreEqual(new Vector2Int(3, 5), creature.GridPosition);
            Assert.AreEqual(Facing.NW, creature.Facing);
        }

        [Test]
        public void test_CreatureInstance_LeveledUp_EventFires()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            int levelUpCount = 0;
            creature.LeveledUp += _ => levelUpCount++;
            creature.AwardXP(40);
            Assert.AreEqual(1, levelUpCount);
        }

        [Test]
        public void test_CreatureInstance_Fainted_EventFires()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            bool fainted = false;
            creature.Fainted += _ => fainted = true;
            creature.TakeDamage(creature.MaxHP);
            Assert.IsTrue(fainted);
        }

        [Test]
        public void test_CreatureInstance_StatsChanged_EventFires()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            int statsChangedCount = 0;
            creature.StatsChanged += _ => statsChangedCount++;
            creature.SetPersonality(PersonalityTrait.Aggressive);
            Assert.GreaterOrEqual(statsChangedCount, 1);
        }

        [Test]
        public void test_CreatureInstance_Nickname_FallbackToDisplayName()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            Assert.AreEqual("Emberfox", creature.Nickname);

            var named = CreatureInstance.Create(_testConfig, 1, "Sparky");
            Assert.AreEqual("Sparky", named.Nickname);
        }

        [Test]
        public void test_CreatureInstance_LevelCap_50()
        {
            var creature = CreatureInstance.Create(_testConfig, 49);
            creature.AwardXP(999999);
            Assert.AreEqual(50, creature.Level);
        }

        // ================================================================
        // Additional Tests
        // ================================================================

        [Test]
        public void test_CreatureInstance_ForgetMove_RemovesAtIndex()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.ForgetMove(0);

            Assert.AreEqual(1, creature.LearnedMoveIds.Count);
            Assert.AreEqual("move-b", creature.LearnedMoveIds[0]);
        }

        [Test]
        public void test_CreatureInstance_ForgetMove_InvalidIndex_NoOp()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            int count = creature.LearnedMoveIds.Count;
            creature.ForgetMove(-1);
            creature.ForgetMove(99);
            Assert.AreEqual(count, creature.LearnedMoveIds.Count);
        }

        [Test]
        public void test_CreatureInstance_DeductPP_InvalidSlot_NoOp()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.DeductPP(-1);
            creature.DeductPP(99);
        }

        [Test]
        public void test_CreatureInstance_Affinity_CappedAt10()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            for (int i = 0; i < 20; i++)
                creature.IncreaseAffinity("ally-001");
            Assert.AreEqual(10, creature.GetAffinity("ally-001"));
        }

        [Test]
        public void test_CreatureInstance_Affinity_Unknown_Returns0()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            Assert.AreEqual(0, creature.GetAffinity("unknown"));
        }

        [Test]
        public void test_CreatureInstance_Create_Level0_ClampsTo1()
        {
            var creature = CreatureInstance.Create(_testConfig, 0);
            Assert.AreEqual(1, creature.Level);
        }

        [Test]
        public void test_CreatureInstance_HasMoved_HasActed_Flags()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            Assert.IsFalse(creature.HasMoved);
            Assert.IsFalse(creature.HasActed);
            creature.SetMoved(true);
            creature.SetActed(true);
            Assert.IsTrue(creature.HasMoved);
            Assert.IsTrue(creature.HasActed);
        }

        [Test]
        public void test_CreatureInstance_HealFull_RestoresMaxHP()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.TakeDamage(20);
            creature.HealFull();
            Assert.AreEqual(creature.MaxHP, creature.CurrentHP);
        }

        [Test]
        public void test_CreatureInstance_HealFull_FaintedCreature_NoOp()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.TakeDamage(creature.MaxHP);
            creature.HealFull();
            Assert.AreEqual(0, creature.CurrentHP);
            Assert.IsTrue(creature.IsFainted);
        }

        [Test]
        public void test_CreatureInstance_RemoveDNAMod_DecreasesInstability()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.ApplyDNAMod("mod-a", 20);
            creature.ApplyDNAMod("mod-b", 20);
            creature.RemoveDNAMod("mod-a", 15);

            Assert.AreEqual(25, creature.Instability);
            Assert.AreEqual(1, creature.AppliedDNAMods.Count);
        }

        [Test]
        public void test_CreatureInstance_LearnMove_AlreadyKnown_NoOp()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            int count = creature.LearnedMoveIds.Count;
            creature.LearnMove("move-a");
            Assert.AreEqual(count, creature.LearnedMoveIds.Count);
        }

        [Test]
        public void test_CreatureInstance_AwardXP_FullHealOnLevelUp()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            creature.TakeDamage(creature.MaxHP - 1);
            creature.AwardXP(40);
            Assert.AreEqual(creature.MaxHP, creature.CurrentHP);
        }

        [Test]
        public void test_CreatureInstance_MultiLevelUp_EventFiresPerLevel()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            int count = 0;
            creature.LeveledUp += _ => count++;
            creature.AwardXP(999999);
            Assert.AreEqual(49, count);
        }

        [Test]
        public void test_CreatureInstance_XPThreshold_Level2_Is40()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            Assert.AreEqual(40, creature.XPNextLevel);
        }

        [Test]
        public void test_CreatureInstance_PersonalityChange_RecalculatesStats()
        {
            var creature = CreatureInstance.Create(_testConfig, 25);
            int noneATK = creature.ComputedStats.ATK;
            creature.SetPersonality(PersonalityTrait.Aggressive);
            int aggressiveATK = creature.ComputedStats.ATK;
            creature.SetPersonality(PersonalityTrait.None);

            Assert.Greater(aggressiveATK, noneATK);
            Assert.AreEqual(noneATK, creature.ComputedStats.ATK);
        }

        [Test]
        public void test_CreatureInstance_IsDualType_FalseWhenNoSecondaryAndLowInstability()
        {
            var creature = CreatureInstance.Create(_testConfig, 1);
            Assert.IsFalse(creature.IsDualType);
        }
    }
}
