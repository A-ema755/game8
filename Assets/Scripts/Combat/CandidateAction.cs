using GeneForge.Creatures;
using UnityEngine;

namespace GeneForge.Combat
{
    /// <summary>
    /// Represents one candidate action for AI evaluation.
    /// Created during action enumeration, scored by AIActionScorer.
    /// Implements GDD ai-decision-system.md §3.1.
    /// </summary>
    public class CandidateAction
    {
        /// <summary>Move to use. Null = Wait action.</summary>
        public MoveConfig Move { get; }

        /// <summary>Target creature. Null for self-targeting or AoE moves.</summary>
        public CreatureInstance Target { get; }

        /// <summary>Tile the creature will occupy when executing this action.</summary>
        public Vector2Int DestinationTile { get; }

        /// <summary>Weighted composite score assigned by AIActionScorer. Mutable during scoring pass.</summary>
        public float CompositeScore { get; internal set; }

        /// <summary>
        /// Create a candidate action for AI evaluation.
        /// </summary>
        /// <param name="move">Move to use, or null for Wait.</param>
        /// <param name="target">Target creature, or null for self/AoE.</param>
        /// <param name="destinationTile">Tile position for this action.</param>
        public CandidateAction(MoveConfig move, CreatureInstance target, Vector2Int destinationTile)
        {
            Move = move;
            Target = target;
            DestinationTile = destinationTile;
        }
    }
}
