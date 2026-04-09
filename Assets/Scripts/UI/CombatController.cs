using System;
using System.Collections;
using System.Collections.Generic;
using GeneForge.Combat;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Gameplay;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.UI
{
    /// <summary>
    /// UI phase states driven by CombatController. Maps TurnManager's internal
    /// synchronous phases to a coroutine-friendly state machine for UI.
    /// </summary>
    public enum CombatUIPhase
    {
        Inactive,
        RoundStart,
        PlayerSelect,
        PlayerExecuting,
        EnemyExecuting,
        RoundEnd,
        CombatEnd
    }

    /// <summary>
    /// MonoBehaviour wrapper for TurnManager. Bridges the synchronous
    /// AdvanceRound() call with a coroutine-based combat loop that yields
    /// during player input collection. Fires CombatUIPhase events for
    /// CombatHUDController to drive panel visibility.
    ///
    /// Implements the architecture described in design/ux/combat-ui-ux-spec.md §8.1.
    /// ADR-003 exception: MonoBehaviour required for coroutine lifecycle.
    /// </summary>
    public class CombatController : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────

        /// <summary>Fired when the UI phase changes. CombatHUDController subscribes to drive panel visibility.</summary>
        public event Action<CombatUIPhase> PhaseChanged;

        /// <summary>Relayed from TurnManager.CreatureActed for UI updates.</summary>
        public event Action<CreatureActedArgs> CreatureActed;

        /// <summary>Relayed from TurnManager.CreatureFainted for UI updates.</summary>
        public event Action<CreatureInstance> CreatureFainted;

        /// <summary>Relayed from TurnManager.CreatureCaptured for UI updates.</summary>
        public event Action<CaptureResultArgs> CreatureCaptured;

        /// <summary>Relayed from TurnManager.RoundStarted.</summary>
        public event Action<int> RoundStarted;

        /// <summary>Relayed from TurnManager.RoundEnded.</summary>
        public event Action<int> RoundEnded;

        // ── Serialized Fields ─────────────────────────────────────────────

        [SerializeField] int initialTrapCount = 5;

        // ── Public State ──────────────────────────────────────────────────

        /// <summary>Current UI phase.</summary>
        public CombatUIPhase CurrentUIPhase { get; private set; } = CombatUIPhase.Inactive;

        /// <summary>Remaining Gene Traps available this encounter.</summary>
        public int RemainingTraps => _remainingTraps;

        /// <summary>Encounter type for this combat session.</summary>
        public EncounterType EncounterType => _encounterType;

        /// <summary>The underlying TurnManager instance. Null before StartCombat.</summary>
        public TurnManager TurnManager => _turnManager;

        /// <summary>The grid system for this encounter.</summary>
        public GridSystem Grid => _grid;

        /// <summary>Player party creatures.</summary>
        public IReadOnlyList<CreatureInstance> PlayerParty => _playerParty;

        /// <summary>Enemy party creatures.</summary>
        public IReadOnlyList<CreatureInstance> EnemyParty => _enemyParty;

        /// <summary>The BattleContext for this encounter. Null before StartCombat.</summary>
        public BattleContext Context => _context;

        // ── Private State ─────────────────────────────────────────────────

        private TurnManager _turnManager;
        private GridSystem _grid;
        private List<CreatureInstance> _playerParty;
        private List<CreatureInstance> _enemyParty;
        private EncounterType _encounterType;
        private int _remainingTraps;
        private BattleContext _context;

        // Input collection via IPlayerInputProvider
        private PlayerInputCollector _inputCollector;

        // Reusable buffer for active (non-fainted) player creatures — avoids per-round allocation
        private readonly List<CreatureInstance> _activeBuffer = new();

        // Phase tracking for EnemyExecuting detection
        private bool _enemyPhaseSignaled;

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Convenience entry point: initialize and start combat from an EncounterConfig.
        /// Uses EncounterManager to build BattleContext, creates all combat subsystems
        /// with default constructors. Suitable for production use where ConfigLoader
        /// is initialized.
        /// </summary>
        /// <param name="config">Encounter configuration ScriptableObject.</param>
        /// <param name="playerParty">Player's current party state.</param>
        /// <param name="seed">RNG seed. 0 = non-deterministic.</param>
        public void StartCombat(EncounterConfig config, PartyState playerParty, int seed = 0)
        {
            var encounterManager = new EncounterManager();
            var context = encounterManager.InitializeEncounter(config, playerParty);

            _context = context;

            // Resolve AI personality — use first available from ConfigLoader.
            AIPersonalityConfig aiPersonality = null;
            if (ConfigLoader.AIPersonalities != null)
            {
                foreach (var kvp in ConfigLoader.AIPersonalities)
                {
                    aiPersonality = kvp.Value as AIPersonalityConfig;
                    break;
                }
            }
            if (aiPersonality == null)
            {
                Debug.LogWarning("[CombatController] No AIPersonalityConfig found. Creating default.");
                aiPersonality = ScriptableObject.CreateInstance<AIPersonalityConfig>();
            }

            var combatSettings = Resources.Load<CombatSettings>("Data/CombatSettings");
            if (combatSettings == null)
            {
                Debug.LogError("[CombatController] CombatSettings not found at Resources/Data/CombatSettings. Using defaults.");
                combatSettings = ScriptableObject.CreateInstance<CombatSettings>();
            }

            var rng = seed == 0 ? new System.Random() : new System.Random(seed);
            var damageCalculator = new DamageCalculator(combatSettings, rng);
            var captureSystem = new CaptureSystem(rng);
            var moveEffectApplier = new MoveEffectApplier(rng);
            var statusEffectProcessor = new StatusEffectProcessor(combatSettings);
            var aiDecisionSystem = new AIDecisionSystem(aiPersonality, rng);

            StartCombat(
                context.Grid,
                new List<CreatureInstance>(context.PlayerCreatures),
                new List<CreatureInstance>(context.EnemyCreatures),
                context.EncounterType,
                combatSettings,
                damageCalculator,
                captureSystem,
                aiDecisionSystem,
                moveEffectApplier,
                statusEffectProcessor,
                seed);
        }

        /// <summary>
        /// Full-parameter entry point: initialize and start a combat encounter.
        /// Creates the TurnManager with all injected dependencies and begins
        /// the coroutine-based combat loop.
        /// </summary>
        public void StartCombat(
            GridSystem grid,
            List<CreatureInstance> playerParty,
            List<CreatureInstance> enemyParty,
            EncounterType encounterType,
            CombatSettings settings,
            IDamageCalculator damageCalculator,
            ICaptureSystem captureSystem,
            IAIDecisionSystem aiDecisionSystem,
            IMoveEffectApplier moveEffectApplier,
            IStatusEffectProcessor statusEffectProcessor,
            int seed = 0)
        {
            _grid = grid;
            _playerParty = playerParty;
            _enemyParty = enemyParty;
            _encounterType = encounterType;
            _remainingTraps = initialTrapCount;
            _context = null; // Only set by convenience overload; null signals full-param init

            _inputCollector = new PlayerInputCollector();

            _turnManager = new TurnManager(
                grid, playerParty, enemyParty, encounterType, settings,
                damageCalculator, captureSystem, aiDecisionSystem,
                moveEffectApplier, statusEffectProcessor,
                _inputCollector, seed);

            SubscribeToTurnManager();
            StartCoroutine(CombatLoop());
        }

        /// <summary>
        /// Submit a player creature's action for this round. Called by
        /// MoveSelectionPanelController when the player confirms an action.
        /// Sets AllActionsReady when all non-fainted player creatures have submitted.
        /// </summary>
        public void SubmitAction(CreatureInstance creature, TurnAction action)
        {
            if (CurrentUIPhase != CombatUIPhase.PlayerSelect) return;

            _inputCollector.SubmitAction(creature, action);
        }

        /// <summary>
        /// Cancel a previously submitted action for a creature (e.g., on re-targeting).
        /// </summary>
        public void CancelAction(CreatureInstance creature)
        {
            _inputCollector.CancelAction(creature);
        }

        // ── Coroutine Combat Loop ─────────────────────────────────────────

        private IEnumerator CombatLoop()
        {
            while (_turnManager.CombatActive)
            {
                // RoundStart — yield one frame for UI to update
                SetPhase(CombatUIPhase.RoundStart);
                yield return null;

                // PlayerSelect — begin input collection, yield until all actions submitted
                _activeBuffer.Clear();
                foreach (var c in _playerParty)
                {
                    if (!c.IsFainted) _activeBuffer.Add(c);
                }
                _inputCollector.BeginActionCollection(_activeBuffer);
                _enemyPhaseSignaled = false;

                SetPhase(CombatUIPhase.PlayerSelect);

                while (!_inputCollector.AllActionsReady)
                    yield return null;

                // PlayerExecuting — fire phase, then run AdvanceRound synchronously
                SetPhase(CombatUIPhase.PlayerExecuting);
                yield return null;

                // AdvanceRound runs all phases synchronously.
                // CreatureActed callbacks will fire EnemyExecuting phase.
                _turnManager.AdvanceRound();

                // RoundEnd
                if (_turnManager.CombatActive)
                {
                    SetPhase(CombatUIPhase.RoundEnd);
                    yield return null;
                }
            }

            SetPhase(CombatUIPhase.CombatEnd);
        }

        // ── TurnManager Event Handlers ────────────────────────────────────

        private void SubscribeToTurnManager()
        {
            _turnManager.RoundStarted += OnRoundStarted;
            _turnManager.RoundEnded += OnRoundEnded;
            _turnManager.CreatureActed += OnCreatureActed;
            _turnManager.CreatureFainted += OnCreatureFainted;
            _turnManager.CreatureCaptured += OnCreatureCaptured;
        }

        private void UnsubscribeFromTurnManager()
        {
            if (_turnManager == null) return;
            _turnManager.RoundStarted -= OnRoundStarted;
            _turnManager.RoundEnded -= OnRoundEnded;
            _turnManager.CreatureActed -= OnCreatureActed;
            _turnManager.CreatureFainted -= OnCreatureFainted;
            _turnManager.CreatureCaptured -= OnCreatureCaptured;
        }

        private void OnRoundStarted(int round)
        {
            RoundStarted?.Invoke(round);
        }

        private void OnRoundEnded(int round)
        {
            RoundEnded?.Invoke(round);
        }

        private void OnCreatureActed(CreatureActedArgs args)
        {
            // Detect EnemyAction phase transition
            if (!_enemyPhaseSignaled &&
                _turnManager.CurrentPhase == CombatPhase.EnemyAction)
            {
                _enemyPhaseSignaled = true;
                SetPhase(CombatUIPhase.EnemyExecuting);
            }

            CreatureActed?.Invoke(args);
        }

        private void OnCreatureFainted(CreatureInstance creature)
        {
            CreatureFainted?.Invoke(creature);

            // Check for immediate combat end (spec §6.8)
            if (_turnManager != null && !_turnManager.CombatActive)
                SetPhase(CombatUIPhase.CombatEnd);
        }

        private void OnCreatureCaptured(CaptureResultArgs args)
        {
            if (args.Success)
                _remainingTraps = Mathf.Max(0, _remainingTraps - 1);

            CreatureCaptured?.Invoke(args);
        }

        // ── Phase Management ──────────────────────────────────────────────

        private void SetPhase(CombatUIPhase phase)
        {
            CurrentUIPhase = phase;
            PhaseChanged?.Invoke(phase);
        }

        // ── Cleanup ───────────────────────────────────────────────────────

        private void OnDestroy()
        {
            UnsubscribeFromTurnManager();
            StopAllCoroutines();
        }

        // ── PlayerInputCollector ──────────────────────────────────────────

        /// <summary>
        /// Concrete <see cref="IPlayerInputProvider"/> that buffers actions
        /// submitted by MoveSelectionPanelController. CombatController owns
        /// one instance and routes SubmitAction/CancelAction calls through it.
        /// </summary>
        private class PlayerInputCollector : IPlayerInputProvider
        {
            private readonly Dictionary<CreatureInstance, TurnAction> _actions = new();
            private int _expectedCount;

            /// <inheritdoc/>
            public bool AllActionsReady => _expectedCount > 0 && _actions.Count >= _expectedCount;

            /// <inheritdoc/>
            public void BeginActionCollection(IReadOnlyList<CreatureInstance> creatures)
            {
                _actions.Clear();
                _expectedCount = creatures.Count;
            }

            /// <inheritdoc/>
            public IReadOnlyDictionary<CreatureInstance, TurnAction> GetActions()
            {
                return new Dictionary<CreatureInstance, TurnAction>(_actions);
            }

            /// <summary>Submit an action for a creature. Called by CombatController.SubmitAction.</summary>
            public void SubmitAction(CreatureInstance creature, TurnAction action)
            {
                _actions[creature] = action;
            }

            /// <summary>Cancel a previously submitted action. Called by CombatController.CancelAction.</summary>
            public void CancelAction(CreatureInstance creature)
            {
                _actions.Remove(creature);
            }
        }
    }
}
