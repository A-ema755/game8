using System.Linq;
using System.Reflection;
using NUnit.Framework;
using GeneForge.Combat;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.Tests
{
    /// <summary>
    /// Unit tests for <see cref="MoveEffectApplier"/>.
    /// Covers status application, self-targeting, chance gating, and no-op branches.
    /// Implements GDD turn-manager.md §3.8 step 9.
    /// </summary>
    [TestFixture]
    public class MoveEffectApplierTests
    {
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

        // ── Factory Helpers ──────────────────────────────────────────────

        private CreatureConfig CreateCreatureConfig(string id, BaseStats stats,
            CreatureType primary = CreatureType.Thermal)
        {
            var config = ScriptableObject.CreateInstance<CreatureConfig>();
            SetField(config, "id", id);
            SetField(config, "displayName", id);
            SetField(config, "primaryType", primary);
            SetField(config, "baseStats", stats);
            return config;
        }

        /// <summary>
        /// Build a MoveEffect via reflection — all fields are private [SerializeField].
        /// </summary>
        private MoveEffect CreateEffect(
            MoveEffectType effectType,
            float chance,
            StatusEffect statusToApply = StatusEffect.Burn,
            bool affectsSelf = false,
            int magnitude = 0,
            int statTarget = 0)
        {
            var effect = new MoveEffect();
            SetField(effect, "effectType", effectType);
            SetField(effect, "chance", chance);
            SetField(effect, "statusToApply", statusToApply);
            SetField(effect, "affectsSelf", affectsSelf);
            SetField(effect, "magnitude", magnitude);
            SetField(effect, "statTarget", statTarget);
            return effect;
        }

        // ── Shared State ─────────────────────────────────────────────────

        private CreatureConfig _actorConfig;
        private CreatureConfig _targetConfig;
        private GridSystem _grid;

        [SetUp]
        public void SetUp()
        {
            var stats = new BaseStats(50, 30, 20, 20, 30);
            _actorConfig  = CreateCreatureConfig("actor",  stats, CreatureType.Thermal);
            _targetConfig = CreateCreatureConfig("target", stats, CreatureType.Aqua);
            _grid = new GridSystem(8, 8);
        }

        // ══════════════════════════════════════════════════════════════════
        // ApplyStatus — guaranteed chance
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_MoveEffectApplier_applyStatus_chance1_applies_status_to_target()
        {
            // Arrange
            var applier = new MoveEffectApplier();
            var actor   = CreatureInstance.Create(_actorConfig,  5);
            var target  = CreatureInstance.Create(_targetConfig, 5);
            var effect  = CreateEffect(MoveEffectType.ApplyStatus, chance: 1.0f,
                                       statusToApply: StatusEffect.Burn, affectsSelf: false);

            // Act
            applier.Apply(effect, actor, target, _grid);

            // Assert
            Assert.IsTrue(target.ActiveStatusEffects.Any(s => s == StatusEffect.Burn),
                "Burn should be in target's active status effects when chance = 1.0");
            Assert.IsFalse(actor.ActiveStatusEffects.Any(s => s == StatusEffect.Burn),
                "Actor should not receive the status when AffectsSelf is false");
        }

        // ══════════════════════════════════════════════════════════════════
        // ApplyStatus — self-targeting
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_MoveEffectApplier_applyStatus_affectsSelf_applies_status_to_actor()
        {
            // Arrange
            var applier = new MoveEffectApplier();
            var actor   = CreatureInstance.Create(_actorConfig,  5);
            var target  = CreatureInstance.Create(_targetConfig, 5);
            var effect  = CreateEffect(MoveEffectType.ApplyStatus, chance: 1.0f,
                                       statusToApply: StatusEffect.Paralysis, affectsSelf: true);

            // Act
            applier.Apply(effect, actor, target, _grid);

            // Assert
            Assert.IsTrue(actor.ActiveStatusEffects.Any(s => s == StatusEffect.Paralysis),
                "Actor should receive the status when AffectsSelf is true");
            Assert.IsFalse(target.ActiveStatusEffects.Any(s => s == StatusEffect.Paralysis),
                "Target should not receive the status when AffectsSelf is true");
        }

        // ══════════════════════════════════════════════════════════════════
        // Chance gating — roll fails (no effect applied)
        // Random(0).NextDouble() ≈ 0.7262 which is >= 0.5, so the roll fails.
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_MoveEffectApplier_chance_rollFails_does_not_apply_status()
        {
            // Arrange — seed 0 produces NextDouble() ≈ 0.726, which is >= 0.5 → roll fails
            var rng     = new System.Random(0);
            var applier = new MoveEffectApplier(rng);
            var actor   = CreatureInstance.Create(_actorConfig,  5);
            var target  = CreatureInstance.Create(_targetConfig, 5);
            var effect  = CreateEffect(MoveEffectType.ApplyStatus, chance: 0.5f,
                                       statusToApply: StatusEffect.Poison, affectsSelf: false);

            // Act
            applier.Apply(effect, actor, target, _grid);

            // Assert — roll failed, no status should be applied
            Assert.IsFalse(target.ActiveStatusEffects.Any(s => s == StatusEffect.Poison),
                "Status should not be applied when the chance roll fails");
        }

        // ══════════════════════════════════════════════════════════════════
        // Chance gating — roll passes (effect applied)
        // Random(1).NextDouble() ≈ 0.2488 which is < 0.5, so the roll passes.
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_MoveEffectApplier_chance_rollPasses_applies_status()
        {
            // Arrange — seed 1 produces NextDouble() ≈ 0.249, which is < 0.5 → roll passes
            var rng     = new System.Random(1);
            var applier = new MoveEffectApplier(rng);
            var actor   = CreatureInstance.Create(_actorConfig,  5);
            var target  = CreatureInstance.Create(_targetConfig, 5);
            var effect  = CreateEffect(MoveEffectType.ApplyStatus, chance: 0.5f,
                                       statusToApply: StatusEffect.Poison, affectsSelf: false);

            // Act
            applier.Apply(effect, actor, target, _grid);

            // Assert — roll passed, status should be applied
            Assert.IsTrue(target.ActiveStatusEffects.Any(s => s == StatusEffect.Poison),
                "Status should be applied when the chance roll passes");
        }

        // ══════════════════════════════════════════════════════════════════
        // Null target with AffectsSelf=false — must not throw
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_MoveEffectApplier_nullTarget_affectsSelfFalse_does_not_throw()
        {
            // Arrange
            var applier = new MoveEffectApplier();
            var actor   = CreatureInstance.Create(_actorConfig, 5);
            var effect  = CreateEffect(MoveEffectType.ApplyStatus, chance: 1.0f,
                                       statusToApply: StatusEffect.Burn, affectsSelf: false);

            // Act + Assert — null target must be handled gracefully, no exception
            Assert.DoesNotThrow(() => applier.Apply(effect, actor, null, _grid),
                "Apply should not throw when target is null and AffectsSelf is false");
        }

        // ══════════════════════════════════════════════════════════════════
        // StatStage — MVP no-op, must not crash
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_MoveEffectApplier_statStage_isMvpNoop_does_not_throw()
        {
            // Arrange
            var applier = new MoveEffectApplier();
            var actor   = CreatureInstance.Create(_actorConfig,  5);
            var target  = CreatureInstance.Create(_targetConfig, 5);
            var effect  = CreateEffect(MoveEffectType.StatStage, chance: 1.0f,
                                       magnitude: 1, statTarget: 0);

            // Act + Assert — StatStage is MVP no-op, must not throw or alter status lists
            Assert.DoesNotThrow(() => applier.Apply(effect, actor, target, _grid),
                "StatStage effect should be a silent no-op in MVP without throwing");
            Assert.AreEqual(0, target.ActiveStatusEffects.Count,
                "StatStage no-op should not add any status effects to the target");
        }
    }
}
