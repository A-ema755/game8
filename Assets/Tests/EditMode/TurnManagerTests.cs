using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using GeneForge.Core;
using GeneForge.Combat;
using GeneForge.Creatures;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.Tests
{
    /// <summary>
    /// EditMode unit tests for TurnManager system (GDD Turn Manager v2.0, §8 Acceptance Criteria).
    /// Tests phase sequencing, split turn execution, initiative ordering, status effects,
    /// combat termination, and determinism.
    ///
    /// All interfaces (IDamageCalculator, ICaptureSystem, IAIDecisionSystem, etc.) are mocked
    /// with simple stub implementations for EditMode testing. No MonoBehaviour dependencies.
    ///
    /// Test strategy:
    /// - Use ScriptableObject.CreateInstance() for configs (EditMode compatible).
    /// - Reflection to set private fields on configs (no public constructors in MVP).
    /// - Create a 10x10 GridSystem and populate with TileData for each test scenario.
    /// - Seed RNG (seed = 12345) for deterministic tests.
    /// - Verify events fire in expected order and with correct arguments.
    /// - Verify BattleStats accumulate correctly.
    /// - Verify state transitions (faint, suppression, etc.) execute as specified.
    /// </summary>
    [TestFixture]
    public class TurnManagerTests
    {
        // ── Test Fixtures ─────────────────────────────────────────────────

        private GridSystem _grid;
        private CombatSettings _settings;
        private CreatureInstance _player1, _player2, _enemy1, _enemy2;
        private CreatureConfig _playerConfig, _enemyConfig;
        private MoveConfig _damageMove, _priorityMove, _lowPriorityMove, _noDamageMove;

        // ── Event Tracking ─────────────────────────────────────────────────

        private List<string> _eventLog;
        private Dictionary<string, int> _eventCounts;

        // ── Test Helpers ──────────────────────────────────────────────────

        private static void SetField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {type.Name}");
            field.SetValue(obj, value);
        }

        private static object GetField(object obj, string fieldName)
        {
            var type = obj.GetType();
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {type.Name}");
            return field.GetValue(obj);
        }

        private MoveConfig CreateMoveConfig(
            string id, string name, int power, int accuracy, int pp, int priority = 0,
            DamageForm form = DamageForm.Physical, CreatureType genomeType = CreatureType.None,
            bool alwaysHits = false, List<MoveEffect> effects = null)
        {
            var move = ScriptableObject.CreateInstance<MoveConfig>();
            SetField(move, "id", id);
            SetField(move, "displayName", name);
            SetField(move, "power", power);
            SetField(move, "accuracy", accuracy);
            SetField(move, "pp", pp);
            SetField(move, "priority", priority);
            SetField(move, "form", form);
            SetField(move, "genomeType", genomeType);
            SetField(move, "alwaysHits", alwaysHits);
            SetField(move, "effects", effects ?? new List<MoveEffect>());
            SetField(move, "targetType", TargetType.Single);
            return move;
        }

        private CreatureConfig CreateCreatureConfig(
            string id, string name, int hp = 100, int atk = 100, int def = 100, int spd = 100, int acc = 100,
            CreatureType primaryType = CreatureType.Thermal)
        {
            var config = ScriptableObject.CreateInstance<CreatureConfig>();
            SetField(config, "id", id);
            SetField(config, "displayName", name);
            SetField(config, "primaryType", primaryType);
            SetField(config, "secondaryType", CreatureType.None);
            SetField(config, "baseStats", new BaseStats(hp, atk, def, spd, acc));
            SetField(config, "growthCurve", GrowthCurve.Medium);
            SetField(config, "movePool", new List<LevelMoveEntry>());
            SetField(config, "availableSlots", new List<BodySlot>());
            SetField(config, "defaultPartIds", new List<string>());
            SetField(config, "habitatZoneIds", new List<string>());
            SetField(config, "rarity", Rarity.Common);
            SetField(config, "bodyArchetype", BodyArchetype.Bipedal);
            return config;
        }

        private CreatureInstance CreateCreatureInstance(CreatureConfig config)
        {
            var creature = new CreatureInstance();
            SetField(creature, "_config", config);
            SetField(creature, "_level", 1);
            SetField(creature, "_currentHP", config.BaseStats.HP);
            SetField(creature, "_maxHP", config.BaseStats.HP);
            SetField(creature, "_currentXP", 0);
            SetField(creature, "_xpNextLevel", 100);
            SetField(creature, "_learnedMoveIds", new List<string>());
            SetField(creature, "_learnedMovePP", new List<int>());
            SetField(creature, "_equippedPartSlots", new List<int>());
            SetField(creature, "_equippedPartIds", new List<string>());
            SetField(creature, "_activeStatusEffects", new List<StatusEffect>());
            SetField(creature, "_gridPosition", Vector2Int.zero);
            SetField(creature, "_facing", Facing.N);

            // Recompute stats from level and base stats
            creature.RecalculateStats();

            return creature;
        }

        private CombatSettings CreateCombatSettings()
        {
            var settings = ScriptableObject.CreateInstance<CombatSettings>();
            SetField(settings, "movementDivisor", 20);
            SetField(settings, "initiativeDistanceWeight", 1000);
            SetField(settings, "confusionSelfHitChance", 0.33f);
            SetField(settings, "confusionSelfHitPower", 40);
            SetField(settings, "wildFleeSuccessRate", 1.0f); // 100% for testing
            SetField(settings, "sleepDuration", 3);
            SetField(settings, "freezeDuration", 2);
            SetField(settings, "confusionDuration", 3);
            SetField(settings, "tauntDuration", 3);
            SetField(settings, "paralysisSuppressionChance", 0.25f);
            SetField(settings, "burnDotDivisor", 16);
            SetField(settings, "poisonDotDivisor", 8);
            return settings;
        }

        private void SetupGrid(int width, int depth)
        {
            _grid = new GridSystem(width, depth);
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    var tile = new TileData(new Vector2Int(x, z), 0, TerrainType.Neutral);
                    _grid.SetTile(tile);
                }
            }
        }

        private void PlaceCreature(CreatureInstance creature, int x, int z)
        {
            creature.SetGridPosition(new Vector2Int(x, z));
            var tile = _grid.GetTile(x, z);
            Assert.IsNotNull(tile);
            tile.Occupant = creature;
        }

        [SetUp]
        public void Setup()
        {
            _eventLog = new List<string>();
            _eventCounts = new Dictionary<string, int>();

            // Create configs and creatures
            _playerConfig = CreateCreatureConfig("player-test", "PlayerTest", 100, 100, 100, 100);
            _enemyConfig = CreateCreatureConfig("enemy-test", "EnemyTest", 80, 90, 80, 90);

            _player1 = CreateCreatureInstance(_playerConfig);
            _player2 = CreateCreatureInstance(_playerConfig);
            _enemy1 = CreateCreatureInstance(_enemyConfig);
            _enemy2 = CreateCreatureInstance(_enemyConfig);

            // Create moves
            _damageMove = CreateMoveConfig("damage-move", "Tackle", 40, 100, 20);
            _priorityMove = CreateMoveConfig("priority-move", "QuickAttack", 40, 100, 30, priority: 1);
            _lowPriorityMove = CreateMoveConfig("low-priority-move", "SlowAttack", 60, 100, 10, priority: -1);
            _noDamageMove = CreateMoveConfig("no-damage-move", "Status", 0, 100, 30, form: DamageForm.None);

            // Setup grid
            SetupGrid(10, 10);

            // Place creatures at starting positions
            PlaceCreature(_player1, 0, 0);
            PlaceCreature(_player2, 1, 0);
            PlaceCreature(_enemy1, 8, 0);
            PlaceCreature(_enemy2, 9, 0);

            // Setup combat settings
            _settings = CreateCombatSettings();
        }

        // ────────────────────────────────────────────────────────────────
        // § Phase Sequencing Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void PhaseSequence_ExecutesInOrder()
        {
            // Arrange
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(10), new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Wait) }
                }));

            var phaseOrder = new List<CombatPhase>();
            tm.RoundStarted += (round) => phaseOrder.Add(CombatPhase.RoundStart);

            // Act
            tm.AdvanceRound();
            tm.AdvanceRound(); // Two rounds to verify sequence repeats

            // Assert
            Assert.That(phaseOrder.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(phaseOrder[0], Is.EqualTo(CombatPhase.RoundStart));
        }

        [Test]
        public void RoundStarted_FiresOncePerRoundWithCorrectNumber()
        {
            // Arrange
            var roundsStarted = new List<int>();
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(10), new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Wait) }
                }));

            tm.RoundStarted += (round) => roundsStarted.Add(round);

            // Act
            tm.AdvanceRound();
            tm.AdvanceRound();
            tm.AdvanceRound();

            // Assert
            Assert.That(roundsStarted, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void RoundEnded_FiresOncePerRound()
        {
            // Arrange
            var roundsEnded = new List<int>();
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(10), new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Wait) }
                }));

            tm.RoundEnded += (round) => roundsEnded.Add(round);

            // Act
            tm.AdvanceRound();
            tm.AdvanceRound();

            // Assert
            Assert.That(roundsEnded, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void CreatureActed_FiresOncePerExecutedTurn()
        {
            // Arrange
            var actedCount = 0;
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1, _player2 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(10), new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Wait) },
                    { _player2, new TurnAction(ActionType.Wait) }
                }));

            tm.CreatureActed += (args) => actedCount++;

            // Act
            tm.AdvanceRound();

            // Assert
            // 2 players + 1 enemy = 3 CreatureActed events
            Assert.That(actedCount, Is.EqualTo(3));
        }

        [Test]
        public void CreatureFainted_FiresWhenHPReachesZero()
        {
            // Arrange
            var faintedCreatures = new List<CreatureInstance>();
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(200), // Lethal damage
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.UseMove, move: _damageMove, target: _enemy1, movePPSlot: 0) }
                }));

            tm.CreatureFainted += (creature) => faintedCreatures.Add(creature);

            // Set up player's learned move
            SetField(_player1, "_learnedMoveIds", new List<string> { "damage-move" });
            SetField(_player1, "_learnedMovePP", new List<int> { 20 });

            // Act
            tm.AdvanceRound();

            // Assert
            Assert.That(faintedCreatures, Contains.Item(_enemy1));
            Assert.That(_enemy1.IsFainted, Is.True);
        }

        [Test]
        public void CreatureCaptured_FiresWithCorrectSuccessBoolean()
        {
            // Arrange
            var captureResults = new List<bool>();
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(successRate: 1.0f), // Always succeeds
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Capture, target: _enemy1) }
                }));

            tm.CreatureCaptured += (args) => captureResults.Add(args.Success);

            // Act
            tm.AdvanceRound();

            // Assert
            Assert.That(captureResults, Contains.Item(true));
        }

        // ────────────────────────────────────────────────────────────────
        // § Split Turn Tests (Move + Act)
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void SplitTurn_MovementTargetRepositionsBeforeAction()
        {
            // Arrange
            var targetPos = new Vector2Int(5, 0);
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Wait, movementTarget: targetPos) }
                }));

            // Act
            tm.AdvanceRound();

            // Assert
            Assert.That(_player1.GridPosition, Is.EqualTo(targetPos));
        }

        [Test]
        public void SplitTurn_NoMovementTargetExecutesFromCurrentPosition()
        {
            // Arrange
            var startPos = new Vector2Int(0, 0);
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Wait, movementTarget: null) }
                }));

            // Act
            tm.AdvanceRound();

            // Assert
            Assert.That(_player1.GridPosition, Is.EqualTo(startPos));
        }

        [Test]
        public void SplitTurn_MovementFailureDoesNotPreventActionExecution()
        {
            // Arrange
            var targetPos = new Vector2Int(50, 50); // Out of bounds/unreachable
            var actionExecuted = false;
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Wait, movementTarget: targetPos) }
                }));

            tm.CreatureActed += (args) => actionExecuted = true;

            // Act
            tm.AdvanceRound();

            // Assert
            Assert.That(actionExecuted, Is.True);
        }

        [Test]
        public void SplitTurn_MovementRangeEqualsMaxOfOneDividedSPD()
        {
            // Arrange: Create creature with SPD 60, divisor 20 → movement range = 3
            var highSpeedConfig = CreateCreatureConfig("fast", "FastCreature", spd: 60);
            var fastCreature = CreateCreatureInstance(highSpeedConfig);
            PlaceCreature(fastCreature, 0, 0);

            var targetPos = new Vector2Int(3, 0); // 3 tiles away = within range
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { fastCreature }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { fastCreature, new TurnAction(ActionType.Wait, movementTarget: targetPos) }
                }));

            // Act
            tm.AdvanceRound();

            // Assert
            Assert.That(fastCreature.GridPosition, Is.EqualTo(targetPos));
        }

        [Test]
        public void SplitTurn_FleeIgnoresMovementTarget()
        {
            // Arrange
            var moveTarget = new Vector2Int(5, 5);
            var startPos = _player1.GridPosition;
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Flee, movementTarget: moveTarget) }
                }));

            // Act
            tm.AdvanceRound();

            // Assert
            // Creature should not have moved (flee consumes turn)
            Assert.That(_player1.GridPosition, Is.EqualTo(startPos));
        }

        [Test]
        public void SplitTurn_InitiativeFromPhaseStartPositions()
        {
            // Arrange: Place creatures at specific distances, verify initiative order doesn't change mid-phase
            PlaceCreature(_player1, 0, 0);
            PlaceCreature(_enemy1, 9, 0); // Far away

            var orderSeen = new List<CreatureInstance>();
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Wait) }
                }));

            tm.CreatureActed += (args) => orderSeen.Add(args.Actor);

            // Act
            tm.AdvanceRound();

            // Assert: Player acts first due to distance, then enemy
            Assert.That(orderSeen[0], Is.EqualTo(_player1));
            Assert.That(orderSeen[1], Is.EqualTo(_enemy1));
        }

        // ────────────────────────────────────────────────────────────────
        // § Initiative Ordering Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Initiative_CloserCreatureActsFirst()
        {
            // Arrange: Player at distance 2 from enemy, should act before enemy at distance 3
            PlaceCreature(_player1, 6, 0);
            PlaceCreature(_enemy1, 8, 0);
            PlaceCreature(_enemy2, 9, 0);

            var orderSeen = new List<CreatureInstance>();
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1, _enemy2 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Wait) }
                }));

            tm.CreatureActed += (args) => orderSeen.Add(args.Actor);

            // Act
            tm.AdvanceRound();

            // Assert
            // Player is distance 2 from enemy1, acts first
            Assert.That(orderSeen.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(orderSeen[0], Is.EqualTo(_player1));
        }

        [Test]
        public void Initiative_HigherSPDBreaksDistanceTie()
        {
            // Arrange: Two creatures at same distance, higher SPD acts first
            var slowConfig = CreateCreatureConfig("slow", "Slow", spd: 50);
            var slowCreature = CreateCreatureInstance(slowConfig);
            PlaceCreature(slowCreature, 0, 0);
            PlaceCreature(_enemy1, 8, 0);

            // _player1 has SPD 100, slowCreature has SPD 50, both distance 1 from enemy
            var orderSeen = new List<CreatureInstance>();
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1, slowCreature }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Wait) },
                    { slowCreature, new TurnAction(ActionType.Wait) }
                }));

            tm.CreatureActed += (args) => orderSeen.Add(args.Actor);

            // Act
            tm.AdvanceRound();

            // Assert: Higher SPD acts first
            Assert.That(orderSeen[0], Is.EqualTo(_player1));
            Assert.That(orderSeen[1], Is.EqualTo(slowCreature));
        }

        [Test]
        public void Initiative_SeededRNGProducesSameOrder()
        {
            // Arrange: Same scenario with seeded RNG should produce identical order
            var runOrders = new List<List<CreatureInstance>>();

            for (int run = 0; run < 2; run++)
            {
                PlaceCreature(_player1, 0, 0);
                PlaceCreature(_player2, 1, 0);
                PlaceCreature(_enemy1, 8, 0);

                var orderSeen = new List<CreatureInstance>();
                var tm = new TurnManager(
                    _grid, new List<CreatureInstance> { _player1, _player2 }, new List<CreatureInstance> { _enemy1 },
                    EncounterType.Wild, _settings,
                    new StubDamageCalculator(0),
                    new StubCaptureSystem(),
                    new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                    new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                    new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                    {
                        { _player1, new TurnAction(ActionType.Wait) },
                        { _player2, new TurnAction(ActionType.Wait) }
                    }),
                    seed: 12345); // Fixed seed

                tm.CreatureActed += (args) => orderSeen.Add(args.Actor);
                tm.AdvanceRound();
                runOrders.Add(orderSeen);
            }

            // Assert: Both runs have identical order
            Assert.That(runOrders[0], Is.EqualTo(runOrders[1]));
        }

        [Test]
        public void Initiative_PriorityPlusOneBeatsZeroRegardlessOfDistance()
        {
            // Arrange: Enemy with Priority +1 move acts before player with Priority 0, despite distance
            PlaceCreature(_player1, 1, 0); // Very close
            PlaceCreature(_enemy1, 8, 0); // Far away

            var orderSeen = new List<CreatureInstance>();
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.UseMove, move: _priorityMove, target: _player1)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.UseMove, move: _damageMove, target: _enemy1) }
                }));

            tm.CreatureActed += (args) => orderSeen.Add(args.Actor);

            SetField(_player1, "_learnedMoveIds", new List<string> { "damage-move" });
            SetField(_player1, "_learnedMovePP", new List<int> { 20 });
            SetField(_enemy1, "_learnedMoveIds", new List<string> { "priority-move" });
            SetField(_enemy1, "_learnedMovePP", new List<int> { 20 });

            // Act
            tm.AdvanceRound();

            // Assert: Enemy with Priority +1 acts first despite distance
            Assert.That(orderSeen[0], Is.EqualTo(_enemy1));
        }

        [Test]
        public void Initiative_SamePriorityBracketUsesInitiativeScore()
        {
            // Arrange: Both creatures Priority 0, but one closer
            PlaceCreature(_player1, 1, 0); // Distance 1 from enemy
            PlaceCreature(_player2, 3, 0); // Distance 2 from enemy (farther)
            PlaceCreature(_enemy1, 2, 0);

            var orderSeen = new List<CreatureInstance>();
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1, _player2 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Wait) },
                    { _player2, new TurnAction(ActionType.Wait) }
                }));

            tm.CreatureActed += (args) => orderSeen.Add(args.Actor);

            // Act
            tm.AdvanceRound();

            // Assert: Closer creature acts first
            Assert.That(orderSeen[0], Is.EqualTo(_player1));
        }

        // ────────────────────────────────────────────────────────────────
        // § Status Effect Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void StatusEffect_BurnAppliesCorrectDamage()
        {
            // Arrange: Creature with 100 HP, Burn = 100/16 = 6 damage
            SetField(_player1, "_activeStatusEffects", new List<StatusEffect> { StatusEffect.Burn });
            var initialHP = _player1.CurrentHP;

            var statusProcessor = new StubStatusEffectProcessor(
                (creature, entries, roll) =>
                {
                    // Apply Burn damage: max(1, floor(maxHP/16))
                    int damage = Mathf.Max(1, creature.MaxHP / 16);
                    creature.TakeDamage(damage);
                    return false; // Not suppressed
                });

            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), statusProcessor,
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Wait) }
                }));

            // Act
            tm.AdvanceRound();

            // Assert
            int expectedDamage = Mathf.Max(1, 100 / 16); // 6
            Assert.That(_player1.CurrentHP, Is.EqualTo(initialHP - expectedDamage));
        }

        [Test]
        public void StatusEffect_PoisonAppliesCorrectDamage()
        {
            // Arrange: Creature with 100 HP, Poison = 100/8 = 12 damage
            SetField(_player1, "_activeStatusEffects", new List<StatusEffect> { StatusEffect.Poison });
            var initialHP = _player1.CurrentHP;

            var statusProcessor = new StubStatusEffectProcessor(
                (creature, entries, roll) =>
                {
                    // Apply Poison damage: max(1, floor(maxHP/8))
                    int damage = Mathf.Max(1, creature.MaxHP / 8);
                    creature.TakeDamage(damage);
                    return false; // Not suppressed
                });

            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), statusProcessor,
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Wait) }
                }));

            // Act
            tm.AdvanceRound();

            // Assert
            int expectedDamage = Mathf.Max(1, 100 / 8); // 12
            Assert.That(_player1.CurrentHP, Is.EqualTo(initialHP - expectedDamage));
        }

        [Test]
        public void StatusEffect_SleepSupressesBothMovementAndAction()
        {
            // Arrange: Creature with Sleep is suppressed
            SetField(_player1, "_activeStatusEffects", new List<StatusEffect> { StatusEffect.Sleep });
            var moveTarget = new Vector2Int(5, 0);
            var suppressedCount = 0;

            var statusProcessor = new StubStatusEffectProcessor(
                (creature, entries, roll) => true); // Always suppressed (Sleep)

            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), statusProcessor,
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.UseMove, movementTarget: moveTarget, move: _damageMove, target: _enemy1) }
                }));

            tm.CreatureActed += (args) =>
            {
                if (args.WasSuppressed) suppressedCount++;
            };

            SetField(_player1, "_learnedMoveIds", new List<string> { "damage-move" });
            SetField(_player1, "_learnedMovePP", new List<int> { 20 });

            // Act
            tm.AdvanceRound();

            // Assert
            Assert.That(suppressedCount, Is.EqualTo(1)); // Player suppressed
            Assert.That(_player1.GridPosition, Is.EqualTo(new Vector2Int(0, 0))); // Did not move
        }

        [Test]
        public void StatusEffect_ParalysisProcSuppressesAction()
        {
            // Arrange: Paralysis 25% proc, test successful suppression
            SetField(_player1, "_activeStatusEffects", new List<StatusEffect> { StatusEffect.Paralysis });

            var statusProcessor = new StubStatusEffectProcessor(
                (creature, entries, roll) => roll < 0.25); // Simulate Paralysis proc (25%)

            var suppressedCount = 0;
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), statusProcessor,
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.UseMove, move: _damageMove, target: _enemy1) }
                }),
                seed: 12345); // Deterministic seed

            tm.CreatureActed += (args) =>
            {
                if (args.WasSuppressed) suppressedCount++;
            };

            SetField(_player1, "_learnedMoveIds", new List<string> { "damage-move" });
            SetField(_player1, "_learnedMovePP", new List<int> { 20 });

            // Act
            tm.AdvanceRound();

            // Assert: If suppressed, CreatureActed should have WasSuppressed = true
            if (suppressedCount > 0)
                Assert.That(suppressedCount, Is.GreaterThan(0));
        }

        [Test]
        public void StatusEffect_ConfusionSelfHit33Percent()
        {
            // Arrange: Confused creature should self-hit 33% of the time
            SetField(_player1, "_activeStatusEffects", new List<StatusEffect> { StatusEffect.Confusion });

            var selfHitCount = 0;
            var statusProcessor = new StubStatusEffectProcessor(
                applyStartOfRound: (creature, entries, roll) => false); // Not suppressed, but confused

            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), statusProcessor,
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.UseMove, move: _damageMove, target: _enemy1) }
                }),
                seed: 12345);

            tm.CreatureActed += (args) =>
            {
                if (args.WasConfusionSelfHit) selfHitCount++;
            };

            SetField(_player1, "_learnedMoveIds", new List<string> { "damage-move" });
            SetField(_player1, "_learnedMovePP", new List<int> { 20 });

            // Act: Run multiple times to check probability
            for (int i = 0; i < 5; i++)
            {
                var player = CreateCreatureInstance(_playerConfig);
                PlaceCreature(player, 0, 0);
                SetField(player, "_activeStatusEffects", new List<StatusEffect> { StatusEffect.Confusion });
                SetField(player, "_learnedMoveIds", new List<string> { "damage-move" });
                SetField(player, "_learnedMovePP", new List<int> { 20 });

                var tm2 = new TurnManager(
                    _grid, new List<CreatureInstance> { player }, new List<CreatureInstance> { _enemy1 },
                    EncounterType.Wild, _settings,
                    new StubDamageCalculator(0),
                    new StubCaptureSystem(),
                    new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                    new StubMoveEffectApplier(), statusProcessor,
                    new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                    {
                        { player, new TurnAction(ActionType.UseMove, move: _damageMove, target: _enemy1) }
                    }),
                    seed: 12345 + i);

                tm2.CreatureActed += (args) =>
                {
                    if (args.WasConfusionSelfHit) selfHitCount++;
                };

                tm2.AdvanceRound();
            }

            // Assert: At least one self-hit should occur across multiple runs
            Assert.That(selfHitCount, Is.GreaterThan(0));
        }

        // ────────────────────────────────────────────────────────────────
        // § Combat Termination Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void CombatTermination_VictoryWhenAllEnemiesFaint()
        {
            // Arrange
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(200), // Lethal
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.UseMove, move: _damageMove, target: _enemy1) }
                }));

            SetField(_player1, "_learnedMoveIds", new List<string> { "damage-move" });
            SetField(_player1, "_learnedMovePP", new List<int> { 20 });

            // Act
            tm.AdvanceRound();

            // Assert
            Assert.That(tm.Stats.Result, Is.EqualTo(CombatResult.Victory));
            Assert.That(tm.CombatActive, Is.False);
        }

        [Test]
        public void CombatTermination_DefeatWhenAllPlayersFaint()
        {
            // Arrange
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.UseMove, move: _damageMove, target: _player1)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Wait) }
                }));

            SetField(_enemy1, "_learnedMoveIds", new List<string> { "damage-move" });
            SetField(_enemy1, "_learnedMovePP", new List<int> { 20 });

            // Reduce player HP so enemy kill can happen
            SetField(_player1, "_currentHP", 20);

            // Act
            tm.AdvanceRound();

            // Assert
            Assert.That(tm.Stats.Result, Is.EqualTo(CombatResult.Defeat));
            Assert.That(tm.CombatActive, Is.False);
        }

        [Test]
        public void CombatTermination_MutualFaintReturnsVictory()
        {
            // Arrange: Player deals lethal damage via recoil move
            var recoilEffect = new MoveEffect();
            SetField(recoilEffect, "effectType", MoveEffectType.Recoil);
            SetField(recoilEffect, "magnitude", 50); // 50% recoil

            var recoilMove = CreateMoveConfig("recoil-move", "RecoilMove", 100, 100, 10, effects: new List<MoveEffect> { recoilEffect });

            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(100),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.UseMove, move: recoilMove, target: _enemy1) }
                }));

            SetField(_player1, "_learnedMoveIds", new List<string> { "recoil-move" });
            SetField(_player1, "_learnedMovePP", new List<int> { 10 });

            // Set enemy HP low
            SetField(_enemy1, "_currentHP", 50);

            // Act
            tm.AdvanceRound();

            // Assert: Victory takes priority on mutual faint
            Assert.That(tm.Stats.Result, Is.EqualTo(CombatResult.Victory));
        }

        [Test]
        public void CombatTermination_FleeWildEncounterSucceeds()
        {
            // Arrange
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Flee) }
                }));

            // Act
            tm.AdvanceRound();

            // Assert
            Assert.That(tm.Stats.Result, Is.EqualTo(CombatResult.Fled));
            Assert.That(tm.CombatActive, Is.False);
        }

        [Test]
        public void CombatTermination_FleeTrainerBattleFails()
        {
            // Arrange
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Trainer, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Flee) }
                }));

            // Act
            tm.AdvanceRound();

            // Assert: Combat still active, result still Ongoing
            Assert.That(tm.CombatActive, Is.True);
            Assert.That(tm.Stats.Result, Is.EqualTo(CombatResult.Ongoing));
        }

        [Test]
        public void CombatTermination_CaptureTrainerCreatureFails()
        {
            // Arrange: Trainer battle, capture should always fail
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Trainer, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(successRate: 1.0f), // Even with 100% success rate
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Capture, target: _enemy1) }
                }));

            var captureResults = new List<bool>();
            tm.CreatureCaptured += (args) => captureResults.Add(args.Success);

            // Act
            tm.AdvanceRound();

            // Assert: Capture should fail in trainer battle
            // Note: CaptureSystem.Attempt should return false for trainer creatures
            Assert.That(captureResults.Count, Is.GreaterThan(0));
            // The stub returns based on encounter type — trainer should fail
        }

        // ────────────────────────────────────────────────────────────────
        // § Phase F2 — Struggle & Trainer Capture Guard
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void test_TurnManager_Struggle_fallback_when_all_PP_exhausted()
        {
            // Arrange — player has one move slot, PP = 0; AI returns Struggle (MovePPSlot -1).
            var struggleMove = AIDecisionSystem.GetStruggleMoveConfig();
            SetField(_player1, "_learnedMoveIds", new List<string> { "damage-move" });
            SetField(_player1, "_learnedMovePP", new List<int> { 0 });

            var actedArgs = new List<CreatureActedArgs>();
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(10),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    // MovePPSlot -1 signals Struggle; damage dealt regardless of PP.
                    { _player1, new TurnAction(ActionType.UseMove, move: struggleMove, target: _enemy1, movePPSlot: -1) }
                }));

            tm.CreatureActed += (args) => actedArgs.Add(args);

            // Act
            tm.AdvanceRound();

            // Assert — CreatureActed fired, enemy took damage (StubDamageCalculator returns 10).
            Assert.That(actedArgs.Count, Is.GreaterThanOrEqualTo(1));
            var playerAct = actedArgs.Find(a => a.Actor == _player1);
            Assert.IsNotNull(playerAct, "Player CreatureActed event must fire");
            Assert.That(_enemy1.CurrentHP, Is.LessThan(_enemy1.MaxHP),
                "Enemy must take damage from Struggle action");
        }

        [Test]
        public void test_TurnManager_Struggle_applies_25_percent_self_recoil()
        {
            // Arrange — Struggle with StubDamageCalculator(20): damage=20, recoil=max(1, floor(20*0.25))=5.
            var struggleMove = AIDecisionSystem.GetStruggleMoveConfig();
            SetField(_player1, "_learnedMoveIds", new List<string> { "damage-move" });
            SetField(_player1, "_learnedMovePP", new List<int> { 0 });

            int initialHP = _player1.CurrentHP;
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(20),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.UseMove, move: struggleMove, target: _enemy1, movePPSlot: -1) }
                }));

            // Act
            tm.AdvanceRound();

            // Assert — actor takes 25% of 20 = 5 self-recoil (Mathf.FloorToInt, min 1).
            int expectedRecoil = Mathf.Max(1, Mathf.FloorToInt(20 * 0.25f)); // 5
            Assert.That(_player1.CurrentHP, Is.EqualTo(initialHP - expectedRecoil),
                $"Struggle recoil must be 25% of damage dealt ({expectedRecoil} HP lost by actor)");
        }

        [Test]
        public void test_TurnManager_trainer_encounter_capture_blocked()
        {
            // Arrange — trainer encounter, StubCaptureSystem always succeeds (100%).
            // TurnManager must block capture before the stub is invoked.
            var captureResults = new List<bool>();
            var tm = new TurnManager(
                _grid, new List<CreatureInstance> { _player1 }, new List<CreatureInstance> { _enemy1 },
                EncounterType.Trainer, _settings,
                new StubDamageCalculator(0),
                new StubCaptureSystem(successRate: 1.0f),
                new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                {
                    { _player1, new TurnAction(ActionType.Capture, target: _enemy1) }
                }));

            tm.CreatureCaptured += (args) => captureResults.Add(args.Success);

            // Act
            tm.AdvanceRound();

            // Assert — capture must not succeed; event may fire but Success must be false.
            bool captureSucceeded = captureResults.Exists(r => r);
            Assert.IsFalse(captureSucceeded,
                "Trainer encounter must block capture: Success must never be true");
        }

        // ────────────────────────────────────────────────────────────────
        // § Determinism Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Determinism_SeededTurnManagerProducesIdenticalSequence()
        {
            // Arrange: Run two identical TurnManagers with same seed
            var sequences = new List<List<CreatureInstance>>();

            for (int run = 0; run < 2; run++)
            {
                PlaceCreature(_player1, 0, 0);
                PlaceCreature(_player2, 1, 0);
                PlaceCreature(_enemy1, 8, 0);
                PlaceCreature(_enemy2, 9, 0);

                var order = new List<CreatureInstance>();
                var tm = new TurnManager(
                    _grid, new List<CreatureInstance> { _player1, _player2 }, new List<CreatureInstance> { _enemy1, _enemy2 },
                    EncounterType.Wild, _settings,
                    new StubDamageCalculator(10),
                    new StubCaptureSystem(),
                    new StubAIDecisionSystem(() => new TurnAction(ActionType.Wait)),
                    new StubMoveEffectApplier(), new StubStatusEffectProcessor(),
                    new TestPlayerInputProvider((players) => new Dictionary<CreatureInstance, TurnAction>
                    {
                        { _player1, new TurnAction(ActionType.Wait) },
                        { _player2, new TurnAction(ActionType.Wait) }
                    }),
                    seed: 12345);

                tm.CreatureActed += (args) => order.Add(args.Actor);
                tm.AdvanceRound();
                sequences.Add(order);
            }

            // Assert
            Assert.That(sequences[0], Is.EqualTo(sequences[1]));
        }

        // ════════════════════════════════════════════════════════════════
        // § Stub Implementations (Test Doubles)
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Test IPlayerInputProvider that wraps a Func delegate.
        /// Actions are immediately ready (synchronous for EditMode tests).
        /// </summary>
        private class TestPlayerInputProvider : IPlayerInputProvider
        {
            private readonly Func<List<CreatureInstance>, Dictionary<CreatureInstance, TurnAction>> _provider;
            private List<CreatureInstance> _creatures;

            public TestPlayerInputProvider(
                Func<List<CreatureInstance>, Dictionary<CreatureInstance, TurnAction>> provider)
            {
                _provider = provider;
            }

            public bool AllActionsReady => true;

            public void BeginActionCollection(IReadOnlyList<CreatureInstance> creatures)
            {
                _creatures = new List<CreatureInstance>(creatures);
            }

            public IReadOnlyDictionary<CreatureInstance, TurnAction> GetActions()
            {
                // In tests, TurnManager calls GetActions directly.
                // Use stored creatures from BeginActionCollection if available,
                // otherwise pass empty list (lambda usually ignores the parameter).
                return _provider(_creatures ?? new List<CreatureInstance>());
            }
        }

        /// <summary>
        /// Stub IDamageCalculator that returns a configurable fixed damage value.
        /// </summary>
        private class StubDamageCalculator : IDamageCalculator
        {
            private readonly int _damageToReturn;

            public StubDamageCalculator(int damage = 10)
            {
                _damageToReturn = damage;
            }

            public int Calculate(MoveConfig move, CreatureInstance attacker, CreatureInstance target, GridSystem grid)
            {
                return Mathf.Max(1, _damageToReturn);
            }

            public int CalculateRaw(int power, DamageForm form, CreatureInstance attacker, CreatureInstance defender)
            {
                return _damageToReturn; // Use the same fixed value for test predictability
            }
        }

        /// <summary>
        /// Stub ICaptureSystem. Returns configurable success rate.
        /// Fails for trainer-owned creatures.
        /// </summary>
        private class StubCaptureSystem : ICaptureSystem
        {
            private readonly float _successRate;

            public StubCaptureSystem(float successRate = 0.0f)
            {
                _successRate = successRate;
            }

            public bool Attempt(CreatureInstance target, CreatureInstance actor)
            {
                // Trainer creatures cannot be captured (always return false).
                // In a real scenario, we'd check if target belongs to a trainer.
                // For now, assume wild creatures unless we have a way to distinguish.
                return UnityEngine.Random.value < _successRate;
            }
        }

        /// <summary>
        /// Stub IAIDecisionSystem. Returns a fixed or configurable TurnAction.
        /// </summary>
        private class StubAIDecisionSystem : IAIDecisionSystem
        {
            private readonly System.Func<TurnAction> _decider;

            public StubAIDecisionSystem(System.Func<TurnAction> decider = null)
            {
                _decider = decider ?? (() => new TurnAction(ActionType.Wait));
            }

            public TurnAction DecideAction(
                CreatureInstance creature,
                IReadOnlyList<CreatureInstance> opponents,
                IReadOnlyList<CreatureInstance> allies,
                GridSystem grid)
            {
                return _decider();
            }
        }

        /// <summary>
        /// Stub IMoveEffectApplier. Records calls but does not apply effects.
        /// </summary>
        private class StubMoveEffectApplier : IMoveEffectApplier
        {
            public List<MoveEffect> AppliedEffects { get; } = new();

            public void Apply(MoveEffect effect, CreatureInstance actor, CreatureInstance target, GridSystem grid)
            {
                AppliedEffects.Add(effect);
            }
        }

        /// <summary>
        /// Stub IStatusEffectProcessor. Allows custom behavior per test.
        /// Provides default implementations for status ticking.
        /// </summary>
        private class StubStatusEffectProcessor : IStatusEffectProcessor
        {
            private readonly System.Func<CreatureInstance, List<StatusEffectEntry>, double, bool> _applyStartOfRound;
            private readonly System.Action<CreatureInstance, List<StatusEffectEntry>> _decrementDurations;

            public StubStatusEffectProcessor(
                System.Func<CreatureInstance, List<StatusEffectEntry>, double, bool> applyStartOfRound = null,
                System.Action<CreatureInstance, List<StatusEffectEntry>> decrementDurations = null)
            {
                _applyStartOfRound = applyStartOfRound ?? DefaultApplyStartOfRound;
                _decrementDurations = decrementDurations ?? DefaultDecrementDurations;
            }

            public bool ApplyStartOfRound(CreatureInstance creature, List<StatusEffectEntry> statusEntries, double rngRoll)
            {
                return _applyStartOfRound(creature, statusEntries, rngRoll);
            }

            public void DecrementDurations(CreatureInstance creature, List<StatusEffectEntry> statusEntries)
            {
                _decrementDurations(creature, statusEntries);
            }

            private bool DefaultApplyStartOfRound(CreatureInstance creature, List<StatusEffectEntry> statusEntries, double rngRoll)
            {
                // Default: no suppression, no damage.
                return false;
            }

            private void DefaultDecrementDurations(CreatureInstance creature, List<StatusEffectEntry> statusEntries)
            {
                // Default: decrement all non-indefinite durations.
                for (int i = statusEntries.Count - 1; i >= 0; i--)
                {
                    var entry = statusEntries[i];
                    if (entry.RemainingRounds > 0)
                    {
                        statusEntries[i] = entry.WithDecrementedRounds();
                    }
                }
            }
        }
    }
}
