using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using GeneForge.Combat;
using GeneForge.Core;
using GeneForge.Creatures;
using UnityEngine;
using Random = System.Random;

namespace GeneForge.Tests
{
    /// <summary>
    /// EditMode unit tests for <see cref="CaptureSystem"/> and <see cref="CaptureCalculator"/>.
    /// Implements GDD capture-system.md §3–5 acceptance criteria.
    /// </summary>
    [TestFixture]
    public class CaptureSystemTests
    {
        // ── Reflection Helper ────────────────────────────────────────────

        private static void SetField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {obj.GetType().Name}");
            field.SetValue(obj, value);
        }

        // ── Factory Helper ───────────────────────────────────────────────

        private CreatureConfig CreateConfig(
            string id,
            int catchRate,
            CreatureType primary = CreatureType.Thermal)
        {
            var config = ScriptableObject.CreateInstance<CreatureConfig>();
            SetField(config, "id", id);
            SetField(config, "displayName", id);
            SetField(config, "primaryType", primary);
            SetField(config, "baseStats", new BaseStats(50, 30, 20, 20, 100));
            SetField(config, "catchRate", catchRate);
            SetField(config, "growthCurve", GrowthCurve.Medium);
            SetField(config, "movePool", new List<LevelMoveEntry>());
            SetField(config, "availableSlots", new List<BodySlot>());
            SetField(config, "defaultPartIds", new List<string>());
            SetField(config, "habitatZoneIds", new List<string>());
            return config;
        }

        // ── Shared State ─────────────────────────────────────────────────

        private CreatureConfig _actorConfig;
        private CreatureInstance _actor;

        [SetUp]
        public void SetUp()
        {
            _actorConfig = CreateConfig("actor", catchRate: 128);
            _actor = CreatureInstance.Create(_actorConfig, 10);
        }

        // ══════════════════════════════════════════════════════════════════
        // Determinism
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_CaptureSystem_seeded_rng_produces_deterministic_result()
        {
            // Arrange
            var config = CreateConfig("target-det", catchRate: 128);
            var target1 = CreatureInstance.Create(config, 10);
            var target2 = CreatureInstance.Create(config, 10);
            target1.TakeDamage(target1.MaxHP / 2);
            target2.TakeDamage(target2.MaxHP / 2);

            var system1 = new CaptureSystem(new Random(42));
            var system2 = new CaptureSystem(new Random(42));

            // Act
            bool result1 = system1.Attempt(target1, _actor);
            bool result2 = system2.Attempt(target2, _actor);

            // Assert
            Assert.AreEqual(result1, result2,
                "Identical seeded RNG must produce identical capture outcomes");
        }

        // ══════════════════════════════════════════════════════════════════
        // HP Factor — full HP vs low HP
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_CaptureSystem_lowHp_target_captured_more_often_than_fullHp_target()
        {
            // Arrange
            var config = CreateConfig("target-hp", catchRate: 128);
            const int Trials = 1000;
            int fullHpCaptures = 0;
            int lowHpCaptures = 0;

            for (int i = 0; i < Trials; i++)
            {
                var rngFull = new Random(i);
                var rngLow = new Random(i);

                var fullHpTarget = CreatureInstance.Create(config, 10);

                var lowHpTarget = CreatureInstance.Create(config, 10);
                lowHpTarget.TakeDamage((int)(lowHpTarget.MaxHP * 0.9f));

                if (new CaptureSystem(rngFull).Attempt(fullHpTarget, _actor))
                    fullHpCaptures++;

                if (new CaptureSystem(rngLow).Attempt(lowHpTarget, _actor))
                    lowHpCaptures++;
            }

            // Assert
            Assert.Greater(lowHpCaptures, fullHpCaptures,
                $"Low-HP captures ({lowHpCaptures}) must exceed full-HP captures ({fullHpCaptures}) over {Trials} trials");
        }

        // ══════════════════════════════════════════════════════════════════
        // Status Bonus — Sleep
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_CaptureSystem_sleep_status_increases_capture_rate_vs_no_status()
        {
            // Arrange
            var config = CreateConfig("target-status", catchRate: 60);
            const int Trials = 1000;
            int noStatusCaptures = 0;
            int sleepCaptures = 0;

            for (int i = 0; i < Trials; i++)
            {
                var rngNone = new Random(i);
                var rngSleep = new Random(i);

                var noStatusTarget = CreatureInstance.Create(config, 10);
                noStatusTarget.TakeDamage(noStatusTarget.MaxHP / 2);

                var sleepTarget = CreatureInstance.Create(config, 10);
                sleepTarget.TakeDamage(sleepTarget.MaxHP / 2);
                sleepTarget.ApplyStatusEffect(StatusEffect.Sleep);

                if (new CaptureSystem(rngNone).Attempt(noStatusTarget, _actor))
                    noStatusCaptures++;

                if (new CaptureSystem(rngSleep).Attempt(sleepTarget, _actor))
                    sleepCaptures++;
            }

            // Assert
            Assert.Greater(sleepCaptures, noStatusCaptures,
                $"Sleep captures ({sleepCaptures}) must exceed no-status captures ({noStatusCaptures}) over {Trials} trials");
        }

        // ══════════════════════════════════════════════════════════════════
        // CatchRate boundary — 0 (uncatchable)
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_CaptureSystem_catchRateZero_always_returns_false()
        {
            // Arrange
            var config = CreateConfig("uncatchable", catchRate: 0);

            for (int seed = 0; seed < 50; seed++)
            {
                var target = CreatureInstance.Create(config, 10);
                target.TakeDamage(1);
                var system = new CaptureSystem(new Random(seed));

                // Act
                bool result = system.Attempt(target, _actor);

                // Assert
                Assert.IsFalse(result,
                    $"CatchRate=0 must always return false (seed={seed})");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // CatchRate boundary — 255 (guaranteed with Sleep status)
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_CaptureSystem_catchRate255_with_sleep_always_returns_true()
        {
            // Arrange — max catch rate + Sleep status clamps rate to 1.0
            // Formula: (255 * 1.0 * hpFactor * 2.5) / 255 = 2.5 * hpFactor >= 1.0 at any HP
            var config = CreateConfig("guaranteed", catchRate: 255);

            for (int seed = 0; seed < 50; seed++)
            {
                var target = CreatureInstance.Create(config, 10);
                target.ApplyStatusEffect(StatusEffect.Sleep);
                var system = new CaptureSystem(new Random(seed));

                // Act
                bool result = system.Attempt(target, _actor);

                // Assert
                Assert.IsTrue(result,
                    $"CatchRate=255 with Sleep must always return true (seed={seed})");
            }
        }
    }
}
