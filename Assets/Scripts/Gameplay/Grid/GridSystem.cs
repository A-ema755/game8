using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GeneForge.Grid
{
    /// <summary>
    /// Flanking arc relative to a defender's facing direction.
    /// </summary>
    public enum FlankingType
    {
        Front = 0,
        Side = 1,
        Rear = 2
    }

    /// <summary>
    /// Central grid authority. Owns the TileData grid, A* pathfinding,
    /// BFS reachable-tile queries, flanking detection, and line-of-sight.
    /// Pure C# — no MonoBehaviour dependency (ADR-002).
    /// </summary>
    public class GridSystem
    {
        // ── Constants (GDD Section 7) ──────────────────────────────────────
        public const int MaxHeight = 4;
        public const float TileSizeWorld = 1.0f;
        public const float TileHeightStep = 0.5f;
        public const int MaxHeightDeltaPassable = 1;
        public const float ClimbStepCost = 1.5f;
        public const float FlatStepCost = 1.0f;
        public const int MinGridWidth = 6;
        public const int MaxGridWidth = 16;
        public const int MinGridDepth = 6;
        public const int MaxGridDepth = 12;
        public const float FlankFrontDotThreshold = 0.5f;
        public const float FlankRearDotThreshold = -0.5f;

        // ── 8-directional offsets (reused by neighbours, corner-cut checks) ─
        private static readonly Vector2Int[] Directions =
        {
            new Vector2Int( 0,  1), // N
            new Vector2Int( 1,  1), // NE
            new Vector2Int( 1,  0), // E
            new Vector2Int( 1, -1), // SE
            new Vector2Int( 0, -1), // S
            new Vector2Int(-1, -1), // SW
            new Vector2Int(-1,  0), // W
            new Vector2Int(-1,  1), // NW
        };

        // ── State ──────────────────────────────────────────────────────────
        private readonly TileData[,] _grid;

        /// <summary>Grid width in tiles (x-axis).</summary>
        public int Width { get; }

        /// <summary>Grid depth in tiles (z-axis).</summary>
        public int Depth { get; }

        /// <summary>
        /// Creates a new combat grid. Dimensions are validated against
        /// MinGridWidth/MaxGridWidth and MinGridDepth/MaxGridDepth.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when dimensions are out of range.</exception>
        public GridSystem(int width, int depth)
        {
            if (width < MinGridWidth || width > MaxGridWidth)
                throw new ArgumentException(
                    $"Width {width} outside allowed range [{MinGridWidth}, {MaxGridWidth}].",
                    nameof(width));
            if (depth < MinGridDepth || depth > MaxGridDepth)
                throw new ArgumentException(
                    $"Depth {depth} outside allowed range [{MinGridDepth}, {MaxGridDepth}].",
                    nameof(depth));

            Width = width;
            Depth = depth;
            _grid = new TileData[width, depth];
        }

        // ── Grid Management ────────────────────────────────────────────────

        /// <summary>Register a tile at its grid position.</summary>
        public void SetTile(TileData tile)
        {
            _grid[tile.GridPosition.x, tile.GridPosition.y] = tile;
        }

        /// <summary>Returns null if coordinates are out of bounds.</summary>
        public TileData GetTile(int x, int z)
        {
            if (!IsInBounds(x, z)) return null;
            return _grid[x, z];
        }

        /// <summary>Returns null if coordinates are out of bounds.</summary>
        public TileData GetTile(Vector2Int pos) => GetTile(pos.x, pos.y);

        /// <summary>True if (x, z) lies within the grid boundaries.</summary>
        public bool IsInBounds(int x, int z) =>
            x >= 0 && x < Width && z >= 0 && z < Depth;

        /// <summary>
        /// Returns all non-null neighbours in 8 directions. No filtering for
        /// passability or height — callers that need filtering use
        /// <see cref="GetPassableNeighbours"/>.
        /// </summary>
        public List<TileData> GetNeighbours(Vector2Int pos)
        {
            var result = new List<TileData>(8);
            for (int i = 0; i < Directions.Length; i++)
            {
                var tile = GetTile(pos.x + Directions[i].x, pos.y + Directions[i].y);
                if (tile != null)
                    result.Add(tile);
            }
            return result;
        }

        /// <summary>
        /// Returns passable neighbours reachable from <paramref name="from"/>,
        /// respecting height delta, occupancy, and corner-cutting rules.
        /// Diagonal moves are blocked if both adjacent cardinal tiles are impassable.
        /// </summary>
        /// <param name="from">Source tile.</param>
        /// <param name="allowOccupiedGoal">
        /// If set, this position is treated as passable even if occupied
        /// (used for attack targeting where the goal tile is the target).
        /// </param>
        public List<TileData> GetPassableNeighbours(TileData from, Vector2Int? allowOccupiedGoal = null)
        {
            var result = new List<TileData>(8);
            var pos = from.GridPosition;

            for (int i = 0; i < Directions.Length; i++)
            {
                var dir = Directions[i];
                var nx = pos.x + dir.x;
                var nz = pos.y + dir.y;
                var tile = GetTile(nx, nz);
                if (tile == null || !tile.IsPassable) continue;

                // Occupancy check: occupied tiles block unless it's the allowed goal
                if (tile.IsOccupied && !(allowOccupiedGoal.HasValue
                    && tile.GridPosition == allowOccupiedGoal.Value))
                    continue;

                // Height: can only climb +1. Descend any amount is allowed.
                int heightDelta = tile.Height - from.Height;
                if (heightDelta > MaxHeightDeltaPassable) continue;

                // Corner-cutting prevention for diagonals
                if (dir.x != 0 && dir.y != 0)
                {
                    var cardinalX = GetTile(pos.x + dir.x, pos.y);
                    var cardinalZ = GetTile(pos.x, pos.y + dir.y);
                    bool xBlocked = cardinalX == null || !cardinalX.IsPassable;
                    bool zBlocked = cardinalZ == null || !cardinalZ.IsPassable;
                    if (xBlocked && zBlocked) continue;
                }

                result.Add(tile);
            }
            return result;
        }

        // ── Distance ───────────────────────────────────────────────────────

        /// <summary>
        /// Chebyshev distance: max(|dx|, |dz|). Matches 8-directional movement
        /// where diagonal steps cost the same as cardinal steps.
        /// </summary>
        public static int ChebyshevDistance(Vector2Int a, Vector2Int b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        // ── A* Pathfinding (GDD Section 3.5) ───────────────────────────────

        /// <summary>
        /// Height-aware A* pathfinding. Returns the tile positions along the
        /// shortest path from <paramref name="start"/> to <paramref name="goal"/>,
        /// or an empty list if no path exists.
        /// </summary>
        /// <remarks>
        /// - Climbing +1 costs <see cref="ClimbStepCost"/>; flat/descend costs
        ///   <see cref="FlatStepCost"/>.
        /// - Occupied tiles are impassable except the goal (for attack targeting).
        /// - Corner-cutting through two diagonally adjacent impassable tiles is
        ///   blocked.
        /// - If start == goal, returns a single-element list.
        /// </remarks>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            var startTile = GetTile(start);
            var goalTile = GetTile(goal);

            if (startTile == null || goalTile == null) return new List<Vector2Int>();
            if (!goalTile.IsPassable) return new List<Vector2Int>();

            // Start == Goal edge case
            if (start == goal) return new List<Vector2Int> { start };

            // Open set keyed by F cost; ties broken by hash
            var open = new SortedSet<AStarNode>(AStarNode.Comparer);
            var closed = new HashSet<Vector2Int>();
            var bestG = new Dictionary<Vector2Int, float>();

            var startNode = new AStarNode(start, 0f, ChebyshevDistance(start, goal), null);
            open.Add(startNode);
            bestG[start] = 0f;

            while (open.Count > 0)
            {
                var current = open.Min;
                open.Remove(current);

                if (current.Position == goal)
                    return ReconstructPath(current);

                if (!closed.Add(current.Position)) continue;

                var currentTile = GetTile(current.Position);
                var neighbours = GetPassableNeighbours(currentTile, goal);

                for (int i = 0; i < neighbours.Count; i++)
                {
                    var nb = neighbours[i];
                    if (closed.Contains(nb.GridPosition)) continue;

                    int heightDelta = nb.Height - currentTile.Height;
                    float stepCost = heightDelta > 0 ? ClimbStepCost : FlatStepCost;
                    float newG = current.G + stepCost;

                    if (!bestG.TryGetValue(nb.GridPosition, out float existing) || newG < existing)
                    {
                        bestG[nb.GridPosition] = newG;
                        var node = new AStarNode(nb.GridPosition, newG,
                            ChebyshevDistance(nb.GridPosition, goal), current);
                        open.Add(node);
                    }
                }
            }

            return new List<Vector2Int>(); // No path
        }

        private static List<Vector2Int> ReconstructPath(AStarNode node)
        {
            var path = new List<Vector2Int>();
            while (node != null)
            {
                path.Add(node.Position);
                node = node.Parent;
            }
            path.Reverse();
            return path;
        }

        // ── Reachable Tiles (GDD Section 3.7) ──────────────────────────────

        /// <summary>
        /// BFS flood-fill returning all tiles reachable within
        /// <paramref name="movementRange"/> movement budget. Respects height
        /// costs, occupancy, and corner-cutting rules.
        /// </summary>
        public HashSet<TileData> GetReachableTiles(Vector2Int start, int movementRange)
        {
            var reachable = new HashSet<TileData>();
            var bestCost = new Dictionary<Vector2Int, float> { [start] = 0f };

            var origin = GetTile(start);
            if (origin == null) return reachable;

            // Dijkstra: process tiles in cost order to guarantee optimal costs
            var frontier = new SortedSet<(float cost, int id, TileData tile)>(
                Comparer<(float cost, int id, TileData tile)>.Create((a, b) =>
                {
                    int cmp = a.cost.CompareTo(b.cost);
                    return cmp != 0 ? cmp : a.id.CompareTo(b.id);
                }));

            int nextId = 0;
            frontier.Add((0f, nextId++, origin));

            while (frontier.Count > 0)
            {
                var current = frontier.Min;
                frontier.Remove(current);

                // Skip if we already found a cheaper path to this tile
                if (bestCost.TryGetValue(current.tile.GridPosition, out float known)
                    && current.cost > known)
                    continue;

                var neighbours = GetPassableNeighbours(current.tile);

                for (int i = 0; i < neighbours.Count; i++)
                {
                    var nb = neighbours[i];
                    int heightDelta = nb.Height - current.tile.Height;
                    float stepCost = heightDelta > 0 ? ClimbStepCost : FlatStepCost;
                    float newCost = current.cost + stepCost;

                    if (newCost > movementRange) continue;

                    if (!bestCost.TryGetValue(nb.GridPosition, out float prev) || newCost < prev)
                    {
                        bestCost[nb.GridPosition] = newCost;
                        reachable.Add(nb);
                        frontier.Add((newCost, nextId++, nb));
                    }
                }
            }

            return reachable;
        }

        // ── Flanking (GDD Section 3.8) ─────────────────────────────────────

        /// <summary>
        /// Determines the flanking arc of an attacker relative to a defender's
        /// facing direction. Uses dot product of the attack direction against
        /// the defender's facing vector.
        /// </summary>
        /// <param name="attackerPos">Attacker's grid position.</param>
        /// <param name="defenderPos">Defender's grid position.</param>
        /// <param name="defenderFacing">
        /// Defender's facing as a normalized grid direction (e.g., (0,1) for North).
        /// </param>
        public static FlankingType GetFlankingType(Vector2Int attackerPos, Vector2Int defenderPos,
            Vector2Int defenderFacing)
        {
            var attackDir = attackerPos - defenderPos;
            // Normalize to -1/0/1 per axis
            attackDir = new Vector2Int(
                attackDir.x == 0 ? 0 : (attackDir.x > 0 ? 1 : -1),
                attackDir.y == 0 ? 0 : (attackDir.y > 0 ? 1 : -1));

            float dot = Vector2.Dot(
                new Vector2(attackDir.x, attackDir.y),
                new Vector2(defenderFacing.x, defenderFacing.y));

            if (dot >= FlankFrontDotThreshold) return FlankingType.Front;
            if (dot <= FlankRearDotThreshold) return FlankingType.Rear;
            return FlankingType.Side;
        }

        // ── Line-of-Sight (GDD Section 3.9) ────────────────────────────────

        /// <summary>
        /// Bresenham line-of-sight check. Returns true if no tile with
        /// <see cref="TileData.BlocksLineOfSight"/> interrupts the line
        /// from <paramref name="from"/> to <paramref name="to"/>.
        /// Origin and destination tiles are not checked.
        /// </summary>
        public bool HasLineOfSight(Vector2Int from, Vector2Int to)
        {
            // Adjacent tiles always have LoS
            if (ChebyshevDistance(from, to) <= 1) return true;

            var sourceTile = GetTile(from);
            var targetTile = GetTile(to);
            if (sourceTile == null || targetTile == null) return false;

            bool checkHeightBlock = targetTile.Height > sourceTile.Height + 2;

            int x0 = from.x, z0 = from.y, x1 = to.x, z1 = to.y;
            int dx = Mathf.Abs(x1 - x0), dz = Mathf.Abs(z1 - z0);
            int sx = x0 < x1 ? 1 : -1, sz = z0 < z1 ? 1 : -1;
            int err = dx - dz;

            while (x0 != x1 || z0 != z1)
            {
                // Skip origin tile
                if (x0 != from.x || z0 != from.y)
                {
                    // Skip destination tile
                    if (x0 == x1 && z0 == z1) break;

                    var t = GetTile(x0, z0);
                    if (t == null || t.BlocksLineOfSight) return false;

                    // Height delta rule: intervening tall tiles block if target is
                    // much higher than source
                    if (checkHeightBlock && t.Height >= targetTile.Height)
                        return false;
                }

                int e2 = 2 * err;
                if (e2 > -dz) { err -= dz; x0 += sx; }
                if (e2 < dx) { err += dx; z0 += sz; }
            }

            return true;
        }

        // ── Fall Damage (GDD Section 3.4) ───────────────────────────────────

        /// <summary>
        /// Calculates fall damage for a height delta. Returns 0 if delta is
        /// below <paramref name="fallDamageMinDelta"/>.
        /// </summary>
        /// <param name="heightDelta">Absolute height difference (positive).</param>
        /// <param name="fallDamageBase">Base damage per excess level (from GameSettings).</param>
        /// <param name="fallDamageMinDelta">Minimum delta to trigger damage (from GameSettings).</param>
        public static int GetFallDamage(int heightDelta, int fallDamageBase, int fallDamageMinDelta)
        {
            if (heightDelta < fallDamageMinDelta) return 0;
            return fallDamageBase * (heightDelta - fallDamageMinDelta + 1);
        }
    }

    /// <summary>
    /// Internal A* node used during pathfinding. Not exposed publicly.
    /// </summary>
    internal class AStarNode
    {
        public Vector2Int Position { get; }
        public float G { get; }
        public float H { get; }
        public float F => G + H;
        public AStarNode Parent { get; }

        public AStarNode(Vector2Int position, float g, float h, AStarNode parent)
        {
            Position = position;
            G = g;
            H = h;
            Parent = parent;
        }

        private static int _nextId;
        private readonly int _id = Interlocked.Increment(ref _nextId);

        /// <summary>
        /// Comparer that sorts by F cost, breaking ties by unique ID to avoid
        /// SortedSet deduplication of equal-F nodes.
        /// </summary>
        public static readonly IComparer<AStarNode> Comparer =
            Comparer<AStarNode>.Create((a, b) =>
            {
                int cmp = a.F.CompareTo(b.F);
                return cmp != 0 ? cmp : a._id.CompareTo(b._id);
            });
    }
}
