using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;

namespace GeneForge.Tests
{
    [TestFixture]
    public class GridSystemTests
    {
        private GridSystem _grid;
        private CreatureConfig _blockerConfig;

        private static void SetField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }
            field?.SetValue(obj, value);
        }

        /// <summary>Creates a minimal CreatureInstance for use as a tile occupant in tests.</summary>
        private CreatureInstance CreateBlocker()
        {
            if (_blockerConfig == null)
            {
                _blockerConfig = ScriptableObject.CreateInstance<CreatureConfig>();
                SetField(_blockerConfig, "id", "blocker");
                SetField(_blockerConfig, "displayName", "Blocker");
                SetField(_blockerConfig, "primaryType", CreatureType.None);
                SetField(_blockerConfig, "secondaryType", CreatureType.None);
                SetField(_blockerConfig, "baseStats", new BaseStats(10, 10, 10, 10, 100));
                SetField(_blockerConfig, "growthCurve", GrowthCurve.Medium);
                SetField(_blockerConfig, "movePool", new List<LevelMoveEntry>());
                SetField(_blockerConfig, "availableSlots", new List<BodySlot>());
                SetField(_blockerConfig, "defaultPartIds", new List<string>());
                SetField(_blockerConfig, "habitatZoneIds", new List<string>());
            }
            return CreatureInstance.Create(_blockerConfig, 1);
        }

        /// <summary>
        /// Creates a flat grid with all tiles at height 0, Neutral terrain, passable.
        /// </summary>
        private GridSystem CreateFlatGrid(int width, int depth, int height = 0)
        {
            var grid = new GridSystem(width, depth);
            for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
                grid.SetTile(new TileData(new Vector2Int(x, z), height, TerrainType.Neutral));
            return grid;
        }

        [SetUp]
        public void SetUp()
        {
            _grid = CreateFlatGrid(8, 8);
        }

        [TearDown]
        public void TearDown()
        {
            if (_blockerConfig != null)
            {
                ScriptableObject.DestroyImmediate(_blockerConfig);
                _blockerConfig = null;
            }
        }

        // ================================================================
        // Grid Basics
        // ================================================================

        [Test]
        public void test_GridSystem_Create6x6_AllTilesAccessible()
        {
            // Arrange
            var grid = CreateFlatGrid(6, 6);

            // Assert
            for (int x = 0; x < 6; x++)
            for (int z = 0; z < 6; z++)
            {
                var tile = grid.GetTile(x, z);
                Assert.IsNotNull(tile, $"Tile ({x},{z}) should not be null");
                Assert.IsTrue(tile.IsPassable);
            }
        }

        [Test]
        public void test_GridSystem_Create16x12_BoundaryTilesCorrect()
        {
            // Arrange
            var grid = CreateFlatGrid(16, 12);

            // Assert — corners
            Assert.IsNotNull(grid.GetTile(0, 0));
            Assert.IsNotNull(grid.GetTile(15, 0));
            Assert.IsNotNull(grid.GetTile(0, 11));
            Assert.IsNotNull(grid.GetTile(15, 11));
            Assert.AreEqual(16, grid.Width);
            Assert.AreEqual(12, grid.Depth);
        }

        [Test]
        public void test_GridSystem_GetTile_OutOfBounds_ReturnsNull()
        {
            Assert.IsNull(_grid.GetTile(-1, 0));
            Assert.IsNull(_grid.GetTile(0, -1));
            Assert.IsNull(_grid.GetTile(8, 0));
            Assert.IsNull(_grid.GetTile(0, 8));
        }

        [Test]
        public void test_GridSystem_SetTileGetTile_RoundTrip()
        {
            // Arrange
            var grid = new GridSystem(6, 6);
            var tile = new TileData(new Vector2Int(3, 4), 2, TerrainType.Mineral);
            grid.SetTile(tile);

            // Act
            var retrieved = grid.GetTile(3, 4);

            // Assert
            Assert.AreSame(tile, retrieved);
            Assert.AreEqual(2, retrieved.Height);
            Assert.AreEqual(TerrainType.Mineral, retrieved.Terrain);
        }

        [Test]
        public void test_GridSystem_Constructor_RejectsDimensionsOutOfRange()
        {
            Assert.Throws<ArgumentException>(() => new GridSystem(5, 6));  // width too small
            Assert.Throws<ArgumentException>(() => new GridSystem(17, 6)); // width too big
            Assert.Throws<ArgumentException>(() => new GridSystem(6, 5));  // depth too small
            Assert.Throws<ArgumentException>(() => new GridSystem(6, 13)); // depth too big
        }

        // ================================================================
        // TileData
        // ================================================================

        [Test]
        public void test_TileData_HeightClampedTo0Through4()
        {
            var tileBelow = new TileData(new Vector2Int(0, 0), -1, TerrainType.Neutral);
            var tileAbove = new TileData(new Vector2Int(0, 0), 7, TerrainType.Neutral);

            Assert.AreEqual(0, tileBelow.Height);
            Assert.AreEqual(4, tileAbove.Height);
        }

        [Test]
        public void test_TileData_WorldPosition_ComputedCorrectly()
        {
            var tile = new TileData(new Vector2Int(3, 5), 2, TerrainType.Neutral);
            var expected = new Vector3(
                3 * GridSystem.TileSizeWorld,
                2 * GridSystem.TileHeightStep,
                5 * GridSystem.TileSizeWorld);
            Assert.AreEqual(expected, tile.WorldPosition);
        }

        [Test]
        public void test_TileData_OccupantTracking()
        {
            var tile = new TileData(new Vector2Int(0, 0), 0, TerrainType.Neutral);
            Assert.IsFalse(tile.IsOccupied);
            Assert.IsNull(tile.Occupant);

            tile.Occupant = CreateBlocker();
            Assert.IsTrue(tile.IsOccupied);

            tile.Occupant = null;
            Assert.IsFalse(tile.IsOccupied);
        }

        // ================================================================
        // Height & Movement
        // ================================================================

        [Test]
        public void test_GridSystem_HeightDelta1_IsPassable()
        {
            // Arrange — two adjacent tiles with height delta 1
            var grid = new GridSystem(6, 6);
            grid.SetTile(new TileData(new Vector2Int(0, 0), 0, TerrainType.Neutral));
            grid.SetTile(new TileData(new Vector2Int(1, 0), 1, TerrainType.Neutral));

            // Act
            var neighbours = grid.GetPassableNeighbours(grid.GetTile(0, 0));

            // Assert
            Assert.IsTrue(neighbours.Exists(t => t.GridPosition == new Vector2Int(1, 0)));
        }

        [Test]
        public void test_GridSystem_HeightDelta2_BlocksPathfinding()
        {
            // Arrange — cliff of height 2
            var grid = new GridSystem(6, 6);
            for (int x = 0; x < 6; x++)
            for (int z = 0; z < 6; z++)
                grid.SetTile(new TileData(new Vector2Int(x, z), 0, TerrainType.Neutral));
            grid.SetTile(new TileData(new Vector2Int(1, 0), 2, TerrainType.Neutral));

            // Act
            var neighbours = grid.GetPassableNeighbours(grid.GetTile(0, 0));

            // Assert — height-2 tile is NOT reachable from height-0 tile
            Assert.IsFalse(neighbours.Exists(t => t.GridPosition == new Vector2Int(1, 0)));
        }

        [Test]
        public void test_GridSystem_ClimbCost_Is15()
        {
            Assert.AreEqual(1.5f, GridSystem.ClimbStepCost, 0.001f);
        }

        // ================================================================
        // A* Pathfinding
        // ================================================================

        [Test]
        public void test_GridSystem_FindPath_FlatGrid_FindsShortestPath()
        {
            // Arrange
            var grid = CreateFlatGrid(6, 6);

            // Act
            var path = grid.FindPath(new Vector2Int(0, 0), new Vector2Int(5, 5));

            // Assert — diagonal path should be 6 tiles (0,0 to 5,5 in 5 diagonal steps + start)
            Assert.IsNotEmpty(path);
            Assert.AreEqual(new Vector2Int(0, 0), path[0]);
            Assert.AreEqual(new Vector2Int(5, 5), path[path.Count - 1]);
            Assert.AreEqual(6, path.Count); // start + 5 diagonal steps
        }

        [Test]
        public void test_GridSystem_FindPath_RoutesAroundImpassable()
        {
            // Arrange — wall blocking direct path
            var grid = CreateFlatGrid(8, 8);
            grid.SetTile(new TileData(new Vector2Int(3, 3), 0, TerrainType.Neutral, isPassable: false));
            grid.SetTile(new TileData(new Vector2Int(3, 4), 0, TerrainType.Neutral, isPassable: false));
            grid.SetTile(new TileData(new Vector2Int(3, 5), 0, TerrainType.Neutral, isPassable: false));

            // Act
            var path = grid.FindPath(new Vector2Int(0, 4), new Vector2Int(6, 4));

            // Assert
            Assert.IsNotEmpty(path);
            Assert.AreEqual(new Vector2Int(0, 4), path[0]);
            Assert.AreEqual(new Vector2Int(6, 4), path[path.Count - 1]);
            // Path should not contain any impassable tile
            Assert.IsFalse(path.Contains(new Vector2Int(3, 3)));
            Assert.IsFalse(path.Contains(new Vector2Int(3, 4)));
            Assert.IsFalse(path.Contains(new Vector2Int(3, 5)));
        }

        [Test]
        public void test_GridSystem_FindPath_RoutesAroundOccupied()
        {
            // Arrange
            var grid = CreateFlatGrid(8, 8);
            grid.GetTile(3, 3).Occupant = CreateBlocker();

            // Act — path from (0,3) to (6,3) must go around (3,3)
            var path = grid.FindPath(new Vector2Int(0, 3), new Vector2Int(6, 3));

            // Assert
            Assert.IsNotEmpty(path);
            Assert.IsFalse(path.Contains(new Vector2Int(3, 3)));
        }

        [Test]
        public void test_GridSystem_FindPath_NoPath_ReturnsEmpty()
        {
            // Arrange — completely surrounded
            var grid = CreateFlatGrid(6, 6);
            grid.SetTile(new TileData(new Vector2Int(1, 0), 0, TerrainType.Neutral, isPassable: false));
            grid.SetTile(new TileData(new Vector2Int(0, 1), 0, TerrainType.Neutral, isPassable: false));
            grid.SetTile(new TileData(new Vector2Int(1, 1), 0, TerrainType.Neutral, isPassable: false));

            // Act
            var path = grid.FindPath(new Vector2Int(0, 0), new Vector2Int(5, 5));

            // Assert
            Assert.IsEmpty(path);
        }

        [Test]
        public void test_GridSystem_FindPath_RespectsHeightConstraints()
        {
            // Arrange — cliff wall of height 3 blocking direct horizontal path
            var grid = new GridSystem(6, 6);
            for (int x = 0; x < 6; x++)
            for (int z = 0; z < 6; z++)
                grid.SetTile(new TileData(new Vector2Int(x, z), 0, TerrainType.Neutral));

            // Create a height-3 wall at x=3
            for (int z = 0; z < 6; z++)
                grid.SetTile(new TileData(new Vector2Int(3, z), 3, TerrainType.Neutral));

            // Act
            var path = grid.FindPath(new Vector2Int(0, 0), new Vector2Int(5, 0));

            // Assert — no path because height delta 3 is impassable
            Assert.IsEmpty(path);
        }

        [Test]
        public void test_GridSystem_FindPath_CornerCuttingPrevention()
        {
            // Arrange — two diagonally adjacent impassable tiles
            var grid = CreateFlatGrid(6, 6);
            grid.SetTile(new TileData(new Vector2Int(2, 2), 0, TerrainType.Neutral, isPassable: false));
            grid.SetTile(new TileData(new Vector2Int(3, 3), 0, TerrainType.Neutral, isPassable: false));

            // Act — path from (2,3) to (3,2) would normally cut diagonally
            var path = grid.FindPath(new Vector2Int(2, 3), new Vector2Int(3, 2));

            // Assert — path exists but must not cut through the diagonal gap
            Assert.IsNotEmpty(path);
            // Path should go around, not directly through the corner
            // A direct diagonal from (2,3) to (3,2) goes through the corner of (2,2) and (3,3)
            // which should be blocked. Path length should be > 2.
            Assert.Greater(path.Count, 2);
        }

        [Test]
        public void test_GridSystem_FindPath_StartEqualsGoal_ReturnsSingleElement()
        {
            var path = _grid.FindPath(new Vector2Int(3, 3), new Vector2Int(3, 3));
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(new Vector2Int(3, 3), path[0]);
        }

        [Test]
        public void test_GridSystem_FindPath_ClimbCostAffectsPath()
        {
            // Arrange — grid with a hill in the middle
            var grid = new GridSystem(6, 6);
            for (int x = 0; x < 6; x++)
            for (int z = 0; z < 6; z++)
                grid.SetTile(new TileData(new Vector2Int(x, z), 0, TerrainType.Neutral));

            // Create a height-1 hill in the direct path
            grid.SetTile(new TileData(new Vector2Int(2, 0), 1, TerrainType.Neutral));
            grid.SetTile(new TileData(new Vector2Int(2, 1), 1, TerrainType.Neutral));
            grid.SetTile(new TileData(new Vector2Int(2, 2), 1, TerrainType.Neutral));

            // Act
            var path = grid.FindPath(new Vector2Int(0, 0), new Vector2Int(4, 0));

            // Assert — path exists; climbing is allowed but costs more
            Assert.IsNotEmpty(path);
            Assert.AreEqual(new Vector2Int(4, 0), path[path.Count - 1]);
        }

        // ================================================================
        // Reachable Tiles (BFS)
        // ================================================================

        [Test]
        public void test_GridSystem_GetReachableTiles_Range3_FlatGrid_CorrectCount()
        {
            // Arrange
            var grid = CreateFlatGrid(8, 8);
            var start = new Vector2Int(4, 4);

            // Act
            var reachable = grid.GetReachableTiles(start, 3);

            // Assert — on flat grid with 8-dir movement, range 3 Chebyshev
            // forms a 7×7 diamond minus the center = 48 tiles
            Assert.AreEqual(48, reachable.Count);
        }

        [Test]
        public void test_GridSystem_GetReachableTiles_RespectsHeightCosts()
        {
            // Arrange — grid with height 1 tiles around the start
            var grid = new GridSystem(8, 8);
            for (int x = 0; x < 8; x++)
            for (int z = 0; z < 8; z++)
                grid.SetTile(new TileData(new Vector2Int(x, z), 1, TerrainType.Neutral));
            grid.SetTile(new TileData(new Vector2Int(4, 4), 0, TerrainType.Neutral)); // lower start

            // Act
            var flatGrid = CreateFlatGrid(8, 8);
            var flatReachable = flatGrid.GetReachableTiles(new Vector2Int(4, 4), 3);
            var hillyReachable = grid.GetReachableTiles(new Vector2Int(4, 4), 3);

            // Assert — hilly grid should have fewer reachable tiles due to climb cost
            Assert.Less(hillyReachable.Count, flatReachable.Count);
        }

        [Test]
        public void test_GridSystem_GetReachableTiles_ExcludesOccupied()
        {
            // Arrange
            var grid = CreateFlatGrid(8, 8);
            grid.GetTile(4, 5).Occupant = CreateBlocker();

            // Act
            var reachable = grid.GetReachableTiles(new Vector2Int(4, 4), 1);

            // Assert — occupied tile excluded
            Assert.IsFalse(reachable.Contains(grid.GetTile(4, 5)));
        }

        [Test]
        public void test_GridSystem_GetReachableTiles_ExcludesImpassable()
        {
            // Arrange
            var grid = CreateFlatGrid(8, 8);
            grid.SetTile(new TileData(new Vector2Int(4, 5), 0, TerrainType.Neutral, isPassable: false));

            // Act
            var reachable = grid.GetReachableTiles(new Vector2Int(4, 4), 1);

            // Assert
            Assert.IsFalse(reachable.Contains(grid.GetTile(4, 5)));
        }

        // ================================================================
        // Distance
        // ================================================================

        [Test]
        public void test_GridSystem_ChebyshevDistance_KnownPairs()
        {
            Assert.AreEqual(3, GridSystem.ChebyshevDistance(new Vector2Int(0, 0), new Vector2Int(3, 3)));
            Assert.AreEqual(3, GridSystem.ChebyshevDistance(new Vector2Int(0, 0), new Vector2Int(3, 1)));
            Assert.AreEqual(5, GridSystem.ChebyshevDistance(new Vector2Int(1, 2), new Vector2Int(6, 4)));
        }

        [Test]
        public void test_GridSystem_ChebyshevDistance_SamePosition_Returns0()
        {
            Assert.AreEqual(0, GridSystem.ChebyshevDistance(new Vector2Int(3, 3), new Vector2Int(3, 3)));
        }

        // ================================================================
        // Flanking
        // ================================================================

        [Test]
        public void test_GridSystem_Flanking_AttackerBehindDefender_ReturnsRear()
        {
            // Defender faces North (0,1), attacker is directly South
            var result = GridSystem.GetFlankingType(
                attackerPos: new Vector2Int(3, 2),
                defenderPos: new Vector2Int(3, 3),
                defenderFacing: new Vector2Int(0, 1));
            Assert.AreEqual(FlankingType.Rear, result);
        }

        [Test]
        public void test_GridSystem_Flanking_AttackerInFrontOfDefender_ReturnsFront()
        {
            // Defender faces North (0,1), attacker is directly North
            var result = GridSystem.GetFlankingType(
                attackerPos: new Vector2Int(3, 4),
                defenderPos: new Vector2Int(3, 3),
                defenderFacing: new Vector2Int(0, 1));
            Assert.AreEqual(FlankingType.Front, result);
        }

        [Test]
        public void test_GridSystem_Flanking_AttackerAt90Degrees_ReturnsSide()
        {
            // Defender faces North (0,1), attacker is directly East
            var result = GridSystem.GetFlankingType(
                attackerPos: new Vector2Int(4, 3),
                defenderPos: new Vector2Int(3, 3),
                defenderFacing: new Vector2Int(0, 1));
            Assert.AreEqual(FlankingType.Side, result);
        }

        [Test]
        public void test_GridSystem_Flanking_All8Directions_NorthFacingDefender()
        {
            var defPos = new Vector2Int(4, 4);
            var facing = new Vector2Int(0, 1); // North

            // Front arc: N, NE, NW
            Assert.AreEqual(FlankingType.Front, GridSystem.GetFlankingType(new Vector2Int(4, 5), defPos, facing), "N");
            Assert.AreEqual(FlankingType.Front, GridSystem.GetFlankingType(new Vector2Int(5, 5), defPos, facing), "NE");
            Assert.AreEqual(FlankingType.Front, GridSystem.GetFlankingType(new Vector2Int(3, 5), defPos, facing), "NW");

            // Side arc: E, W
            Assert.AreEqual(FlankingType.Side, GridSystem.GetFlankingType(new Vector2Int(5, 4), defPos, facing), "E");
            Assert.AreEqual(FlankingType.Side, GridSystem.GetFlankingType(new Vector2Int(3, 4), defPos, facing), "W");

            // Rear arc: S, SE, SW
            Assert.AreEqual(FlankingType.Rear, GridSystem.GetFlankingType(new Vector2Int(4, 3), defPos, facing), "S");
            Assert.AreEqual(FlankingType.Rear, GridSystem.GetFlankingType(new Vector2Int(5, 3), defPos, facing), "SE");
            Assert.AreEqual(FlankingType.Rear, GridSystem.GetFlankingType(new Vector2Int(3, 3), defPos, facing), "SW");
        }

        // ================================================================
        // Line-of-Sight
        // ================================================================

        [Test]
        public void test_GridSystem_LoS_ClearFlatGrid_ReturnsTrue()
        {
            Assert.IsTrue(_grid.HasLineOfSight(new Vector2Int(0, 0), new Vector2Int(7, 7)));
        }

        [Test]
        public void test_GridSystem_LoS_BlockedByBlockingTile_ReturnsFalse()
        {
            // Arrange
            var grid = CreateFlatGrid(8, 8);
            grid.SetTile(new TileData(new Vector2Int(3, 3), 0, TerrainType.Neutral,
                isPassable: true, blocksLoS: true));

            // Act/Assert
            Assert.IsFalse(grid.HasLineOfSight(new Vector2Int(0, 0), new Vector2Int(6, 6)));
        }

        [Test]
        public void test_GridSystem_LoS_NonBlockingTile_ReturnsTrue()
        {
            // Arrange — tile that does NOT block LoS
            var grid = CreateFlatGrid(8, 8);
            grid.SetTile(new TileData(new Vector2Int(3, 3), 0, TerrainType.Neutral,
                isPassable: true, blocksLoS: false));

            // Act/Assert
            Assert.IsTrue(grid.HasLineOfSight(new Vector2Int(0, 0), new Vector2Int(6, 6)));
        }

        [Test]
        public void test_GridSystem_LoS_AdjacentTiles_AlwaysTrue()
        {
            // Even with blocking tile property, adjacent tiles always have LoS
            var grid = CreateFlatGrid(8, 8);
            Assert.IsTrue(grid.HasLineOfSight(new Vector2Int(3, 3), new Vector2Int(4, 4)));
            Assert.IsTrue(grid.HasLineOfSight(new Vector2Int(3, 3), new Vector2Int(3, 4)));
            Assert.IsTrue(grid.HasLineOfSight(new Vector2Int(3, 3), new Vector2Int(4, 3)));
        }

        [Test]
        public void test_GridSystem_LoS_LongRange_ThroughClearTiles_ReturnsTrue()
        {
            // Arrange — 16x12 grid, completely clear
            var grid = CreateFlatGrid(16, 12);

            // Act/Assert — long range LoS
            Assert.IsTrue(grid.HasLineOfSight(new Vector2Int(0, 0), new Vector2Int(15, 11)));
        }

        // ================================================================
        // Descend Movement
        // ================================================================

        [Test]
        public void test_GridSystem_Descend3Levels_IsPassable()
        {
            // Arrange — height 3 tile adjacent to height 0 tile
            var grid = new GridSystem(6, 6);
            for (int x = 0; x < 6; x++)
            for (int z = 0; z < 6; z++)
                grid.SetTile(new TileData(new Vector2Int(x, z), 0, TerrainType.Neutral));
            grid.SetTile(new TileData(new Vector2Int(2, 2), 3, TerrainType.Neutral));

            // Act — descending from height 3 to height 0
            var neighbours = grid.GetPassableNeighbours(grid.GetTile(2, 2));

            // Assert — descending any amount is allowed (cost 1.0, fall damage handled separately)
            Assert.IsTrue(neighbours.Count > 0, "Should have passable neighbours when descending");
            Assert.IsTrue(neighbours.Exists(t => t.Height == 0), "Should be able to descend to height 0");
        }

        [Test]
        public void test_GridSystem_FindPath_Descend3Levels_PathExists()
        {
            // Arrange — stepping-stone path: 0→3→0 (can't climb 3, but can descend 3)
            var grid = new GridSystem(6, 6);
            for (int x = 0; x < 6; x++)
            for (int z = 0; z < 6; z++)
                grid.SetTile(new TileData(new Vector2Int(x, z), 3, TerrainType.Neutral));
            grid.SetTile(new TileData(new Vector2Int(0, 0), 3, TerrainType.Neutral));
            grid.SetTile(new TileData(new Vector2Int(5, 5), 0, TerrainType.Neutral));

            // Act — path from high ground to low ground
            var path = grid.FindPath(new Vector2Int(0, 0), new Vector2Int(5, 5));

            // Assert — path exists since all tiles are height 3 except goal at 0
            Assert.IsNotEmpty(path);
            Assert.AreEqual(new Vector2Int(5, 5), path[path.Count - 1]);
        }

        // ================================================================
        // Phase F4 — Height-3 Passability
        // ================================================================

        [Test]
        public void test_GridSystem_height3_tile_is_impassable_for_pathfinding()
        {
            // Arrange — 6x6 grid with a height-3 column at x=3 (all z).
            // GDD encounter-system.md §3.3: height 3 = cliff/unreachable → IsPassable false.
            var grid = new GridSystem(6, 6);
            for (int x = 0; x < 6; x++)
            for (int z = 0; z < 6; z++)
                grid.SetTile(new TileData(new Vector2Int(x, z), 0, TerrainType.Neutral));

            for (int z = 0; z < 6; z++)
                grid.SetTile(new TileData(new Vector2Int(3, z), 3, TerrainType.Neutral,
                    isPassable: false, blocksLoS: true, providesCover: false));

            // Assert — every height-3 tile must report IsPassable == false.
            for (int z = 0; z < 6; z++)
            {
                var tile = grid.GetTile(3, z);
                Assert.IsFalse(tile.IsPassable,
                    $"Height-3 tile at (3,{z}) must be impassable");
            }

            // Act — A* from left side to right side must find no path (wall blocks all rows).
            var path = grid.FindPath(new Vector2Int(0, 0), new Vector2Int(5, 0));

            // Assert — no path exists because the height-3 column is fully impassable.
            Assert.IsEmpty(path,
                "A* must return empty path when height-3 cliff column blocks all routes");
        }

        // ================================================================
        // Performance
        // ================================================================

        [Test]
        public void test_GridSystem_FindPath_16x12_CompletesWithin50ms()
        {
            // Arrange — worst case: max grid, corner to corner
            var grid = CreateFlatGrid(16, 12);

            // Act
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var path = grid.FindPath(new Vector2Int(0, 0), new Vector2Int(15, 11));
            sw.Stop();

            // Assert
            Assert.IsNotEmpty(path);
            Assert.Less(sw.ElapsedMilliseconds, 50,
                $"A* on 16x12 took {sw.ElapsedMilliseconds}ms, budget is 50ms");
        }

        // ================================================================
        // Fall Damage
        // ================================================================

        [Test]
        public void test_GridSystem_FallDamage_BelowMinDelta_Returns0()
        {
            Assert.AreEqual(0, GridSystem.GetFallDamage(0, 10, 3));
            Assert.AreEqual(0, GridSystem.GetFallDamage(1, 10, 3));
            Assert.AreEqual(0, GridSystem.GetFallDamage(2, 10, 3));
        }

        [Test]
        public void test_GridSystem_FallDamage_AtMinDelta_ReturnsBase()
        {
            // heightDelta=3, base=10, minDelta=3 → 10 * (3-3+1) = 10
            Assert.AreEqual(10, GridSystem.GetFallDamage(3, 10, 3));
        }

        [Test]
        public void test_GridSystem_FallDamage_AboveMinDelta_ReturnsScaled()
        {
            // heightDelta=4, base=10, minDelta=3 → 10 * (4-3+1) = 20
            Assert.AreEqual(20, GridSystem.GetFallDamage(4, 10, 3));
            // heightDelta=5 → 10 * (5-3+1) = 30
            Assert.AreEqual(30, GridSystem.GetFallDamage(5, 10, 3));
        }
    }
}
