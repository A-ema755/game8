using System;
using System.Collections.Generic;
using GeneForge.Combat;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.UI
{
    /// <summary>
    /// Pure C# class that computes tile highlight sets for the Combat UI.
    /// Returns HashSet&lt;Vector2Int&gt; tile positions — does NOT touch renderers.
    /// A MonoBehaviour caller applies the visual tints to grid tile materials.
    ///
    /// Implements design/ux/combat-ui-ux-spec.md §4.4.
    /// Form-specific range: Physical=melee no LoS, Energy=range+LoS, Bio=range ignoring LoS.
    /// </summary>
    public class TileHighlightController
    {
        private readonly GridSystem _grid;
        private readonly Dictionary<(Vector2Int, int), HashSet<Vector2Int>> _reachableCache = new();
        private float _lastMoveHoverTime;

        /// <summary>Creates a TileHighlightController for the given grid.</summary>
        public TileHighlightController(GridSystem grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        /// <summary>
        /// Get movement tiles reachable from a position within the given range.
        /// Results are cached per (position, range) until ClearCache is called.
        /// </summary>
        public HashSet<Vector2Int> GetMovementTiles(Vector2Int position, int movementRange)
        {
            var key = (position, movementRange);
            if (_reachableCache.TryGetValue(key, out var cached))
                return cached;

            var reachableTileData = _grid.GetReachableTiles(position, movementRange);
            var result = new HashSet<Vector2Int>();
            foreach (var tile in reachableTileData)
                result.Add(tile.GridPosition);

            _reachableCache[key] = result;
            return result;
        }

        /// <summary>
        /// Get attack tiles for a move from a creature position.
        /// Form-specific: Physical = Chebyshev range, no LoS.
        /// Energy = Chebyshev range + LoS filter.
        /// Bio = Chebyshev range, ignoring LoS.
        /// Status (Form.None) = same as Physical range.
        /// </summary>
        /// <param name="move">The move config.</param>
        /// <param name="creaturePos">The creature's grid position.</param>
        /// <param name="blockedTiles">
        /// Out parameter: tiles in range but blocked by LoS (Energy only).
        /// Empty for Physical/Bio. Caller can render these at reduced opacity.
        /// </param>
        public HashSet<Vector2Int> GetAttackTiles(
            MoveConfig move,
            Vector2Int creaturePos,
            out HashSet<Vector2Int> blockedTiles)
        {
            blockedTiles = new HashSet<Vector2Int>();
            var result = new HashSet<Vector2Int>();

            if (move == null) return result;

            int range = move.Range;

            for (int x = 0; x < _grid.Width; x++)
            {
                for (int z = 0; z < _grid.Depth; z++)
                {
                    var pos = new Vector2Int(x, z);
                    if (pos == creaturePos) continue;

                    int dist = GridSystem.ChebyshevDistance(creaturePos, pos);
                    if (dist > range) continue;

                    var tile = _grid.GetTile(pos);
                    if (tile == null || !tile.IsPassable) continue;

                    // Energy moves require line of sight
                    if (move.Form == DamageForm.Energy)
                    {
                        if (_grid.HasLineOfSight(creaturePos, pos))
                            result.Add(pos);
                        else
                            blockedTiles.Add(pos);
                    }
                    else
                    {
                        // Physical, Bio, and Status: no LoS check
                        result.Add(pos);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Get capture-valid tiles within range containing non-fainted enemy creatures.
        /// </summary>
        public HashSet<Vector2Int> GetCaptureTiles(
            Vector2Int position,
            int captureRange,
            IReadOnlyList<CreatureInstance> enemies)
        {
            var result = new HashSet<Vector2Int>();

            if (enemies == null) return result;

            foreach (var enemy in enemies)
            {
                if (enemy.IsFainted) continue;

                int dist = GridSystem.ChebyshevDistance(position, enemy.GridPosition);
                if (dist <= captureRange)
                    result.Add(enemy.GridPosition);
            }

            return result;
        }

        /// <summary>
        /// Get terrain synergy tiles where the terrain type matches the creature's type.
        /// </summary>
        public HashSet<Vector2Int> GetSynergyTiles(CreatureType creatureType)
        {
            var result = new HashSet<Vector2Int>();

            for (int x = 0; x < _grid.Width; x++)
            {
                for (int z = 0; z < _grid.Depth; z++)
                {
                    var tile = _grid.GetTile(x, z);
                    if (tile != null &&
                        TypeChart.TerrainMatchesCreatureType(tile.Terrain, creatureType))
                    {
                        result.Add(new Vector2Int(x, z));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Clear all cached tile sets. Call on phase change.
        /// </summary>
        public void ClearCache()
        {
            _reachableCache.Clear();
        }

        /// <summary>
        /// Check if enough time has elapsed since last move hover for debounce (50ms).
        /// Call before computing attack tiles on move hover.
        /// Returns true if the caller should proceed with the computation.
        /// </summary>
        public bool ShouldComputeOnHover()
        {
            float now = Time.realtimeSinceStartup;
            if ((now - _lastMoveHoverTime) < 0.05f)
                return false;

            _lastMoveHoverTime = now;
            return true;
        }
    }
}
