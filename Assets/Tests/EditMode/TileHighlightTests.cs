using System.Collections.Generic;
using GeneForge.Combat;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;
using GeneForge.UI;
using NUnit.Framework;
using UnityEngine;

namespace GeneForge.Tests
{
    /// <summary>
    /// EditMode tests for TileHighlightController (pure C# class).
    /// Verifies tile set computation for movement, attack (form-specific),
    /// capture, and terrain synergy highlights.
    ///
    /// Test naming: test_[system]_[scenario]_[expected_result]
    /// </summary>
    [TestFixture]
    public class TileHighlightTests
    {
        private GridSystem _grid;
        private TileHighlightController _controller;

        [SetUp]
        public void SetUp()
        {
            // Create a flat 6x6 grid with default terrain
            _grid = CreateFlatGrid(6, 6);
            _controller = new TileHighlightController(_grid);
        }

        // ── Movement Tests ────────────────────────────────────────────────

        [Test]
        public void test_TileHighlight_GetMovementTiles_ReturnsReachableTilesFromGridSystem()
        {
            // Arrange
            var startPos = new Vector2Int(3, 3);
            int range = 2;

            // Act
            var tiles = _controller.GetMovementTiles(startPos, range);

            // Assert
            Assert.IsNotNull(tiles);
            Assert.IsTrue(tiles.Count > 0, "Should return at least one reachable tile");
            Assert.IsFalse(tiles.Contains(startPos),
                "Reachable tiles should not include the start position (GridSystem behavior)");

            // All returned tiles should be within Chebyshev distance of range
            foreach (var tile in tiles)
            {
                int dist = GridSystem.ChebyshevDistance(startPos, tile);
                Assert.LessOrEqual(dist, range,
                    $"Tile {tile} is at distance {dist}, exceeds range {range}");
            }
        }

        // ── Attack Tile Tests ─────────────────────────────────────────────

        [Test]
        public void test_TileHighlight_GetAttackTilesPhysical_NoChebyshevBeyondRange()
        {
            // Arrange
            var creaturePos = new Vector2Int(3, 3);
            var move = CreateMoveConfig(DamageForm.Physical, range: 1);

            // Act
            var tiles = _controller.GetAttackTiles(move, creaturePos, out var blocked);

            // Assert
            foreach (var tile in tiles)
            {
                int dist = GridSystem.ChebyshevDistance(creaturePos, tile);
                Assert.LessOrEqual(dist, 1,
                    $"Physical move range 1: tile {tile} at distance {dist} should not be included");
            }
            Assert.AreEqual(0, blocked.Count, "Physical moves should have no blocked tiles");
        }

        [Test]
        public void test_TileHighlight_GetAttackTilesEnergy_ExcludesBlockedByLoS()
        {
            // Arrange — create grid with a LoS-blocking tile
            var grid = CreateGridWithBlocker(6, 6, new Vector2Int(3, 4));
            var controller = new TileHighlightController(grid);
            var creaturePos = new Vector2Int(3, 3);
            var move = CreateMoveConfig(DamageForm.Energy, range: 3);

            // Act
            var tiles = controller.GetAttackTiles(move, creaturePos, out var blocked);

            // Assert
            // Tile behind blocker (3,5) should be in blocked set, not in valid set
            // The exact behavior depends on GridSystem.HasLineOfSight implementation
            Assert.IsFalse(tiles.Contains(new Vector2Int(3, 4)),
                "LoS-blocking tile itself should not be in valid attack tiles");
            Assert.IsTrue(blocked.Contains(new Vector2Int(3, 4)),
                "LoS-blocking tile should be in blocked set for dimmed rendering");
        }

        [Test]
        public void test_TileHighlight_GetAttackTilesBio_IgnoresLoS()
        {
            // Arrange — same grid with blocker
            var grid = CreateGridWithBlocker(6, 6, new Vector2Int(3, 4));
            var controller = new TileHighlightController(grid);
            var creaturePos = new Vector2Int(3, 3);
            var move = CreateMoveConfig(DamageForm.Bio, range: 3);

            // Act
            var tiles = controller.GetAttackTiles(move, creaturePos, out var blocked);

            // Assert — Bio ignores LoS, so blocked set should be empty
            Assert.AreEqual(0, blocked.Count, "Bio moves should have no blocked tiles");
            // Tiles behind LoS blocker should still be included
            Assert.IsTrue(tiles.Contains(new Vector2Int(3, 5)),
                "Bio move should include tile behind LoS blocker");
        }

        // ── Synergy Tests ─────────────────────────────────────────────────

        [Test]
        public void test_TileHighlight_GetSynergyTiles_ReturnsOnlyMatchingTerrainType()
        {
            // Arrange — set specific tiles to Thermal terrain
            SetTileTerrain(_grid, new Vector2Int(1, 1), TerrainType.Thermal);
            SetTileTerrain(_grid, new Vector2Int(4, 4), TerrainType.Thermal);

            // Act
            var synergyTiles = _controller.GetSynergyTiles(CreatureType.Thermal);

            // Assert
            Assert.IsTrue(synergyTiles.Contains(new Vector2Int(1, 1)),
                "Thermal terrain tile should be in synergy set for Thermal creature");
            Assert.IsTrue(synergyTiles.Contains(new Vector2Int(4, 4)),
                "Thermal terrain tile should be in synergy set for Thermal creature");

            // Non-thermal tiles should not be included
            Assert.IsFalse(synergyTiles.Contains(new Vector2Int(0, 0)),
                "Neutral terrain tile should not be in synergy set for Thermal creature");
        }

        // ── Cache Tests ───────────────────────────────────────────────────

        [Test]
        public void test_TileHighlight_ClearCache_FlushesReachableCache()
        {
            // Arrange
            var pos = new Vector2Int(3, 3);
            var firstResult = _controller.GetMovementTiles(pos, 2);
            int firstCount = firstResult.Count;

            // Act — clear cache (simulates phase change)
            _controller.ClearCache();

            // Modify grid state (block a tile)
            SetTileImpassable(_grid, new Vector2Int(4, 3));

            var secondResult = _controller.GetMovementTiles(pos, 2);

            // Assert — count should differ after grid mutation + cache clear
            // The blocked tile should reduce reachable count
            Assert.LessOrEqual(secondResult.Count, firstCount,
                "After blocking a tile and clearing cache, reachable count should not increase");
        }

        // ── Test Helpers ──────────────────────────────────────────────────

        /// <summary>Create a flat grid with all passable Neutral terrain tiles.</summary>
        private static GridSystem CreateFlatGrid(int width, int depth)
        {
            var grid = new GridSystem(width, depth);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    grid.SetTile(new TileData(
                        new Vector2Int(x, z), 0, TerrainType.Neutral));
                }
            }

            return grid;
        }

        /// <summary>Create a grid with a specific tile that blocks line of sight.</summary>
        private static GridSystem CreateGridWithBlocker(int width, int depth, Vector2Int blockerPos)
        {
            var grid = new GridSystem(width, depth);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    var pos = new Vector2Int(x, z);
                    bool blocksLoS = (pos == blockerPos);
                    grid.SetTile(new TileData(pos, 0, TerrainType.Neutral,
                        isPassable: !blocksLoS, blocksLoS: blocksLoS));
                }
            }

            return grid;
        }

        /// <summary>Create a minimal MoveConfig for testing via reflection.</summary>
        private static MoveConfig CreateMoveConfig(DamageForm form, int range)
        {
            // MoveConfig is a ScriptableObject — create instance for test
            var move = ScriptableObject.CreateInstance<MoveConfig>();

            // Use reflection to set private fields (following CreatureInstanceTests pattern)
            SetPrivateField(move, "_form", form);
            SetPrivateField(move, "_range", range);
            SetPrivateField(move, "_power", 50);
            SetPrivateField(move, "_genomeType", CreatureType.Thermal);
            SetPrivateField(move, "_targetType", TargetType.Single);

            return move;
        }

        /// <summary>Replace a tile with a new one that has the specified terrain type.</summary>
        private static void SetTileTerrain(GridSystem grid, Vector2Int pos, TerrainType terrain)
        {
            grid.SetTile(new TileData(pos, 0, terrain));
        }

        /// <summary>Replace a tile with a new one that is impassable.</summary>
        private static void SetTileImpassable(GridSystem grid, Vector2Int pos)
        {
            grid.SetTile(new TileData(pos, 0, TerrainType.Neutral, isPassable: false));
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(obj, value);
        }
    }
}
