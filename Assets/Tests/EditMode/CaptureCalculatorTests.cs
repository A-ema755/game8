using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using GeneForge.Combat;
using GeneForge.Core;
using GeneForge.Creatures;
using UnityEngine;

namespace GeneForge.Tests
{
    [TestFixture]
    public class CaptureCalculatorTests
    {
        // ── Test Configs ─────────────────────────────────────────────────
        private CreatureConfig _commonConfig;   // catchRate = 180
        private CreatureConfig _rareConfig;     // catchRate = 45
        private CreatureConfig _uncatchable;    // catchRate = 0
        private CreatureConfig _guaranteed;     // catchRate = 255

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

        private CreatureConfig CreateConfig(string id, int catchRate, CreatureType primary = CreatureType.Thermal)
        {
            var config = ScriptableObject.CreateInstance<CreatureConfig>();
            SetField(config, "id", id);
            SetField(config, "displayName", id);
            SetField(config, "catchRate", catchRate);
            SetField(config, "primaryType", primary);
            SetField(config, "secondaryType", CreatureType.None);
            SetField(config, "baseStats", new BaseStats(50, 40, 30, 35, 100));
            SetField(config, "growthCurve", GrowthCurve.Medium);
            SetField(config, "movePool", new List<LevelMoveEntry>());
            SetField(config, "availableSlots", new List<BodySlot>());
            SetField(config, "defaultPartIds", new List<string>());
            SetField(config, "habitatZoneIds", new List<string>());
            return config;
        }

        // ── Setup / Teardown ─────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _commonConfig = CreateConfig("common-creature", 180);
            _rareConfig = CreateConfig("rare-creature", 45);
            _uncatchable = CreateConfig("uncatchable", 0);
            _guaranteed = CreateConfig("guaranteed", 255);
        }

        [TearDown]
        public void TearDown()
        {
            ScriptableObject.DestroyImmediate(_commonConfig);
            ScriptableObject.DestroyImmediate(_rareConfig);
            ScriptableObject.DestroyImmediate(_uncatchable);
            ScriptableObject.DestroyImmediate(_guaranteed);
        }

        // ================================================================
        // CalculateCatchRate Tests
        // ================================================================

        [Test]
        public void test_CaptureCalculator_GddExample_ReturnsApprox33Percent()
        {
            // GDD §4 example: baseCatchRate=45, 25% HP (25/100), Paralysis (1.5), Enhanced Trap (1.5)
            // hpFactor = (300 - 50) / 300 = 0.833
            // rawRate = (45 * 1.5 * 0.833 * 1.5) / 255 = 84.3 / 255 ≈ 0.331
            float rate = CaptureCalculator.CalculateCatchRate(_rareConfig, 25, 100, 1.5f, 1.5f);

            Assert.AreEqual(0.331f, rate, 0.01f);
        }

        [Test]
        public void test_CaptureCalculator_FullHp_HpFactorApprox033()
        {
            // hpFactor at full HP = (3*100 - 2*100) / (3*100) = 100/300 = 0.333
            // rawRate = (255 * 1.0 * 0.333 * 1.0) / 255 = 0.333
            float rate = CaptureCalculator.CalculateCatchRate(_guaranteed, 100, 100, 1.0f, 1.0f);

            Assert.AreEqual(0.333f, rate, 0.01f);
        }

        [Test]
        public void test_CaptureCalculator_1Hp_HpFactorApprox1()
        {
            // hpFactor at 1 HP = (3*100 - 2*1) / (3*100) = 298/300 = 0.993
            // rawRate = (255 * 1.0 * 0.993 * 1.0) / 255 ≈ 0.993
            float rate = CaptureCalculator.CalculateCatchRate(_guaranteed, 1, 100, 1.0f, 1.0f);

            Assert.AreEqual(1.0f, rate, 0.01f);
        }

        [Test]
        public void test_CaptureCalculator_CatchRate0_AlwaysReturns0()
        {
            float rate = CaptureCalculator.CalculateCatchRate(_uncatchable, 1, 100, 2.0f, 2.5f);

            Assert.AreEqual(0f, rate);
        }

        [Test]
        public void test_CaptureCalculator_CatchRate255_WithBonuses_ClampsTo1()
        {
            // rawRate = (255 * 2.0 * ~1.0 * 2.5) / 255 = 5.0 → clamped to 1.0
            float rate = CaptureCalculator.CalculateCatchRate(_guaranteed, 1, 100, 2.0f, 2.5f);

            Assert.AreEqual(1.0f, rate);
        }

        [Test]
        public void test_CaptureCalculator_StandardTrap_NoStatus_CommonCreature()
        {
            // hpFactor at 50% = (300 - 100) / 300 = 0.667
            // rawRate = (180 * 1.0 * 0.667 * 1.0) / 255 ≈ 0.471
            float rate = CaptureCalculator.CalculateCatchRate(_commonConfig, 50, 100, 1.0f, 1.0f);

            Assert.AreEqual(0.471f, rate, 0.01f);
        }

        // ================================================================
        // GetStatusBonus Tests
        // ================================================================

        [Test]
        public void test_CaptureCalculator_StatusBonus_None_Returns1()
        {
            var effects = new List<StatusEffect>();

            float bonus = CaptureCalculator.GetStatusBonus(effects);

            Assert.AreEqual(1.0f, bonus);
        }

        [Test]
        public void test_CaptureCalculator_StatusBonus_Poison_Returns12()
        {
            var effects = new List<StatusEffect> { StatusEffect.Poison };

            Assert.AreEqual(1.2f, CaptureCalculator.GetStatusBonus(effects));
        }

        [Test]
        public void test_CaptureCalculator_StatusBonus_Burn_Returns12()
        {
            var effects = new List<StatusEffect> { StatusEffect.Burn };

            Assert.AreEqual(1.2f, CaptureCalculator.GetStatusBonus(effects));
        }

        [Test]
        public void test_CaptureCalculator_StatusBonus_Paralysis_Returns15()
        {
            var effects = new List<StatusEffect> { StatusEffect.Paralysis };

            Assert.AreEqual(1.5f, CaptureCalculator.GetStatusBonus(effects));
        }

        [Test]
        public void test_CaptureCalculator_StatusBonus_Freeze_Returns15()
        {
            var effects = new List<StatusEffect> { StatusEffect.Freeze };

            Assert.AreEqual(1.5f, CaptureCalculator.GetStatusBonus(effects));
        }

        [Test]
        public void test_CaptureCalculator_StatusBonus_Sleep_Returns25()
        {
            var effects = new List<StatusEffect> { StatusEffect.Sleep };

            Assert.AreEqual(2.5f, CaptureCalculator.GetStatusBonus(effects));
        }

        [Test]
        public void test_CaptureCalculator_StatusBonus_MultipleStatuses_HighestWins()
        {
            var effects = new List<StatusEffect>
            {
                StatusEffect.Poison,     // 1.2
                StatusEffect.Paralysis,  // 1.5
                StatusEffect.Sleep       // 2.5
            };

            Assert.AreEqual(2.5f, CaptureCalculator.GetStatusBonus(effects));
        }

        // ================================================================
        // GetSpecialistModifier Tests
        // ================================================================

        [Test]
        public void test_CaptureCalculator_SpecialistTrap_TypeMatch_Returns2x()
        {
            float mod = CaptureCalculator.GetSpecialistModifier(CreatureType.Thermal, CreatureType.Thermal);

            Assert.AreEqual(2.0f, mod);
        }

        [Test]
        public void test_CaptureCalculator_SpecialistTrap_TypeMismatch_Returns1x()
        {
            float mod = CaptureCalculator.GetSpecialistModifier(CreatureType.Aqua, CreatureType.Thermal);

            Assert.AreEqual(1.0f, mod);
        }

        [Test]
        public void test_CaptureCalculator_SpecialistTrap_IntegratedInFormula()
        {
            // Specialist match (2.0x) vs mismatch (1.0x) — rate should double
            float rateMatch = CaptureCalculator.CalculateCatchRate(_rareConfig, 50, 100, 2.0f, 1.0f);
            float rateMiss = CaptureCalculator.CalculateCatchRate(_rareConfig, 50, 100, 1.0f, 1.0f);

            Assert.AreEqual(rateMiss * 2f, rateMatch, 0.001f);
        }

        // ================================================================
        // AttemptCapture Tests
        // ================================================================

        [Test]
        public void test_CaptureCalculator_AttemptCapture_SeededRng_Deterministic()
        {
            var rng1 = new System.Random(42);
            var rng2 = new System.Random(42);

            bool result1 = CaptureCalculator.AttemptCapture(0.5f, rng1);
            bool result2 = CaptureCalculator.AttemptCapture(0.5f, rng2);

            Assert.AreEqual(result1, result2);
        }

        [Test]
        public void test_CaptureCalculator_AttemptCapture_0Percent_AlwaysFails()
        {
            var rng = new System.Random(12345);

            for (int i = 0; i < 100; i++)
                Assert.IsFalse(CaptureCalculator.AttemptCapture(0f, rng));
        }

        [Test]
        public void test_CaptureCalculator_AttemptCapture_100Percent_AlwaysSucceeds()
        {
            var rng = new System.Random(12345);

            for (int i = 0; i < 100; i++)
                Assert.IsTrue(CaptureCalculator.AttemptCapture(1.0f, rng));
        }
    }
}
