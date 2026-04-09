using System.Collections.Generic;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.Combat
{
    /// <summary>
    /// Pure-math scoring functions for AI action evaluation.
    /// Static class — no state, all methods are deterministic for identical inputs.
    /// Implements GDD ai-decision-system.md §3.2.
    /// </summary>
    public static class AIActionScorer
    {
        /// <summary>
        /// Scores a candidate action across all eight dimensions.
        /// Returns the weighted composite score using personality weights.
        /// Computes estimated damage once and shares it across damage-dependent dimensions.
        /// </summary>
        /// <param name="action">Candidate action to evaluate.</param>
        /// <param name="actor">The creature performing the action.</param>
        /// <param name="opponents">All opposing creatures (may include fainted).</param>
        /// <param name="grid">The combat grid for position and terrain queries.</param>
        /// <param name="personality">AI personality config providing scoring weights.</param>
        /// <returns>Weighted composite score. Higher is better.</returns>
        public static float ScoreAction(
            CandidateAction action,
            CreatureInstance actor,
            IReadOnlyList<CreatureInstance> opponents,
            GridSystem grid,
            AIPersonalityConfig personality)
        {
            // Compute estimated damage once for damage-dependent dimensions
            int estimatedDamage = EstimateDamage(action, actor, grid);

            float scoreDamage  = ScoreDamage(action, estimatedDamage);
            float scoreKill    = ScoreKillPotential(action, estimatedDamage);
            float scoreThreat  = ScoreFinishTarget(action);
            float scorePos     = ScorePosition(action, actor, opponents, grid, personality.AggressionBias);
            float scoreTerrain = ScoreTerrainSynergy(action, actor, grid);
            float scoreSelf    = ScoreSelfPreservation(actor, personality.LowHpThreshold);
            float scoreGenome  = ScoreGenomeMatchup(action, actor);
            float scoreForm    = ScoreFormTactics(action, actor, grid);

            return (scoreDamage  * personality.WeightDamage)
                 + (scoreKill    * personality.WeightKill)
                 + (scoreThreat  * personality.WeightThreat)
                 + (scorePos     * personality.WeightPosition)
                 + (scoreTerrain * personality.WeightTerrain)
                 + (scoreSelf    * personality.WeightSelfPreservation)
                 + (scoreGenome  * personality.WeightGenomeMatch)
                 + (scoreForm    * personality.WeightFormTactic);
        }

        /// <summary>
        /// Computes estimated damage for a candidate action.
        /// Uses DamageCalculator.Estimate() — no variance, no crit.
        /// Returns 0 for non-damaging moves or null targets.
        /// </summary>
        public static int EstimateDamage(CandidateAction action, CreatureInstance actor, GridSystem grid)
        {
            if (action.Move == null || action.Target == null || !action.Move.IsDamaging)
                return 0;

            return DamageCalculator.Estimate(action.Move, actor, action.Target, grid);
        }

        /// <summary>
        /// Normalized damage score (0-2 range).
        /// Uses pre-computed estimated damage to avoid redundant calculation.
        /// </summary>
        /// <param name="action">Candidate action with target info.</param>
        /// <param name="estimatedDamage">Pre-computed damage from EstimateDamage().</param>
        public static float ScoreDamage(CandidateAction action, int estimatedDamage)
        {
            if (action.Target == null || estimatedDamage <= 0 || action.Target.MaxHP <= 0)
                return 0f;

            return Mathf.Clamp(estimatedDamage / (float)action.Target.MaxHP, 0f, 2f);
        }

        /// <summary>
        /// Returns 1.0 if estimated damage would faint the target this turn, else 0.0.
        /// Uses pre-computed estimated damage.
        /// </summary>
        /// <param name="action">Candidate action with target info.</param>
        /// <param name="estimatedDamage">Pre-computed damage from EstimateDamage().</param>
        public static float ScoreKillPotential(CandidateAction action, int estimatedDamage)
        {
            if (action.Target == null || estimatedDamage <= 0)
                return 0f;

            return estimatedDamage >= action.Target.CurrentHP ? 1.0f : 0.0f;
        }

        /// <summary>
        /// Scores target priority based on remaining HP fraction.
        /// Lower HP fraction = higher score (prioritizes finishing wounded targets).
        /// Simplified MVP targeting — no Threat/Aggro System dependency.
        /// Post-MVP: replace with threat values from Threat/Aggro System.
        /// </summary>
        public static float ScoreFinishTarget(CandidateAction action)
        {
            if (action.Target == null || action.Target.MaxHP <= 0)
                return 0f;

            return 1.0f - ((float)action.Target.CurrentHP / action.Target.MaxHP);
        }

        /// <summary>
        /// Scores positional value: approach vs retreat based on aggressionBias.
        /// Uses A* path length to score distance to the nearest live opponent.
        /// Path results are computed once per call; callers should avoid redundant
        /// invocations across the same scoring pass where possible.
        /// </summary>
        public static float ScorePosition(
            CandidateAction action,
            CreatureInstance actor,
            IReadOnlyList<CreatureInstance> opponents,
            GridSystem grid,
            float aggressionBias)
        {
            int minDist = int.MaxValue;
            for (int i = 0; i < opponents.Count; i++)
            {
                if (opponents[i].IsFainted) continue;
                // Use A* path length for accurate walkable distance (C2 fix)
                var path = grid.FindPath(action.DestinationTile, opponents[i].GridPosition);
                int dist = path != null ? path.Count : int.MaxValue;
                if (dist < minDist) minDist = dist;
            }

            if (minDist == int.MaxValue)
                return 0f;

            float maxDist = Mathf.Max(grid.Width, grid.Depth);
            float approachScore = 1.0f - (minDist / maxDist);
            float retreatScore = minDist / maxDist;

            return (approachScore * aggressionBias) + (retreatScore * (1f - aggressionBias));
        }

        /// <summary>
        /// Returns 1.0 for terrain synergy match, penalizes harmful terrain by -0.5.
        /// Uses TypeChart.TerrainMatchesCreatureType for synergy detection.
        /// </summary>
        public static float ScoreTerrainSynergy(CandidateAction action, CreatureInstance actor, GridSystem grid)
        {
            var tile = grid.GetTile(action.DestinationTile);
            if (tile == null)
                return 0f;

            float score = 0f;

            // Reward: terrain matches creature's synergy type
            if (TypeChart.TerrainMatchesCreatureType(tile.Terrain, actor.Config.TerrainSynergyType))
                score += 1.0f;

            // Penalty: terrain is harmful (maps to a type super-effective against actor)
            CreatureType terrainType = MapTerrainToCreatureType(tile.Terrain);
            if (terrainType != CreatureType.None)
            {
                float mult = TypeChart.GetMultiplier(terrainType, actor.Config.PrimaryType, actor.ActiveSecondaryType);
                if (mult >= 2.0f)
                    score -= 0.5f;
            }

            return score;
        }

        /// <summary>
        /// Self-preservation score: returns 1.0 when actor HP is below lowHpThreshold, else 0.0.
        /// Rewards defensive actions when creature health is low.
        /// </summary>
        public static float ScoreSelfPreservation(CreatureInstance actor, float lowHpThreshold)
        {
            return (float)actor.CurrentHP / actor.MaxHP < lowHpThreshold ? 1.0f : 0.0f;
        }

        /// <summary>
        /// Genome type matchup score using TypeChart.
        /// Rewards super-effective moves, penalizes resisted.
        /// Type effectiveness normalized to [-0.25, +0.33] range.
        /// STAB is NOT included here — it is already factored into
        /// DamageCalculator.Estimate() which feeds ScoreDamage (C5 fix).
        /// </summary>
        public static float ScoreGenomeMatchup(CandidateAction action, CreatureInstance actor)
        {
            if (action.Move == null || action.Target == null || !action.Move.IsDamaging)
                return 0f;

            float mult = TypeChart.GetMultiplier(
                action.Move.GenomeType,
                action.Target.Config.PrimaryType,
                action.Target.ActiveSecondaryType);

            // Normalize effectiveness: -0.25 for 0.25x, 0 for 1.0x, +0.33 for 2.0x
            return (mult - 1.0f) / 3.0f;
        }

        /// <summary>
        /// Form tactical advantage based on stat pairing favorability and grid positioning.
        /// Evaluates stat ratio plus positional bonuses per damage form:
        /// Physical: bonus when adjacent and on higher ground.
        /// Energy: bonus with height advantage.
        /// Bio: bonus when target is behind cover (Bio ignores cover).
        /// Implements GDD §3.2 ScoreFormTactics.
        /// </summary>
        public static float ScoreFormTactics(CandidateAction action, CreatureInstance actor, GridSystem grid)
        {
            if (action.Move == null || action.Target == null || !action.Move.IsDamaging)
                return 0f;

            // Stat pairing favorability
            GetFormStatPairing(action.Move.Form, actor, action.Target, out int offStat, out int defStat);
            float statRatio = (float)offStat / Mathf.Max(1, defStat);
            float formStatScore = Mathf.Clamp((statRatio - 1.0f) / 2.0f, -0.5f, 1.0f);

            // Positional component
            float formPositionScore = 0f;
            var actorTile = grid.GetTile(action.DestinationTile);
            var targetTile = grid.GetTile(action.Target.GridPosition);

            if (actorTile != null && targetTile != null)
            {
                int heightDiff = actorTile.Height - targetTile.Height;
                int dist = GridSystem.ChebyshevDistance(action.DestinationTile, action.Target.GridPosition);

                switch (action.Move.Form)
                {
                    case DamageForm.Physical:
                        // Bonus when truly adjacent (dist 1) and on higher ground (C4 fix: was <= 2)
                        if (dist <= 1) formPositionScore += 0.3f;
                        if (heightDiff > 0) formPositionScore += 0.2f * heightDiff;
                        break;

                    case DamageForm.Energy:
                        // Bonus with height advantage
                        if (heightDiff > 0) formPositionScore += 0.3f * heightDiff;
                        break;

                    case DamageForm.Bio:
                        // Bonus when target is behind cover (Bio ignores cover — exploiting advantage)
                        if (targetTile.ProvidesCover) formPositionScore += 0.5f;
                        break;
                }
            }

            return formStatScore + Mathf.Clamp(formPositionScore, 0f, 1.0f);
        }

        /// <summary>
        /// Select offensive and defensive stats based on damage form.
        /// Physical: ATK vs DEF. Energy: ATK vs SPD. Bio: ACC vs DEF.
        /// Mirrors DamageCalculator stat pairing logic.
        /// </summary>
        private static void GetFormStatPairing(
            DamageForm form,
            CreatureInstance attacker,
            CreatureInstance defender,
            out int offStat,
            out int defStat)
        {
            switch (form)
            {
                case DamageForm.Energy:
                    offStat = attacker.ComputedStats.ATK;
                    defStat = defender.ComputedStats.SPD;
                    break;
                case DamageForm.Bio:
                    offStat = attacker.ComputedStats.ACC;
                    defStat = defender.ComputedStats.DEF;
                    break;
                case DamageForm.Physical:
                default:
                    offStat = attacker.ComputedStats.ATK;
                    defStat = defender.ComputedStats.DEF;
                    break;
            }
        }

        /// <summary>
        /// Maps a TerrainType to its corresponding CreatureType for harmful terrain checks.
        /// Returns CreatureType.None for terrain types without a creature type mapping.
        /// </summary>
        private static CreatureType MapTerrainToCreatureType(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Thermal => CreatureType.Thermal,
                TerrainType.Aqua    => CreatureType.Aqua,
                TerrainType.Organic => CreatureType.Organic,
                TerrainType.Cryo    => CreatureType.Cryo,
                TerrainType.Mineral => CreatureType.Mineral,
                TerrainType.Kinetic => CreatureType.Kinetic,
                TerrainType.Neural  => CreatureType.Neural,
                TerrainType.Toxic   => CreatureType.Toxic,
                _                   => CreatureType.None,
            };
        }
    }
}
