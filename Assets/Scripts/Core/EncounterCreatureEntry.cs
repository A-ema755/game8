using System.Collections.Generic;
using UnityEngine;

namespace GeneForge.Core
{
    /// <summary>
    /// One enemy entry inside an EncounterConfig.
    /// Class (not struct) because it contains reference types (string, List).
    /// Serializable for Unity Inspector embedding in EncounterConfig.
    /// Implements GDD: design/gdd/encounter-system.md §3.2.
    /// </summary>
    [System.Serializable]
    public class EncounterCreatureEntry
    {
        [SerializeField] string speciesId;
        [SerializeField] int level = 1;
        [SerializeField] string personalityConfigId;
        [SerializeField] List<string> overrideMoves = new();
        [SerializeField] Vector2Int spawnTile;
        [SerializeField] bool isBoss;

        /// <summary>Kebab-case species ID referencing a CreatureConfig asset.</summary>
        public string SpeciesId => speciesId;

        /// <summary>Enemy creature level.</summary>
        public int Level => level;

        /// <summary>AI personality config ID for this enemy. Null = use default balanced preset.</summary>
        public string PersonalityConfigId => personalityConfigId;

        /// <summary>Override move IDs. Empty = use default level-up moves.</summary>
        public IReadOnlyList<string> OverrideMoves => overrideMoves;

        /// <summary>Grid tile where this enemy spawns.</summary>
        public Vector2Int SpawnTile => spawnTile;

        /// <summary>If true, blocks capture and shows boss health bar.</summary>
        public bool IsBoss => isBoss;
    }
}
