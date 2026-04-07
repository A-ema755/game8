using System;
using System.Collections.Generic;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;

namespace GeneForge.Combat
{
    /// <summary>
    /// Scoring-based AI decision system for enemy creature combat behavior.
    /// Enumerates all legal actions, scores each via AIActionScorer, adds jitter,
    /// applies tiebreaker logic, and returns the highest-scoring TurnAction.
    /// Pure C# class — no MonoBehaviour (ADR-002). Dependencies injected (ADR-003).
    /// Implements GDD ai-decision-system.md.
    /// </summary>
    public class AIDecisionSystem : IAIDecisionSystem
    {
        private readonly AIPersonalityConfig _personality;
        private readonly System.Random _rng;
        private readonly Func<string, MoveConfig> _moveLookup;

        /// <summary>Score difference below which tiebreaker logic triggers (GDD §3.7).</summary>
        private const float TieTolerance = 0.01f;

        /// <summary>
        /// Create an AI decision system with injected dependencies.
        /// </summary>
        /// <param name="personality">Scoring weights and behavioral biases.</param>
        /// <param name="rng">Random number generator for jitter. Seeded for deterministic tests.</param>
        /// <param name="moveLookup">
        /// Function to resolve move IDs to MoveConfig. Typically ConfigLoader.GetMove.
        /// Injected for testability (ADR-003).
        /// </param>
        public AIDecisionSystem(AIPersonalityConfig personality, System.Random rng,
            Func<string, MoveConfig> moveLookup = null)
        {
            _personality = personality;
            _rng = rng;
            _moveLookup = moveLookup ?? ConfigLoader.GetMove;
        }

        /// <summary>
        /// Decide the best action for a creature this round.
        /// Enumerates all legal move+target combinations, scores each,
        /// adds random jitter, applies tiebreaker, and returns the highest-scoring TurnAction.
        /// Always returns a valid (non-null) TurnAction.
        /// </summary>
        /// <param name="creature">The creature that needs a decision.</param>
        /// <param name="opponents">All opposing (player) creatures, including fainted ones.</param>
        /// <param name="allies">All allied (enemy) creatures, including fainted ones.</param>
        /// <param name="grid">The combat grid for position and pathfinding queries.</param>
        /// <returns>A fully populated TurnAction. Never null.</returns>
        public TurnAction DecideAction(
            CreatureInstance creature,
            IReadOnlyList<CreatureInstance> opponents,
            IReadOnlyList<CreatureInstance> allies,
            GridSystem grid)
        {
            var candidates = EnumerateCandidates(creature, opponents, grid);

            // Score all candidates
            foreach (var candidate in candidates)
            {
                candidate.CompositeScore = AIActionScorer.ScoreAction(
                    candidate, creature, opponents, grid, _personality);

                // Add random jitter for unpredictability (GDD §3.6)
                float jitter = ((float)_rng.NextDouble() * 2f - 1f) * _personality.RandomnessFactor;
                candidate.CompositeScore += jitter;
            }

            // Select highest-scoring candidate with tiebreaker (GDD §3.7)
            CandidateAction best = SelectBestCandidate(candidates);

            // Convert to TurnAction
            if (best == null || best.Move == null)
                return new TurnAction(ActionType.Wait);

            // Find PP slot index for the selected move
            int ppSlot = -1;
            for (int i = 0; i < creature.LearnedMoveIds.Count; i++)
            {
                if (creature.LearnedMoveIds[i] == best.Move.Id)
                {
                    ppSlot = i;
                    break;
                }
            }

            return new TurnAction(
                ActionType.UseMove,
                movementTarget: null,
                move: best.Move,
                target: best.Target,
                movePPSlot: ppSlot);
        }

        /// <summary>
        /// Selects the highest-scoring candidate. When candidates are within
        /// TieTolerance, breaks ties by: (1) higher base power, (2) higher accuracy,
        /// (3) random selection. Implements GDD §3.7.
        /// </summary>
        private CandidateAction SelectBestCandidate(List<CandidateAction> candidates)
        {
            if (candidates.Count == 0)
                return null;

            // Find max score
            float maxScore = float.MinValue;
            foreach (var c in candidates)
            {
                if (c.CompositeScore > maxScore)
                    maxScore = c.CompositeScore;
            }

            // Collect candidates within tie tolerance
            var tied = new List<CandidateAction>();
            foreach (var c in candidates)
            {
                if (maxScore - c.CompositeScore <= TieTolerance)
                    tied.Add(c);
            }

            if (tied.Count == 1)
                return tied[0];

            // Tiebreak 1: higher base power
            int maxPower = -1;
            foreach (var c in tied)
            {
                int power = c.Move?.Power ?? 0;
                if (power > maxPower) maxPower = power;
            }
            var powerTied = new List<CandidateAction>();
            foreach (var c in tied)
            {
                if ((c.Move?.Power ?? 0) == maxPower)
                    powerTied.Add(c);
            }
            if (powerTied.Count == 1)
                return powerTied[0];

            // Tiebreak 2: higher accuracy
            int maxAcc = -1;
            foreach (var c in powerTied)
            {
                int acc = c.Move?.Accuracy ?? 0;
                if (acc > maxAcc) maxAcc = acc;
            }
            var accTied = new List<CandidateAction>();
            foreach (var c in powerTied)
            {
                if ((c.Move?.Accuracy ?? 0) == maxAcc)
                    accTied.Add(c);
            }
            if (accTied.Count == 1)
                return accTied[0];

            // Tiebreak 3: random
            return accTied[_rng.Next(accTied.Count)];
        }

        /// <summary>
        /// Generates all legal CandidateActions for the creature.
        /// Iterates learned moves, checks PP, form access, and target range.
        /// Always includes a Wait candidate as baseline (score 0).
        /// </summary>
        private List<CandidateAction> EnumerateCandidates(
            CreatureInstance creature,
            IReadOnlyList<CreatureInstance> opponents,
            GridSystem grid)
        {
            var candidates = new List<CandidateAction>();

            // Enumerate move + target combinations
            for (int slot = 0; slot < creature.LearnedMoveIds.Count; slot++)
            {
                // Check PP
                if (creature.LearnedMovePP[slot] <= 0)
                    continue;

                var moveConfig = _moveLookup(creature.LearnedMoveIds[slot]);
                if (moveConfig == null)
                    continue;

                // Check form access (DamageForm.None is always accessible — status moves)
                if (moveConfig.Form != DamageForm.None && !creature.AvailableForms.Contains(moveConfig.Form))
                    continue;

                // Generate candidates based on targeting type
                switch (moveConfig.TargetType)
                {
                    case TargetType.Self:
                    case TargetType.AllAllies:
                        candidates.Add(new CandidateAction(moveConfig, null, creature.GridPosition));
                        break;

                    case TargetType.Single:
                    case TargetType.Adjacent:
                    case TargetType.Line:
                    default:
                        for (int i = 0; i < opponents.Count; i++)
                        {
                            if (opponents[i].IsFainted) continue;

                            int dist = GridSystem.ChebyshevDistance(
                                creature.GridPosition, opponents[i].GridPosition);

                            if (dist <= moveConfig.Range)
                            {
                                candidates.Add(new CandidateAction(
                                    moveConfig, opponents[i], creature.GridPosition));
                            }
                        }
                        break;

                    case TargetType.AoE:
                        // AoE: target the closest live opponent in range
                        CreatureInstance closestForAoE = null;
                        int closestDist = int.MaxValue;
                        for (int i = 0; i < opponents.Count; i++)
                        {
                            if (opponents[i].IsFainted) continue;
                            int dist = GridSystem.ChebyshevDistance(
                                creature.GridPosition, opponents[i].GridPosition);
                            if (dist <= moveConfig.Range && dist < closestDist)
                            {
                                closestDist = dist;
                                closestForAoE = opponents[i];
                            }
                        }
                        if (closestForAoE != null)
                            candidates.Add(new CandidateAction(moveConfig, closestForAoE, creature.GridPosition));
                        break;
                }
            }

            // Always include Wait as baseline candidate (score 0)
            candidates.Add(new CandidateAction(null, null, creature.GridPosition));

            return candidates;
        }
    }
}
