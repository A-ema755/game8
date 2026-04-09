using System.Collections.Generic;
using System.Text.RegularExpressions;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;
using GeneForge.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GeneForge.Tests
{
    /// <summary>
    /// EditMode tests for TargetingHelper (pure C# static class).
    /// Verifies target tile computation, creature target filtering,
    /// and movement range calculation.
    ///
    /// Test naming: test_[system]_[scenario]_[expected_result]
    /// </summary>
    [TestFixture]
    public class TargetingHelperTests
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

        // ── GetValidTargetTiles Tests ─────────────────────────────────────

        [Test]
        public void test_TargetingHelper_GetValidTargetTilesPhysical_ReturnsTilesInRange()
        {
            // Arrange
            var move = CreateMoveConfig(DamageForm.Physical, TargetType.Single, range: 1);
            var actor = CreateCreature(new Vector2Int(3, 3));

            // Act
            var tiles = TargetingHelper.GetValidTargetTiles(move, actor, _grid);

            // Assert — Chebyshev ring at range 1 on open 8x8 grid from (3,3) = 8 tiles
            Assert.AreEqual(8, tiles.Count, "Range 1 from center of open grid should yield 8 tiles");
            foreach (var tile in tiles)
            {
                int dist = GridSystem.ChebyshevDistance(new Vector2Int(3, 3), tile);
                Assert.LessOrEqual(dist, 1, $"Tile {tile} exceeds range 1");
            }
        }

        [Test]
        public void test_TargetingHelper_GetValidTargetTilesEnergy_ExcludesBlockedLoS()
        {
            // Arrange — grid with LoS blocker at (3,4)
            var grid = CreateGridWithBlocker(8, 8, new Vector2Int(3, 4));
            var move = CreateMoveConfig(DamageForm.Energy, TargetType.Single, range: 3);
            var actor = CreateCreature(new Vector2Int(3, 3));

            // Act
            var tiles = TargetingHelper.GetValidTargetTiles(move, actor, grid);

            // Assert — blocker tile and tiles behind it should be excluded
            Assert.IsFalse(tiles.Contains(new Vector2Int(3, 4)),
                "LoS-blocking tile should not be in valid target tiles for Energy");
        }

        [Test]
        public void test_TargetingHelper_GetValidTargetTilesBio_IgnoresLoS()
        {
            // Arrange — grid with LoS blocker at (3,4)
            var grid = CreateGridWithBlocker(8, 8, new Vector2Int(3, 4));
            var move = CreateMoveConfig(DamageForm.Bio, TargetType.Single, range: 3);
            var actor = CreateCreature(new Vector2Int(3, 3));

            // Act
            var tiles = TargetingHelper.GetValidTargetTiles(move, actor, grid);

            // Assert — Bio ignores LoS, tiles behind blocker included
            Assert.IsTrue(tiles.Contains(new Vector2Int(3, 5)),
                "Bio move should include tile behind LoS blocker");
        }

        [Test]
        public void test_TargetingHelper_GetValidTargetTilesSelf_ReturnsActorPosition()
        {
            // Arrange
            var move = CreateMoveConfig(DamageForm.None, TargetType.Self, range: 0);
            var actor = CreateCreature(new Vector2Int(4, 4));

            // Act
            var tiles = TargetingHelper.GetValidTargetTiles(move, actor, _grid);

            // Assert
            Assert.AreEqual(1, tiles.Count, "Self-targeting should return exactly one tile");
            Assert.AreEqual(new Vector2Int(4, 4), tiles[0],
                "Self-targeting should return actor position");
        }

        [Test]
        public void test_TargetingHelper_GetValidTargetTiles_ExcludesImpassableTiles()
        {
            // Arrange — block a tile within range
            SetTileImpassable(_grid, new Vector2Int(4, 3));
            var move = CreateMoveConfig(DamageForm.Physical, TargetType.Single, range: 2);
            var actor = CreateCreature(new Vector2Int(3, 3));

            // Act
            var tiles = TargetingHelper.GetValidTargetTiles(move, actor, _grid);

            // Assert
            Assert.IsFalse(tiles.Contains(new Vector2Int(4, 3)),
                "Impassable tile should not be in valid target tiles");
        }

        [Test]
        public void test_TargetingHelper_GetValidTargetTiles_NullInputsReturnEmpty()
        {
            // Arrange / Act / Assert
            Assert.AreEqual(0, TargetingHelper.GetValidTargetTiles(null, null, null).Count);
        }

        // ── GetValidCreatureTargets Tests ─────────────────────────────────

        [Test]
        public void test_TargetingHelper_GetValidCreatureTargets_FiltersOutOfRange()
        {
            // Arrange
            var move = CreateMoveConfig(DamageForm.Physical, TargetType.Single, range: 1);
            var actor = CreateCreature(new Vector2Int(0, 0));

            var nearEnemy = CreateCreature(new Vector2Int(1, 0)); // dist 1 — in range
            var farEnemy = CreateCreature(new Vector2Int(5, 5));  // dist 5 — out of range
            var enemies = new List<CreatureInstance> { nearEnemy, farEnemy };

            // Act
            var targets = TargetingHelper.GetValidCreatureTargets(move, actor, _grid, enemies);

            // Assert
            Assert.AreEqual(1, targets.Count, "Only near enemy should be valid target");
            Assert.AreSame(nearEnemy, targets[0]);
        }

        [Test]
        public void test_TargetingHelper_GetValidCreatureTargets_ExcludesFainted()
        {
            // Arrange
            var move = CreateMoveConfig(DamageForm.Physical, TargetType.Single, range: 5);
            var actor = CreateCreature(new Vector2Int(0, 0));

            var faintedEnemy = CreateCreature(new Vector2Int(1, 0));
            FaintCreature(faintedEnemy);
            var enemies = new List<CreatureInstance> { faintedEnemy };

            // Act
            var targets = TargetingHelper.GetValidCreatureTargets(move, actor, _grid, enemies);

            // Assert
            Assert.AreEqual(0, targets.Count, "Fainted creatures should not be valid targets");
        }

        [Test]
        public void test_TargetingHelper_GetValidCreatureTargets_EnergyExcludesNoLoS()
        {
            // Arrange — blocker between actor and enemy
            var grid = CreateGridWithBlocker(8, 8, new Vector2Int(2, 0));
            var move = CreateMoveConfig(DamageForm.Energy, TargetType.Single, range: 5);
            var actor = CreateCreature(new Vector2Int(0, 0));
            var enemy = CreateCreature(new Vector2Int(4, 0));
            var enemies = new List<CreatureInstance> { enemy };

            // Act
            var targets = TargetingHelper.GetValidCreatureTargets(move, actor, grid, enemies);

            // Assert
            Assert.AreEqual(0, targets.Count,
                "Energy move should exclude targets with blocked LoS");
        }

        // ── GetMovementTiles Tests ────────────────────────────────────────

        [Test]
        public void test_TargetingHelper_GetMovementTiles_CalculatesRangeFromSPD()
        {
            // Arrange — SPD 60, divisor 20 = range 3
            var creature = CreateCreature(new Vector2Int(4, 4), spd: 60);
            int divisor = 20;

            // Act
            var tiles = TargetingHelper.GetMovementTiles(creature, _grid, divisor);

            // Assert — SPD 60 / divisor 20 = range 3
            Assert.Greater(tiles.Count, 0, "Should return reachable tiles");
            foreach (var tile in tiles)
            {
                int dist = GridSystem.ChebyshevDistance(new Vector2Int(4, 4), tile);
                Assert.LessOrEqual(dist, 3,
                    $"Tile {tile} at distance {dist} exceeds expected movement range 3");
            }
        }

        [Test]
        public void test_TargetingHelper_GetMovementTiles_MinimumRangeOne()
        {
            // Arrange — SPD 5, divisor 20 = floor(0.25) = 0, clamped to 1
            var creature = CreateCreature(new Vector2Int(4, 4), spd: 5);
            int divisor = 20;

            // Act
            var tiles = TargetingHelper.GetMovementTiles(creature, _grid, divisor);

            // Assert — should still get tiles at distance 1 (min range clamped to 1)
            Assert.GreaterOrEqual(tiles.Count, 4,
                "Minimum range 1 should reach at least 4 cardinal tiles");
        }

        [Test]
        public void test_TargetingHelper_GetMovementTiles_ZeroDivisorReturnsEmpty()
        {
            // Arrange
            var creature = CreateCreature(new Vector2Int(4, 4));

            // Act — zero divisor fires LogError
            LogAssert.Expect(LogType.Error, new Regex("movementDivisor must be > 0"));
            var tiles = TargetingHelper.GetMovementTiles(creature, _grid, 0);

            // Assert
            Assert.AreEqual(0, tiles.Count,
                "Zero divisor should return empty (guard clause)");
        }

        // ── Additional Coverage ───────────────────────────────────────────

        [Test]
        public void test_TargetingHelper_GetValidCreatureTargetsBio_IgnoresLoS()
        {
            // Arrange — blocker between actor and enemy, Bio move ignores LoS
            var grid = CreateGridWithBlocker(8, 8, new Vector2Int(2, 0));
            var move = CreateMoveConfig(DamageForm.Bio, TargetType.Single, range: 5);
            var actor = CreateCreature(new Vector2Int(0, 0));
            var enemy = CreateCreature(new Vector2Int(4, 0));
            var enemies = new List<CreatureInstance> { enemy };

            // Act
            var targets = TargetingHelper.GetValidCreatureTargets(move, actor, grid, enemies);

            // Assert — Bio ignores LoS, enemy should be valid
            Assert.AreEqual(1, targets.Count,
                "Bio move should include targets behind LoS blocker");
        }

        [Test]
        public void test_TargetingHelper_GetValidTargetTiles_GridBoundary_ClipsCorrectly()
        {
            // Arrange — actor at corner (0,0), range 2
            var move = CreateMoveConfig(DamageForm.Physical, TargetType.Single, range: 2);
            var actor = CreateCreature(new Vector2Int(0, 0));

            // Act
            var tiles = TargetingHelper.GetValidTargetTiles(move, actor, _grid);

            // Assert — at corner, range 2 covers a 3x3 area minus actor = 8 tiles
            Assert.AreEqual(8, tiles.Count,
                "Range 2 from corner (0,0) should yield 8 tiles (3x3 - self)");
            foreach (var tile in tiles)
            {
                Assert.IsTrue(tile.x >= 0 && tile.x < 8 && tile.y >= 0 && tile.y < 8,
                    $"Tile {tile} should be within grid bounds");
            }
        }

        [Test]
        public void test_TargetingHelper_GetMovementTiles_SurroundedByImpassable_ReturnsEmpty()
        {
            // Arrange — block all 8 neighbours of (4,4)
            for (int dx = -1; dx <= 1; dx++)
                for (int dz = -1; dz <= 1; dz++)
                    if (dx != 0 || dz != 0)
                        SetTileImpassable(_grid, new Vector2Int(4 + dx, 4 + dz));

            var creature = CreateCreature(new Vector2Int(4, 4));

            // Act
            var tiles = TargetingHelper.GetMovementTiles(creature, _grid, 20);

            // Assert
            Assert.AreEqual(0, tiles.Count,
                "Creature surrounded by impassable tiles should have no movement");
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

        private static GridSystem CreateGridWithBlocker(int width, int depth, Vector2Int blockerPos)
        {
            var grid = new GridSystem(width, depth);
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    var pos = new Vector2Int(x, z);
                    bool isBlocker = (pos == blockerPos);
                    grid.SetTile(new TileData(pos, 0, TerrainType.Neutral,
                        isPassable: !isBlocker, blocksLoS: isBlocker));
                }
            }
            return grid;
        }

        private static void SetTileImpassable(GridSystem grid, Vector2Int pos)
        {
            grid.SetTile(new TileData(pos, 0, TerrainType.Neutral, isPassable: false));
        }

        private CreatureInstance CreateCreature(Vector2Int position, int spd = 100)
        {
            var config = CreateAsset<CreatureConfig>();
            SetField(config, "id", "test-creature");
            SetField(config, "displayName", "TestCreature");
            SetField(config, "primaryType", CreatureType.Thermal);
            SetField(config, "secondaryType", CreatureType.None);
            SetField(config, "baseStats", new BaseStats(100, 100, 100, spd, 100));
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

        private void FaintCreature(CreatureInstance creature)
        {
            SetField(creature, "_currentHP", 0);
            SetField(creature, "_isFainted", true);
        }

        private MoveConfig CreateMoveConfig(DamageForm form, TargetType targetType, int range)
        {
            var move = CreateAsset<MoveConfig>();
            SetField(move, "id", "test-move");
            SetField(move, "displayName", "TestMove");
            SetField(move, "form", form);
            SetField(move, "targetType", targetType);
            SetField(move, "range", range);
            SetField(move, "power", 50);
            SetField(move, "accuracy", 100);
            SetField(move, "pp", 10);
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
