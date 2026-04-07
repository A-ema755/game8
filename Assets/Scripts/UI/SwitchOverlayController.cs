using System;
using System.Collections.Generic;
using GeneForge.Creatures;
using GeneForge.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GeneForge.UI
{
    /// <summary>
    /// Controls the Switch Overlay that appears when the player wants to
    /// swap their active creature during combat. Shows party slots with
    /// faint state, and handles selection/cancellation.
    ///
    /// Implements design/ux/combat-ui-ux-spec.md §1.4 and §3.5.
    /// </summary>
    public class SwitchOverlayController
    {
        /// <summary>Fired when the player confirms a switch to a specific creature.</summary>
        public event Action<CreatureInstance> SwitchConfirmed;

        /// <summary>Fired when the overlay closes (cancel or confirm).</summary>
        public event Action Closed;

        private readonly VisualElement _root;
        private readonly VisualElement _backdrop;
        private readonly VisualElement _panel;
        private readonly VisualElement _slotsContainer;
        private readonly Label _infoLabel;
        private readonly CombatController _combatController;

        private List<CreatureInstance> _party;
        private CreatureInstance _activeCreature;
        private int _focusIndex = -1;
        private readonly List<VisualElement> _slotElements = new();

        /// <summary>Creates a SwitchOverlayController bound to the overlay element.</summary>
        public SwitchOverlayController(VisualElement root, CombatController controller)
        {
            _root = root;
            _combatController = controller;
            _backdrop = root.Q("switch-backdrop");
            _panel = root.Q("switch-panel");
            _slotsContainer = root.Q("switch-slots");
            _infoLabel = root.Q<Label>("switch-info");

            _backdrop?.RegisterCallback<ClickEvent>(_ => Hide());
        }

        /// <summary>
        /// Show the overlay with the given party. Active creature is non-interactive.
        /// Fainted creatures are grayed.
        /// </summary>
        public void Show(List<CreatureInstance> party, CreatureInstance activeCreature)
        {
            _party = party;
            _activeCreature = activeCreature;
            _focusIndex = -1;
            _slotElements.Clear();
            _slotsContainer.Clear();

            int switchableCount = 0;

            for (int i = 0; i < party.Count; i++)
            {
                var creature = party[i];
                var slot = CreateSlot(creature, creature == activeCreature);
                _slotsContainer.Add(slot);
                _slotElements.Add(slot);

                if (!creature.IsFainted && creature != activeCreature)
                    switchableCount++;
            }

            // Show info if no valid switch targets
            if (switchableCount == 0)
            {
                _infoLabel.text = "No other creatures available.";
                _infoLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                _infoLabel.style.display = DisplayStyle.None;
            }

            _root.style.display = DisplayStyle.Flex;
        }

        /// <summary>Hide the overlay without submitting an action.</summary>
        public void Hide()
        {
            _root.style.display = DisplayStyle.None;
            Closed?.Invoke();
        }

        /// <summary>Handle keyboard input when the overlay is active.</summary>
        public void HandleKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    Hide();
                    evt.StopPropagation();
                    break;

                case KeyCode.Tab:
                    CycleFocus(evt.shiftKey);
                    evt.StopPropagation();
                    break;

                case KeyCode.Return:
                case KeyCode.Space:
                    ConfirmFocused();
                    evt.StopPropagation();
                    break;
            }
        }

        // ── Private ───────────────────────────────────────────────────────

        private VisualElement CreateSlot(CreatureInstance creature, bool isActive)
        {
            var slot = new VisualElement();
            slot.AddToClassList("switch-slot");

            if (creature.IsFainted)
            {
                slot.AddToClassList("switch-slot--fainted");
                slot.pickingMode = PickingMode.Ignore;
            }
            else if (isActive)
            {
                slot.AddToClassList("switch-slot--active");
                slot.pickingMode = PickingMode.Ignore;
            }
            else
            {
                slot.pickingMode = PickingMode.Position;
                var captured = creature; // Closure capture
                slot.RegisterCallback<ClickEvent>(_ => OnSlotClicked(captured));
            }

            // Type-colored background for portrait placeholder
            var type = creature.Config?.PrimaryType ?? CreatureType.None;
            if (TurnOrderBarController.TypeColors.TryGetValue(type, out var color))
                slot.style.backgroundColor = new Color(color.r, color.g, color.b, 0.5f);

            var nameLabel = new Label();
            nameLabel.AddToClassList("slot-name");
            nameLabel.text = creature.Nickname ?? creature.DisplayName ?? "???";
            slot.Add(nameLabel);

            var hpLabel = new Label();
            hpLabel.AddToClassList("slot-hp");

            if (creature.IsFainted)
                hpLabel.text = "FAINTED";
            else if (isActive)
                hpLabel.text = "ACTIVE";
            else
                hpLabel.text = $"{creature.CurrentHP} / {creature.MaxHP}";

            slot.Add(hpLabel);

            return slot;
        }

        private void OnSlotClicked(CreatureInstance creature)
        {
            SwitchConfirmed?.Invoke(creature);
            Hide();
        }

        private void CycleFocus(bool reverse)
        {
            if (_party == null || _party.Count == 0) return;

            int direction = reverse ? -1 : 1;
            int start = _focusIndex < 0 ? 0 : _focusIndex + direction;

            for (int attempts = 0; attempts < _party.Count; attempts++)
            {
                int idx = ((start + attempts * direction) % _party.Count + _party.Count) % _party.Count;
                var creature = _party[idx];

                if (!creature.IsFainted && creature != _activeCreature)
                {
                    SetFocus(idx);
                    return;
                }
            }
        }

        private void SetFocus(int index)
        {
            // Remove previous focus
            if (_focusIndex >= 0 && _focusIndex < _slotElements.Count)
                _slotElements[_focusIndex].RemoveFromClassList("switch-slot--focused");

            _focusIndex = index;

            if (_focusIndex >= 0 && _focusIndex < _slotElements.Count)
                _slotElements[_focusIndex].AddToClassList("switch-slot--focused");
        }

        private void ConfirmFocused()
        {
            if (_focusIndex < 0 || _focusIndex >= _party.Count) return;

            var creature = _party[_focusIndex];
            if (creature.IsFainted || creature == _activeCreature) return;

            OnSlotClicked(creature);
        }
    }
}
