using GeneForge.Creatures;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.Combat
{
    /// <summary>
    /// Describes one creature's full turn for the current round.
    /// Split turn: optional movement step + one action step.
    /// Immutable — submitted during PlayerCreatureSelect, consumed during action phases.
    /// Implements GDD Turn Manager §3.3.
    ///
    /// Field invariants:
    ///   UseMove  — Move is non-null, MovePPSlot is 0–3.
    ///              Target is non-null for TargetType.Single.
    ///              TargetTile is non-null for TargetType.AoE / TargetType.Line.
    ///   Capture  — Target is non-null.
    ///   Flee     — MovementTarget must be null (flee consumes the entire turn).
    ///   Wait     — all optional fields null / -1.
    ///   MovementTarget may be set alongside any ActionType except Flee.
    /// </summary>
    public readonly struct TurnAction
    {
        // ── Movement Step (optional) ──────────────────────────────────────

        /// <summary>Target tile coordinate for the reposition step. Null = don't move this turn.</summary>
        public readonly Vector2Int? MovementTarget;

        // ── Action Step ───────────────────────────────────────────────────

        /// <summary>Action type for this turn.</summary>
        public readonly ActionType Action;

        /// <summary>Move blueprint for UseMove actions. Null for all other action types.</summary>
        public readonly MoveConfig Move;

        /// <summary>
        /// Target creature for UseMove (single-target) or Capture.
        /// Null for AoE, Line, Wait, Flee.
        /// </summary>
        public readonly CreatureInstance Target;

        /// <summary>
        /// Target tile for AoE and Line moves.
        /// Null for single-target moves, Capture, Wait, Flee.
        /// </summary>
        public readonly TileData TargetTile;

        /// <summary>
        /// Index 0–3 into the actor's LearnedMoveIds list for PP tracking.
        /// -1 when not applicable (Capture, Wait, Flee, Item).
        /// </summary>
        public readonly int MovePPSlot;

        /// <summary>
        /// Creates a TurnAction.
        /// </summary>
        /// <param name="action">The action type for this turn.</param>
        /// <param name="movementTarget">Optional reposition target. Must be null for Flee.</param>
        /// <param name="move">Move blueprint (UseMove only).</param>
        /// <param name="target">Target creature (UseMove single-target or Capture).</param>
        /// <param name="targetTile">Target tile (AoE or Line moves).</param>
        /// <param name="movePPSlot">PP slot index 0–3, or -1 if N/A.</param>
        public TurnAction(
            ActionType action,
            Vector2Int? movementTarget = null,
            MoveConfig move            = null,
            CreatureInstance target    = null,
            TileData targetTile        = null,
            int movePPSlot             = -1)
        {
            Action         = action;
            MovementTarget = movementTarget;
            Move           = move;
            Target         = target;
            TargetTile     = targetTile;
            MovePPSlot     = movePPSlot;
        }
    }
}
