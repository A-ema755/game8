using System.Collections.Generic;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.Combat
{
    /// <summary>
    /// Pure C# static utility that computes valid targets for moves and movement.
    /// Used by PlayerInputController to determine what the player can click on.
    /// No state, no MonoBehaviour dependency — fully testable.
    /// </summary>
    public static class TargetingHelper
    {
        /// <summary>
        /// Returns valid target tile positions for a move from the actor's current position.
        /// Applies form-specific LoS rules: Physical/Bio/None ignore LoS, Energy requires LoS.
        /// For Self-targeting moves, returns only the actor's position.
        /// </summary>
        /// <remarks>
        /// For <see cref="TargetType.AllAllies"/> and <see cref="TargetType.SingleAlly"/>,
        /// this method returns all passable tiles in range — the caller must post-filter
        /// to only tiles occupied by allied creatures.
        /// </remarks>
        /// <param name="move">Move to evaluate targeting for.</param>
        /// <param name="actor">Creature using the move.</param>
        /// <param name="grid">Grid system for range and LoS queries.</param>
        /// <returns>List of valid target tile positions.</returns>
        public static List<Vector2Int> GetValidTargetTiles(
            MoveConfig move, CreatureInstance actor, GridSystem grid)
        {
            var result = new List<Vector2Int>();
            if (move == null || actor == null || grid == null) return result;

            var actorPos = actor.GridPosition;

            switch (move.TargetType)
            {
                case TargetType.Self:
                    result.Add(actorPos);
                    break;

                case TargetType.Single:
                case TargetType.AoE:
                case TargetType.Line:
                case TargetType.Adjacent:
                case TargetType.AllAllies:
                case TargetType.SingleAlly:
                    AddTilesInRange(result, move, actorPos, grid);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Returns valid creature targets for a single-target move.
        /// Filters by: not fainted, within range, LoS for Energy moves, on a valid tile.
        /// </summary>
        /// <param name="move">Move to evaluate.</param>
        /// <param name="actor">Creature using the move.</param>
        /// <param name="grid">Grid system for range and LoS queries.</param>
        /// <param name="candidates">Potential target creatures (enemies or allies depending on move).</param>
        /// <returns>List of creatures that can be targeted.</returns>
        public static List<CreatureInstance> GetValidCreatureTargets(
            MoveConfig move, CreatureInstance actor, GridSystem grid,
            IReadOnlyList<CreatureInstance> candidates)
        {
            var result = new List<CreatureInstance>();
            if (move == null || actor == null || grid == null || candidates == null)
                return result;

            var actorPos = actor.GridPosition;

            foreach (var candidate in candidates)
            {
                if (candidate == null || candidate.IsFainted) continue;

                var targetPos = candidate.GridPosition;
                int dist = GridSystem.ChebyshevDistance(actorPos, targetPos);
                if (dist > move.Range) continue;

                // Energy moves require line of sight
                if (move.Form == DamageForm.Energy && !grid.HasLineOfSight(actorPos, targetPos))
                    continue;

                result.Add(candidate);
            }

            return result;
        }

        /// <summary>
        /// Returns reachable movement tile positions for a creature.
        /// Movement range = max(1, SPD / movementDivisor).
        /// Delegates to GridSystem.GetReachableTiles for BFS pathfinding.
        /// </summary>
        /// <param name="creature">Creature to calculate movement for.</param>
        /// <param name="grid">Grid system for reachability queries.</param>
        /// <param name="movementDivisor">SPD divisor from CombatSettings. Must be > 0.</param>
        /// <returns>List of reachable tile positions (excludes start position).</returns>
        public static List<Vector2Int> GetMovementTiles(
            CreatureInstance creature, GridSystem grid, int movementDivisor)
        {
            var result = new List<Vector2Int>();
            if (creature == null || grid == null) return result;

            if (movementDivisor <= 0)
            {
                Debug.LogError("[TargetingHelper] movementDivisor must be > 0. " +
                    "Check CombatSettings.MovementDivisor is configured.");
                return result;
            }

            int movementRange = Mathf.Max(1, creature.ComputedStats.SPD / movementDivisor);
            var reachableTiles = grid.GetReachableTiles(creature.GridPosition, movementRange);

            foreach (var tile in reachableTiles)
                result.Add(tile.GridPosition);

            return result;
        }

        /// <summary>
        /// Adds passable tiles within Chebyshev range, applying form-specific LoS rules.
        /// Physical, Bio, and None (status) moves ignore LoS. Energy requires LoS.
        /// </summary>
        private static void AddTilesInRange(
            List<Vector2Int> result, MoveConfig move, Vector2Int actorPos, GridSystem grid)
        {
            int range = move.Range;

            for (int x = 0; x < grid.Width; x++)
            {
                for (int z = 0; z < grid.Depth; z++)
                {
                    var pos = new Vector2Int(x, z);
                    if (pos == actorPos) continue;

                    int dist = GridSystem.ChebyshevDistance(actorPos, pos);
                    if (dist > range) continue;

                    var tile = grid.GetTile(pos);
                    if (tile == null || !tile.IsPassable) continue;

                    // Energy moves require line of sight; all other forms ignore LoS
                    if (move.Form == DamageForm.Energy && !grid.HasLineOfSight(actorPos, pos))
                        continue;

                    result.Add(pos);
                }
            }
        }
    }
}
