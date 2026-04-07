using System.Collections.Generic;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Gameplay;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.Combat
{
    /// <summary>
    /// Initializes combat encounters from EncounterConfig ScriptableObjects.
    /// Builds the grid, spawns creatures, and produces a BattleContext.
    /// Pure C# — no MonoBehaviour (ADR-002).
    /// Implements GDD: design/gdd/encounter-system.md §3.3.
    /// </summary>
    public class EncounterManager : IEncounterManager
    {
        const string Tag = "[EncounterManager]";

        private readonly System.Func<string, CreatureConfig> _creatureLookup;

        /// <summary>
        /// Create an EncounterManager with default ConfigLoader creature lookup.
        /// </summary>
        public EncounterManager() : this(ConfigLoader.GetCreature) { }

        /// <summary>
        /// Create an EncounterManager with an injected creature lookup function.
        /// Enables testing without ConfigLoader static state.
        /// </summary>
        public EncounterManager(System.Func<string, CreatureConfig> creatureLookup)
        {
            _creatureLookup = creatureLookup;
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Initialize a battle from config and player party.
        /// Calls ValidateConfig internally and applies fallbacks on errors.
        /// Returns a fully populated BattleContext ready for TurnManager.
        /// </summary>
        public BattleContext InitializeEncounter(EncounterConfig config, PartyState playerParty)
        {
            var errors = ValidateConfig(config);
            foreach (var error in errors)
                Debug.LogError($"{Tag} {error}");

            var grid = BuildGrid(config);
            var enemyCreatures = SpawnEnemies(config, grid);
            var playerCreatures = PlacePlayerCreatures(playerParty, config.PlayerStartTiles, grid);

            return new BattleContext(
                config,
                grid,
                playerCreatures,
                enemyCreatures,
                config.EncounterType,
                config.CaptureAllowed,
                config.RetreatAllowed);
        }

        /// <summary>
        /// Validate config integrity. Returns list of error strings.
        /// Empty list means config is valid.
        /// </summary>
        public List<string> ValidateConfig(EncounterConfig config)
        {
            var errors = new List<string>();

            if (config == null)
            {
                errors.Add("EncounterConfig is null.");
                return errors;
            }

            int width = config.GridDimensions.x;
            int depth = config.GridDimensions.y;

            // Grid dimension bounds
            if (width < GridSystem.MinGridWidth || width > GridSystem.MaxGridWidth)
                errors.Add($"Grid width {width} outside allowed range [{GridSystem.MinGridWidth}, {GridSystem.MaxGridWidth}].");
            if (depth < GridSystem.MinGridDepth || depth > GridSystem.MaxGridDepth)
                errors.Add($"Grid depth {depth} outside allowed range [{GridSystem.MinGridDepth}, {GridSystem.MaxGridDepth}].");

            int expectedSize = width * depth;

            // Flat array lengths
            if (config.HeightMapFlat.Count != expectedSize)
                errors.Add($"HeightMapFlat length {config.HeightMapFlat.Count} != expected {expectedSize} (width * depth).");
            if (config.TileLayoutFlat.Count != expectedSize)
                errors.Add($"TileLayoutFlat length {config.TileLayoutFlat.Count} != expected {expectedSize} (width * depth).");

            // Player start tiles in bounds
            for (int i = 0; i < config.PlayerStartTiles.Length; i++)
            {
                var tile = config.PlayerStartTiles[i];
                if (tile.x < 0 || tile.x >= width || tile.y < 0 || tile.y >= depth)
                    errors.Add($"PlayerStartTile[{i}] ({tile.x}, {tile.y}) is out of grid bounds.");
            }

            // Enemy entries
            if (config.Enemies.Count == 0)
                errors.Add("Enemies list is empty.");

            for (int i = 0; i < config.Enemies.Count; i++)
            {
                var entry = config.Enemies[i];

                if (string.IsNullOrEmpty(entry.SpeciesId))
                {
                    errors.Add($"Enemy[{i}] has empty speciesId.");
                    continue;
                }

                if (_creatureLookup(entry.SpeciesId) == null)
                    errors.Add($"Enemy[{i}] speciesId '{entry.SpeciesId}' not found in creature database.");

                var spawn = entry.SpawnTile;
                if (spawn.x < 0 || spawn.x >= width || spawn.y < 0 || spawn.y >= depth)
                    errors.Add($"Enemy[{i}] spawnTile ({spawn.x}, {spawn.y}) is out of grid bounds.");
            }

            return errors;
        }

        // ── Private Helpers ─────────────────────────────────────────────

        /// <summary>
        /// Build a GridSystem from config dimensions and populate tiles
        /// from the flat height and terrain arrays.
        /// </summary>
        private GridSystem BuildGrid(EncounterConfig config)
        {
            int width = Mathf.Clamp(config.GridDimensions.x, GridSystem.MinGridWidth, GridSystem.MaxGridWidth);
            int depth = Mathf.Clamp(config.GridDimensions.y, GridSystem.MinGridDepth, GridSystem.MaxGridDepth);

            var grid = new GridSystem(width, depth);
            int expectedSize = width * depth;

            bool hasValidHeightMap = config.HeightMapFlat.Count == expectedSize;
            bool hasValidTileLayout = config.TileLayoutFlat.Count == expectedSize;

            for (int z = 0; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = z * width + x;
                    int height = hasValidHeightMap ? config.HeightMapFlat[index] : 0;
                    var terrain = hasValidTileLayout ? config.TileLayoutFlat[index] : TerrainType.Neutral;

                    var (isPassable, blocksLoS, providesCover) = GetTileProperties(terrain, height);

                    var tile = new TileData(
                        new Vector2Int(x, z),
                        height,
                        terrain,
                        isPassable,
                        blocksLoS,
                        providesCover);

                    grid.SetTile(tile);
                }
            }

            return grid;
        }

        /// <summary>
        /// Spawn enemy creatures from config entries onto the grid.
        /// Skips entries with invalid speciesId (logs error, continues).
        /// </summary>
        private List<CreatureInstance> SpawnEnemies(EncounterConfig config, GridSystem grid)
        {
            var enemies = new List<CreatureInstance>();

            for (int i = 0; i < config.Enemies.Count; i++)
            {
                var entry = config.Enemies[i];
                var creatureConfig = _creatureLookup(entry.SpeciesId);

                if (creatureConfig == null)
                {
                    Debug.LogError($"{Tag} Skipping enemy[{i}]: species '{entry.SpeciesId}' not found.");
                    continue;
                }

                var creature = CreatureInstance.Create(creatureConfig, entry.Level);
                var spawnTile = GetValidTile(entry.SpawnTile, grid);

                creature.SetGridPosition(spawnTile);

                var tileData = grid.GetTile(spawnTile);
                if (tileData != null)
                {
                    if (tileData.Occupant != null)
                        Debug.LogWarning($"{Tag} Enemy[{i}] tile ({spawnTile.x}, {spawnTile.y}) already occupied. Overwriting.");
                    tileData.Occupant = creature;
                }

                enemies.Add(creature);
            }

            return enemies;
        }

        /// <summary>
        /// Place non-fainted player creatures at start tiles in party-slot order.
        /// Creatures beyond available start tiles use grid center as fallback.
        /// </summary>
        private List<CreatureInstance> PlacePlayerCreatures(
            PartyState party, Vector2Int[] startTiles, GridSystem grid)
        {
            var placed = new List<CreatureInstance>();
            int tileIndex = 0;

            foreach (var creature in party.ActiveParty)
            {
                if (creature.IsFainted) continue;

                Vector2Int targetTile;
                if (tileIndex < startTiles.Length)
                    targetTile = GetValidTile(startTiles[tileIndex], grid);
                else
                    targetTile = GetGridCenter(grid);

                creature.SetGridPosition(targetTile);

                var tileData = grid.GetTile(targetTile);
                if (tileData != null)
                {
                    if (tileData.Occupant != null)
                        Debug.LogWarning($"{Tag} Player tile ({targetTile.x}, {targetTile.y}) already occupied. Overwriting.");
                    tileData.Occupant = creature;
                }

                placed.Add(creature);
                tileIndex++;
            }

            return placed;
        }

        /// <summary>
        /// Maps TerrainType and height to TileData constructor properties.
        /// Height at MaxHeight (4) is impassable cliff.
        /// MVP: no terrain provides cover.
        /// </summary>
        private static (bool isPassable, bool blocksLoS, bool providesCover) GetTileProperties(
            TerrainType terrain, int height)
        {
            // Cliff: impassable at max height
            if (height >= GridSystem.MaxHeight)
                return (false, true, false);

            // All terrain types are passable at MVP
            // Hazard/Difficult tiles have gameplay effects handled elsewhere
            return (true, false, false);
        }

        /// <summary>
        /// Returns the given tile position if it's in bounds, otherwise falls back to grid center.
        /// </summary>
        private static Vector2Int GetValidTile(Vector2Int tile, GridSystem grid)
        {
            if (tile.x >= 0 && tile.x < grid.Width && tile.y >= 0 && tile.y < grid.Depth)
                return tile;

            Debug.LogWarning($"[EncounterManager] Tile ({tile.x}, {tile.y}) out of bounds. Falling back to grid center.");
            return GetGridCenter(grid);
        }

        /// <summary>Returns the center tile of the grid.</summary>
        private static Vector2Int GetGridCenter(GridSystem grid) =>
            new Vector2Int(grid.Width / 2, grid.Depth / 2);
    }
}
