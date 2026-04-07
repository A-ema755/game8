using System.Collections.Generic;
using UnityEngine;

namespace GeneForge.Core
{
    /// <summary>
    /// Config asset defining a single combat encounter.
    /// All encounter scenarios are spawned from these ScriptableObjects.
    /// Implements GDD: design/gdd/encounter-system.md.
    /// </summary>
    [CreateAssetMenu(menuName = "GeneForge/Encounter Config", fileName = "NewEncounterConfig")]
    public class EncounterConfig : ConfigBase
    {
        // ── Core ────────────────────────────────────────────────────────
        [SerializeField] EncounterType encounterType;
        [SerializeField] Vector2Int gridDimensions = new Vector2Int(8, 6);

        /// <summary>Flat height array. Index = z * GridDimensions.x + x.</summary>
        [SerializeField] int[] heightMapFlat = System.Array.Empty<int>();

        /// <summary>Flat terrain array. Index = z * GridDimensions.x + x.</summary>
        [SerializeField] TerrainType[] tileLayoutFlat = System.Array.Empty<TerrainType>();

        [SerializeField] Vector2Int[] playerStartTiles = System.Array.Empty<Vector2Int>();
        [SerializeField] List<EncounterCreatureEntry> enemies = new();
        [SerializeField] bool captureAllowed = true;
        [SerializeField] bool retreatAllowed = true;
        [SerializeField] int rpBase = 10;

        // ── Post-MVP stubs (fields present for serialization, no logic) ─
        [SerializeField] int totalWaves = 1;
        [SerializeField] List<WaveConfig> waves = new();
        [SerializeField] string preEncounterDialogueId;
        [SerializeField] string postEncounterDialogueId;

        // ── Properties ─────────────────────────────────────────────────

        /// <summary>Encounter type (Wild, Trainer, Nest, Trophy, Horde).</summary>
        public EncounterType EncounterType => encounterType;

        /// <summary>Grid dimensions (width, depth).</summary>
        public Vector2Int GridDimensions => gridDimensions;

        /// <summary>Flat height map. Use GetHeight(x, z) for indexed access.</summary>
        public int[] HeightMapFlat => heightMapFlat;

        /// <summary>Flat terrain layout. Use GetTerrain(x, z) for indexed access.</summary>
        public TerrainType[] TileLayoutFlat => tileLayoutFlat;

        /// <summary>Tiles where player creatures spawn, in party-slot order.</summary>
        public Vector2Int[] PlayerStartTiles => playerStartTiles;

        /// <summary>Enemy creature entries with species, level, and spawn tile.</summary>
        public IReadOnlyList<EncounterCreatureEntry> Enemies => enemies;

        /// <summary>Whether the player can use Gene Traps in this encounter.</summary>
        public bool CaptureAllowed => captureAllowed;

        /// <summary>Whether the player can attempt to flee.</summary>
        public bool RetreatAllowed => retreatAllowed;

        /// <summary>Base Research Points awarded on victory.</summary>
        public int RpBase => rpBase;

        // Post-MVP properties
        /// <summary>Total waves for Nest/Horde encounters (post-MVP).</summary>
        public int TotalWaves => totalWaves;

        /// <summary>Wave definitions (post-MVP).</summary>
        public IReadOnlyList<WaveConfig> Waves => waves;

        /// <summary>Pre-encounter dialogue ID (post-MVP).</summary>
        public string PreEncounterDialogueId => preEncounterDialogueId;

        /// <summary>Post-encounter dialogue ID (post-MVP).</summary>
        public string PostEncounterDialogueId => postEncounterDialogueId;

        // ── Helpers ────────────────────────────────────────────────────

        /// <summary>Returns height at grid coordinate (x, z) using flat-array indexing.</summary>
        public int GetHeight(int x, int z) =>
            heightMapFlat[z * gridDimensions.x + x];

        /// <summary>Returns terrain type at grid coordinate (x, z) using flat-array indexing.</summary>
        public TerrainType GetTerrain(int x, int z) =>
            tileLayoutFlat[z * gridDimensions.x + x];
    }

    /// <summary>
    /// Post-MVP stub for wave-based encounter configuration (Nest/Horde).
    /// </summary>
    [System.Serializable]
    public class WaveConfig
    {
        [SerializeField] int waveIndex;
        [SerializeField] List<EncounterCreatureEntry> creatures = new();
        [SerializeField] int spawnDelayTurns;

        /// <summary>Wave sequence number (0-based).</summary>
        public int WaveIndex => waveIndex;

        /// <summary>Creatures spawned in this wave.</summary>
        public IReadOnlyList<EncounterCreatureEntry> Creatures => creatures;

        /// <summary>Turns after wave start before creatures spawn.</summary>
        public int SpawnDelayTurns => spawnDelayTurns;
    }
}
