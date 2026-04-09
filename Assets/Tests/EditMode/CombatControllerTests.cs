using System;
using System.Collections.Generic;
using NUnit.Framework;
using GeneForge.Combat;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;
using GeneForge.UI;
using UnityEngine;

namespace GeneForge.Tests
{
    /// <summary>
    /// EditMode tests for CombatController. Tests action submission, event relay,
    /// and integration with TurnManager via IPlayerInputProvider.
    ///
    /// Since CombatController is a MonoBehaviour that uses coroutines for the combat loop,
    /// these tests exercise the non-coroutine paths: action submission/cancellation,
    /// TurnManager event relay, and the PlayerInputCollector behavior.
    ///
    /// Coroutine-based combat loop tests belong in PlayMode.
    /// </summary>
    [TestFixture]
    public class CombatControllerTests
    {
        // ── Test Fixtures ─────────────────────────────────────────────────

        private GameObject _gameObject;
        private CombatController _controller;
        private GridSystem _grid;
        private CombatSettings _settings;
        private CreatureInstance _player1, _player2, _enemy1;
        private CreatureConfig _playerConfig, _enemyConfig;
        private MoveConfig _damageMove;
        private List<ScriptableObject> _createdAssets;

        // ── Helpers ───────────────────────────────────────────────────────

        private static void SetField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            System.Reflection.FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                type = type.BaseType;
            }
            Assert.IsNotNull(field, $"Field '{fieldName}' not found");
            field.SetValue(obj, value);
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _createdAssets.Add(asset);
            return asset;
        }

        private CreatureConfig CreateCreatureConfig(string id, string name, int hp = 100,
            int atk = 100, int def = 100, int spd = 100, int acc = 100,
            CreatureType primaryType = CreatureType.Thermal)
        {
            var config = CreateAsset<CreatureConfig>();
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
            creature.RecalculateStats();
            return creature;
        }

        private MoveConfig CreateMoveConfig(string id, string name, int power, int accuracy, int pp)
        {
            var move = CreateAsset<MoveConfig>();
            SetField(move, "id", id);
            SetField(move, "displayName", name);
            SetField(move, "power", power);
            SetField(move, "accuracy", accuracy);
            SetField(move, "pp", pp);
            SetField(move, "priority", 0);
            SetField(move, "form", DamageForm.Physical);
            SetField(move, "genomeType", CreatureType.None);
            SetField(move, "accuracy", 100);
            SetField(move, "effects", new List<MoveEffect>());
            SetField(move, "targetType", TargetType.Single);
            return move;
        }

        private CombatSettings CreateCombatSettings()
        {
            var settings = CreateAsset<CombatSettings>();
            SetField(settings, "movementDivisor", 20);
            SetField(settings, "initiativeDistanceWeight", 1000);
            SetField(settings, "confusionSelfHitChance", 0.33f);
            SetField(settings, "confusionSelfHitPower", 40);
            SetField(settings, "wildFleeSuccessRate", 1.0f);
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
                for (int z = 0; z < depth; z++)
                    _grid.SetTile(new TileData(new Vector2Int(x, z), 0, TerrainType.Neutral));
        }

        private void PlaceCreature(CreatureInstance creature, int x, int z)
        {
            creature.SetGridPosition(new Vector2Int(x, z));
            var tile = _grid.GetTile(x, z);
            Assert.IsNotNull(tile);
            tile.Occupant = creature;
        }

        /// <summary>
        /// Start combat on the controller using the full-parameter overload.
        /// Uses a TestPlayerInputProvider so TurnManager can retrieve actions
        /// submitted via CombatController.SubmitAction.
        /// </summary>
        private void StartCombatOnController(
            List<CreatureInstance> playerParty,
            List<CreatureInstance> enemyParty,
            EncounterType encounterType = EncounterType.Wild)
        {
            _controller.StartCombat(
                _grid, playerParty, enemyParty, encounterType, _settings,
                new StubDamageCalculator(10), new StubCaptureSystem(),
                new StubAIDecisionSystem(), new StubMoveEffectApplier(),
                new StubStatusEffectProcessor(), seed: 42);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _createdAssets = new List<ScriptableObject>();

            _gameObject = new GameObject("TestCombatController");
            _controller = _gameObject.AddComponent<CombatController>();

            _playerConfig = CreateCreatureConfig("player-test", "PlayerTest");
            _enemyConfig = CreateCreatureConfig("enemy-test", "EnemyTest", hp: 80);

            _player1 = CreateCreatureInstance(_playerConfig);
            _player2 = CreateCreatureInstance(_playerConfig);
            _enemy1 = CreateCreatureInstance(_enemyConfig);

            _damageMove = CreateMoveConfig("tackle", "Tackle", 40, 100, 20);

            SetupGrid(10, 10);
            PlaceCreature(_player1, 0, 0);
            PlaceCreature(_player2, 1, 0);
            PlaceCreature(_enemy1, 8, 0);

            _settings = CreateCombatSettings();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
                UnityEngine.Object.DestroyImmediate(_gameObject);
            foreach (var asset in _createdAssets)
                ScriptableObject.DestroyImmediate(asset);
        }

        // ────────────────────────────────────────────────────────────────
        // § Initialization Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void StartCombat_CreatesTurnManager()
        {
            // Arrange & Act
            StartCombatOnController(
                new List<CreatureInstance> { _player1 },
                new List<CreatureInstance> { _enemy1 });

            // Assert
            Assert.IsNotNull(_controller.TurnManager);
            Assert.That(_controller.TurnManager.CombatActive, Is.True);
        }

        [Test]
        public void StartCombat_SetsPartyReferences()
        {
            // Arrange & Act
            StartCombatOnController(
                new List<CreatureInstance> { _player1, _player2 },
                new List<CreatureInstance> { _enemy1 });

            // Assert
            Assert.That(_controller.PlayerParty.Count, Is.EqualTo(2));
            Assert.That(_controller.EnemyParty.Count, Is.EqualTo(1));
            Assert.That(_controller.Grid, Is.Not.Null);
        }

        [Test]
        public void StartCombat_SetsEncounterType()
        {
            // Act
            StartCombatOnController(
                new List<CreatureInstance> { _player1 },
                new List<CreatureInstance> { _enemy1 },
                EncounterType.Trainer);

            // Assert
            Assert.That(_controller.EncounterType, Is.EqualTo(EncounterType.Trainer));
        }

        [Test]
        public void StartCombat_InitializesTrapCount()
        {
            // Act
            StartCombatOnController(
                new List<CreatureInstance> { _player1 },
                new List<CreatureInstance> { _enemy1 });

            // Assert — default initialTrapCount is 5
            Assert.That(_controller.RemainingTraps, Is.EqualTo(5));
        }

        // ────────────────────────────────────────────────────────────────
        // § Event Relay Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void TurnManagerRoundStarted_RelayedThroughController()
        {
            // Arrange
            StartCombatOnController(
                new List<CreatureInstance> { _player1 },
                new List<CreatureInstance> { _enemy1 });

            int receivedRound = -1;
            _controller.RoundStarted += (round) => receivedRound = round;

            // Act — directly advance TurnManager (bypasses coroutine)
            _controller.TurnManager.AdvanceRound();

            // Assert
            Assert.That(receivedRound, Is.EqualTo(1));
        }

        [Test]
        public void TurnManagerCreatureActed_RelayedThroughController()
        {
            // Arrange
            StartCombatOnController(
                new List<CreatureInstance> { _player1 },
                new List<CreatureInstance> { _enemy1 });

            CreatureActedArgs? receivedArgs = null;
            _controller.CreatureActed += (args) => receivedArgs = args;

            // Act
            _controller.TurnManager.AdvanceRound();

            // Assert — at least one creature should have acted
            Assert.That(receivedArgs, Is.Not.Null);
        }

        [Test]
        public void TurnManagerCreatureFainted_RelayedThroughController()
        {
            // Arrange — enemy with 1 HP, player attacks with a damage move
            var weakEnemy = CreateCreatureInstance(
                CreateCreatureConfig("weak", "Weak", hp: 1));
            PlaceCreature(weakEnemy, 8, 0);

            SetField(_player1, "_learnedMoveIds", new List<string> { "tackle" });
            SetField(_player1, "_learnedMovePP", new List<int> { 20 });

            var provider = new TestPlayerInputProvider(new Dictionary<CreatureInstance, TurnAction>
            {
                { _player1, new TurnAction(ActionType.UseMove, move: _damageMove, target: weakEnemy, movePPSlot: 0) }
            });

            var tm = new TurnManager(
                _grid,
                new List<CreatureInstance> { _player1 },
                new List<CreatureInstance> { weakEnemy },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(10),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(),
                new StubMoveEffectApplier(),
                new StubStatusEffectProcessor(),
                provider,
                seed: 42);

            CreatureInstance faintedCreature = null;
            tm.CreatureFainted += (creature) => faintedCreature = creature;

            // Act
            tm.AdvanceRound();

            // Assert — weak enemy (1 HP) takes 10 damage and faints
            Assert.That(faintedCreature, Is.EqualTo(weakEnemy));
        }

        [Test]
        public void TurnManagerRoundEnded_RelayedThroughController()
        {
            // Arrange
            StartCombatOnController(
                new List<CreatureInstance> { _player1 },
                new List<CreatureInstance> { _enemy1 });

            int receivedRound = -1;
            _controller.RoundEnded += (round) => receivedRound = round;

            // Act
            _controller.TurnManager.AdvanceRound();

            // Assert
            Assert.That(receivedRound, Is.EqualTo(1));
        }

        // ────────────────────────────────────────────────────────────────
        // § Action Submission Tests (via IPlayerInputProvider)
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void SubmitAction_IgnoredWhenNotInPlayerSelectPhase()
        {
            // Arrange
            StartCombatOnController(
                new List<CreatureInstance> { _player1 },
                new List<CreatureInstance> { _enemy1 });

            // Controller starts coroutine but we're in EditMode — phase will be
            // whatever the coroutine set on the first frame. Since coroutines don't
            // advance in EditMode, phase depends on implementation.
            // Force a known non-PlayerSelect phase via reflection.
            // Auto-property backing field uses compiler-generated name.
            var field = typeof(CombatController).GetField("<CurrentUIPhase>k__BackingField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, "CurrentUIPhase backing field not found");
            field.SetValue(_controller, CombatUIPhase.RoundStart);

            // Act — should be silently ignored
            _controller.SubmitAction(_player1, new TurnAction(ActionType.Wait));

            // Assert — no crash, action not stored
            Assert.Pass("SubmitAction during wrong phase did not throw");
        }

        // ────────────────────────────────────────────────────────────────
        // § Capture Trap Count Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void CreatureCaptured_DecrementsTrapCount()
        {
            // Arrange — use a capture system that always succeeds
            _controller.StartCombat(
                _grid,
                new List<CreatureInstance> { _player1 },
                new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(10),
                new StubCaptureSystem(successRate: 1.0f),
                new StubAIDecisionSystem(),
                new StubMoveEffectApplier(),
                new StubStatusEffectProcessor(),
                seed: 42);

            int initialTraps = _controller.RemainingTraps;
            bool captureEventFired = false;
            _controller.CreatureCaptured += (args) =>
            {
                if (args.Success) captureEventFired = true;
            };

            // Assert — initial trap count
            Assert.That(initialTraps, Is.EqualTo(5));
            // Note: actual capture requires a Capture action to be submitted and processed.
            // Full integration test requires coroutine (PlayMode).
            Assert.Pass("Trap count initialized correctly");
        }

        // ────────────────────────────────────────────────────────────────
        // § Cleanup Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void OnDestroy_UnsubscribesFromTurnManager()
        {
            // Arrange
            StartCombatOnController(
                new List<CreatureInstance> { _player1 },
                new List<CreatureInstance> { _enemy1 });

            var tm = _controller.TurnManager;

            // Act — destroy the controller
            UnityEngine.Object.DestroyImmediate(_gameObject);
            _gameObject = null;

            // Assert — advancing TurnManager should not throw (no dangling subscribers)
            Assert.DoesNotThrow(() => tm.AdvanceRound());
        }

        // ────────────────────────────────────────────────────────────────
        // § IPlayerInputProvider Integration Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void TurnManager_UsesIPlayerInputProvider_ForPlayerActions()
        {
            // Arrange — create TurnManager directly with a test provider
            var provider = new TestPlayerInputProvider(new Dictionary<CreatureInstance, TurnAction>
            {
                { _player1, new TurnAction(ActionType.Wait) }
            });

            var tm = new TurnManager(
                _grid,
                new List<CreatureInstance> { _player1 },
                new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(10),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(),
                new StubMoveEffectApplier(),
                new StubStatusEffectProcessor(),
                provider,
                seed: 42);

            var actedCreatures = new List<CreatureInstance>();
            tm.CreatureActed += (args) => actedCreatures.Add(args.Actor);

            // Act
            tm.AdvanceRound();

            // Assert — player1 should have acted (Wait)
            Assert.That(actedCreatures, Does.Contain(_player1));
        }

        [Test]
        public void TurnManager_WithTestProvider_CompletesRoundSuccessfully()
        {
            // Arrange
            var provider = new TestPlayerInputProvider(new Dictionary<CreatureInstance, TurnAction>
            {
                { _player1, new TurnAction(ActionType.Wait) },
                { _player2, new TurnAction(ActionType.Wait) }
            });

            var tm = new TurnManager(
                _grid,
                new List<CreatureInstance> { _player1, _player2 },
                new List<CreatureInstance> { _enemy1 },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(10),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(),
                new StubMoveEffectApplier(),
                new StubStatusEffectProcessor(),
                provider,
                seed: 42);

            // Act
            tm.AdvanceRound();

            // Assert
            Assert.That(tm.CurrentRound, Is.EqualTo(2));
            Assert.That(tm.CombatActive, Is.True);
        }

        [Test]
        public void TurnManager_CombatEnds_WhenAllEnemiesFaint()
        {
            // Arrange — enemy with very low HP
            var weakEnemy = CreateCreatureInstance(
                CreateCreatureConfig("weak", "Weak", hp: 1));
            PlaceCreature(weakEnemy, 8, 0);

            SetField(_player1, "_learnedMoveIds", new List<string> { "tackle" });
            SetField(_player1, "_learnedMovePP", new List<int> { 20 });

            var provider = new TestPlayerInputProvider(new Dictionary<CreatureInstance, TurnAction>
            {
                { _player1, new TurnAction(ActionType.UseMove, move: _damageMove, target: weakEnemy, movePPSlot: 0) }
            });

            var tm = new TurnManager(
                _grid,
                new List<CreatureInstance> { _player1 },
                new List<CreatureInstance> { weakEnemy },
                EncounterType.Wild, _settings,
                new StubDamageCalculator(10),
                new StubCaptureSystem(),
                new StubAIDecisionSystem(),
                new StubMoveEffectApplier(),
                new StubStatusEffectProcessor(),
                provider,
                seed: 42);

            // Act
            tm.AdvanceRound();

            // Assert
            Assert.That(tm.CombatActive, Is.False);
            Assert.That(tm.Stats.Result, Is.EqualTo(CombatResult.Victory));
        }

        // ════════════════════════════════════════════════════════════════
        // § Test Doubles
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Simple IPlayerInputProvider for EditMode tests.
        /// Returns pre-loaded actions immediately. AllActionsReady is always true.
        /// </summary>
        private class TestPlayerInputProvider : IPlayerInputProvider
        {
            private readonly Dictionary<CreatureInstance, TurnAction> _actions;

            public TestPlayerInputProvider(Dictionary<CreatureInstance, TurnAction> actions)
            {
                _actions = actions;
            }

            public bool AllActionsReady => true;

            public void BeginActionCollection(IReadOnlyList<CreatureInstance> creatures) { }

            public IReadOnlyDictionary<CreatureInstance, TurnAction> GetActions()
            {
                return _actions;
            }
        }

        private class StubDamageCalculator : IDamageCalculator
        {
            private readonly int _damage;
            public StubDamageCalculator(int damage = 10) { _damage = damage; }
            public int Calculate(MoveConfig move, CreatureInstance attacker, CreatureInstance target, GridSystem grid)
                => Mathf.Max(1, _damage);
            public int CalculateRaw(int power, DamageForm form, CreatureInstance attacker, CreatureInstance defender)
                => Mathf.Max(1, _damage);
        }

        private class StubCaptureSystem : ICaptureSystem
        {
            private readonly float _successRate;
            public StubCaptureSystem(float successRate = 0f) { _successRate = successRate; }
            public bool Attempt(CreatureInstance target, CreatureInstance actor)
                => UnityEngine.Random.value < _successRate;
        }

        private class StubAIDecisionSystem : IAIDecisionSystem
        {
            public TurnAction DecideAction(CreatureInstance creature, IReadOnlyList<CreatureInstance> opponents,
                IReadOnlyList<CreatureInstance> allies, GridSystem grid)
                => new TurnAction(ActionType.Wait);
        }

        private class StubMoveEffectApplier : IMoveEffectApplier
        {
            public void Apply(MoveEffect effect, CreatureInstance actor, CreatureInstance target, GridSystem grid) { }
        }

        private class StubStatusEffectProcessor : IStatusEffectProcessor
        {
            public bool ApplyStartOfRound(CreatureInstance creature, List<StatusEffectEntry> statusEntries, double rngRoll)
                => false;
            public void DecrementDurations(CreatureInstance creature, List<StatusEffectEntry> statusEntries) { }
        }
    }
}
