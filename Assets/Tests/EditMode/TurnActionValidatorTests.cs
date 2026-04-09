using System.Collections.Generic;
using GeneForge.Combat;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;
using NUnit.Framework;
using UnityEngine;

namespace GeneForge.Tests
{
    /// <summary>
    /// EditMode tests for TurnActionValidator (pure C# static class).
    /// Verifies TurnAction invariants from GDD Turn Manager §3.3.
    ///
    /// Test naming: test_[system]_[scenario]_[expected_result]
    /// </summary>
    [TestFixture]
    public class TurnActionValidatorTests
    {
        private GridSystem _grid;
        private readonly List<Object> _createdAssets = new();

        [SetUp]
        public void SetUp()
        {
            _grid = CreateFlatGrid(8, 8);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _createdAssets)
            {
                if (asset != null) Object.DestroyImmediate(asset);
            }
            _createdAssets.Clear();
        }

        // ── UseMove Validation ────────────────────────────────────────────

        [Test]
        public void test_TurnActionValidator_ValidUseMove_Passes()
        {
            // Arrange
            var move = CreateMoveConfig(DamageForm.Physical, TargetType.Single, range: 2, pp: 10);
            var actor = CreateCreatureWithMove(new Vector2Int(3, 3), move, ppRemaining: 10);
            var target = CreateCreature(new Vector2Int(4, 3));
            var action = new TurnAction(ActionType.UseMove, move: move, target: target, movePPSlot: 0);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsTrue(result.IsValid, $"Valid UseMove should pass: {result.Reason}");
        }

        [Test]
        public void test_TurnActionValidator_UseMoveNullMove_Fails()
        {
            // Arrange
            var actor = CreateCreature(new Vector2Int(3, 3));
            var action = new TurnAction(ActionType.UseMove, movePPSlot: 0);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Reason.Contains("Move is null"));
        }

        [Test]
        public void test_TurnActionValidator_UseMoveBadSlot_Fails()
        {
            // Arrange
            var move = CreateMoveConfig(DamageForm.Physical, TargetType.Single, range: 1, pp: 10);
            var actor = CreateCreatureWithMove(new Vector2Int(3, 3), move, ppRemaining: 10);
            var target = CreateCreature(new Vector2Int(4, 3));
            var action = new TurnAction(ActionType.UseMove, move: move, target: target, movePPSlot: 5);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Reason.Contains("out of range 0-3"));
        }

        [Test]
        public void test_TurnActionValidator_UseMoveZeroPP_Fails()
        {
            // Arrange
            var move = CreateMoveConfig(DamageForm.Physical, TargetType.Single, range: 1, pp: 10);
            var actor = CreateCreatureWithMove(new Vector2Int(3, 3), move, ppRemaining: 0);
            var target = CreateCreature(new Vector2Int(4, 3));
            var action = new TurnAction(ActionType.UseMove, move: move, target: target, movePPSlot: 0);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Reason.Contains("No PP remaining"));
        }

        [Test]
        public void test_TurnActionValidator_UseMoveSingleTargetNull_Fails()
        {
            // Arrange
            var move = CreateMoveConfig(DamageForm.Physical, TargetType.Single, range: 1, pp: 10);
            var actor = CreateCreatureWithMove(new Vector2Int(3, 3), move, ppRemaining: 10);
            var action = new TurnAction(ActionType.UseMove, move: move, movePPSlot: 0);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Reason.Contains("Target creature is null"));
        }

        [Test]
        public void test_TurnActionValidator_UseMoveTargetOutOfRange_Fails()
        {
            // Arrange — target at distance 5, move range 1
            var move = CreateMoveConfig(DamageForm.Physical, TargetType.Single, range: 1, pp: 10);
            var actor = CreateCreatureWithMove(new Vector2Int(0, 0), move, ppRemaining: 10);
            var target = CreateCreature(new Vector2Int(5, 5));
            var action = new TurnAction(ActionType.UseMove, move: move, target: target, movePPSlot: 0);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Reason.Contains("exceeds move range"));
        }

        [Test]
        public void test_TurnActionValidator_UseMoveAoENullTile_Fails()
        {
            // Arrange
            var move = CreateMoveConfig(DamageForm.Physical, TargetType.AoE, range: 3, pp: 10);
            var actor = CreateCreatureWithMove(new Vector2Int(3, 3), move, ppRemaining: 10);
            var action = new TurnAction(ActionType.UseMove, move: move, movePPSlot: 0);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Reason.Contains("TargetTile is null"));
        }

        [Test]
        public void test_TurnActionValidator_UseMoveSelfTarget_Passes()
        {
            // Arrange — Self-targeting move needs no target creature or tile
            var move = CreateMoveConfig(DamageForm.None, TargetType.Self, range: 0, pp: 10);
            var actor = CreateCreatureWithMove(new Vector2Int(3, 3), move, ppRemaining: 10);
            var action = new TurnAction(ActionType.UseMove, move: move, movePPSlot: 0);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsTrue(result.IsValid, $"Self-target move should pass: {result.Reason}");
        }

        [Test]
        public void test_TurnActionValidator_UseMoveWithMovement_Passes()
        {
            // Arrange — UseMove + movement target (split turn)
            var move = CreateMoveConfig(DamageForm.Physical, TargetType.Single, range: 2, pp: 10);
            var actor = CreateCreatureWithMove(new Vector2Int(3, 3), move, ppRemaining: 10);
            var target = CreateCreature(new Vector2Int(4, 3));
            var action = new TurnAction(
                ActionType.UseMove,
                movementTarget: new Vector2Int(3, 4),
                move: move, target: target, movePPSlot: 0);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsTrue(result.IsValid,
                $"UseMove with movement target should pass: {result.Reason}");
        }

        // ── Capture Validation ────────────────────────────────────────────

        [Test]
        public void test_TurnActionValidator_ValidCapture_Passes()
        {
            // Arrange
            var actor = CreateCreature(new Vector2Int(3, 3));
            var target = CreateCreature(new Vector2Int(4, 3));
            var action = new TurnAction(ActionType.Capture, target: target);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsTrue(result.IsValid, $"Valid capture should pass: {result.Reason}");
        }

        [Test]
        public void test_TurnActionValidator_CaptureNullTarget_Fails()
        {
            // Arrange
            var actor = CreateCreature(new Vector2Int(3, 3));
            var action = new TurnAction(ActionType.Capture);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Reason.Contains("Target is null"));
        }

        [Test]
        public void test_TurnActionValidator_CaptureInTrainerBattle_Fails()
        {
            // Arrange
            var actor = CreateCreature(new Vector2Int(3, 3));
            var target = CreateCreature(new Vector2Int(4, 3));
            var action = new TurnAction(ActionType.Capture, target: target);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Trainer, _grid);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Reason.Contains("Cannot capture in trainer battles"));
        }

        // ── Flee Validation ───────────────────────────────────────────────

        [Test]
        public void test_TurnActionValidator_ValidFlee_Passes()
        {
            // Arrange
            var actor = CreateCreature(new Vector2Int(3, 3));
            var action = new TurnAction(ActionType.Flee);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsTrue(result.IsValid, $"Valid flee should pass: {result.Reason}");
        }

        [Test]
        public void test_TurnActionValidator_FleeWithMovement_Fails()
        {
            // Arrange — Flee must have null MovementTarget
            var actor = CreateCreature(new Vector2Int(3, 3));
            var action = new TurnAction(ActionType.Flee, movementTarget: new Vector2Int(4, 3));

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Reason.Contains("MovementTarget must be null"));
        }

        // ── Wait Validation ───────────────────────────────────────────────

        [Test]
        public void test_TurnActionValidator_ValidWait_Passes()
        {
            // Arrange
            var actor = CreateCreature(new Vector2Int(3, 3));
            var action = new TurnAction(ActionType.Wait);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsTrue(result.IsValid, $"Valid wait should pass: {result.Reason}");
        }

        [Test]
        public void test_TurnActionValidator_WaitWithMove_Fails()
        {
            // Arrange — Wait should have no Move set
            var move = CreateMoveConfig(DamageForm.Physical, TargetType.Single, range: 1, pp: 10);
            var actor = CreateCreature(new Vector2Int(3, 3));
            var action = new TurnAction(ActionType.Wait, move: move);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Reason.Contains("Move should be null"));
        }

        [Test]
        public void test_TurnActionValidator_WaitWithMovement_Passes()
        {
            // Arrange — Wait CAN have movement (split turn — move but don't act)
            var actor = CreateCreature(new Vector2Int(3, 3));
            var action = new TurnAction(ActionType.Wait, movementTarget: new Vector2Int(4, 3));

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsTrue(result.IsValid,
                $"Wait with movement should pass (split turn): {result.Reason}");
        }

        // ── Edge Cases ────────────────────────────────────────────────────

        [Test]
        public void test_TurnActionValidator_NullActor_Fails()
        {
            // Arrange
            var action = new TurnAction(ActionType.Wait);

            // Act
            var result = TurnActionValidator.Validate(action, null, EncounterType.Wild, _grid);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Reason.Contains("Actor is null"));
        }

        // ── Additional Coverage ──────────────────────────────────────────

        [Test]
        public void test_TurnActionValidator_UseMoveNegativeSlot_Fails()
        {
            // Arrange — MovePPSlot -1 is invalid for UseMove
            var move = CreateMoveConfig(DamageForm.Physical, TargetType.Single, range: 1, pp: 10);
            var actor = CreateCreatureWithMove(new Vector2Int(3, 3), move, ppRemaining: 10);
            var target = CreateCreature(new Vector2Int(4, 3));
            var action = new TurnAction(ActionType.UseMove, move: move, target: target, movePPSlot: -1);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Reason.Contains("out of range 0-3"));
        }

        [Test]
        public void test_TurnActionValidator_CaptureInTrophyEncounter_Passes()
        {
            // Arrange — capture is allowed in Trophy encounters
            var actor = CreateCreature(new Vector2Int(3, 3));
            var target = CreateCreature(new Vector2Int(4, 3));
            var action = new TurnAction(ActionType.Capture, target: target);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Trophy, _grid);

            // Assert
            Assert.IsTrue(result.IsValid,
                $"Capture in Trophy encounter should pass: {result.Reason}");
        }

        [Test]
        public void test_TurnActionValidator_CaptureInHordeEncounter_Passes()
        {
            // Arrange — capture is allowed in Horde encounters
            var actor = CreateCreature(new Vector2Int(3, 3));
            var target = CreateCreature(new Vector2Int(4, 3));
            var action = new TurnAction(ActionType.Capture, target: target);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Horde, _grid);

            // Assert
            Assert.IsTrue(result.IsValid,
                $"Capture in Horde encounter should pass: {result.Reason}");
        }

        [Test]
        public void test_TurnActionValidator_WaitWithNonDefaultPPSlot_Fails()
        {
            // Arrange — Wait must have MovePPSlot == -1
            var actor = CreateCreature(new Vector2Int(3, 3));
            var action = new TurnAction(ActionType.Wait, movePPSlot: 2);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Reason.Contains("MovePPSlot should be -1"));
        }

        [Test]
        public void test_TurnActionValidator_UseMoveAdjacentTargetTooFar_Fails()
        {
            // Arrange — Adjacent move requires distance == 1
            var move = CreateMoveConfig(DamageForm.Physical, TargetType.Adjacent, range: 1, pp: 10);
            var actor = CreateCreatureWithMove(new Vector2Int(0, 0), move, ppRemaining: 10);
            var target = CreateCreature(new Vector2Int(3, 3));
            var action = new TurnAction(ActionType.UseMove, move: move, target: target, movePPSlot: 0);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Wild, _grid);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Reason.Contains("Adjacent target at distance"));
        }

        [Test]
        public void test_TurnActionValidator_FleeInTrainerBattle_StillValid()
        {
            // Arrange — Flee in trainer battle is valid at submission time
            // (TurnManager handles the no-op per GDD §3.8)
            var actor = CreateCreature(new Vector2Int(3, 3));
            var action = new TurnAction(ActionType.Flee);

            // Act
            var result = TurnActionValidator.Validate(action, actor, EncounterType.Trainer, _grid);

            // Assert — Flee is accepted; TurnManager handles the trainer no-op
            Assert.IsTrue(result.IsValid,
                $"Flee in trainer battle should pass validation (no-op at execution): {result.Reason}");
        }

        // ── Test Helpers ──────────────────────────────────────────────────

        private static GridSystem CreateFlatGrid(int width, int depth)
        {
            var grid = new GridSystem(width, depth);
            for (int x = 0; x < width; x++)
                for (int z = 0; z < depth; z++)
                    grid.SetTile(new TileData(new Vector2Int(x, z), 0, TerrainType.Neutral));
            return grid;
        }

        private CreatureInstance CreateCreature(Vector2Int position)
        {
            var config = CreateAsset<CreatureConfig>();
            SetField(config, "id", "test-creature");
            SetField(config, "displayName", "TestCreature");
            SetField(config, "primaryType", CreatureType.Thermal);
            SetField(config, "secondaryType", CreatureType.None);
            SetField(config, "baseStats", new BaseStats(100, 100, 100, 100, 100));
            SetField(config, "growthCurve", GrowthCurve.Medium);
            SetField(config, "movePool", new List<LevelMoveEntry>());
            SetField(config, "availableSlots", new List<BodySlot>());
            SetField(config, "defaultPartIds", new List<string>());
            SetField(config, "habitatZoneIds", new List<string>());
            SetField(config, "rarity", Rarity.Common);
            SetField(config, "bodyArchetype", BodyArchetype.Bipedal);

            var creature = new CreatureInstance();
            SetField(creature, "_config", config);
            SetField(creature, "_level", 1);
            SetField(creature, "_currentHP", 100);
            SetField(creature, "_maxHP", 100);
            SetField(creature, "_currentXP", 0);
            SetField(creature, "_xpNextLevel", 100);
            SetField(creature, "_learnedMoveIds", new List<string>());
            SetField(creature, "_learnedMovePP", new List<int>());
            SetField(creature, "_equippedPartSlots", new List<int>());
            SetField(creature, "_equippedPartIds", new List<string>());
            SetField(creature, "_activeStatusEffects", new List<StatusEffect>());
            SetField(creature, "_gridPosition", position);
            SetField(creature, "_facing", Facing.N);
            creature.RecalculateStats();

            return creature;
        }

        private CreatureInstance CreateCreatureWithMove(
            Vector2Int position, MoveConfig move, int ppRemaining)
        {
            var creature = CreateCreature(position);
            var moveIds = new List<string> { move.Id ?? "test-move" };
            var movePP = new List<int> { ppRemaining };
            SetField(creature, "_learnedMoveIds", moveIds);
            SetField(creature, "_learnedMovePP", movePP);
            return creature;
        }

        private MoveConfig CreateMoveConfig(
            DamageForm form, TargetType targetType, int range, int pp)
        {
            var move = CreateAsset<MoveConfig>();
            SetField(move, "id", "test-move");
            SetField(move, "displayName", "TestMove");
            SetField(move, "form", form);
            SetField(move, "targetType", targetType);
            SetField(move, "range", range);
            SetField(move, "power", 50);
            SetField(move, "accuracy", 100);
            SetField(move, "pp", pp);
            SetField(move, "priority", 0);
            SetField(move, "genomeType", CreatureType.Thermal);
            SetField(move, "effects", new List<MoveEffect>());
            return move;
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _createdAssets.Add(asset);
            return asset;
        }

        private static void SetField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            System.Reflection.FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                type = type.BaseType;
            }
            if (field != null)
                field.SetValue(obj, value);
        }
    }
}
