using System;
using System.Collections.Generic;
using GeneForge.Combat;
using GeneForge.Core;
using GeneForge.Creatures;
using UnityEngine;
using UnityEngine.UIElements;

namespace GeneForge.UI
{
    /// <summary>
    /// Root controller for the Combat HUD. Attaches to the UIDocument GameObject.
    /// Manages the UI state machine, routes CombatController events to sub-panel
    /// controllers, and handles keyboard input dispatch.
    ///
    /// Implements design/ux/combat-ui-ux-spec.md §2 (panel visibility) and §5 (keyboard).
    /// ADR-003 exception: MonoBehaviour required for UIDocument attachment.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CombatHUDController : MonoBehaviour
    {
        // ── Serialized Fields ─────────────────────────────────────────────

        [SerializeField] CombatController combatController;

        // ── Sub-Panel Controllers ─────────────────────────────────────────

        private TurnOrderBarController _turnOrderBar;
        private CreatureInfoPanelController _creatureInfoPanel;
        private MoveSelectionPanelController _moveSelectionPanel;
        private SwitchOverlayController _switchOverlay;
        private TypeEffectivenessCallout _typeCallout;

        // ── UI Elements ───────────────────────────────────────────────────

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _turnOrderBarElement;
        private VisualElement _creatureInfoPanelElement;
        private VisualElement _moveSelectionPanelElement;
        private VisualElement _switchOverlayElement;
        private VisualElement _calloutContainer;
        private VisualElement _combatEndOverlay;
        private Label _combatEndLabel;

        // ── State ─────────────────────────────────────────────────────────

        private CombatUIPhase _currentPhase = CombatUIPhase.Inactive;
        private bool _subscribed;
        private bool _initialized;
        private bool _switchOverlayOpen;
        private Camera _cachedCamera;

        // ── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (combatController != null && !_subscribed)
                SubscribeToController();
        }

        private void OnDisable()
        {
            UnsubscribeFromController();
            UnregisterInputCallbacks();
        }

        /// <summary>
        /// Initialize the HUD after the UIDocument has loaded. Must be called
        /// in Start or later — not Awake — to ensure rootVisualElement is ready.
        /// </summary>
        public void Initialize(CombatController controller)
        {
            if (_initialized) return;

            combatController = controller;
            _cachedCamera = Camera.main;
            CacheUIElements();
            CreateSubControllers();

            if (!_subscribed)
                SubscribeToController();

            SetAllPanelsHidden();
            _initialized = true;
        }

        // ── UI Element Caching ────────────────────────────────────────────

        private void CacheUIElements()
        {
            _root = _uiDocument.rootVisualElement;
            _turnOrderBarElement = _root.Q("turn-order-bar");
            _creatureInfoPanelElement = _root.Q("creature-info-panel");
            _moveSelectionPanelElement = _root.Q("move-selection-panel");
            _switchOverlayElement = _root.Q("switch-overlay");
            _calloutContainer = _root.Q("callout-container");
            _combatEndOverlay = _root.Q("combat-end-overlay");
            _combatEndLabel = _root.Q<Label>("combat-end-label");

            // Register keyboard input on root
            _root.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void UnregisterInputCallbacks()
        {
            if (_root != null)
                _root.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void CreateSubControllers()
        {
            _turnOrderBar = new TurnOrderBarController(
                _root.Q("turn-order-icons"));

            _creatureInfoPanel = new CreatureInfoPanelController(
                _creatureInfoPanelElement,
                combatController);

            _moveSelectionPanel = new MoveSelectionPanelController(
                _moveSelectionPanelElement,
                combatController);

            _moveSelectionPanel.ActionSubmitted += OnActionSubmitted;
            _moveSelectionPanel.SwitchRequested += OnSwitchRequested;

            _switchOverlay = new SwitchOverlayController(
                _switchOverlayElement,
                combatController);

            _switchOverlay.SwitchConfirmed += OnSwitchConfirmed;
            _switchOverlay.Closed += () => _switchOverlayOpen = false;

            _typeCallout = new TypeEffectivenessCallout(_calloutContainer);
        }

        // ── Event Subscriptions ───────────────────────────────────────────

        private void SubscribeToController()
        {
            if (_subscribed || combatController == null) return;

            combatController.PhaseChanged += OnPhaseChanged;
            combatController.CreatureActed += OnCreatureActed;
            combatController.CreatureFainted += OnCreatureFainted;
            combatController.CreatureCaptured += OnCreatureCaptured;
            combatController.RoundStarted += OnRoundStarted;
            combatController.RoundEnded += OnRoundEnded;
            _subscribed = true;
        }

        private void UnsubscribeFromController()
        {
            if (!_subscribed || combatController == null) return;

            combatController.PhaseChanged -= OnPhaseChanged;
            combatController.CreatureActed -= OnCreatureActed;
            combatController.CreatureFainted -= OnCreatureFainted;
            combatController.CreatureCaptured -= OnCreatureCaptured;
            combatController.RoundStarted -= OnRoundStarted;
            combatController.RoundEnded -= OnRoundEnded;

            // Unsubscribe sub-panel events
            if (_moveSelectionPanel != null)
            {
                _moveSelectionPanel.ActionSubmitted -= OnActionSubmitted;
                _moveSelectionPanel.SwitchRequested -= OnSwitchRequested;
            }
            if (_switchOverlay != null)
                _switchOverlay.SwitchConfirmed -= OnSwitchConfirmed;

            _subscribed = false;
        }

        // ── Phase Handling ────────────────────────────────────────────────

        private void OnPhaseChanged(CombatUIPhase phase)
        {
            _currentPhase = phase;
            UpdatePanelVisibility(phase);
        }

        /// <summary>
        /// Sets panel visibility per UX spec §2.1 visibility table.
        /// </summary>
        private void UpdatePanelVisibility(CombatUIPhase phase)
        {
            switch (phase)
            {
                case CombatUIPhase.Inactive:
                    SetAllPanelsHidden();
                    break;

                case CombatUIPhase.RoundStart:
                    ShowElement(_turnOrderBarElement);
                    ShowElement(_creatureInfoPanelElement);
                    HideElement(_moveSelectionPanelElement);
                    HideElement(_switchOverlayElement);
                    HideElement(_combatEndOverlay);
                    break;

                case CombatUIPhase.PlayerSelect:
                    ShowElement(_turnOrderBarElement);
                    ShowElement(_creatureInfoPanelElement);
                    ShowElement(_moveSelectionPanelElement);
                    _moveSelectionPanel.SetLocked(false);
                    _moveSelectionPanel.RefreshForCreature();
                    HideElement(_combatEndOverlay);
                    break;

                case CombatUIPhase.PlayerExecuting:
                case CombatUIPhase.EnemyExecuting:
                    ShowElement(_turnOrderBarElement);
                    ShowElement(_creatureInfoPanelElement);
                    ShowElement(_moveSelectionPanelElement);
                    _moveSelectionPanel.SetLocked(true);
                    HideElement(_switchOverlayElement);
                    HideElement(_combatEndOverlay);
                    break;

                case CombatUIPhase.RoundEnd:
                    ShowElement(_turnOrderBarElement);
                    ShowElement(_creatureInfoPanelElement);
                    HideElement(_moveSelectionPanelElement);
                    HideElement(_switchOverlayElement);
                    HideElement(_combatEndOverlay);
                    break;

                case CombatUIPhase.CombatEnd:
                    HideElement(_turnOrderBarElement);
                    HideElement(_creatureInfoPanelElement);
                    HideElement(_moveSelectionPanelElement);
                    HideElement(_switchOverlayElement);
                    ShowCombatEndOverlay();
                    break;
            }
        }

        // ── TurnManager Event Handlers ────────────────────────────────────

        private void OnRoundStarted(int round)
        {
            var allCreatures = new List<CreatureInstance>();
            allCreatures.AddRange(combatController.PlayerParty);
            allCreatures.AddRange(combatController.EnemyParty);

            // Remove fainted, sort by initiative approximation (higher SPD = earlier)
            allCreatures.RemoveAll(c => c.IsFainted);
            allCreatures.Sort((a, b) => b.ComputedStats.SPD.CompareTo(a.ComputedStats.SPD));

            // Active creature = first in initiative order
            CreatureInstance active = allCreatures.Count > 0 ? allCreatures[0] : null;
            if (active == null)
            {
                foreach (var c in combatController.PlayerParty)
                {
                    if (!c.IsFainted) { active = c; break; }
                }
            }

            _turnOrderBar.Refresh(allCreatures, active,
                new HashSet<CreatureInstance>(combatController.PlayerParty));

            _creatureInfoPanel.SetDefaultTarget(active);
        }

        private void OnRoundEnded(int round)
        {
            // Update status effect displays at round end (spec §1.1)
            if (combatController.PlayerParty != null)
            {
                foreach (var c in combatController.PlayerParty)
                    _creatureInfoPanel.RefreshIfShowing(c);
            }
            if (combatController.EnemyParty != null)
            {
                foreach (var c in combatController.EnemyParty)
                    _creatureInfoPanel.RefreshIfShowing(c);
            }
        }

        private void OnCreatureActed(CreatureActedArgs args)
        {
            _turnOrderBar.UpdateCreature(args.Actor);
            _creatureInfoPanel.RefreshIfShowing(args.Actor);

            // Type effectiveness callout — best-effort using last enemy hit.
            // CreatureActedArgs lacks target reference, so we check each non-fainted
            // enemy for HP change as a proxy. Post-MVP: extend CreatureActedArgs.
            if (args.Action == ActionType.UseMove && args.DamageDealt > 0 &&
                _typeCallout != null && combatController.EnemyParty != null)
            {
                // Find likely target (enemy with lowest HP that isn't fainted)
                foreach (var enemy in combatController.EnemyParty)
                {
                    if (enemy.IsFainted) continue;

                    // Use actor's primary type as a rough GenomeType proxy
                    var actorType = args.Actor.Config?.PrimaryType ?? CreatureType.None;
                    var multiplier = TypeChart.GetMultiplier(
                        actorType, enemy.Config?.PrimaryType ?? CreatureType.None,
                        enemy.ActiveSecondaryType);
                    var label = TypeChart.GetLabel(multiplier);

                    if (label != EffectivenessLabel.Neutral)
                    {
                        var worldPos = enemy.GridPosition;
                        var tile = combatController.Grid?.GetTile(worldPos);
                        if (tile != null)
                        {
                            var screenPos = _cachedCamera != null
                                ? _cachedCamera.WorldToScreenPoint(
                                    tile.WorldPosition + Vector3.up * 2f)
                                : Vector3.zero;
                            _typeCallout.ShowCallout(label, screenPos.x,
                                Screen.height - screenPos.y);
                        }
                    }
                    break; // Only check first non-fainted enemy as likely target
                }
            }
        }

        private void OnCreatureFainted(CreatureInstance creature)
        {
            _turnOrderBar.RemoveCreature(creature);
            _creatureInfoPanel.HandleFaint(creature);
        }

        private void OnCreatureCaptured(CaptureResultArgs args)
        {
            if (args.Success)
            {
                _turnOrderBar.RemoveCreature(args.Target);
                _moveSelectionPanel.UpdateTrapCount(combatController.RemainingTraps);
            }
        }

        // ── Action Routing ────────────────────────────────────────────────

        private void OnActionSubmitted(CreatureInstance creature, TurnAction action)
        {
            combatController.SubmitAction(creature, action);
        }

        private void OnSwitchRequested()
        {
            var party = new List<CreatureInstance>(combatController.PlayerParty);
            CreatureInstance active = null;
            foreach (var c in party)
            {
                if (!c.IsFainted) { active = c; break; }
            }

            _switchOverlay.Show(party, active);
            _switchOverlayOpen = true;
        }

        private void OnSwitchConfirmed(CreatureInstance switchTo)
        {
            _switchOverlayOpen = false;
            // ActionType enum has no Switch value — use Wait to consume the turn.
            // TurnManager treats Wait as "no action, turn consumed."
            // Post-MVP: add ActionType.Switch and handle bench swap in TurnManager.
            var switchAction = new TurnAction(ActionType.Wait);

            // Submit for current active creature (the one being switched out)
            CreatureInstance active = null;
            foreach (var c in combatController.PlayerParty)
            {
                if (!c.IsFainted) { active = c; break; }
            }

            if (active != null)
                combatController.SubmitAction(active, switchAction);
        }

        // ── Keyboard Input ────────────────────────────────────────────────

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (_currentPhase == CombatUIPhase.Inactive ||
                _currentPhase == CombatUIPhase.CombatEnd)
                return;

            // Switch overlay takes priority
            if (_switchOverlayOpen)
            {
                _switchOverlay.HandleKeyDown(evt);
                return;
            }

            if (_currentPhase != CombatUIPhase.PlayerSelect)
                return;

            switch (evt.keyCode)
            {
                case KeyCode.Alpha1:
                case KeyCode.Keypad1:
                    _moveSelectionPanel.SelectMoveSlot(0);
                    evt.StopPropagation();
                    break;
                case KeyCode.Alpha2:
                case KeyCode.Keypad2:
                    _moveSelectionPanel.SelectMoveSlot(1);
                    evt.StopPropagation();
                    break;
                case KeyCode.Alpha3:
                case KeyCode.Keypad3:
                    _moveSelectionPanel.SelectMoveSlot(2);
                    evt.StopPropagation();
                    break;
                case KeyCode.Alpha4:
                case KeyCode.Keypad4:
                    _moveSelectionPanel.SelectMoveSlot(3);
                    evt.StopPropagation();
                    break;
                case KeyCode.Alpha5:
                case KeyCode.Keypad5:
                    _moveSelectionPanel.SelectGeneTrap();
                    evt.StopPropagation();
                    break;
                case KeyCode.Alpha6:
                case KeyCode.Keypad6:
                    OnSwitchRequested();
                    evt.StopPropagation();
                    break;
                case KeyCode.Escape:
                    _moveSelectionPanel.CancelTargeting();
                    evt.StopPropagation();
                    break;
                case KeyCode.Tab:
                    _moveSelectionPanel.CycleTarget(evt.shiftKey);
                    evt.StopPropagation();
                    break;
                case KeyCode.Return:
                case KeyCode.Space:
                    _moveSelectionPanel.ConfirmTarget();
                    evt.StopPropagation();
                    break;
            }
        }

        // ── Combat End ────────────────────────────────────────────────────

        private void ShowCombatEndOverlay()
        {
            ShowElement(_combatEndOverlay);

            var result = combatController.TurnManager?.Stats?.Result ?? CombatResult.Ongoing;
            string text = result switch
            {
                CombatResult.Victory => "VICTORY",
                CombatResult.Defeat => "DEFEAT",
                CombatResult.Fled => "ESCAPED",
                CombatResult.Draw => "DRAW",
                _ => ""
            };

            _combatEndLabel.text = text;
        }

        // ── Visibility Helpers ────────────────────────────────────────────

        private void SetAllPanelsHidden()
        {
            HideElement(_turnOrderBarElement);
            HideElement(_creatureInfoPanelElement);
            HideElement(_moveSelectionPanelElement);
            HideElement(_switchOverlayElement);
            HideElement(_combatEndOverlay);
        }

        private static void ShowElement(VisualElement element)
        {
            if (element != null)
                element.style.display = DisplayStyle.Flex;
        }

        private static void HideElement(VisualElement element)
        {
            if (element != null)
                element.style.display = DisplayStyle.None;
        }
    }
}
