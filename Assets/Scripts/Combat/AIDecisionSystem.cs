using System;
using System.Collections.Generic;
using System.Reflection;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;
using UnityEngine;

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

        /// <summary>Cached Struggle MoveConfig: power 10, Physical, typeless, always hits, range 1.</summary>
        private static MoveConfig _struggleMove;

        /// <summary>Struggle move ID used to identify Struggle actions.</summary>
        public const string StruggleMoveId = "__struggle__";

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

            // Determine if creature is in retreat state (C1: RetreatHpThreshold)
            float hpFraction = creature.MaxHP > 0
                ? (float)creature.CurrentHP / creature.MaxHP
                : 1f;
            bool isRetreating = hpFraction < _personality.RetreatHpThreshold;

            // Score all candidates
            foreach (var candidate in candidates)
            {
                candidate.CompositeScore = AIActionScorer.ScoreAction(
                    candidate, creature, opponents, grid, _personality);

                // C1a: Retreat — heavily boost self-preservation when HP is critically low.
                // Re-score self-preservation at 3x weight and add the delta to the composite.
                if (isRetreating)
                {
                    float baseSelf = AIActionScorer.ScoreSelfPreservation(creature, _personality.LowHpThreshold);
                    candidate.CompositeScore += baseSelf * _personality.WeightSelfPreservation * 2f; // +2x on top of existing 1x = 3x total
                }

                // C1b: FocusFireBias — scale the FinishTarget contribution by the bias value.
                // ScoreFinishTarget is already included in the composite at WeightThreat.
                // We add an additional bonus proportional to FocusFireBias so Hunter archetypes
                // (high FocusFireBias) compound pressure on wounded targets.
                if (candidate.Move != null && candidate.Target != null)
                {
                    float finishScore = AIActionScorer.ScoreFinishTarget(candidate);
                    candidate.CompositeScore += finishScore * _personality.FocusFireBias;

                    // C1c: AbilityPreference — bonus for status moves (power 0) when > 0.5;
                    // bonus for damage moves when < 0.5.
                    bool isStatusMove = !candidate.Move.IsDamaging;
                    if (isStatusMove && _personality.AbilityPreference > 0.5f)
                        candidate.CompositeScore += (_personality.AbilityPreference - 0.5f) * 2f;
                    else if (!isStatusMove && _personality.AbilityPreference < 0.5f)
                        candidate.CompositeScore += (0.5f - _personality.AbilityPreference) * 2f;
                }

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
                        // C3 fix: generate a candidate per in-range opponent so the scorer
                        // can evaluate multi-hit value for each possible AoE center point.
                        // Previously only the single closest opponent was generated, which
                        // prevented the scorer from discovering better AoE positions.
                        for (int i = 0; i < opponents.Count; i++)
                        {
                            if (opponents[i].IsFainted) continue;
                            int dist = GridSystem.ChebyshevDistance(
                                creature.GridPosition, opponents[i].GridPosition);
                            if (dist <= moveConfig.Range)
                                candidates.Add(new CandidateAction(moveConfig, opponents[i], creature.GridPosition));
                        }
                        break;
                }
            }

            // Struggle fallback: when all move slots have PP <= 0, generate Struggle
            // candidates against each opponent in range (GDD: power 10, Physical, typeless).
            if (candidates.Count == 0)
            {
                var struggle = GetStruggleMoveConfig();
                for (int i = 0; i < opponents.Count; i++)
                {
                    if (opponents[i].IsFainted) continue;
                    int dist = GridSystem.ChebyshevDistance(
                        creature.GridPosition, opponents[i].GridPosition);
                    if (dist <= struggle.Range)
                        candidates.Add(new CandidateAction(struggle, opponents[i], creature.GridPosition));
                }

                // If no opponents in Struggle range, add untargeted Struggle (will miss).
                if (candidates.Count == 0)
                    candidates.Add(new CandidateAction(struggle, null, creature.GridPosition));
            }

            // Always include Wait as baseline candidate (score 0)
            candidates.Add(new CandidateAction(null, null, creature.GridPosition));

            return candidates;
        }

        /// <summary>
        /// Returns a cached Struggle MoveConfig. Created once via ScriptableObject.CreateInstance.
        /// Power 10, Physical, GenomeType.None (typeless), always hits (accuracy 0), range 1.
        /// </summary>
        public static MoveConfig GetStruggleMoveConfig()
        {
            if (_struggleMove != null) return _struggleMove;

            _struggleMove = ScriptableObject.CreateInstance<MoveConfig>();
            SetField(_struggleMove, "id", StruggleMoveId);
            SetField(_struggleMove, "displayName", "Struggle");
            SetField(_struggleMove, "genomeType", CreatureType.None);
            SetField(_struggleMove, "form", DamageForm.Physical);
            SetField(_struggleMove, "power", 10);
            SetField(_struggleMove, "accuracy", 0); // always hits
            SetField(_struggleMove, "pp", 0);
            SetField(_struggleMove, "priority", 0);
            SetField(_struggleMove, "targetType", TargetType.Single);
            SetField(_struggleMove, "range", 1);
            SetField(_struggleMove, "effects", new System.Collections.Generic.List<MoveEffect>());
            return _struggleMove;
        }

        private static void SetField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }
            field?.SetValue(obj, value);
        }
    }
}
