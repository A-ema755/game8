using System;
using System.Collections.Generic;
using GeneForge.Combat;
using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;
using UnityEngine;
using UnityEngine.UIElements;

namespace GeneForge.UI
{
    /// <summary>
    /// Controls the Move Selection Panel at the bottom of the Combat HUD.
    /// Manages move buttons (slots 0-3), Gene Trap, and Switch button states.
    /// Handles move selection, targeting sub-states, and action submission.
    ///
    /// Implements design/ux/combat-ui-ux-spec.md §3.1 and §4.3.
    /// </summary>
    public class MoveSelectionPanelController
    {
        // ── Events ────────────────────────────────────────────────────────

        /// <summary>Fired when a complete action is submitted for a creature.</summary>
        public event Action<CreatureInstance, TurnAction> ActionSubmitted;

        /// <summary>Fired when the Switch button is pressed.</summary>
        public event Action SwitchRequested;

        /// <summary>Fired when a move is selected/deselected (for tile highlighting).</summary>
        public event Action<MoveConfig> MoveHighlightRequested;

        /// <summary>Fired when tile highlights should be cleared.</summary>
        public event Action HighlightClearRequested;

        /// <summary>Fired when the focused target changes via Tab cycling.</summary>
        public event Action<CreatureInstance> TargetFocusChanged;

        // ── State ─────────────────────────────────────────────────────────

        private readonly VisualElement _root;
        private readonly CombatController _combatController;
        private readonly VisualElement[] _moveButtons = new VisualElement[4];
        private readonly Label[] _moveNames = new Label[4];
        private readonly Label[] _moveTypes = new Label[4];
        private readonly Label[] _moveForms = new Label[4];
        private readonly Label[] _movePPs = new Label[4];
        private VisualElement _geneTrapBtn;
        private Label _trapCountLabel;
        private VisualElement _switchBtn;

        private CreatureInstance _activeCreature;
        private MoveConfig[] _loadedMoves = new MoveConfig[4];
        private int _selectedSlot = -1;
        private bool _isLocked;
        private bool _isStruggleMode;
        private CreatureInstance _focusedTarget;

        /// <summary>Creates a MoveSelectionPanelController bound to the panel element.</summary>
        public MoveSelectionPanelController(VisualElement root, CombatController controller)
        {
            _root = root;
            _combatController = controller;

            CacheElements();
            RegisterClickHandlers();
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Lock or unlock the panel (locked during execution phases).</summary>
        public void SetLocked(bool locked)
        {
            _isLocked = locked;
            if (locked)
            {
                _root.AddToClassList("panel--locked");
                _root.pickingMode = PickingMode.Ignore;
            }
            else
            {
                _root.RemoveFromClassList("panel--locked");
                _root.pickingMode = PickingMode.Position;
            }
        }

        /// <summary>
        /// Refresh the panel for the given creature. Called by PlayerInputController
        /// when advancing to each creature's action selection turn.
        /// </summary>
        /// <param name="creature">The creature whose moves should be displayed.</param>
        public void RefreshForCreature(CreatureInstance creature)
        {
            _selectedSlot = -1;
            _activeCreature = creature;

            if (_activeCreature == null) return;
            BindCreature(_activeCreature);
        }

        /// <summary>Select a move slot (0-3) or toggle targeting.</summary>
        public void SelectMoveSlot(int slot)
        {
            if (_isLocked || _activeCreature == null) return;
            if (slot < 0 || slot >= 4) return;

            // Struggle mode: slot 0 triggers Struggle targeting
            if (_isStruggleMode && slot == 0)
            {
                DeselectAll();
                _selectedSlot = 0;
                _moveButtons[0].AddToClassList("move-button--selected");
                // Signal Struggle targeting — pass null to indicate melee-range targeting
                MoveHighlightRequested?.Invoke(null);
                return;
            }

            if (_loadedMoves[slot] == null) return;

            // Check if move is usable
            if (_activeCreature.LearnedMovePP[slot] <= 0) return;
            if (_loadedMoves[slot].Form != DamageForm.None &&
                !_activeCreature.AvailableForms.Contains(_loadedMoves[slot].Form))
                return;

            if (_selectedSlot == slot)
            {
                // Toggle off — cancel targeting
                CancelTargeting();
                return;
            }

            // Select new move
            DeselectAll();
            _selectedSlot = slot;
            _moveButtons[slot].AddToClassList("move-button--selected");
            MoveHighlightRequested?.Invoke(_loadedMoves[slot]);
        }

        /// <summary>Select Gene Trap for capture targeting.</summary>
        public void SelectGeneTrap()
        {
            if (_isLocked || _activeCreature == null) return;
            if (_combatController.EncounterType == EncounterType.Trainer) return;
            if (_combatController.RemainingTraps <= 0) return;

            DeselectAll();
            _selectedSlot = -1;
            _geneTrapBtn.AddToClassList("move-button--selected");
            // Capture targeting mode — highlight capturable tiles
            MoveHighlightRequested?.Invoke(null); // null signals capture mode
        }

        /// <summary>Cancel current targeting and deselect.</summary>
        public void CancelTargeting()
        {
            DeselectAll();
            _selectedSlot = -1;
            HighlightClearRequested?.Invoke();
        }

        /// <summary>Cycle to next valid target (Tab/Shift+Tab). Iterates non-fainted enemies.</summary>
        public void CycleTarget(bool reverse)
        {
            if (_combatController.EnemyParty == null) return;

            var enemies = new List<CreatureInstance>();
            foreach (var e in _combatController.EnemyParty)
            {
                if (!e.IsFainted) enemies.Add(e);
            }
            if (enemies.Count == 0) return;

            int direction = reverse ? -1 : 1;
            int currentIdx = _focusedTarget != null ? enemies.IndexOf(_focusedTarget) : -1;
            int nextIdx = ((currentIdx + direction) % enemies.Count + enemies.Count) % enemies.Count;

            _focusedTarget = enemies[nextIdx];
            TargetFocusChanged?.Invoke(_focusedTarget);
        }

        /// <summary>Confirm currently focused target (Enter/Space).</summary>
        public void ConfirmTarget()
        {
            if (_activeCreature == null) return;

            // Use focused target if available, otherwise first non-fainted enemy
            CreatureInstance target = _focusedTarget;
            if (target == null && _combatController.EnemyParty != null)
            {
                foreach (var e in _combatController.EnemyParty)
                {
                    if (!e.IsFainted) { target = e; break; }
                }
            }
            if (target == null) return;

            if (_isStruggleMode && _selectedSlot == 0)
            {
                // Struggle: use the cached Struggle MoveConfig, MovePPSlot = -1 (no PP deduction).
                var struggleMove = AIDecisionSystem.GetStruggleMoveConfig();
                var action = new TurnAction(
                    ActionType.UseMove,
                    move: struggleMove,
                    target: target,
                    movePPSlot: -1);

                ActionSubmitted?.Invoke(_activeCreature, action);
            }
            else if (_selectedSlot >= 0 && _loadedMoves[_selectedSlot] != null)
            {
                var action = new TurnAction(
                    ActionType.UseMove,
                    move: _loadedMoves[_selectedSlot],
                    target: target,
                    movePPSlot: _selectedSlot);

                ActionSubmitted?.Invoke(_activeCreature, action);
            }
            else if (_geneTrapBtn != null &&
                     _geneTrapBtn.ClassListContains("move-button--selected"))
            {
                // Capture action
                var action = new TurnAction(ActionType.Capture, target: target);
                ActionSubmitted?.Invoke(_activeCreature, action);
            }

            _focusedTarget = null;
            CancelTargeting();
        }

        /// <summary>Update the trap count display.</summary>
        public void UpdateTrapCount(int count)
        {
            if (_trapCountLabel != null)
                _trapCountLabel.text = string.Format(CombatStrings.TrapCountFormat, count);

            UpdateGeneTrapState(count);
        }

        // ── Binding ───────────────────────────────────────────────────────

        private void BindCreature(CreatureInstance creature)
        {
            bool allPPDepleted = true;

            for (int i = 0; i < 4; i++)
            {
                if (i < creature.LearnedMoveIds.Count)
                {
                    string moveId = creature.LearnedMoveIds[i];
                    var move = ConfigLoader.GetMove(moveId);
                    _loadedMoves[i] = move;

                    if (move != null)
                    {
                        _moveNames[i].text = move.DisplayName ?? moveId;
                        _moveTypes[i].text = move.GenomeType.ToString().ToUpper();
                        _moveForms[i].text = move.Form != DamageForm.None
                            ? move.Form.ToString()
                            : "Status";
                        _movePPs[i].text = $"{creature.LearnedMovePP[i]} / {move.PP}";

                        // Apply type color class (visual spec §1.6)
                        ApplyTypeColorClass(_moveTypes[i], move.GenomeType);

                        // Apply form color class (visual spec §1.7)
                        ApplyFormClass(_moveForms[i], move.Form);

                        // Determine button state
                        SetMoveButtonState(i, creature, move);

                        if (creature.LearnedMovePP[i] > 0)
                            allPPDepleted = false;
                    }
                    else
                    {
                        SetEmptySlot(i);
                    }
                }
                else
                {
                    _loadedMoves[i] = null;
                    SetEmptySlot(i);
                }
            }

            // Struggle edge case: all PP depleted
            _isStruggleMode = allPPDepleted && creature.LearnedMoveIds.Count > 0;
            if (_isStruggleMode)
            {
                _moveNames[0].text = CombatStrings.Struggle;
                _moveTypes[0].text = "";
                _moveForms[0].text = "Physical";
                _movePPs[0].text = "—";
                ClearMoveButtonClasses(0);
                _moveButtons[0].AddToClassList("move-button--struggle");
                _moveButtons[0].pickingMode = PickingMode.Position;

                // Slots 2-4 remain visible but grayed with No PP (spec §6.2)
                for (int i = 1; i < 4; i++)
                {
                    if (i < creature.LearnedMoveIds.Count)
                    {
                        ClearMoveButtonClasses(i);
                        _moveButtons[i].AddToClassList("move-button--no-pp");
                        _moveButtons[i].pickingMode = PickingMode.Ignore;
                        _moveButtons[i].style.display = DisplayStyle.Flex;
                    }
                }
            }

            // Gene Trap state
            UpdateGeneTrapState(_combatController.RemainingTraps);
        }

        private void SetMoveButtonState(int slot, CreatureInstance creature, MoveConfig move)
        {
            ClearMoveButtonClasses(slot);

            if (creature.LearnedMovePP[slot] <= 0)
            {
                _moveButtons[slot].AddToClassList("move-button--no-pp");
                _moveButtons[slot].pickingMode = PickingMode.Ignore;
            }
            else if (move.Form != DamageForm.None &&
                     !creature.AvailableForms.Contains(move.Form))
            {
                _moveButtons[slot].AddToClassList("move-button--no-access");
                _moveButtons[slot].pickingMode = PickingMode.Ignore;
            }
            else
            {
                _moveButtons[slot].pickingMode = PickingMode.Position;
            }

            _moveButtons[slot].style.display = DisplayStyle.Flex;
        }

        private void SetEmptySlot(int slot)
        {
            _moveNames[slot].text = "—";
            _moveTypes[slot].text = "";
            _moveForms[slot].text = "";
            _movePPs[slot].text = "";
            ClearMoveButtonClasses(slot);
            _moveButtons[slot].AddToClassList("move-button--empty");
            _moveButtons[slot].pickingMode = PickingMode.Ignore;
        }

        private void ClearMoveButtonClasses(int slot)
        {
            _moveButtons[slot].RemoveFromClassList("move-button--selected");
            _moveButtons[slot].RemoveFromClassList("move-button--no-pp");
            _moveButtons[slot].RemoveFromClassList("move-button--no-access");
            _moveButtons[slot].RemoveFromClassList("move-button--struggle");
            _moveButtons[slot].RemoveFromClassList("move-button--empty");
        }

        private void DeselectAll()
        {
            for (int i = 0; i < 4; i++)
                _moveButtons[i].RemoveFromClassList("move-button--selected");
            _geneTrapBtn?.RemoveFromClassList("move-button--selected");
        }

        private void UpdateGeneTrapState(int trapCount)
        {
            if (_geneTrapBtn == null) return;

            bool disabled = _combatController.EncounterType == EncounterType.Trainer ||
                            trapCount <= 0;

            if (disabled)
            {
                _geneTrapBtn.AddToClassList("move-button--disabled");
                _geneTrapBtn.pickingMode = PickingMode.Ignore;
            }
            else
            {
                _geneTrapBtn.RemoveFromClassList("move-button--disabled");
                _geneTrapBtn.pickingMode = PickingMode.Position;
            }
        }

        // Cached type label class names to avoid per-call Enum.GetValues allocation
        private static readonly Dictionary<CreatureType, string> TypeLabelClasses = BuildTypeLabelClasses();

        private static Dictionary<CreatureType, string> BuildTypeLabelClasses()
        {
            var dict = new Dictionary<CreatureType, string>();
            foreach (CreatureType t in System.Enum.GetValues(typeof(CreatureType)))
                dict[t] = $"type-label--{t.ToString().ToLower()}";
            return dict;
        }

        private static void ApplyTypeColorClass(Label label, CreatureType type)
        {
            foreach (var kvp in TypeLabelClasses)
                label.RemoveFromClassList(kvp.Value);

            if (type != CreatureType.None && TypeLabelClasses.TryGetValue(type, out var cls))
                label.AddToClassList(cls);
        }

        private static void ApplyFormClass(Label label, DamageForm form)
        {
            label.RemoveFromClassList("form--physical");
            label.RemoveFromClassList("form--energy");
            label.RemoveFromClassList("form--bio");

            switch (form)
            {
                case DamageForm.Physical: label.AddToClassList("form--physical"); break;
                case DamageForm.Energy: label.AddToClassList("form--energy"); break;
                case DamageForm.Bio: label.AddToClassList("form--bio"); break;
            }
        }

        // ── Element Caching ───────────────────────────────────────────────

        private void CacheElements()
        {
            for (int i = 0; i < 4; i++)
            {
                _moveButtons[i] = _root.Q($"move-btn-{i}");
                _moveNames[i] = _root.Q<Label>($"move-{i}-name");
                _moveTypes[i] = _root.Q<Label>($"move-{i}-type");
                _moveForms[i] = _root.Q<Label>($"move-{i}-form");
                _movePPs[i] = _root.Q<Label>($"move-{i}-pp");
            }

            _geneTrapBtn = _root.Q("gene-trap-btn");
            _trapCountLabel = _root.Q<Label>("trap-count-label");
            _switchBtn = _root.Q("switch-btn");
        }

        private void RegisterClickHandlers()
        {
            for (int i = 0; i < 4; i++)
            {
                int slot = i; // Capture for closure
                _moveButtons[i]?.RegisterCallback<ClickEvent>(_ => SelectMoveSlot(slot));

                // Hover triggers tile highlight preview (spec §3.1)
                _moveButtons[i]?.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    if (!_isLocked && _loadedMoves[slot] != null)
                        MoveHighlightRequested?.Invoke(_loadedMoves[slot]);
                });
                _moveButtons[i]?.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    if (!_isLocked && _selectedSlot != slot)
                        HighlightClearRequested?.Invoke();
                });
            }

            _geneTrapBtn?.RegisterCallback<ClickEvent>(_ => SelectGeneTrap());
            _switchBtn?.RegisterCallback<ClickEvent>(_ => SwitchRequested?.Invoke());
        }
    }
}
