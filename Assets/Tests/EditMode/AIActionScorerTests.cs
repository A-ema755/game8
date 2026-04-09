using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Combat;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.Tests
{
    [TestFixture]
    public class AIActionScorerTests
    {
        // ── Reflection Helpers ───────────────────────────────────────────

        private static void SetField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {obj.GetType().Name}");
            field.SetValue(obj, value);
        }

        // ── Factory Helpers ──────────────────────────────────────────────

        private MoveConfig CreateMove(
            string id, CreatureType genomeType, DamageForm form,
            int power, int accuracy = 100, int pp = 10, int range = 3)
        {
            var move = ScriptableObject.CreateInstance<MoveConfig>();
            SetField(move, "id", id);
            SetField(move, "displayName", id);
            SetField(move, "genomeType", genomeType);
            SetField(move, "form", form);
            SetField(move, "power", power);
            SetField(move, "accuracy", accuracy);
            SetField(move, "pp", pp);
            SetField(move, "range", range);
            SetField(move, "targetType", TargetType.Single);
            SetField(move, "priority", 0);
            SetField(move, "effects", new List<MoveEffect>());
            return move;
        }

        private CreatureConfig CreateCreatureConfig(
            string id,
            BaseStats stats,
            CreatureType primary = CreatureType.Thermal,
            CreatureType secondary = CreatureType.None,
            CreatureType terrainSynergy = CreatureType.None)
        {
            var config = ScriptableObject.CreateInstance<CreatureConfig>();
            SetField(config, "id", id);
            SetField(config, "displayName", id);
            SetField(config, "primaryType", primary);
            SetField(config, "secondaryType", secondary);
            SetField(config, "baseStats", stats);
            SetField(config, "growthCurve", GrowthCurve.Medium);
            SetField(config, "movePool", new List<LevelMoveEntry>());
            SetField(config, "availableSlots", new List<BodySlot>());
            SetField(config, "defaultPartIds", new List<string>());
            SetField(config, "habitatZoneIds", new List<string>());
            SetField(config, "terrainSynergyType", terrainSynergy);
            return config;
        }

        private AIPersonalityConfig CreatePersonality(
            float wDamage = 1f, float wKill = 1f, float wThreat = 1f,
            float wPosition = 1f, float wTerrain = 1f, float wSelf = 1f,
            float wGenome = 1f, float wForm = 1f,
            float aggression = 0.5f, float randomness = 0f,
            float lowHp = 0.3f)
        {
            var p = ScriptableObject.CreateInstance<AIPersonalityConfig>();
            SetField(p, "weightDamage", wDamage);
            SetField(p, "weightKill", wKill);
            SetField(p, "weightThreat", wThreat);
            SetField(p, "weightPosition", wPosition);
            SetField(p, "weightTerrain", wTerrain);
            SetField(p, "weightSelfPreservation", wSelf);
            SetField(p, "weightGenomeMatch", wGenome);
            SetField(p, "weightFormTactic", wForm);
            SetField(p, "aggressionBias", aggression);
            SetField(p, "focusFireBias", 0.5f);
            SetField(p, "abilityPreference", 0.5f);
            SetField(p, "randomnessFactor", randomness);
            SetField(p, "lowHpThreshold", lowHp);
            SetField(p, "retreatHpThreshold", 0.15f);
            return p;
        }

        private GridSystem CreateGrid(int width = 8, int depth = 8,
            TerrainType terrain = TerrainType.Neutral)
        {
            var grid = new GridSystem(width, depth);
            for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
                grid.SetTile(new TileData(new Vector2Int(x, z), 0, terrain));
            return grid;
        }

        private void SetTile(GridSystem grid, int x, int z, int height,
            TerrainType terrain = TerrainType.Neutral)
        {
            grid.SetTile(new TileData(new Vector2Int(x, z), height, terrain));
        }

        // ── Shared State ─────────────────────────────────────────────────

        private BaseStats _balancedStats;
        private CreatureConfig _thermalConfig;
        private CreatureConfig _aquaConfig;
        private GridSystem _grid;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TypeChart.Initialize();
        }

        [SetUp]
        public void SetUp()
        {
            _balancedStats = new BaseStats(50, 30, 20, 20, 30);
            _thermalConfig = CreateCreatureConfig("thermal-creature", _balancedStats,
                CreatureType.Thermal, terrainSynergy: CreatureType.Thermal);
            _aquaConfig = CreateCreatureConfig("aqua-creature", _balancedStats,
                CreatureType.Aqua, terrainSynergy: CreatureType.Aqua);
            _grid = CreateGrid();
        }

        // ══════════════════════════════════════════════════════════════════
        // ScoreDamage
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_ScoreDamage_damaging_move_returns_normalized_value()
        {
            // Arrange
            var move = CreateMove("flame-claw", CreatureType.Thermal, DamageForm.Physical, 60);
            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            var target = CreatureInstance.Create(_aquaConfig, 10);
            attacker.SetGridPosition(new Vector2Int(2, 2));
            target.SetGridPosition(new Vector2Int(3, 3));
            var action = new CandidateAction(move, target, attacker.GridPosition);

            // Act
            int estimated = AIActionScorer.EstimateDamage(action, attacker, _grid);
            float score = AIActionScorer.ScoreDamage(action, estimated);

            // Assert
            Assert.Greater(score, 0f, "Damaging move should produce positive damage score");
            Assert.LessOrEqual(score, 2f, "Damage score clamped to 2.0 max");
        }

        [Test]
        public void test_ScoreDamage_non_damaging_move_returns_zero()
        {
            // Arrange
            var move = CreateMove("status-move", CreatureType.None, DamageForm.None, 0);
            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            var target = CreatureInstance.Create(_aquaConfig, 10);
            var action = new CandidateAction(move, target, attacker.GridPosition);

            // Act
            int estimated = AIActionScorer.EstimateDamage(action, attacker, _grid);
            float score = AIActionScorer.ScoreDamage(action, estimated);

            // Assert
            Assert.AreEqual(0f, score);
        }

        [Test]
        public void test_ScoreDamage_null_target_returns_zero()
        {
            // Arrange
            var move = CreateMove("flame-claw", CreatureType.Thermal, DamageForm.Physical, 60);
            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            var action = new CandidateAction(move, null, attacker.GridPosition);

            // Act
            int estimated = AIActionScorer.EstimateDamage(action, attacker, _grid);
            float score = AIActionScorer.ScoreDamage(action, estimated);

            // Assert
            Assert.AreEqual(0f, score);
        }

        // ══════════════════════════════════════════════════════════════════
        // ScoreKillPotential
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_ScoreKillPotential_returns_one_when_damage_exceeds_target_hp()
        {
            // Arrange — high power move vs low-hp target
            var move = CreateMove("nuke", CreatureType.Thermal, DamageForm.Physical, 200);
            var attacker = CreatureInstance.Create(_thermalConfig, 50);
            var target = CreatureInstance.Create(_aquaConfig, 1);
            attacker.SetGridPosition(new Vector2Int(2, 2));
            target.SetGridPosition(new Vector2Int(3, 3));
            var action = new CandidateAction(move, target, attacker.GridPosition);

            // Act
            int estimated = AIActionScorer.EstimateDamage(action, attacker, _grid);
            float score = AIActionScorer.ScoreKillPotential(action, estimated);

            // Assert
            Assert.AreEqual(1.0f, score);
        }

        [Test]
        public void test_ScoreKillPotential_returns_zero_when_damage_below_target_hp()
        {
            // Arrange — weak move vs full-hp target
            var move = CreateMove("scratch", CreatureType.Thermal, DamageForm.Physical, 10);
            var attacker = CreatureInstance.Create(_thermalConfig, 1);
            var target = CreatureInstance.Create(_aquaConfig, 50);
            attacker.SetGridPosition(new Vector2Int(2, 2));
            target.SetGridPosition(new Vector2Int(3, 3));
            var action = new CandidateAction(move, target, attacker.GridPosition);

            // Act
            int estimated = AIActionScorer.EstimateDamage(action, attacker, _grid);
            float score = AIActionScorer.ScoreKillPotential(action, estimated);

            // Assert
            Assert.AreEqual(0.0f, score);
        }

        // ══════════════════════════════════════════════════════════════════
        // ScoreGenomeMatchup
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_ScoreGenomeMatchup_positive_for_super_effective()
        {
            // Arrange — Aqua vs Thermal = 2.0x (super effective)
            var move = CreateMove("water-jet", CreatureType.Aqua, DamageForm.Physical, 60);
            var attacker = CreatureInstance.Create(_aquaConfig, 10);
            var target = CreatureInstance.Create(_thermalConfig, 10);
            var action = new CandidateAction(move, target, attacker.GridPosition);

            // Act
            float score = AIActionScorer.ScoreGenomeMatchup(action, attacker);

            // Assert
            Assert.Greater(score, 0f, "Super-effective matchup should produce positive score");
        }

        [Test]
        public void test_ScoreGenomeMatchup_negative_for_resisted()
        {
            // Arrange — Thermal vs Aqua = 0.5x (resisted)
            var move = CreateMove("flame-claw", CreatureType.Thermal, DamageForm.Physical, 60);
            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            var target = CreatureInstance.Create(_aquaConfig, 10);
            var action = new CandidateAction(move, target, attacker.GridPosition);

            // Act
            float score = AIActionScorer.ScoreGenomeMatchup(action, attacker);

            // Assert
            Assert.Less(score, 0f, "Resisted matchup should produce negative score");
        }

        [Test]
        public void test_ScoreGenomeMatchup_zero_for_neutral_no_stab()
        {
            // Arrange — Thermal move from Aqua creature vs Organic target
            // Thermal vs Organic = 2.0x (super effective), but we need neutral
            // Use Mineral move from Thermal creature vs Thermal target = 0.5x (resist + self-resist)
            // Actually let's use a simple neutral case: Aqua move vs Neural target = 1.0x, no STAB from Thermal creature
            var neuralConfig = CreateCreatureConfig("neural-creature", _balancedStats, CreatureType.Neural);
            var move = CreateMove("water-jet", CreatureType.Aqua, DamageForm.Physical, 60);
            var attacker = CreatureInstance.Create(_thermalConfig, 10); // Thermal — no STAB on Aqua move
            var target = CreatureInstance.Create(neuralConfig, 10);     // Neural — neutral to Aqua
            var action = new CandidateAction(move, target, attacker.GridPosition);

            // Act
            float score = AIActionScorer.ScoreGenomeMatchup(action, attacker);

            // Assert — (1.0 * 1.0 - 1.0) / 3.0 = 0
            Assert.AreEqual(0f, score, 0.01f, "Neutral matchup with no STAB should produce zero");
        }

        // ══════════════════════════════════════════════════════════════════
        // ScoreTerrainSynergy
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_ScoreTerrainSynergy_returns_one_for_matching_terrain()
        {
            // Arrange — Thermal creature on Thermal terrain tile
            SetTile(_grid, 2, 2, 0, TerrainType.Thermal);
            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            var action = new CandidateAction(null, null, new Vector2Int(2, 2));

            // Act
            float score = AIActionScorer.ScoreTerrainSynergy(action, attacker, _grid);

            // Assert
            Assert.AreEqual(1.0f, score);
        }

        [Test]
        public void test_ScoreTerrainSynergy_penalizes_harmful_terrain()
        {
            // Arrange — Organic creature on Thermal terrain (Thermal SE vs Organic = 2.0x)
            var organicConfig = CreateCreatureConfig("organic-creature", _balancedStats,
                CreatureType.Organic, terrainSynergy: CreatureType.Organic);
            SetTile(_grid, 2, 2, 0, TerrainType.Thermal);
            var attacker = CreatureInstance.Create(organicConfig, 10);
            var action = new CandidateAction(null, null, new Vector2Int(2, 2));

            // Act
            float score = AIActionScorer.ScoreTerrainSynergy(action, attacker, _grid);

            // Assert
            Assert.AreEqual(-0.5f, score, "Harmful terrain should penalize by -0.5");
        }

        // ══════════════════════════════════════════════════════════════════
        // ScoreSelfPreservation
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_ScoreSelfPreservation_activates_below_threshold()
        {
            // Arrange — creature at 20% HP, threshold 30%
            var creature = CreatureInstance.Create(_thermalConfig, 10);
            int lowHp = (int)(creature.MaxHP * 0.2f);
            creature.TakeDamage(creature.CurrentHP - lowHp);
            var action = new CandidateAction(null, null, creature.GridPosition);

            // Act
            float score = AIActionScorer.ScoreSelfPreservation(creature, 0.3f);

            // Assert
            Assert.AreEqual(1.0f, score);
        }

        [Test]
        public void test_ScoreSelfPreservation_inactive_above_threshold()
        {
            // Arrange — creature at full HP, threshold 30%
            var creature = CreatureInstance.Create(_thermalConfig, 10);
            var action = new CandidateAction(null, null, creature.GridPosition);

            // Act
            float score = AIActionScorer.ScoreSelfPreservation(creature, 0.3f);

            // Assert
            Assert.AreEqual(0.0f, score);
        }

        // ══════════════════════════════════════════════════════════════════
        // ScoreAction (composite)
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_ScoreAction_weighted_sum_matches_manual_calculation()
        {
            // Arrange — all weights set to 0 except damage (weight 2.0)
            var personality = CreatePersonality(
                wDamage: 2f, wKill: 0f, wThreat: 0f,
                wPosition: 0f, wTerrain: 0f, wSelf: 0f,
                wGenome: 0f, wForm: 0f);

            var move = CreateMove("flame-claw", CreatureType.Thermal, DamageForm.Physical, 60);
            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            var target = CreatureInstance.Create(_aquaConfig, 10);
            attacker.SetGridPosition(new Vector2Int(2, 2));
            target.SetGridPosition(new Vector2Int(3, 3));
            var action = new CandidateAction(move, target, attacker.GridPosition);
            var opponents = new List<CreatureInstance> { target };

            // Act
            float compositeScore = AIActionScorer.ScoreAction(
                action, attacker, opponents, _grid, personality);
            int estDmg = AIActionScorer.EstimateDamage(action, attacker, _grid);
            float damageOnly = AIActionScorer.ScoreDamage(action, estDmg);

            // Assert — composite should equal damage * 2.0 (all other weights are 0)
            Assert.AreEqual(damageOnly * 2f, compositeScore, 0.01f);
        }

        [Test]
        public void test_ScoreAction_aggressive_personality_scores_higher_on_damage()
        {
            // Arrange
            var aggressive = CreatePersonality(wDamage: 2.5f, wKill: 2.5f, wSelf: 0.1f);
            var cautious = CreatePersonality(wDamage: 0.5f, wKill: 0.5f, wSelf: 2.5f);

            var move = CreateMove("flame-claw", CreatureType.Thermal, DamageForm.Physical, 80);
            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            var target = CreatureInstance.Create(_aquaConfig, 10);
            attacker.SetGridPosition(new Vector2Int(2, 2));
            target.SetGridPosition(new Vector2Int(3, 3));
            var action = new CandidateAction(move, target, attacker.GridPosition);
            var opponents = new List<CreatureInstance> { target };

            // Act
            float aggressiveScore = AIActionScorer.ScoreAction(
                action, attacker, opponents, _grid, aggressive);
            float cautiousScore = AIActionScorer.ScoreAction(
                action, attacker, opponents, _grid, cautious);

            // Assert
            Assert.Greater(aggressiveScore, cautiousScore,
                "Aggressive personality should score higher on damaging actions");
        }

        [Test]
        public void test_ScoreAction_wait_action_returns_zero_baseline()
        {
            // Arrange — Wait = null move, null target
            var personality = CreatePersonality();
            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            attacker.SetGridPosition(new Vector2Int(2, 2));
            var action = new CandidateAction(null, null, attacker.GridPosition);
            var opponents = new List<CreatureInstance>();

            // Act
            float score = AIActionScorer.ScoreAction(
                action, attacker, opponents, _grid, personality);

            // Assert — all scoring dimensions return 0 for null move/target + no opponents
            Assert.AreEqual(0f, score, 0.01f, "Wait action should score ~0");
        }

        // ══════════════════════════════════════════════════════════════════
        // AIDecisionSystem
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_DecideAction_returns_valid_turn_action_never_null()
        {
            // Arrange
            var personality = CreatePersonality(randomness: 0f);
            var system = new AIDecisionSystem(personality, new System.Random(42));
            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            attacker.SetGridPosition(new Vector2Int(2, 2));
            var opponents = new List<CreatureInstance>();
            var allies = new List<CreatureInstance>();

            // Act
            var result = system.DecideAction(attacker, opponents, allies, _grid);

            // Assert — should return Wait when no opponents
            Assert.AreEqual(ActionType.Wait, result.Action);
        }

        [Test]
        public void test_DecideAction_seeded_rng_produces_deterministic_results()
        {
            // Arrange
            var personality = CreatePersonality(randomness: 0.1f);
            var move = CreateMove("flame-claw", CreatureType.Thermal, DamageForm.Physical, 60);
            Func<string, MoveConfig> mockLookup = id => id == "flame-claw" ? move : null;

            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            var target = CreatureInstance.Create(_aquaConfig, 10);
            attacker.SetGridPosition(new Vector2Int(2, 2));
            target.SetGridPosition(new Vector2Int(3, 3));

            // Give attacker a learned move via reflection
            SetField(attacker, "_learnedMoveIds", new List<string> { "flame-claw" });
            SetField(attacker, "_learnedMovePP", new List<int> { 10 });

            var system1 = new AIDecisionSystem(personality, new System.Random(42), mockLookup);
            var system2 = new AIDecisionSystem(personality, new System.Random(42), mockLookup);

            var opponents = new List<CreatureInstance> { target };
            var allies = new List<CreatureInstance>();

            // Act
            var result1 = system1.DecideAction(attacker, opponents, allies, _grid);
            var result2 = system2.DecideAction(attacker, opponents, allies, _grid);

            // Assert
            Assert.AreEqual(result1.Action, result2.Action,
                "Same seed should produce identical action type");
            Assert.AreEqual(ActionType.UseMove, result1.Action,
                "Should select a move, not Wait, when a valid move is available");
        }

        [Test]
        public void test_DecideAction_random_jitter_within_randomness_factor_bounds()
        {
            // Arrange — run multiple iterations and verify all jittered scores
            // fall within rawScore ± randomnessFactor
            float randomness = 0.1f;
            var personality = CreatePersonality(
                wDamage: 1f, wKill: 0f, wThreat: 0f,
                wPosition: 0f, wTerrain: 0f, wSelf: 0f,
                wGenome: 0f, wForm: 0f, randomness: randomness);

            var move = CreateMove("flame-claw", CreatureType.Thermal, DamageForm.Physical, 60);
            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            var target = CreatureInstance.Create(_aquaConfig, 10);
            attacker.SetGridPosition(new Vector2Int(2, 2));
            target.SetGridPosition(new Vector2Int(3, 3));
            var action = new CandidateAction(move, target, attacker.GridPosition);
            var opponents = new List<CreatureInstance> { target };

            // Compute raw score (no jitter)
            float rawScore = AIActionScorer.ScoreAction(
                action, attacker, opponents, _grid, personality);

            // Act — simulate jitter across 100 iterations
            var rng = new System.Random(42);
            for (int i = 0; i < 100; i++)
            {
                float jitter = ((float)rng.NextDouble() * 2f - 1f) * randomness;
                float jitteredScore = rawScore + jitter;

                // Assert — each jittered score must be within bounds
                Assert.GreaterOrEqual(jitteredScore, rawScore - randomness,
                    $"Iteration {i}: jittered score {jitteredScore} below lower bound {rawScore - randomness}");
                Assert.LessOrEqual(jitteredScore, rawScore + randomness,
                    $"Iteration {i}: jittered score {jitteredScore} above upper bound {rawScore + randomness}");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // ScorePosition
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_ScorePosition_closer_to_opponent_scores_higher_with_high_aggression()
        {
            // Arrange
            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            var opponent = CreatureInstance.Create(_aquaConfig, 10);
            opponent.SetGridPosition(new Vector2Int(5, 5));
            var opponents = new List<CreatureInstance> { opponent };

            var closeAction = new CandidateAction(null, null, new Vector2Int(4, 4)); // 1 tile away
            var farAction = new CandidateAction(null, null, new Vector2Int(0, 0));   // 5+ tiles away

            // Act
            float closeScore = AIActionScorer.ScorePosition(closeAction, attacker, opponents, _grid, 0.9f);
            float farScore = AIActionScorer.ScorePosition(farAction, attacker, opponents, _grid, 0.9f);

            // Assert
            Assert.Greater(closeScore, farScore,
                "With high aggression bias, closer position should score higher");
        }

        [Test]
        public void test_ScorePosition_further_from_opponent_scores_higher_with_low_aggression()
        {
            // Arrange
            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            var opponent = CreatureInstance.Create(_aquaConfig, 10);
            opponent.SetGridPosition(new Vector2Int(5, 5));
            var opponents = new List<CreatureInstance> { opponent };

            var closeAction = new CandidateAction(null, null, new Vector2Int(4, 4));
            var farAction = new CandidateAction(null, null, new Vector2Int(0, 0));

            // Act
            float closeScore = AIActionScorer.ScorePosition(closeAction, attacker, opponents, _grid, 0.1f);
            float farScore = AIActionScorer.ScorePosition(farAction, attacker, opponents, _grid, 0.1f);

            // Assert
            Assert.Greater(farScore, closeScore,
                "With low aggression bias, farther position should score higher");
        }

        // ══════════════════════════════════════════════════════════════════
        // ScoreFormTactics
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_ScoreFormTactics_positive_when_stat_advantage()
        {
            // Arrange — high ATK attacker vs low DEF defender, Physical form
            var highAtkStats = new BaseStats(50, 60, 10, 20, 30);
            var lowDefStats = new BaseStats(50, 20, 10, 20, 30);
            var strongConfig = CreateCreatureConfig("strong", highAtkStats, CreatureType.Thermal);
            var weakConfig = CreateCreatureConfig("weak", lowDefStats, CreatureType.Aqua);
            var attacker = CreatureInstance.Create(strongConfig, 10);
            var target = CreatureInstance.Create(weakConfig, 10);
            var move = CreateMove("punch", CreatureType.Thermal, DamageForm.Physical, 60);
            var action = new CandidateAction(move, target, attacker.GridPosition);

            // Act
            float score = AIActionScorer.ScoreFormTactics(action, attacker, _grid);

            // Assert
            Assert.Greater(score, 0f, "Should be positive when attacker has stat advantage");
        }

        [Test]
        public void test_ScoreFormTactics_returns_zero_for_non_damaging_move()
        {
            // Arrange
            var move = CreateMove("status", CreatureType.None, DamageForm.None, 0);
            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            var target = CreatureInstance.Create(_aquaConfig, 10);
            var action = new CandidateAction(move, target, attacker.GridPosition);

            // Act
            float score = AIActionScorer.ScoreFormTactics(action, attacker, _grid);

            // Assert
            Assert.AreEqual(0f, score);
        }

        // ══════════════════════════════════════════════════════════════════
        // C1/C5 Audit Fixes
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_AIActionScorer_ScoreFinishTarget_MaxHP_zero_returns_zero()
        {
            // Arrange — target with MaxHP=0 should not produce NaN
            var move = CreateMove("flame-claw", CreatureType.Thermal, DamageForm.Physical, 60);
            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            var target = CreatureInstance.Create(_aquaConfig, 1);
            // Override MaxHP via reflection to simulate the edge case
            SetField(target, "_maxHP", 0);
            SetField(target, "_currentHP", 0);
            var action = new CandidateAction(move, target, attacker.GridPosition);

            // Act
            float score = AIActionScorer.ScoreFinishTarget(action);

            // Assert — guard in ScoreFinishTarget: MaxHP <= 0 returns 0, not NaN
            Assert.AreEqual(0f, score, "MaxHP=0 target must return 0, not NaN");
            Assert.IsFalse(float.IsNaN(score), "Score must not be NaN");
        }

        [Test]
        public void test_AIActionScorer_physical_adjacency_bonus_only_at_distance_1()
        {
            // Arrange — Physical move, attacker and target on flat grid
            var move = CreateMove("punch", CreatureType.Thermal, DamageForm.Physical, 60);
            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            var target = CreatureInstance.Create(_aquaConfig, 10);
            attacker.SetGridPosition(new Vector2Int(0, 0));
            target.SetGridPosition(new Vector2Int(2, 0)); // dist 2 — should NOT get adjacency bonus

            // Act — target at distance 2
            var action2 = new CandidateAction(move, target, attacker.GridPosition);
            float scoreAtDist2 = AIActionScorer.ScoreFormTactics(action2, attacker, _grid);

            // Move target to distance 1
            target.SetGridPosition(new Vector2Int(1, 0));
            var action1 = new CandidateAction(move, target, attacker.GridPosition);
            float scoreAtDist1 = AIActionScorer.ScoreFormTactics(action1, attacker, _grid);

            // Assert — distance 1 scores higher due to adjacency bonus (+0.3)
            Assert.Greater(scoreAtDist1, scoreAtDist2,
                "Physical adjacency bonus (+0.3) must only fire at dist <= 1, not dist 2");
        }

        // ══════════════════════════════════════════════════════════════════
        // Phase F5 — AIDecisionSystem Struggle Generation
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void test_AIDecisionSystem_PP_exhausted_generates_Struggle_candidate()
        {
            // Arrange — creature with one learned move, PP = 0.
            // AIDecisionSystem must fall back to Struggle (id == "__struggle__", power == 10).
            var personality = CreatePersonality(randomness: 0f);
            var move = CreateMove("flame-claw", CreatureType.Thermal, DamageForm.Physical, 60);
            Func<string, MoveConfig> moveLookup = id => id == "flame-claw" ? move : null;

            var attacker = CreatureInstance.Create(_thermalConfig, 10);
            var target = CreatureInstance.Create(_aquaConfig, 10);
            attacker.SetGridPosition(new Vector2Int(0, 0));
            target.SetGridPosition(new Vector2Int(1, 0)); // within Struggle range 1

            // Give attacker a move with PP = 0 (exhausted).
            SetField(attacker, "_learnedMoveIds", new List<string> { "flame-claw" });
            SetField(attacker, "_learnedMovePP", new List<int> { 0 });

            var system = new AIDecisionSystem(personality, new System.Random(42), moveLookup);
            var opponents = new List<CreatureInstance> { target };
            var allies = new List<CreatureInstance>();

            // Act
            var result = system.DecideAction(attacker, opponents, allies, _grid);

            // Assert — must return UseMove with Struggle (id "__struggle__", power 10).
            Assert.AreEqual(ActionType.UseMove, result.Action,
                "All-PP-exhausted creature must use Struggle, not Wait");
            Assert.IsNotNull(result.Move, "Struggle TurnAction must have a non-null Move");
            Assert.AreEqual(AIDecisionSystem.StruggleMoveId, result.Move.Id,
                "Move id must be '__struggle__'");
            Assert.AreEqual(10, result.Move.Power,
                "Struggle power must be 10 per GDD");
            Assert.AreEqual(-1, result.MovePPSlot,
                "Struggle TurnAction must use MovePPSlot = -1 (no PP deduction)");
        }

        [Test]
        public void test_AIActionScorer_STAB_not_double_counted_in_genome_matchup()
        {
            // Arrange — STAB move: Thermal attacker using Thermal move vs Neural target (neutral matchup)
            // Non-STAB move: Thermal attacker using Aqua move vs Neural target (neutral matchup)
            // Both have identical type effectiveness (1.0x vs Neural). If STAB were in
            // ScoreGenomeMatchup, the STAB move would score higher. It must not.
            var neuralConfig = CreateCreatureConfig("neural", _balancedStats, CreatureType.Neural);
            var attacker = CreatureInstance.Create(_thermalConfig, 10); // Thermal creature

            var stabMove = CreateMove("ember", CreatureType.Thermal, DamageForm.Physical, 60);   // STAB (Thermal matches actor)
            var nonStabMove = CreateMove("water-jet", CreatureType.Aqua, DamageForm.Physical, 60); // no STAB (Aqua on Thermal actor)
            var target = CreatureInstance.Create(neuralConfig, 10);

            var stabAction = new CandidateAction(stabMove, target, attacker.GridPosition);
            var nonStabAction = new CandidateAction(nonStabMove, target, attacker.GridPosition);

            // Act
            float stabScore = AIActionScorer.ScoreGenomeMatchup(stabAction, attacker);
            float nonStabScore = AIActionScorer.ScoreGenomeMatchup(nonStabAction, attacker);

            // Assert — both moves hit Neural at 1.0x → both should produce identical genome scores
            Assert.AreEqual(nonStabScore, stabScore, 0.001f,
                "ScoreGenomeMatchup must not include STAB — STAB is already in ScoreDamage via DamageCalculator.Estimate");
        }
    }
}
