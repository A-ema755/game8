using System.Collections.Generic;
using GeneForge.Creatures;
using GeneForge.Grid;

namespace GeneForge.Combat
{
    /// <summary>
    /// Contract for AI action providers.
    /// Implemented by the AI system and injected into TurnManager via constructor.
    /// Must return a valid (non-null) TurnAction for any non-fainted creature.
    /// Implements GDD Turn Manager §6.2.
    /// </summary>
    public interface IAIDecisionSystem
    {
        /// <summary>
        /// Decide the TurnAction for <paramref name="creature"/> this round.
        /// </summary>
        /// <param name="creature">The creature that needs a decision.</param>
        /// <param name="opponents">All opposing (player) creatures, including fainted ones.</param>
        /// <param name="allies">All allied (enemy) creatures, including fainted ones.</param>
        /// <param name="grid">The combat grid for position and pathfinding queries.</param>
        /// <returns>
        /// A fully populated TurnAction. Must never return a TurnAction with
        /// ActionType.Flee for EncounterType.Trainer encounters.
        /// </returns>
        TurnAction DecideAction(
            CreatureInstance creature,
            IReadOnlyList<CreatureInstance> opponents,
            IReadOnlyList<CreatureInstance> allies,
            GridSystem grid);
    }
}
