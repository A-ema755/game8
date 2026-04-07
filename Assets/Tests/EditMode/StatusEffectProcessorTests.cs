using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using GeneForge.Combat;
using GeneForge.Core;
using GeneForge.Creatures;
using UnityEngine;

namespace GeneForge.Tests
{
    /// <summary>
    /// Unit tests for <see cref="StatusEffectProcessor"/>.
    /// Validates GDD Turn Manager §3.7 — status effect tick rules:
    /// Burn/Poison DoT, Paralysis/Sleep/Freeze suppression, duration management.
    /// </summary>
    [TestFixture]
    public class StatusEffectProcessorTests
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

        // ── Factory Helpers ──────────────────────────────────────────────

        private CreatureConfig CreateCreatureConfig(string id, BaseStats stats,
            CreatureType primary = CreatureType.Thermal)
        {
            var config = ScriptableObject.CreateInstance<CreatureConfig>();
            SetField(config, "id", id);
            SetField(config, "displayName", id);
            SetField(config, "primaryType", primary);
            SetField(config, "secondaryType", CreatureType.None);
            SetField(config, "baseStats", stats);
            SetField(config, "growthCurve", GrowthCurve.Medium);
            SetField(config, "movePool", new List<LevelMoveEntry>());
            SetField(config, "availableSlots", new List<BodySlot>());
            SetField(config, "defaultPartIds", new List<string>());
            SetField(config, "habitatZoneIds", new List<string>());
            SetField(config, "terrainSynergyType", CreatureType.None);
            return config;
        }

        // ── Shared State ─────────────────────────────────────────────────

        private StatusEffectProcessor _processor;
        private CreatureConfig _config;

        [SetUp]
        public void SetUp()
        {
            _processor = new StatusEffectProcessor();
            // hp=160 chosen so maxHP/16=10 (Burn) and maxHP/8=20 (Poison) are clean integers.
            var stats = new BaseStats(160, 40, 30, 30, 100);
            _config = CreateCreatureConfig("test-creature", stats);
        }

        // ══════════════════════════════════════════════════════════════════
        // Burn
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_StatusEffectProcessor_burn_deals_max1_maxHP_over_16_damage()
        {
            // Arrange
            var creature = CreatureInstance.Create(_config, 10);
            int expectedDamage = Math.Max(1, creature.MaxHP / 16);
            int hpBefore = creature.CurrentHP;
            var entries = new List<StatusEffectEntry>
            {
                new StatusEffectEntry(StatusEffect.Burn, -1)
            };

            // Act
            _processor.ApplyStartOfRound(creature, entries, 0.5);

            // Assert
            Assert.AreEqual(hpBefore - expectedDamage, creature.CurrentHP,
                "Burn should deal max(1, maxHP/16) damage per round");
        }

        // ══════════════════════════════════════════════════════════════════
        // Poison
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_StatusEffectProcessor_poison_deals_max1_maxHP_over_8_damage()
        {
            // Arrange
            var creature = CreatureInstance.Create(_config, 10);
            int expectedDamage = Math.Max(1, creature.MaxHP / 8);
            int hpBefore = creature.CurrentHP;
            var entries = new List<StatusEffectEntry>
            {
                new StatusEffectEntry(StatusEffect.Poison, -1)
            };

            // Act
            _processor.ApplyStartOfRound(creature, entries, 0.5);

            // Assert
            Assert.AreEqual(hpBefore - expectedDamage, creature.CurrentHP,
                "Poison should deal max(1, maxHP/8) damage per round");
        }

        // ══════════════════════════════════════════════════════════════════
        // Paralysis
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_StatusEffectProcessor_paralysis_suppresses_when_rngRoll_below_0point25()
        {
            // Arrange
            var creature = CreatureInstance.Create(_config, 10);
            var entries = new List<StatusEffectEntry>
            {
                new StatusEffectEntry(StatusEffect.Paralysis, -1)
            };

            // Act
            bool suppressed = _processor.ApplyStartOfRound(creature, entries, 0.24);

            // Assert
            Assert.IsTrue(suppressed,
                "Paralysis should suppress creature when rngRoll < 0.25");
        }

        [Test]
        public void test_StatusEffectProcessor_paralysis_does_not_suppress_when_rngRoll_at_or_above_0point25()
        {
            // Arrange
            var creature = CreatureInstance.Create(_config, 10);
            var entries = new List<StatusEffectEntry>
            {
                new StatusEffectEntry(StatusEffect.Paralysis, -1)
            };

            // Act
            bool suppressed = _processor.ApplyStartOfRound(creature, entries, 0.25);

            // Assert
            Assert.IsFalse(suppressed,
                "Paralysis should NOT suppress creature when rngRoll >= 0.25");
        }

        // ══════════════════════════════════════════════════════════════════
        // Sleep
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_StatusEffectProcessor_sleep_always_suppresses_and_decrements_duration()
        {
            // Arrange
            var creature = CreatureInstance.Create(_config, 10);
            creature.ApplyStatusEffect(StatusEffect.Sleep);
            var entries = new List<StatusEffectEntry>
            {
                new StatusEffectEntry(StatusEffect.Sleep, 3)
            };

            // Act
            bool suppressed = _processor.ApplyStartOfRound(creature, entries, 0.99);

            // Assert
            Assert.IsTrue(suppressed, "Sleep should always suppress");
            Assert.AreEqual(2, entries[0].RemainingRounds,
                "Sleep duration should decrement from 3 to 2");
        }

        [Test]
        public void test_StatusEffectProcessor_sleep_removed_when_duration_reaches_zero()
        {
            // Arrange
            var creature = CreatureInstance.Create(_config, 10);
            creature.ApplyStatusEffect(StatusEffect.Sleep);
            var entries = new List<StatusEffectEntry>
            {
                new StatusEffectEntry(StatusEffect.Sleep, 1)
            };

            // Act
            _processor.ApplyStartOfRound(creature, entries, 0.5);

            // Assert
            Assert.AreEqual(0, entries.Count,
                "Sleep entry should be removed when duration reaches 0");
            Assert.IsFalse(creature.ActiveStatusEffects.Any(s => s == StatusEffect.Sleep),
                "Sleep should be removed from creature's active effects at expiry");
        }

        // ══════════════════════════════════════════════════════════════════
        // Freeze
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_StatusEffectProcessor_freeze_always_suppresses_and_decrements_duration()
        {
            // Arrange
            var creature = CreatureInstance.Create(_config, 10);
            creature.ApplyStatusEffect(StatusEffect.Freeze);
            var entries = new List<StatusEffectEntry>
            {
                new StatusEffectEntry(StatusEffect.Freeze, 2)
            };

            // Act
            bool suppressed = _processor.ApplyStartOfRound(creature, entries, 0.99);

            // Assert
            Assert.IsTrue(suppressed, "Freeze should always suppress");
            Assert.AreEqual(1, entries[0].RemainingRounds,
                "Freeze duration should decrement from 2 to 1");
        }

        [Test]
        public void test_StatusEffectProcessor_freeze_removed_when_duration_reaches_zero()
        {
            // Arrange
            var creature = CreatureInstance.Create(_config, 10);
            creature.ApplyStatusEffect(StatusEffect.Freeze);
            var entries = new List<StatusEffectEntry>
            {
                new StatusEffectEntry(StatusEffect.Freeze, 1)
            };

            // Act
            _processor.ApplyStartOfRound(creature, entries, 0.5);

            // Assert
            Assert.AreEqual(0, entries.Count,
                "Freeze entry should be removed when duration reaches 0");
            Assert.IsFalse(creature.ActiveStatusEffects.Any(s => s == StatusEffect.Freeze),
                "Freeze should be removed from creature's active effects at expiry");
        }

        // ══════════════════════════════════════════════════════════════════
        // DecrementDurations
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_StatusEffectProcessor_decrementDurations_skips_indefinite_entries()
        {
            // Arrange
            var creature = CreatureInstance.Create(_config, 10);
            var entries = new List<StatusEffectEntry>
            {
                new StatusEffectEntry(StatusEffect.Burn, -1),
                new StatusEffectEntry(StatusEffect.Poison, -1)
            };

            // Act
            _processor.DecrementDurations(creature, entries);

            // Assert
            Assert.AreEqual(2, entries.Count,
                "Indefinite entries should not be removed by DecrementDurations");
            Assert.AreEqual(-1, entries[0].RemainingRounds,
                "Burn indefinite duration should remain -1");
            Assert.AreEqual(-1, entries[1].RemainingRounds,
                "Poison indefinite duration should remain -1");
        }

        [Test]
        public void test_StatusEffectProcessor_decrementDurations_removes_entry_when_duration_reaches_zero()
        {
            // Arrange
            var creature = CreatureInstance.Create(_config, 10);
            creature.ApplyStatusEffect(StatusEffect.Confusion);
            var entries = new List<StatusEffectEntry>
            {
                new StatusEffectEntry(StatusEffect.Confusion, 1)
            };

            // Act
            _processor.DecrementDurations(creature, entries);

            // Assert
            Assert.AreEqual(0, entries.Count,
                "Entry at 1 round should be removed after DecrementDurations decrements to 0");
            Assert.IsFalse(creature.ActiveStatusEffects.Any(s => s == StatusEffect.Confusion),
                "Confusion should be removed from creature's active effects");
        }

        // ══════════════════════════════════════════════════════════════════
        // DoT Faint
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_StatusEffectProcessor_burn_dot_can_cause_faint_at_low_hp()
        {
            // Arrange
            var creature = CreatureInstance.Create(_config, 10);
            creature.TakeDamage(creature.CurrentHP - 1);
            var entries = new List<StatusEffectEntry>
            {
                new StatusEffectEntry(StatusEffect.Burn, -1)
            };

            // Act
            _processor.ApplyStartOfRound(creature, entries, 0.5);

            // Assert
            Assert.IsTrue(creature.IsFainted,
                "Burn DoT should faint creature when HP drops to 0");
            Assert.AreEqual(0, creature.CurrentHP,
                "CurrentHP should be 0 after lethal DoT tick");
        }
    }
}
