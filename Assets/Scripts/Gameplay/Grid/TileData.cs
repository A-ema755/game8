using UnityEngine;
using GeneForge.Core;
using GeneForge.Creatures;

namespace GeneForge.Grid
{
    /// <summary>
    /// Data for a single grid tile. Position, height, terrain, and LoS properties
    /// are immutable after construction. Occupant is mutable during combat.
    /// </summary>
    public class TileData
    {
        /// <summary>(x, z) position in grid space.</summary>
        public Vector2Int GridPosition { get; }

        /// <summary>Height level 0–4, clamped on construction.</summary>
        public int Height { get; }

        /// <summary>Terrain type for synergy/hazard interactions.</summary>
        public TerrainType Terrain { get; }

        /// <summary>False for walls, pits, and impassable terrain.</summary>
        public bool IsPassable { get; }

        /// <summary>True for walls and tall cover that block line-of-sight.</summary>
        public bool BlocksLineOfSight { get; }

        /// <summary>True if this tile provides cover (reduces Energy move damage by 0.5×).</summary>
        public bool ProvidesCover { get; }

        /// <summary>The creature occupying this tile, or null if empty.</summary>
        public CreatureInstance Occupant { get; set; }

        /// <summary>True when a creature occupies this tile.</summary>
        public bool IsOccupied => Occupant != null;

        /// <summary>
        /// World-space position for rendering.
        /// x = gridX * TileSizeWorld, y = height * TileHeightStep, z = gridZ * TileSizeWorld.
        /// </summary>
        public Vector3 WorldPosition => new Vector3(
            GridPosition.x * GridSystem.TileSizeWorld,
            Height * GridSystem.TileHeightStep,
            GridPosition.y * GridSystem.TileSizeWorld);

        /// <summary>
        /// Creates a new tile with validated parameters.
        /// </summary>
        /// <param name="gridPosition">Grid (x, z) coordinate.</param>
        /// <param name="height">Height level, clamped to 0–MaxHeight.</param>
        /// <param name="terrain">Terrain type for synergy effects.</param>
        /// <param name="isPassable">Whether creatures can traverse this tile.</param>
        /// <param name="blocksLoS">Whether this tile blocks line-of-sight.</param>
        /// <param name="providesCover">Whether this tile provides cover (reduces Energy damage by 0.5×).</param>
        public TileData(Vector2Int gridPosition, int height, TerrainType terrain,
                        bool isPassable = true, bool blocksLoS = false,
                        bool providesCover = false)
        {
            GridPosition = gridPosition;
            Height = Mathf.Clamp(height, 0, GridSystem.MaxHeight);
            Terrain = terrain;
            IsPassable = isPassable;
            BlocksLineOfSight = blocksLoS;
            ProvidesCover = providesCover;
        }
    }
}
