using System;
using System.Collections.Generic;
using System.Linq;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.Combat
{
    /// <summary>
    /// Phase-based combat sequencer for Gene Forge isometric grid battles.
    /// Pure C# class — no MonoBehaviour (ADR-002). A MonoBehaviour wrapper
    /// is responsible for frame yields between AdvanceRound() calls.
    ///
    /// Implements GDD Turn Manager v2.0 (design/gdd/turn-manager.md).
    ///
    /// Round loop: RoundStart → PlayerCreatureSelect → PlayerAction → EnemyAction → RoundEnd.
    /// Each creature's turn is a split turn: optional Movement step + one Action step.
    /// Initiative = proximity-based with Priority bracket override and seeded RNG tiebreak.
    ///
    /// Synchronous design: AdvanceRound() is synchronous. No async/await inside TurnManager.
    /// A MonoBehaviour wrapper handles per-frame yields between phase steps for animation and UI.
    ///
    /// Dependencies are injected via constructor (ADR-003). All tuning values come from
    /// CombatSettings ScriptableObject (ADR-001). C# events for decoupling (ADR-004).
    /// </summary>
    public class TurnManager
    {
        // ── Events (ADR-004: instance C# events, not static) ──────────────

        /// <summary>Fired at the start of each round after DoT is applied. Arg: round number (1-based).</summary>
        public event Action<int> RoundStarted;

        /// <summary>Fired at the end of each round after duration decrements.</summary>
        public event Action<int> RoundEnded;

        /// <summary>Fired after each creature's turn resolves (including suppressed and wait turns).</summary>
        public event Action<CreatureActedArgs> CreatureActed;

        /// <summary>Fired exactly when a creature's HP reaches 0, before the next creature's turn.</summary>
        public event Action<CreatureInstance> CreatureFainted;

        /// <summary>Fired after each capture attempt, regardless of success.</summary>
        public event Action<CaptureResultArgs> CreatureCaptured;

        // ── Public State ──────────────────────────────────────────────────

        /// <summary>Current round number (1-based). Incremented at RoundEnd.</summary>
        public int CurrentRound { get; private set; } = 1;

        /// <summary>Current phase being executed.</summary>
        public CombatPhase CurrentPhase { get; private set; } = CombatPhase.RoundStart;

        /// <summary>True while combat is running. Set to false on any terminal CombatResult.</summary>
        public bool CombatActive { get; private set; } = true;

        /// <summary>Aggregate metrics for this combat session. Queryable after combat ends.</summary>
        public BattleStats Stats { get; } = new BattleStats();

        // ── Dependencies ──────────────────────────────────────────────────

        private readonly GridSystem _grid;
        private readonly CombatSettings _settings;
        private readonly IDamageCalculator _damageCalculator;
        private readonly ICaptureSystem _captureSystem;
        private readonly IAIDecisionSystem _aiDecisionSystem;
        private readonly IMoveEffectApplier _moveEffectApplier;
        private readonly IStatusEffectProcessor _statusEffectProcessor;

        /// <summary>
        /// Player input provider abstraction. CombatController's coroutine calls
        /// <see cref="IPlayerInputProvider.BeginActionCollection"/> and polls
        /// <see cref="IPlayerInputProvider.AllActionsReady"/> before invoking
        /// <see cref="AdvanceRound"/>. TurnManager only calls
        /// <see cref="IPlayerInputProvider.GetActions"/> during PlayerCreatureSelect.
        /// </summary>
        private readonly IPlayerInputProvider _inputProvider;

        // ── Party State ───────────────────────────────────────────────────

        private readonly List<CreatureInstance> _playerParty;
        private readonly List<CreatureInstance> _enemyParty;
        private readonly HashSet<CreatureInstance> _playerSet;
        private readonly EncounterType _encounterType;

        // ── Per-Round Combat State ────────────────────────────────────────

        /// <summary>Queued actions for the current round. Populated during PlayerCreatureSelect / EnemyAction.</summary>
        private readonly Dictionary<CreatureInstance, TurnAction> _queuedActions = new();

        /// <summary>
        /// Status effect durations for all creatures. TurnManager owns durations;
        /// CreatureInstance.ActiveStatusEffects tracks presence only.
        /// Initialized from CreatureInstance.ActiveStatusEffects at combat start.
        /// Gap fix #3: TurnManager owns duration tracking, not CreatureInstance.
        /// </summary>
        private readonly Dictionary<CreatureInstance, List<StatusEffectEntry>> _statusDurations = new();

        /// <summary>
        /// Creatures suppressed this round (Sleep, Freeze, or Paralysis proc).
        /// Populated during ApplyStartOfRoundEffects; checked during execution.
        /// Gap fix #6: suppression tracked via HashSet, not TurnAction.Suppressed field.
        /// </summary>
        private readonly HashSet<CreatureInstance> _suppressedThisRound = new();

        /// <summary>Reusable list for initiative ordering — avoids per-round allocation.</summary>
        private readonly List<CreatureInstance> _initiativeBuffer = new();

        // ── RNG ───────────────────────────────────────────────────────────

        private readonly System.Random _rng;

        // ── Constructor ───────────────────────────────────────────────────

        /// <summary>
        /// Create a new TurnManager for one combat encounter.
        /// </summary>
        /// <param name="grid">The combat grid.</param>
        /// <param name="playerParty">Non-empty list of player creatures.</param>
        /// <param name="enemyParty">Non-empty list of enemy creatures.</param>
        /// <param name="encounterType">Encounter type (determines Flee and Capture behavior).</param>
        /// <param name="settings">CombatSettings ScriptableObject with all tuning knobs.</param>
        /// <param name="damageCalculator">Damage resolution implementation.</param>
        /// <param name="captureSystem">Capture attempt resolution implementation.</param>
        /// <param name="aiDecisionSystem">AI action provider implementation.</param>
        /// <param name="moveEffectApplier">Move effect application implementation.</param>
        /// <param name="statusEffectProcessor">Status effect tick implementation.</param>
        /// <param name="inputProvider">
        /// Player input abstraction. CombatController guarantees
        /// <see cref="IPlayerInputProvider.AllActionsReady"/> is true before
        /// calling <see cref="AdvanceRound"/>. TurnManager only calls
        /// <see cref="IPlayerInputProvider.GetActions"/> during PlayerCreatureSelect.
        /// </param>
        /// <param name="seed">
        /// RNG seed for deterministic behavior. 0 = use Environment.TickCount (non-deterministic).
        /// Non-zero seeds produce identical action sequences for identical scenarios (useful for tests).
        /// </param>
        public TurnManager(
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
            IPlayerInputProvider inputProvider,
            int seed = 0)
        {
            if (playerParty == null || playerParty.Count == 0)
                Debug.LogError("[TurnManager] playerParty is null or empty. Combat will end immediately.");
            if (enemyParty == null || enemyParty.Count == 0)
                Debug.LogError("[TurnManager] enemyParty is null or empty. Combat will end immediately.");

            _grid                  = grid;
            _playerParty           = playerParty ?? new List<CreatureInstance>();
            _enemyParty            = enemyParty ?? new List<CreatureInstance>();
            _playerSet             = new HashSet<CreatureInstance>(_playerParty);
            _encounterType         = encounterType;
            _settings              = settings;
            _damageCalculator      = damageCalculator;
            _captureSystem         = captureSystem;
            _aiDecisionSystem      = aiDecisionSystem;
            _moveEffectApplier     = moveEffectApplier;
            _statusEffectProcessor = statusEffectProcessor;
            _inputProvider         = inputProvider;
            _rng                   = seed == 0 ? new System.Random() : new System.Random(seed);

            InitializeStatusDurations();
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Execute one full round of combat.
        /// Phases: RoundStart → PlayerCreatureSelect → PlayerAction → EnemyAction → RoundEnd.
        /// Returns immediately if CombatActive is false.
        /// </summary>
        public void AdvanceRound()
        {
            if (!CombatActive) return;

            ExecuteRoundStart();
            if (!CombatActive) return;

            ExecutePlayerCreatureSelect();
            if (!CombatActive) return;

            ExecutePlayerAction();
            if (!CombatActive) return;

            ExecuteEnemyAction();
            if (!CombatActive) return;

            ExecuteRoundEnd();
        }

        // ── Phase: RoundStart ─────────────────────────────────────────────

        private void ExecuteRoundStart()
        {
            CurrentPhase = CombatPhase.RoundStart;
            _suppressedThisRound.Clear();
            Stats.ResetRoundCounters();

            // Processing order: player creatures first, then enemy creatures (GDD §3.7).
            ApplyStartOfRoundEffects(_playerParty);
            if (!CombatActive) return;

            ApplyStartOfRoundEffects(_enemyParty);
            if (!CombatActive) return;

            RoundStarted?.Invoke(CurrentRound);
        }

        private void ApplyStartOfRoundEffects(List<CreatureInstance> party)
        {
            foreach (var creature in party)
            {
                if (creature.IsFainted) continue;

                var entries = GetStatusEntries(creature);
                bool suppressed = _statusEffectProcessor.ApplyStartOfRound(
                    creature, entries, _rng.NextDouble());

                if (suppressed)
                    _suppressedThisRound.Add(creature);

                if (creature.IsFainted)
                {
                    HandleFaint(creature);
                    if (!CombatActive) return;
                }
            }
        }

        // ── Phase: PlayerCreatureSelect ────────────────────────────────────

        private void ExecutePlayerCreatureSelect()
        {
            CurrentPhase = CombatPhase.PlayerCreatureSelect;
            _queuedActions.Clear();

            _initiativeBuffer.Clear();
            foreach (var c in _playerParty)
            {
                if (!c.IsFainted)
                    _initiativeBuffer.Add(c);
            }
            if (_initiativeBuffer.Count == 0) return;

            // Contract: CombatController has already called BeginActionCollection
            // and polled AllActionsReady before calling AdvanceRound().
            var actions = _inputProvider.GetActions();
            if (actions != null)
            {
                foreach (var kvp in actions)
                    _queuedActions[kvp.Key] = kvp.Value;
            }
        }

        // ── Phase: PlayerAction ────────────────────────────────────────────

        private void ExecutePlayerAction()
        {
            CurrentPhase = CombatPhase.PlayerAction;

            var order = GetInitiativeOrder(_playerParty, _enemyParty);
            foreach (var actor in order)
            {
                if (!CombatActive) return;
                if (actor.IsFainted) continue;

                if (!_queuedActions.TryGetValue(actor, out var action))
                    action = new TurnAction(ActionType.Wait);

                ExecuteSplitTurn(actor, action, _enemyParty);
            }
        }

        // ── Phase: EnemyAction ─────────────────────────────────────────────

        private void ExecuteEnemyAction()
        {
            CurrentPhase = CombatPhase.EnemyAction;

            // Collect AI actions before execution so initiative is from phase-start positions.
            var order = GetInitiativeOrder(_enemyParty, _playerParty);

            // Queue AI decisions (mirror of PlayerCreatureSelect for enemy side).
            foreach (var actor in order)
            {
                if (actor.IsFainted) continue;
                _queuedActions[actor] = _aiDecisionSystem.DecideAction(
                    actor, _playerParty, _enemyParty, _grid);
            }

            foreach (var actor in order)
            {
                if (!CombatActive) return;
                if (actor.IsFainted) continue;

                if (!_queuedActions.TryGetValue(actor, out var action))
                    action = new TurnAction(ActionType.Wait);

                ExecuteSplitTurn(actor, action, _playerParty);
            }
        }

        // ── Phase: RoundEnd ────────────────────────────────────────────────

        private void ExecuteRoundEnd()
        {
            CurrentPhase = CombatPhase.RoundEnd;

            // Decrement durations for all creatures (player first per GDD §3.7).
            DecrementAllDurations(_playerParty);
            DecrementAllDurations(_enemyParty);

            _queuedActions.Clear();
            Stats.RoundsElapsed++;
            CurrentRound++;

            // Post-MVP: check max rounds for Draw condition.
            // if (_settings.MaxRoundsPerCombat > 0 && CurrentRound > _settings.MaxRoundsPerCombat)
            // { CombatActive = false; Stats.Result = CombatResult.Draw; }

            RoundEnded?.Invoke(CurrentRound - 1); // Fire with the round that just ended.
        }

        private void DecrementAllDurations(List<CreatureInstance> party)
        {
            foreach (var creature in party)
            {
                if (creature.IsFainted) continue;
                var entries = GetStatusEntries(creature);
                _statusEffectProcessor.DecrementDurations(creature, entries);
            }
        }

        // ── Split Turn Execution ───────────────────────────────────────────

        private void ExecuteSplitTurn(CreatureInstance actor, TurnAction action, List<CreatureInstance> opponents)
        {
            // Suppression: skip both steps. Fire CreatureActed with WasSuppressed = true.
            // Gap fix #6: check _suppressedThisRound set, not action.Suppressed.
            if (_suppressedThisRound.Contains(actor))
            {
                CreatureActed?.Invoke(new CreatureActedArgs(
                    actor, action.Action, CurrentRound, wasSuppressed: true));
                Stats.ActionsThisRound++;
                return;
            }

            // Step 1 — Movement (optional).
            ExecuteMovementStep(actor, action);

            // Check faint from any edge case (fall damage post-MVP, etc.).
            if (actor.IsFainted)
            {
                HandleFaint(actor);
                return;
            }

            // Step 2 — Action.
            ExecuteActionStep(actor, action, opponents);
        }

        // ── Movement Step ──────────────────────────────────────────────────

        private void ExecuteMovementStep(CreatureInstance actor, TurnAction action)
        {
            // Flee consumes entire turn — no movement.
            if (action.Action == ActionType.Flee) return;

            if (action.MovementTarget == null) return;

            var targetPos = action.MovementTarget.Value;
            var startTile = _grid.GetTile(actor.GridPosition);
            var targetTile = _grid.GetTile(targetPos);

            if (startTile == null || targetTile == null) return;
            if (targetTile.IsOccupied) return; // Target occupied — skip movement; action still executes.

            int movementRange = Mathf.Max(1, actor.ComputedStats.SPD / _settings.MovementDivisor);

            // Use GridSystem A* pathfinding. Returns empty list (not null) if no path.
            var path = _grid.FindPath(actor.GridPosition, targetPos);

            // TODO: Gap fix #7 — fall damage skipped entirely (post-MVP).
            // When height-delta movement is finalized, apply fall damage here.

            if (path == null || path.Count == 0) return;

            // Validate path length fits within movement range (path includes start tile).
            // A* returns positions including start; movement cost = path.Count - 1 steps.
            int stepCount = path.Count - 1;
            if (stepCount > movementRange) return;

            // Clear old tile occupancy.
            var oldTile = _grid.GetTile(actor.GridPosition);
            if (oldTile != null) oldTile.Occupant = null;

            // Move to destination.
            var destination = path[path.Count - 1];
            actor.SetGridPosition(destination);
            actor.SetMoved(true);

            // Update tile occupancy.
            var newTile = _grid.GetTile(destination);
            if (newTile != null) newTile.Occupant = actor;

            // Update facing toward movement direction.
            if (path.Count >= 2)
            {
                var dir = path[path.Count - 1] - path[path.Count - 2];
                actor.SetFacing(VectorToFacing(dir));
            }
        }

        // ── Action Step ────────────────────────────────────────────────────

        private void ExecuteActionStep(CreatureInstance actor, TurnAction action, List<CreatureInstance> opponents)
        {
            switch (action.Action)
            {
                case ActionType.UseMove:
                    ExecuteUseMove(actor, action, opponents);
                    break;

                case ActionType.Capture:
                    ExecuteCapture(actor, action);
                    break;

                case ActionType.Flee:
                    ExecuteFlee(actor, action);
                    break;

                case ActionType.Item:
                    // Post-MVP: item usage. Silently treated as Wait in MVP.
                    ExecuteWait(actor, action);
                    break;

                case ActionType.Wait:
                    ExecuteWait(actor, action);
                    break;
            }
        }

        // ── UseMove ────────────────────────────────────────────────────────

        private void ExecuteUseMove(CreatureInstance actor, TurnAction action, List<CreatureInstance> opponents)
        {
            var move = action.Move;
            if (move == null)
            {
                ExecuteWait(actor, action);
                return;
            }

            // Step 2: Confusion self-hit (GDD §3.8 step 2, §4.5).
            if (ResolveConfusionSelfHit(actor))
                return;

            // Step 3: Deduct PP (clamped to 0).
            if (action.MovePPSlot >= 0)
                actor.DeductPP(action.MovePPSlot);

            // Step 4: Hit check.
            if (!MoveHitCheck(move, actor, action.Target))
            {
                Stats.ActionsThisRound++;
                CreatureActed?.Invoke(new CreatureActedArgs(
                    actor, ActionType.UseMove, CurrentRound, wasMiss: true));
                actor.SetActed(true);
                return;
            }

            // Steps 5–9: Damage, faint, recoil, drain, effects.
            int damageDealt = ResolveDamage(actor, move, action.Target);
            if (!CombatActive) return;

            ResolvePostDamageEffects(actor, move, action.Target, damageDealt);
            if (!CombatActive) return;

            // Struggle recoil: 25% of damage dealt as self-damage (GDD combat-ui.md).
            // Identified by MovePPSlot == -1 with a non-null Move (normal moves always have slot >= 0).
            if (action.MovePPSlot < 0 && move != null && damageDealt > 0)
            {
                int recoil = Mathf.Max(1, Mathf.FloorToInt(damageDealt * 0.25f));
                actor.TakeDamage(recoil);
                if (actor.IsFainted)
                {
                    HandleFaint(actor);
                    if (!CombatActive) return;
                }
            }

            // Step 10: Fire CreatureActed.
            Stats.ActionsThisRound++;
            actor.SetActed(true);
            CreatureActed?.Invoke(new CreatureActedArgs(
                actor, ActionType.UseMove, CurrentRound, damageDealt: damageDealt));
        }

        /// <summary>
        /// Resolve confusion self-hit. Returns true if actor was confused and hit itself
        /// (original action is cancelled, no PP consumed). Implements GDD §3.8 step 2.
        /// </summary>
        private bool ResolveConfusionSelfHit(CreatureInstance actor)
        {
            var entries = GetStatusEntries(actor);
            bool isConfused = entries.Any(e => e.Effect == StatusEffect.Confusion && !e.IsExpired);
            if (!isConfused || _rng.NextDouble() >= _settings.ConfusionSelfHitChance)
                return false;

            int selfHitDamage = CalculateConfusionSelfHitDamage(actor);
            actor.TakeDamage(selfHitDamage);
            Stats.ActionsThisRound++;
            actor.SetActed(true);

            CreatureActed?.Invoke(new CreatureActedArgs(
                actor, ActionType.UseMove, CurrentRound,
                wasConfusionSelfHit: true, damageDealt: selfHitDamage));

            if (actor.IsFainted)
                HandleFaint(actor);

            return true;
        }

        /// <summary>
        /// Resolve damage application and target faint. Returns damage dealt.
        /// Implements GDD §3.8 steps 5–6.
        /// </summary>
        private int ResolveDamage(CreatureInstance actor, MoveConfig move, CreatureInstance target)
        {
            int damageDealt = 0;

            if (move.IsDamaging && target != null)
            {
                int damage = _damageCalculator.Calculate(move, actor, target, _grid);
                damageDealt = damage;

                if (_playerSet.Contains(actor))
                    Stats.TotalDamageDealt += damage;
                else
                    Stats.TotalDamageTaken += damage;

                target.TakeDamage(damage);
            }

            // Target faint check.
            if (target != null && target.IsFainted)
            {
                HandleFaint(target);
                if (!CombatActive)
                {
                    // Still apply recoil per GDD §5 mutual faint edge case.
                    ApplyRecoilEffects(actor, move, damageDealt);
                    Stats.ActionsThisRound++;
                    CreatureActed?.Invoke(new CreatureActedArgs(
                        actor, ActionType.UseMove, CurrentRound, damageDealt: damageDealt));
                    actor.SetActed(true);
                }
            }

            return damageDealt;
        }

        /// <summary>
        /// Resolve recoil, drain, and remaining move effects after damage.
        /// Implements GDD §3.8 steps 7–9.
        /// </summary>
        private void ResolvePostDamageEffects(CreatureInstance actor, MoveConfig move,
            CreatureInstance target, int damageDealt)
        {
            // Step 7: Recoil.
            ApplyRecoilEffects(actor, move, damageDealt);
            if (actor.IsFainted)
            {
                HandleFaint(actor);
                Stats.ActionsThisRound++;
                CreatureActed?.Invoke(new CreatureActedArgs(
                    actor, ActionType.UseMove, CurrentRound, damageDealt: damageDealt));
                return;
            }

            // Step 8: Drain.
            ApplyDrainEffects(actor, move, damageDealt);

            // Step 9: Remaining effects (skip Recoil/Drain already applied).
            if (move.Effects != null)
            {
                foreach (var effect in move.Effects)
                {
                    if (effect.EffectType == MoveEffectType.Recoil) continue;
                    if (effect.EffectType == MoveEffectType.Drain) continue;
                    _moveEffectApplier.Apply(effect, actor, target, _grid);
                }
            }
        }

        // ── Capture ────────────────────────────────────────────────────────

        /// <summary>
        /// Execute a capture action. MVP assumption: only player captures enemy creatures.
        /// If capture-from-player-side is added post-MVP, this must determine which
        /// party list to remove from based on the actor's team membership.
        /// </summary>
        private void ExecuteCapture(CreatureInstance actor, TurnAction action)
        {
            // GDD §3.8: cannot capture trainer-owned creatures.
            if (_encounterType == EncounterType.Trainer)
            {
                Debug.LogError("[TurnManager] Capture attempted in trainer encounter — blocked.");
                actor.SetActed(true);
                CreatureActed?.Invoke(new CreatureActedArgs(actor, ActionType.Capture, CurrentRound));
                return;
            }

            bool success = false;

            if (action.Target != null)
            {
                success = _captureSystem.Attempt(action.Target, actor);

                if (success)
                {
                    var tile = _grid.GetTile(action.Target.GridPosition);
                    if (tile != null) tile.Occupant = null;

                    _enemyParty.Remove(action.Target);
                    Stats.CapturesThisRound++;
                    Stats.TotalCaptures++;
                }

                CreatureCaptured?.Invoke(new CaptureResultArgs(action.Target, success, CurrentRound));

                if (success)
                {
                    CheckEndCondition(out var result);
                    if (result != CombatResult.Ongoing)
                    {
                        CombatActive = false;
                        Stats.Result = result;
                    }
                }
            }

            Stats.ActionsThisRound++;
            actor.SetActed(true);
            CreatureActed?.Invoke(new CreatureActedArgs(actor, ActionType.Capture, CurrentRound));
        }

        // ── Flee ───────────────────────────────────────────────────────────

        private void ExecuteFlee(CreatureInstance actor, TurnAction action)
        {
            if (_encounterType == EncounterType.Trainer)
            {
                // Cannot flee trainer battles (GDD §3.8).
                Debug.Log("[TurnManager] Can't flee trainer battle.");
                Stats.ActionsThisRound++;
                actor.SetActed(true);
                CreatureActed?.Invoke(new CreatureActedArgs(actor, ActionType.Flee, CurrentRound));
                return;
            }

            // Wild encounter flee — MVP: always succeeds.
            // Post-MVP: apply formula clamp01(playerAvgSPD / enemyAvgSPD * 0.8).
            bool fleeSuccess = _rng.NextDouble() < _settings.WildFleeSuccessRate;
            if (fleeSuccess)
            {
                CombatActive = false;
                Stats.Result = CombatResult.Fled;
            }

            Stats.ActionsThisRound++;
            actor.SetActed(true);
            CreatureActed?.Invoke(new CreatureActedArgs(actor, ActionType.Flee, CurrentRound));
        }

        // ── Wait ───────────────────────────────────────────────────────────

        private void ExecuteWait(CreatureInstance actor, TurnAction action)
        {
            Stats.ActionsThisRound++;
            actor.SetActed(true);
            CreatureActed?.Invoke(new CreatureActedArgs(actor, action.Action, CurrentRound));
        }

        // ── Faint Handling ─────────────────────────────────────────────────

        /// <summary>
        /// Handle a creature faint: fire event, clear tile, update stats, check end condition.
        /// Implements GDD §3.10.
        /// </summary>
        private void HandleFaint(CreatureInstance creature)
        {
            CreatureFainted?.Invoke(creature);

            var tile = _grid.GetTile(creature.GridPosition);
            if (tile != null) tile.Occupant = null;

            Stats.FaintsThisRound++;

            if (CheckEndCondition(out var result))
            {
                CombatActive = false;
                Stats.Result = result;
            }
        }

        // ── End Condition ──────────────────────────────────────────────────

        /// <summary>
        /// Check whether combat should end. Victory takes priority on mutual faint.
        /// Implements GDD §3.11.
        /// </summary>
        private bool CheckEndCondition(out CombatResult result)
        {
            bool allEnemyFainted  = _enemyParty.Count == 0 || _enemyParty.All(c => c.IsFainted);
            bool allPlayerFainted = _playerParty.Count == 0 || _playerParty.All(c => c.IsFainted);

            // Victory takes priority over mutual faint (recoil scenario — GDD §5).
            if (allEnemyFainted)  { result = CombatResult.Victory; return true; }
            if (allPlayerFainted) { result = CombatResult.Defeat;  return true; }

            result = CombatResult.Ongoing;
            return false;
        }

        // ── Initiative Ordering ────────────────────────────────────────────

        /// <summary>
        /// Build initiative order for one action phase.
        /// Sort key: (-priority, initiativeScore, precomputedTiebreak).
        /// Initiative calculated from positions at phase start (before mid-phase movement).
        /// Implements GDD §3.6.
        /// </summary>
        private List<CreatureInstance> GetInitiativeOrder(
            List<CreatureInstance> actors,
            List<CreatureInstance> opponents)
        {
            _initiativeBuffer.Clear();
            foreach (var c in actors)
            {
                if (!c.IsFainted)
                    _initiativeBuffer.Add(c);
            }

            // Pre-compute tiebreak values once before the sort (Gap fix: not inline in comparator).
            var tiebreaks = new Dictionary<CreatureInstance, int>(_initiativeBuffer.Count);
            foreach (var c in _initiativeBuffer)
                tiebreaks[c] = _rng.Next();

            _initiativeBuffer.Sort((a, b) =>
            {
                // Step 1: Priority bracket (descending — higher priority first).
                int pa = _queuedActions.TryGetValue(a, out var qa) && qa.Move != null ? qa.Move.Priority : 0;
                int pb = _queuedActions.TryGetValue(b, out var qb) && qb.Move != null ? qb.Move.Priority : 0;
                if (pa != pb) return pb.CompareTo(pa);

                // Step 2: Initiative score (ascending — lower score acts earlier).
                int ia = CalculateInitiative(a, opponents);
                int ib = CalculateInitiative(b, opponents);
                if (ia != ib) return ia.CompareTo(ib);

                // Step 3: Pre-computed random tiebreak (stable within round).
                return tiebreaks[a].CompareTo(tiebreaks[b]);
            });

            // Return a copy — callers iterate while combat state mutates.
            return new List<CreatureInstance>(_initiativeBuffer);
        }

        /// <summary>
        /// Initiative score = (minChebyshevDist × InitiativeDistanceWeight) - SPD.
        /// Lower = acts earlier. Implements GDD §4.1.
        /// </summary>
        private int CalculateInitiative(CreatureInstance creature, List<CreatureInstance> opponents)
        {
            int minDist = int.MaxValue;
            foreach (var opp in opponents)
            {
                if (opp.IsFainted) continue;
                int dist = GridSystem.ChebyshevDistance(creature.GridPosition, opp.GridPosition);
                if (dist < minDist) minDist = dist;
            }

            // If no live opponents, use distance 0 (acts immediately — combat should end soon).
            if (minDist == int.MaxValue) minDist = 0;

            return (minDist * _settings.InitiativeDistanceWeight) - creature.ComputedStats.SPD;
        }

        // ── Hit Check ──────────────────────────────────────────────────────

        /// <summary>
        /// Determines whether a move hits its target.
        /// Implements GDD §3.9 and §4.3.
        /// AccuracyStageMultiplier and EvasionStageMultiplier default to 1.0 until
        /// the stat stage system is implemented (post-MVP).
        /// </summary>
        private bool MoveHitCheck(MoveConfig move, CreatureInstance attacker, CreatureInstance target)
        {
            if (move.AlwaysHits) return true;
            if (target == null) return true; // AoE / no explicit target always proceeds.

            float hitChance = (move.Accuracy / 100f)
                            * attacker.AccuracyStageMultiplier
                            * (1f / target.EvasionStageMultiplier);

            hitChance = Mathf.Clamp01(hitChance);
            return _rng.NextDouble() < hitChance;
        }

        // ── Confusion Self-Hit Damage ──────────────────────────────────────

        /// <summary>
        /// Calculate confusion self-hit damage. Delegates to IDamageCalculator.CalculateRaw
        /// with Physical form, actor as both attacker and defender. No STAB, no type effectiveness.
        /// Implements GDD §4.5.
        /// </summary>
        private int CalculateConfusionSelfHitDamage(CreatureInstance actor)
        {
            return _damageCalculator.CalculateRaw(_settings.ConfusionSelfHitPower, DamageForm.Physical, actor, actor);
        }

        // ── Recoil & Drain ────────────────────────────────────────────────

        private void ApplyRecoilEffects(CreatureInstance actor, MoveConfig move, int damageDealt)
        {
            if (move.Effects == null) return;
            foreach (var effect in move.Effects)
            {
                if (effect.EffectType != MoveEffectType.Recoil) continue;
                // Recoil magnitude is stored as integer percentage (e.g. 25 = 25% of damage dealt).
                int recoilDamage = Mathf.Max(1, Mathf.FloorToInt(damageDealt * effect.Magnitude / 100f));
                actor.TakeDamage(recoilDamage);
            }
        }

        private void ApplyDrainEffects(CreatureInstance actor, MoveConfig move, int damageDealt)
        {
            if (move.Effects == null) return;
            foreach (var effect in move.Effects)
            {
                if (effect.EffectType != MoveEffectType.Drain) continue;
                // Drain magnitude is integer percentage of damage dealt healed back.
                int healAmount = Mathf.FloorToInt(damageDealt * effect.Magnitude / 100f);
                if (healAmount > 0) actor.Heal(healAmount);
            }
        }

        // ── Status Duration Initialization ────────────────────────────────

        /// <summary>
        /// Build the _statusDurations dictionary from each creature's existing
        /// ActiveStatusEffects at combat start. Assigns MVP fixed durations per GDD §4.7.
        /// </summary>
        private void InitializeStatusDurations()
        {
            var allCreatures = _playerParty.Concat(_enemyParty);
            foreach (var creature in allCreatures)
            {
                var entries = new List<StatusEffectEntry>();
                foreach (var status in creature.ActiveStatusEffects)
                    entries.Add(new StatusEffectEntry(status, GetDefaultDuration(status)));
                _statusDurations[creature] = entries;
            }
        }

        private int GetDefaultDuration(StatusEffect status) => status switch
        {
            StatusEffect.Sleep     => _settings.SleepDuration,
            StatusEffect.Freeze    => _settings.FreezeDuration,
            StatusEffect.Confusion => _settings.ConfusionDuration,
            StatusEffect.Taunt     => _settings.TauntDuration,
            // Indefinite: Burn, Poison, Paralysis, Stealth.
            _                      => -1
        };

        private List<StatusEffectEntry> GetStatusEntries(CreatureInstance creature)
        {
            if (!_statusDurations.TryGetValue(creature, out var entries))
            {
                entries = new List<StatusEffectEntry>();
                _statusDurations[creature] = entries;
            }
            return entries;
        }

        // ── Utility ───────────────────────────────────────────────────────

        /// <summary>Convert a grid direction vector to the nearest 8-directional Facing enum.</summary>
        private static Facing VectorToFacing(Vector2Int dir)
        {
            if (dir.x == 0  && dir.y > 0)  return Facing.N;
            if (dir.x > 0   && dir.y > 0)  return Facing.NE;
            if (dir.x > 0   && dir.y == 0) return Facing.E;
            if (dir.x > 0   && dir.y < 0)  return Facing.SE;
            if (dir.x == 0  && dir.y < 0)  return Facing.S;
            if (dir.x < 0   && dir.y < 0)  return Facing.SW;
            if (dir.x < 0   && dir.y == 0) return Facing.W;
            return Facing.NW;
        }
    }
}
