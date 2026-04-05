# Grid / Tile System

## 1. Overview

The Grid / Tile System defines the isometric 3D combat arena as a rectangular grid of `TileData` objects, each occupying a discrete (x, z) cell at one of five height levels (0–4). The system handles tile initialization, occupancy tracking, 8-directional movement with height-aware A* pathfinding, Chebyshev distance calculation, line-of-sight queries, and flanking detection based on creature facing direction. All combat positioning logic is routed through this system; no other system stores spatial state.

## 2. Player Fantasy

Battles feel like chess on a living landscape. The player scans the board, spots high ground, maneuvers around cover, and catches enemies from behind. Every step matters — not because movement costs actions, but because position directly changes damage dealt, damage received, and which moves are even available. Climbing a cliff to get the drop on a predator should feel like a decisive, satisfying tactical choice.

## 3. Detailed Rules

### 3.1 Grid Dimensions

- Combat grids are rectangular: minimum 6×6, maximum 16×12 (tunable per encounter config)
- Grid coordinates are `Vector2Int(x, z)` — y is computed from height level × `TileHeightStep`
- Tile (0,0) is bottom-left corner from the player's isometric viewpoint

### 3.2 TileData

```csharp
namespace GeneForge.Grid
{
    /// <summary>
    /// Immutable-at-runtime data for a single grid tile.
    /// Occupant is mutable during combat.
    /// </summary>
    public class TileData
    {
        public Vector2Int GridPosition { get; }   // (x, z) in grid space
        public int Height { get; }                // 0-4 height levels
        public TerrainType Terrain { get; }       // Type synergy / hazard
        public bool IsPassable { get; }           // False for walls, pits, impassable terrain
        public bool BlocksLineOfSight { get; }    // True for walls, tall cover

        // Mutable during combat
        public CreatureInstance Occupant { get; set; }  // null if unoccupied
        public bool IsOccupied => Occupant != null;

        // Computed world position for rendering
        public Vector3 WorldPosition =>
            new Vector3(GridPosition.x * GridSystem.TileSizeWorld,
                        Height * GridSystem.TileHeightStep,
                        GridPosition.y * GridSystem.TileSizeWorld);

        public TileData(Vector2Int gridPosition, int height, TerrainType terrain,
                        bool isPassable = true, bool blocksLoS = false)
        {
            GridPosition = gridPosition;
            Height = Mathf.Clamp(height, 0, GridSystem.MaxHeight);
            Terrain = terrain;
            IsPassable = isPassable;
            BlocksLineOfSight = blocksLoS;
        }
    }
}
```

### 3.3 GridSystem

```csharp
namespace GeneForge.Grid
{
    /// <summary>
    /// Central grid authority. Owns the TileData grid, pathfinding, and spatial queries.
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class GridSystem
    {
        // ── Constants ──────────────────────────────────────────────────────
        public const int MaxHeight = 4;
        public const float TileSizeWorld = 1.0f;   // metres per cell
        public const float TileHeightStep = 0.5f;  // metres per height level
        public const int MaxHeightDeltaPassable = 1; // can only step ±1 height

        // ── State ──────────────────────────────────────────────────────────
        private readonly TileData[,] _grid;
        public int Width { get; }
        public int Depth { get; }

        public GridSystem(int width, int depth)
        {
            Width = width;
            Depth = depth;
            _grid = new TileData[width, depth];
        }

        /// <summary>Register a tile at its grid position.</summary>
        public void SetTile(TileData tile)
        {
            _grid[tile.GridPosition.x, tile.GridPosition.y] = tile;
        }

        /// <summary>Returns null if out of bounds.</summary>
        public TileData GetTile(int x, int z)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth) return null;
            return _grid[x, z];
        }

        public TileData GetTile(Vector2Int pos) => GetTile(pos.x, pos.y);

        /// <summary>
        /// 8 cardinal + diagonal neighbours. Excludes out-of-bounds and null tiles.
        /// </summary>
        public List<TileData> GetNeighbours(Vector2Int pos)
        {
            var result = new List<TileData>(8);
            for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;
                var tile = GetTile(pos.x + dx, pos.y + dz);
                if (tile != null) result.Add(tile);
            }
            return result;
        }
    }
}
```

### 3.4 Height Rules

| Transition | Movement Cost | Allowed? |
|-----------|--------------|---------|
| Same height | 1.0 | Yes |
| Climb +1 level | 1.5 | Yes |
| Climb +2 or more levels | — | No (impassable) |
| Descend any levels | 1.0 | Yes (free) |
| Descend 3+ levels | 1.0 (landing) | Yes, but triggers fall check |

Fall damage: descending 3+ levels in a single step deals `fallDamageBase * (heightDelta - 2)` damage to the mover. This applies to knockback abilities that push creatures off ledges.

### 3.5 A* Pathfinding

```csharp
namespace GeneForge.Grid
{
    public static class Pathfinder
    {
        /// <summary>
        /// A* with height-aware movement costs.
        /// Returns null if no path exists.
        /// </summary>
        public static List<TileData> FindPath(
            GridSystem grid,
            TileData start,
            TileData goal,
            CreatureInstance mover)
        {
            if (goal == null || !goal.IsPassable) return null;
            if (goal.IsOccupied && goal.Occupant != mover) return null;

            var open = new SortedSet<AStarNode>(Comparer<AStarNode>.Create(
                (a, b) => a.F != b.F ? a.F.CompareTo(b.F) : a.GetHashCode().CompareTo(b.GetHashCode())));
            var closed = new HashSet<Vector2Int>();
            var gCost = new Dictionary<Vector2Int, float>();
            var cameFrom = new Dictionary<Vector2Int, AStarNode>();

            var startNode = new AStarNode(start, 0f, Heuristic(start, goal), null);
            open.Add(startNode);
            gCost[start.GridPosition] = 0f;

            while (open.Count > 0)
            {
                var current = open.Min;
                open.Remove(current);

                if (current.Tile.GridPosition == goal.GridPosition)
                    return ReconstructPath(current);

                if (closed.Contains(current.Tile.GridPosition)) continue;
                closed.Add(current.Tile.GridPosition);

                foreach (var neighbour in grid.GetNeighbours(current.Tile.GridPosition))
                {
                    if (!neighbour.IsPassable) continue;
                    if (closed.Contains(neighbour.GridPosition)) continue;
                    if (neighbour.IsOccupied && neighbour != goal) continue;

                    int heightDelta = neighbour.Height - current.Tile.Height;
                    if (heightDelta > GridSystem.MaxHeightDeltaPassable) continue; // impassable cliff

                    float stepCost = heightDelta > 0 ? 1.5f : 1.0f;
                    float newG = current.G + stepCost;

                    if (!gCost.TryGetValue(neighbour.GridPosition, out float existing)
                        || newG < existing)
                    {
                        gCost[neighbour.GridPosition] = newG;
                        var node = new AStarNode(neighbour, newG, Heuristic(neighbour, goal), current);
                        open.Add(node);
                        cameFrom[neighbour.GridPosition] = node;
                    }
                }
            }
            return null; // no path
        }

        static float Heuristic(TileData a, TileData b)
            => ChebyshevDistance(a.GridPosition, b.GridPosition);

        static List<TileData> ReconstructPath(AStarNode node)
        {
            var path = new List<TileData>();
            while (node != null) { path.Add(node.Tile); node = node.Parent; }
            path.Reverse();
            return path;
        }
    }

    public class AStarNode
    {
        public TileData Tile { get; }
        public float G { get; }   // cost from start
        public float H { get; }   // heuristic to goal
        public float F => G + H;
        public AStarNode Parent { get; }

        public AStarNode(TileData tile, float g, float h, AStarNode parent)
        {
            Tile = tile; G = g; H = h; Parent = parent;
        }
    }
}
```

### 3.6 Chebyshev Distance

Chebyshev distance treats diagonal moves as cost 1, matching the 8-directional movement model.

```csharp
public static int ChebyshevDistance(Vector2Int a, Vector2Int b)
    => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
```

Used for: move range checks, threat/aggro proximity, AoE targeting radius, combo move adjacency check.

### 3.7 Reachable Tiles

```csharp
/// <summary>
/// BFS flood-fill returning all tiles reachable within moveRange steps.
/// Respects height rules and occupancy.
/// </summary>
public static HashSet<TileData> GetReachableTiles(
    GridSystem grid, TileData origin, int moveRange, CreatureInstance mover)
{
    var reachable = new HashSet<TileData>();
    var frontier = new Queue<(TileData tile, float cost)>();
    frontier.Enqueue((origin, 0f));
    var visited = new Dictionary<Vector2Int, float> { [origin.GridPosition] = 0f };

    while (frontier.Count > 0)
    {
        var (current, cost) = frontier.Dequeue();
        foreach (var neighbour in grid.GetNeighbours(current.GridPosition))
        {
            if (!neighbour.IsPassable) continue;
            if (neighbour.IsOccupied && neighbour.Occupant != mover) continue;
            int hd = neighbour.Height - current.Height;
            if (hd > GridSystem.MaxHeightDeltaPassable) continue;
            float stepCost = hd > 0 ? 1.5f : 1.0f;
            float newCost = cost + stepCost;
            if (newCost <= moveRange &&
                (!visited.TryGetValue(neighbour.GridPosition, out float prev) || newCost < prev))
            {
                visited[neighbour.GridPosition] = newCost;
                reachable.Add(neighbour);
                frontier.Enqueue((neighbour, newCost));
            }
        }
    }
    return reachable;
}
```

### 3.8 Flanking

A creature is flanked when it is attacked from a direction outside its forward facing arc.

- Each creature has a `FacingDirection` (one of 8 cardinal/diagonal directions stored as `Vector2Int`)
- Forward arc = the 3 tiles in the facing direction (front-left, front, front-right)
- Rear arc = the 3 tiles opposite (rear-left, rear, rear-right)
- Side arc = the 2 remaining tiles (left, right)

```csharp
public enum FlankingType { Front, Side, Rear }

public static FlankingType GetFlankingType(TileData attacker, TileData defender,
    Vector2Int defenderFacing)
{
    var attackDir = attacker.GridPosition - defender.GridPosition;
    // Normalize to -1/0/1 per axis
    attackDir = new Vector2Int(
        attackDir.x == 0 ? 0 : (attackDir.x > 0 ? 1 : -1),
        attackDir.y == 0 ? 0 : (attackDir.y > 0 ? 1 : -1));

    // Dot product against facing to determine arc
    float dot = Vector2.Dot((Vector2)attackDir, (Vector2)defenderFacing);
    if (dot >= 0.5f) return FlankingType.Front;
    if (dot <= -0.5f) return FlankingType.Rear;
    return FlankingType.Side;
}
```

Flanking modifiers are applied in the Damage & Health System:
- Front: no modifier
- Side: +10% damage to attacker
- Rear: +25% damage to attacker

### 3.9 Line-of-Sight

```csharp
/// <summary>
/// Bresenham line-of-sight check. Returns true if no blocking tile interrupts
/// the line from origin to target.
/// </summary>
public static bool HasLineOfSight(GridSystem grid, Vector2Int from, Vector2Int to)
{
    int x0 = from.x, z0 = from.y, x1 = to.x, z1 = to.y;
    int dx = Mathf.Abs(x1 - x0), dz = Mathf.Abs(z1 - z0);
    int sx = x0 < x1 ? 1 : -1, sz = z0 < z1 ? 1 : -1;
    int err = dx - dz;

    while (x0 != x1 || z0 != z1)
    {
        if (x0 != from.x || z0 != from.y) // skip origin
        {
            var t = grid.GetTile(x0, z0);
            if (t == null || t.BlocksLineOfSight) return false;
        }
        int e2 = 2 * err;
        if (e2 > -dz) { err -= dz; x0 += sx; }
        if (e2 < dx) { err += dx; z0 += sz; }
    }
    return true;
}
```

## 4. Formulas

| Formula | Expression |
|---------|-----------|
| Chebyshev Distance | `max(|Δx|, |Δz|)` |
| A* step cost (climbing) | `1.5` |
| A* step cost (flat/descending) | `1.0` |
| Height advantage (damage bonus) | `+0.1x per height level above target` (applied in DamageSystem) |
| Fall damage | `fallDamageBase × (heightDelta − 2)` for Δheight ≥ 3 |
| Flank (side) damage multiplier | `1.1` |
| Flank (rear) damage multiplier | `1.25` |

## 5. Edge Cases

| Situation | Behavior |
|-----------|----------|
| Path requested to occupied tile | Returns null unless occupant is the mover |
| Creature pushed off grid edge by knockback | Creature is placed on nearest valid tile; no fall damage |
| Height delta = exactly 2 | Impassable — wall of height 2 cannot be climbed in one step |
| Diagonal movement through a corner (two diagonally adjacent impassable tiles) | Corner-cutting not allowed; path must go around |
| Two creatures request move to same tile in same phase | Turn manager resolves by initiative order; second mover reroutes |
| Tile destroyed mid-combat (terrain alteration) | Occupant moves to nearest valid tile; IsPassable set to false |
| Grid size 0 or negative | GridSystem constructor throws `ArgumentException` |
| Start == Goal in A* | Returns single-node path containing start |

## 6. Dependencies

| Dependency | Direction | Notes |
|------------|-----------|-------|
| `Enums.cs` (TerrainType) | Inbound | TerrainType used in TileData |
| `CreatureInstance` | Inbound | Occupant reference on TileData |
| Damage & Health System | Outbound | Consumes height advantage and flanking multipliers |
| Threat / Aggro System | Outbound | Uses ChebyshevDistance for proximity scoring |
| Turn Manager | Outbound | Queries reachable tiles per creature action |
| Combat UI | Outbound | Reads grid for tile rendering and highlighting |

## 7. Tuning Knobs

| Parameter | Location | Default | Notes |
|-----------|----------|---------|-------|
| `MaxHeight` | `GridSystem` const | `4` | Height levels 0–4 |
| `TileSizeWorld` | `GridSystem` const | `1.0f` | Metres per grid cell |
| `TileHeightStep` | `GridSystem` const | `0.5f` | World height per level |
| `MaxHeightDeltaPassable` | `GridSystem` const | `1` | Max climb per step |
| `FallDamageBase` | `GameSettings` SO | `10` | Base damage per excess height level |
| `FallDamageMinDelta` | `GameSettings` SO | `3` | Min height delta to trigger fall damage |
| `FlankSideMultiplier` | `GameSettings` SO | `1.1f` | Side attack bonus |
| `FlankRearMultiplier` | `GameSettings` SO | `1.25f` | Rear attack bonus |
| `MinGridWidth` | `GridSystem` const | `6` | Minimum combat grid width |
| `MinGridDepth` | `GridSystem` const | `6` | Minimum combat grid depth |

## 8. Acceptance Criteria

- [ ] `TileData` stores gridPosition, height (clamped 0–4), terrain, occupant correctly
- [ ] A* finds valid path on flat 6×6 grid from corner to corner
- [ ] A* returns null when goal tile is occupied by a different creature
- [ ] A* correctly assigns cost 1.5 for uphill steps and 1.0 for flat/downhill
- [ ] Height delta of 2+ between adjacent tiles makes the step impassable in A*
- [ ] `GetReachableTiles` with moveRange=3 returns correct flood-fill set including height costs
- [ ] `ChebyshevDistance((0,0),(3,3))` returns 3
- [ ] `ChebyshevDistance((0,0),(3,1))` returns 3
- [ ] Flanking: attacker directly behind defender returns `FlankingType.Rear`
- [ ] Flanking: attacker directly in front returns `FlankingType.Front`
- [ ] Line-of-sight blocked by `BlocksLineOfSight` tile returns false
- [ ] Line-of-sight with clear path returns true
- [ ] Fall damage triggers when knockback causes height delta ≥ 3
- [ ] EditMode tests pass for all pathfinding and distance functions
