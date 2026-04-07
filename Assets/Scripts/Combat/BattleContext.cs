using System.Collections.Generic;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;

namespace GeneForge.Combat
{
    /// <summary>
    /// Immutable snapshot of a fully-initialized encounter.
    /// Produced by EncounterManager.InitializeEncounter().
    /// Caller destructures this into TurnManager constructor arguments.
    /// Implements GDD: design/gdd/encounter-system.md §3.3.
    /// </summary>
    public class BattleContext
    {
        /// <summary>Source encounter config.</summary>
        public EncounterConfig Config { get; }

        /// <summary>Fully-initialized combat grid with tiles and occupants.</summary>
        public GridSystem Grid { get; }

        /// <summary>Player creatures placed on the grid (non-fainted only).</summary>
        public IReadOnlyList<CreatureInstance> PlayerCreatures { get; }

        /// <summary>Enemy creatures placed on the grid.</summary>
        public IReadOnlyList<CreatureInstance> EnemyCreatures { get; }

        /// <summary>Encounter type for TurnManager flee/capture rules.</summary>
        public EncounterType EncounterType { get; }

        /// <summary>Whether Gene Trap capture is allowed.</summary>
        public bool CaptureAllowed { get; }

        /// <summary>Whether player retreat is allowed.</summary>
        public bool RetreatAllowed { get; }

        /// <summary>Create a new BattleContext. Lists are wrapped as read-only.</summary>
        public BattleContext(
            EncounterConfig config,
            GridSystem grid,
            List<CreatureInstance> playerCreatures,
            List<CreatureInstance> enemyCreatures,
            EncounterType encounterType,
            bool captureAllowed,
            bool retreatAllowed)
        {
            Config = config;
            Grid = grid;
            PlayerCreatures = playerCreatures.AsReadOnly();
            EnemyCreatures = enemyCreatures.AsReadOnly();
            EncounterType = encounterType;
            CaptureAllowed = captureAllowed;
            RetreatAllowed = retreatAllowed;
        }
    }
}
