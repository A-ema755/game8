using System.Collections.Generic;
using GeneForge.Combat;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.UI
{
    /// <summary>
    /// Input state for the per-creature action selection flow.
    /// </summary>
    public enum InputState
    {
        /// <summary>No input being collected.</summary>
        Idle,

        /// <summary>Highlighting active creature, showing movement tiles.</summary>
        SelectingCreature,

        /// <summary>MoveSelectionPanel active, awaiting move/trap/switch/wait click.</summary>
        SelectingAction,

        /// <summary>Move selected, showing attack range, awaiting target click.</summary>
        SelectingMoveTarget,

        /// <summary>Player clicking a movement tile (optional reposition).</summary>
        SelectingMovement,

        /// <summary>All creatures have actions — ready to advance.</summary>
        Confirming
    }

    /// <summary>
    /// MonoBehaviour that orchestrates the player's per-creature input flow during
    /// the PlayerCreatureSelect combat phase. Manages a state machine that translates
    /// UI interactions (move buttons, tile clicks, creature clicks) into validated
    /// TurnAction structs submitted to CombatController.
    ///
    /// Subscribes to MoveSelectionPanelController, SwitchOverlayController, and
    /// CombatController events. Does NOT modify game state — only builds TurnActions.
    ///
    /// Implements design/gdd/combat-ui.md §3 input flow.
    /// </summary>
    public class PlayerInputController : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────

        /// <summary>Fired when a creature's action is confirmed and submitted.</summary>
        public event System.Action<CreatureInstance, TurnAction> ActionConfirmed;

        /// <summary>Fired when the input state changes (for HUD visibility).</summary>
        public event System.Action<InputState> StateChanged;

        /// <summary>Fired when the active creature changes (for highlighting).</summary>
        public event System.Action<CreatureInstance> ActiveCreatureChanged;

        /// <summary>Fired when movement tiles should be highlighted (blue).</summary>
        public event System.Action<HashSet<Vector2Int>> MovementTilesReady;

        /// <summary>Fired when attack tiles should be highlighted (red).</summary>
        public event System.Action<HashSet<Vector2Int>> AttackTilesReady;

        /// <summary>Fired when capture tiles should be highlighted (green).</summary>
        public event System.Action<HashSet<Vector2Int>> CaptureTilesReady;

        /// <summary>Fired when all tile highlights should be cleared.</summary>
        public event System.Action HighlightsClearRequested;

        // ── State ─────────────────────────────────────────────────────────

        /// <summary>Current input state.</summary>
        public InputState CurrentState { get; private set; } = InputState.Idle;

        /// <summary>Creature currently selecting an action.</summary>
        public CreatureInstance ActiveCreature => _activeCreature;

        /// <summary>Number of creatures still needing actions this round.</summary>
        public int RemainingCreatures => _creaturesNeedingActions.Count - _creatureIndex;

        // ── Dependencies ──────────────────────────────────────────────────

        private CombatController _combatController;
        private MoveSelectionPanelController _movePanel;
        private TileHighlightController _tileHighlight;
        private SwitchOverlayController _switchOverlay;
        private CreatureInfoPanelController _infoPanel;
        private CombatSettings _settings;

        // ── Internal State ────────────────────────────────────────────────

        private readonly List<CreatureInstance> _creaturesNeedingActions = new();
        private int _creatureIndex;
        private CreatureInstance _activeCreature;
        private Vector2Int? _pendingMovement;
        private MoveConfig _pendingMove;
        private int _pendingMoveSlot = -1;
        private bool _inCaptureMode;
        private HashSet<Vector2Int> _currentMovementTiles;
        private bool _switchOverlayOpen;

        // ── Initialization ────────────────────────────────────────────────

        /// <summary>
        /// Wire up dependencies. Called by CombatHUDController or scene setup
        /// after all controllers are created.
        /// </summary>
        /// <param name="combatController">CombatController driving the combat loop.</param>
        /// <param name="movePanel">Move selection UI controller.</param>
        /// <param name="tileHighlight">Tile highlight controller for movement/attack range.</param>
        /// <param name="switchOverlay">Switch overlay controller for creature swapping.</param>
        /// <param name="infoPanel">Creature info panel controller.</param>
        /// <param name="settings">CombatSettings with movement divisor and capture range.</param>
        public void Initialize(
            CombatController combatController,
            MoveSelectionPanelController movePanel,
            TileHighlightController tileHighlight,
            SwitchOverlayController switchOverlay,
            CreatureInfoPanelController infoPanel,
            CombatSettings settings)
        {
            _combatController = combatController;
            _movePanel = movePanel;
            _tileHighlight = tileHighlight;
            _switchOverlay = switchOverlay;
            _infoPanel = infoPanel;

            _settings = settings;
            if (_settings == null)
            {
                Debug.LogError("[PlayerInputController] CombatSettings is null. Using defaults.");
                _settings = ScriptableObject.CreateInstance<CombatSettings>();
            }

            SubscribeToEvents();
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Begin the per-creature action collection flow for a set of creatures.
        /// Called when CombatController enters PlayerSelect phase.
        /// </summary>
        public void BeginCreatureSelection(IReadOnlyList<CreatureInstance> creatures)
        {
            _creaturesNeedingActions.Clear();
            foreach (var c in creatures)
            {
                if (!c.IsFainted)
                    _creaturesNeedingActions.Add(c);
            }

            _creatureIndex = 0;

            if (_creaturesNeedingActions.Count == 0)
            {
                SetState(InputState.Confirming);
                return;
            }

            AdvanceToNextCreature();
        }

        /// <summary>
        /// Handle a tile click from the grid. Routes based on current state.
        /// Blocked while switch overlay is open.
        /// Called by a grid click handler MonoBehaviour.
        /// </summary>
        public void OnTileClicked(Vector2Int tilePos)
        {
            if (_switchOverlayOpen) return;

            switch (CurrentState)
            {
                case InputState.SelectingCreature:
                case InputState.SelectingAction:
                    // Click a movement tile to set pending movement
                    TrySelectMovement(tilePos);
                    break;

                case InputState.SelectingMoveTarget:
                    TrySelectMoveTargetTile(tilePos);
                    break;
            }
        }

        /// <summary>
        /// Handle a creature click from the grid. Routes based on current state.
        /// Blocked while switch overlay is open.
        /// Called by a creature click handler MonoBehaviour.
        /// </summary>
        public void OnCreatureClicked(CreatureInstance creature)
        {
            if (creature == null || _switchOverlayOpen) return;

            switch (CurrentState)
            {
                case InputState.SelectingMoveTarget:
                    TrySelectMoveTargetCreature(creature);
                    break;

                case InputState.SelectingAction:
                    // Clicking an enemy while in capture mode
                    if (_inCaptureMode)
                        TryCapture(creature);
                    break;
            }

            // Always update info panel on creature click
            _infoPanel?.SetExplicitTarget(creature);
        }

        /// <summary>
        /// Submit a Wait action for the active creature and advance.
        /// </summary>
        public void SubmitWait()
        {
            if (_activeCreature == null) return;

            var action = new TurnAction(ActionType.Wait, movementTarget: _pendingMovement);
            SubmitAndAdvance(action);
        }

        /// <summary>
        /// Submit a Flee action for the active creature.
        /// Flee consumes entire turn — clears pending movement.
        /// </summary>
        public void SubmitFlee()
        {
            if (_activeCreature == null) return;

            var action = new TurnAction(ActionType.Flee);
            var result = TurnActionValidator.Validate(
                action, _activeCreature, _combatController.EncounterType, _combatController.Grid);

            if (!result.IsValid)
            {
                Debug.LogWarning($"[PlayerInputController] Flee invalid: {result.Reason}");
                return;
            }

            SubmitAndAdvance(action);
        }

        /// <summary>
        /// Cancel current targeting/selection and return to SelectingAction state.
        /// </summary>
        public void CancelCurrentSelection()
        {
            _pendingMove = null;
            _pendingMoveSlot = -1;
            _inCaptureMode = false;
            _movePanel?.CancelTargeting();

            if (_activeCreature != null)
            {
                SetState(InputState.SelectingAction);
                ShowMovementTiles();
            }
        }

        /// <summary>
        /// Open the switch overlay for the active creature.
        /// Called by CombatHUDController keyboard handler (Alpha6/Keypad6) so that
        /// keyboard input flows through the same path as panel button clicks.
        /// </summary>
        public void RequestSwitch()
        {
            if (CurrentState != InputState.SelectingAction &&
                CurrentState != InputState.SelectingCreature)
                return;

            OnSwitchRequested();
        }

        /// <summary>
        /// Restart all action selections for this round.
        /// Clears all submitted actions and begins from the first creature.
        /// </summary>
        public void RestartAll()
        {
            // Cancel all previously submitted actions
            foreach (var creature in _creaturesNeedingActions)
                _combatController.CancelAction(creature);

            _creatureIndex = 0;
            _pendingMovement = null;
            _pendingMove = null;
            _pendingMoveSlot = -1;
            _inCaptureMode = false;

            if (_creaturesNeedingActions.Count > 0)
                AdvanceToNextCreature();
        }

        // ── Event Subscriptions ───────────────────────────────────────────

        private void SubscribeToEvents()
        {
            if (_combatController != null)
                _combatController.PhaseChanged += OnPhaseChanged;

            if (_movePanel != null)
            {
                _movePanel.ActionSubmitted += OnMoveActionSubmitted;
                _movePanel.SwitchRequested += OnSwitchRequested;
                _movePanel.MoveHighlightRequested += OnMoveHighlightRequested;
                _movePanel.HighlightClearRequested += OnHighlightClearRequested;
            }

            if (_switchOverlay != null)
            {
                _switchOverlay.SwitchConfirmed += OnSwitchConfirmed;
                _switchOverlay.Closed += OnSwitchOverlayClosed;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_combatController != null)
                _combatController.PhaseChanged -= OnPhaseChanged;

            if (_movePanel != null)
            {
                _movePanel.ActionSubmitted -= OnMoveActionSubmitted;
                _movePanel.SwitchRequested -= OnSwitchRequested;
                _movePanel.MoveHighlightRequested -= OnMoveHighlightRequested;
                _movePanel.HighlightClearRequested -= OnHighlightClearRequested;
            }

            if (_switchOverlay != null)
            {
                _switchOverlay.SwitchConfirmed -= OnSwitchConfirmed;
                _switchOverlay.Closed -= OnSwitchOverlayClosed;
            }
        }

        // ── Event Handlers ────────────────────────────────────────────────

        private void OnPhaseChanged(CombatUIPhase phase)
        {
            if (phase == CombatUIPhase.PlayerSelect)
            {
                var active = new List<CreatureInstance>();
                foreach (var c in _combatController.PlayerParty)
                {
                    if (!c.IsFainted) active.Add(c);
                }
                BeginCreatureSelection(active);
            }
            else if (CurrentState != InputState.Idle)
            {
                SetState(InputState.Idle);
                HighlightsClearRequested?.Invoke();
            }
        }

        private void OnMoveActionSubmitted(CreatureInstance creature, TurnAction action)
        {
            // MoveSelectionPanel built a TurnAction — layer in pending movement
            if (_pendingMovement.HasValue && action.Action != ActionType.Flee)
            {
                action = new TurnAction(
                    action.Action,
                    movementTarget: _pendingMovement,
                    move: action.Move,
                    target: action.Target,
                    targetTile: action.TargetTile,
                    movePPSlot: action.MovePPSlot);
            }

            // Validate
            var result = TurnActionValidator.Validate(
                action, _activeCreature, _combatController.EncounterType, _combatController.Grid);

            if (!result.IsValid)
            {
                Debug.LogWarning($"[PlayerInputController] Action invalid: {result.Reason}");
                return;
            }

            SubmitAndAdvance(action);
        }

        private void OnSwitchRequested()
        {
            if (_switchOverlay == null || _combatController == null) return;

            _switchOverlayOpen = true;
            _switchOverlay.Show(
                new List<CreatureInstance>(_combatController.PlayerParty),
                _activeCreature);
        }

        private void OnSwitchConfirmed(CreatureInstance switchTarget)
        {
            _switchOverlayOpen = false;

            // MVP: no ActionType.Switch — submit Wait. TODO: add Switch ActionType post-MVP.
            var action = new TurnAction(ActionType.Wait, movementTarget: _pendingMovement);
            SubmitAndAdvance(action);
        }

        private void OnSwitchOverlayClosed()
        {
            _switchOverlayOpen = false;
        }

        private void OnMoveHighlightRequested(MoveConfig move)
        {
            if (_activeCreature == null) return;

            if (move == null)
            {
                // Null = capture mode requested
                EnterCaptureMode();
                return;
            }

            // Enter move targeting mode
            _pendingMove = move;

            // Find the PP slot for this move
            _pendingMoveSlot = -1;
            for (int i = 0; i < _activeCreature.LearnedMoveIds.Count; i++)
            {
                var loadedMove = ConfigLoader.GetMove(_activeCreature.LearnedMoveIds[i]);
                if (loadedMove == move)
                {
                    _pendingMoveSlot = i;
                    break;
                }
            }

            SetState(InputState.SelectingMoveTarget);
            ShowAttackTiles(move);
        }

        private void OnHighlightClearRequested()
        {
            if (CurrentState == InputState.SelectingAction)
                ShowMovementTiles();
            else
                HighlightsClearRequested?.Invoke();
        }

        // ── State Machine ─────────────────────────────────────────────────

        private void AdvanceToNextCreature()
        {
            if (_creatureIndex >= _creaturesNeedingActions.Count)
            {
                SetState(InputState.Confirming);
                return;
            }

            _activeCreature = _creaturesNeedingActions[_creatureIndex];
            _pendingMovement = null;
            _pendingMove = null;
            _pendingMoveSlot = -1;
            _inCaptureMode = false;

            ActiveCreatureChanged?.Invoke(_activeCreature);
            _infoPanel?.SetDefaultTarget(_activeCreature);
            _movePanel?.RefreshForCreature(_activeCreature);
            _movePanel?.UpdateTrapCount(_combatController.RemainingTraps);
            _tileHighlight?.ClearCache();

            SetState(InputState.SelectingAction);
            ShowMovementTiles();
        }

        private void SetState(InputState state)
        {
            CurrentState = state;
            StateChanged?.Invoke(state);
        }

        // ── Movement Selection ────────────────────────────────────────────

        private void TrySelectMovement(Vector2Int tilePos)
        {
            if (_currentMovementTiles == null || !_currentMovementTiles.Contains(tilePos))
                return;

            var tile = _combatController.Grid.GetTile(tilePos);
            if (tile == null || tile.IsOccupied) return;

            _pendingMovement = tilePos;

            // Stay in SelectingAction — movement is optional, player still needs to pick action
            SetState(InputState.SelectingAction);
        }

        // ── Move Target Selection ─────────────────────────────────────────

        private void TrySelectMoveTargetCreature(CreatureInstance target)
        {
            if (_pendingMove == null || _activeCreature == null) return;

            // Validate target is in range
            var validTargets = TargetingHelper.GetValidCreatureTargets(
                _pendingMove, _activeCreature, _combatController.Grid,
                _combatController.EnemyParty);

            if (!validTargets.Contains(target)) return;

            var action = new TurnAction(
                ActionType.UseMove,
                movementTarget: _pendingMovement,
                move: _pendingMove,
                target: target,
                movePPSlot: _pendingMoveSlot);

            var result = TurnActionValidator.Validate(
                action, _activeCreature, _combatController.EncounterType, _combatController.Grid);

            if (!result.IsValid)
            {
                Debug.LogWarning($"[PlayerInputController] Move target invalid: {result.Reason}");
                return;
            }

            SubmitAndAdvance(action);
        }

        private void TrySelectMoveTargetTile(Vector2Int tilePos)
        {
            if (_pendingMove == null || _activeCreature == null) return;

            // For AoE/Line moves, target is a tile
            if (_pendingMove.TargetType != TargetType.AoE &&
                _pendingMove.TargetType != TargetType.Line)
            {
                // For single-target moves, check if a creature is on this tile
                var tile = _combatController.Grid.GetTile(tilePos);
                if (tile?.Occupant != null)
                    TrySelectMoveTargetCreature(tile.Occupant);
                return;
            }

            // Validate tile is in valid target set
            var validTiles = TargetingHelper.GetValidTargetTiles(
                _pendingMove, _activeCreature, _combatController.Grid);

            if (!validTiles.Contains(tilePos)) return;

            var targetTile = _combatController.Grid.GetTile(tilePos);
            if (targetTile == null) return;

            var action = new TurnAction(
                ActionType.UseMove,
                movementTarget: _pendingMovement,
                move: _pendingMove,
                targetTile: targetTile,
                movePPSlot: _pendingMoveSlot);

            var result = TurnActionValidator.Validate(
                action, _activeCreature, _combatController.EncounterType, _combatController.Grid);

            if (!result.IsValid)
            {
                Debug.LogWarning($"[PlayerInputController] AoE target invalid: {result.Reason}");
                return;
            }

            SubmitAndAdvance(action);
        }

        // ── Capture Mode ──────────────────────────────────────────────────

        private void EnterCaptureMode()
        {
            _inCaptureMode = true;
            _pendingMove = null;
            _pendingMoveSlot = -1;

            SetState(InputState.SelectingAction);

            // Show capture-valid tiles (green)
            if (_activeCreature != null && _tileHighlight != null)
            {
                var captureTiles = _tileHighlight.GetCaptureTiles(
                    _activeCreature.GridPosition,
                    _settings.CaptureRange,
                    _combatController.EnemyParty);

                CaptureTilesReady?.Invoke(captureTiles);
            }
        }

        private void TryCapture(CreatureInstance target)
        {
            if (_activeCreature == null || target == null) return;
            if (target.IsFainted) return;

            // Range check
            int dist = GridSystem.ChebyshevDistance(
                _activeCreature.GridPosition, target.GridPosition);
            if (dist > _settings.CaptureRange) return;

            var action = new TurnAction(
                ActionType.Capture,
                movementTarget: _pendingMovement,
                target: target);

            var result = TurnActionValidator.Validate(
                action, _activeCreature, _combatController.EncounterType, _combatController.Grid);

            if (!result.IsValid)
            {
                Debug.LogWarning($"[PlayerInputController] Capture invalid: {result.Reason}");
                return;
            }

            SubmitAndAdvance(action);
        }

        // ── Submission ────────────────────────────────────────────────────

        private void SubmitAndAdvance(TurnAction action)
        {
            _combatController.SubmitAction(_activeCreature, action);
            ActionConfirmed?.Invoke(_activeCreature, action);

            _pendingMovement = null;
            _pendingMove = null;
            _pendingMoveSlot = -1;
            _inCaptureMode = false;

            _creatureIndex++;
            AdvanceToNextCreature();
        }

        // ── Highlight Helpers ─────────────────────────────────────────────

        private void ShowMovementTiles()
        {
            if (_activeCreature == null || _tileHighlight == null || _settings == null)
                return;

            int movementRange = Mathf.Max(1,
                _activeCreature.ComputedStats.SPD / _settings.MovementDivisor);

            _currentMovementTiles = _tileHighlight.GetMovementTiles(
                _activeCreature.GridPosition, movementRange);

            MovementTilesReady?.Invoke(_currentMovementTiles);
        }

        private void ShowAttackTiles(MoveConfig move)
        {
            if (_activeCreature == null || _tileHighlight == null) return;

            var tiles = _tileHighlight.GetAttackTiles(
                move, _activeCreature.GridPosition, out _);

            AttackTilesReady?.Invoke(tiles);
        }

        // ── Cleanup ───────────────────────────────────────────────────────

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
    }
}
