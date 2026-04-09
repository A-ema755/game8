using System.Collections.Generic;
using GeneForge.Combat;
using GeneForge.Core;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.UI
{
    /// <summary>
    /// Placeholder visualizer for the combat grid. Renders each tile as a
    /// colored cube scaled by terrain type and height. Intended for playtesting
    /// only — not production visuals.
    ///
    /// Implements minimal visual feedback described in the combat playtest brief.
    /// Subscribe to CombatController.PhaseChanged to spawn tiles when combat starts.
    /// </summary>
    public class GridVisualizer : MonoBehaviour
    {
        // ── Serialized Fields ─────────────────────────────────────────────

        [SerializeField] private CombatController _combatController;

        // ── Private State ─────────────────────────────────────────────────

        private readonly Dictionary<Vector2Int, GameObject> _tileObjects = new();
        private readonly Dictionary<Vector2Int, Color> _originalColors = new();
        private GameObject _gridRoot;

        // ── Unity Lifecycle ───────────────────────────────────────────────

        private void OnEnable()
        {
            if (_combatController != null)
                _combatController.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (_combatController != null)
                _combatController.PhaseChanged -= OnPhaseChanged;
        }

        // ── Phase Handling ────────────────────────────────────────────────

        private void OnPhaseChanged(CombatUIPhase phase)
        {
            // Spawn once when combat leaves Inactive
            if (phase != CombatUIPhase.Inactive && _tileObjects.Count == 0)
                SpawnGrid();
        }

        // ── Grid Spawning ─────────────────────────────────────────────────

        private void SpawnGrid()
        {
            if (_combatController == null || _combatController.Grid == null)
            {
                Debug.LogWarning("[GridVisualizer] No GridSystem available.");
                return;
            }

            _gridRoot = new GameObject("Grid");
            _gridRoot.transform.SetParent(transform);

            var grid = _combatController.Grid;

            for (int x = 0; x < grid.Width; x++)
            {
                for (int z = 0; z < grid.Depth; z++)
                {
                    var tile = grid.GetTile(x, z);
                    if (tile == null) continue;

                    SpawnTile(tile);
                }
            }
        }

        private void SpawnTile(TileData tile)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Tile_{tile.GridPosition.x}_{tile.GridPosition.y}";
            go.transform.SetParent(_gridRoot.transform);

            // Position: x = grid x, z = grid z (y.x maps to world z via Vector2Int.y)
            float worldX = tile.GridPosition.x;
            float worldZ = tile.GridPosition.y;
            float worldY = tile.Height * 0.5f;
            go.transform.position = new Vector3(worldX, worldY, worldZ);

            // Scale: thin slab, taller for elevated tiles
            float slabHeight = 0.2f + tile.Height * 0.5f;
            go.transform.localScale = new Vector3(0.9f, slabHeight, 0.9f);

            // Color
            Color tileColor = ResolveTileColor(tile);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = tileColor;
            go.GetComponent<Renderer>().material = mat;

            _tileObjects[tile.GridPosition] = go;
            _originalColors[tile.GridPosition] = tileColor;
        }

        private static Color ResolveTileColor(TileData tile)
        {
            // Impassable tiles always render dark
            if (!tile.IsPassable)
                return new Color(0.2f, 0.2f, 0.2f);

            return tile.Terrain switch
            {
                TerrainType.Thermal   => new Color(0.9f, 0.4f, 0.1f),   // Scorched — orange
                TerrainType.Aqua      => new Color(0.2f, 0.4f, 0.9f),   // Submerged — blue
                TerrainType.Cryo      => new Color(0.6f, 0.8f, 1.0f),   // Frozen — light blue
                TerrainType.Organic   => new Color(0.3f, 0.7f, 0.3f),   // Verdant — green
                TerrainType.Hazard    => new Color(0.8f, 0.2f, 0.2f),   // Hazard — red
                TerrainType.Neutral   => new Color(0.7f, 0.7f, 0.7f),   // Neutral — gray
                _                     => Color.white
            };
        }

        // ── Highlight API ─────────────────────────────────────────────────

        /// <summary>
        /// Tints the given tile positions with the specified color.
        /// Call ClearHighlights() to restore original colors.
        /// </summary>
        public void HighlightTiles(HashSet<Vector2Int> tiles, Color color)
        {
            if (tiles == null) return;

            foreach (var pos in tiles)
            {
                if (!_tileObjects.TryGetValue(pos, out var go)) continue;

                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.color = color;
            }
        }

        /// <summary>
        /// Restores all tiles to their original terrain-based colors.
        /// </summary>
        public void ClearHighlights()
        {
            foreach (var kvp in _tileObjects)
            {
                var renderer = kvp.Value.GetComponent<Renderer>();
                if (renderer == null) continue;

                if (_originalColors.TryGetValue(kvp.Key, out var original))
                    renderer.material.color = original;
            }
        }
    }
}
