using System.Collections.Generic;
using System.Linq;
using GeneForge.Combat;
using GeneForge.Core;
using GeneForge.Creatures;
using UnityEngine;
using UnityEngine.UIElements;

namespace GeneForge.UI
{
    /// <summary>
    /// Controls the Creature Info Panel (left side). Displays name, level,
    /// type badges, HP bar, instability meter, status effects, body parts,
    /// and catch predictor.
    ///
    /// Implements design/ux/combat-ui-ux-spec.md §4.2 and §8.3 (target tracking).
    /// </summary>
    public class CreatureInfoPanelController
    {
        private readonly VisualElement _root;
        private readonly CombatController _combatController;

        // Cached UI elements
        private readonly Label _nameLabel;
        private readonly Label _levelLabel;
        private readonly Label _primaryTypeBadge;
        private readonly Label _secondaryTypeBadge;
        private readonly Label _hpValue;
        private readonly VisualElement _hpBarFill;
        private readonly Label _instabilityValue;
        private readonly VisualElement _instabilityBarFill;
        private readonly Label _instabilityWarning;
        private readonly VisualElement _statusEffects;
        private readonly VisualElement _catchPredictor;
        private readonly Label _catchPredictorValue;

        // Target tracking (priority stack per UX spec §8.3)
        // Priority: (1) explicit click > (2) hover > (3) default
        private CreatureInstance _currentTarget;
        private CreatureInstance _defaultTarget;
        private CreatureInstance _hoverTarget;
        private CreatureInstance _explicitClickTarget;

        // Type badge CSS class lookup
        private static readonly Dictionary<CreatureType, string> TypeBadgeClasses = new()
        {
            { CreatureType.Thermal, "type-badge--thermal" },
            { CreatureType.Aqua, "type-badge--aqua" },
            { CreatureType.Organic, "type-badge--organic" },
            { CreatureType.Bioelectric, "type-badge--bioelectric" },
            { CreatureType.Cryo, "type-badge--cryo" },
            { CreatureType.Mineral, "type-badge--mineral" },
            { CreatureType.Toxic, "type-badge--toxic" },
            { CreatureType.Neural, "type-badge--neural" },
            { CreatureType.Ferro, "type-badge--ferro" },
            { CreatureType.Kinetic, "type-badge--kinetic" },
            { CreatureType.Aero, "type-badge--aero" },
            { CreatureType.Sonic, "type-badge--sonic" },
            { CreatureType.Ark, "type-badge--ark" },
            { CreatureType.Blight, "type-badge--blight" }
        };

        /// <summary>Creates a CreatureInfoPanelController bound to the panel element.</summary>
        public CreatureInfoPanelController(VisualElement root, CombatController controller)
        {
            _root = root;
            _combatController = controller;

            _nameLabel = root.Q<Label>("creature-name");
            _levelLabel = root.Q<Label>("creature-level");
            _primaryTypeBadge = root.Q<Label>("primary-type-badge");
            _secondaryTypeBadge = root.Q<Label>("secondary-type-badge");
            _hpValue = root.Q<Label>("hp-value");
            _hpBarFill = root.Q("hp-bar-fill");
            _instabilityValue = root.Q<Label>("instability-value");
            _instabilityBarFill = root.Q("instability-bar-fill");
            _instabilityWarning = root.Q<Label>("instability-warning");
            _statusEffects = root.Q("status-effects");
            _catchPredictor = root.Q("catch-predictor");
            _catchPredictorValue = root.Q<Label>("catch-predictor-value");
        }

        /// <summary>Set the default target (player's active creature).</summary>
        public void SetDefaultTarget(CreatureInstance creature)
        {
            _defaultTarget = creature;
            if (_hoverTarget == null)
                SetTarget(creature);
        }

        /// <summary>Set an explicit click target (holds until another click or ESC).</summary>
        public void SetExplicitTarget(CreatureInstance creature)
        {
            _explicitClickTarget = creature;
            ResolveTarget();
        }

        /// <summary>Clear explicit click target (on ESC or new click).</summary>
        public void ClearExplicitTarget()
        {
            _explicitClickTarget = null;
            ResolveTarget();
        }

        /// <summary>Set a hover target (temporary, from tile hover).</summary>
        public void SetHoverTarget(CreatureInstance creature)
        {
            _hoverTarget = creature;
            ResolveTarget();
        }

        /// <summary>Clear hover target, revert to higher-priority or default.</summary>
        public void ClearHoverTarget()
        {
            _hoverTarget = null;
            ResolveTarget();
        }

        /// <summary>Resolve target from priority stack: click > hover > default.</summary>
        private void ResolveTarget()
        {
            var target = _explicitClickTarget ?? _hoverTarget ?? _defaultTarget;
            SetTarget(target);
        }

        /// <summary>Refresh display if the given creature is currently shown.</summary>
        public void RefreshIfShowing(CreatureInstance creature)
        {
            if (_currentTarget == creature)
                Refresh();
        }

        /// <summary>Handle creature faint — revert to default if fainted creature was shown.</summary>
        public void HandleFaint(CreatureInstance creature)
        {
            if (_currentTarget == creature)
            {
                _hoverTarget = null;
                SetTarget(_defaultTarget);
            }
        }

        /// <summary>Show catch predictor for a target creature.</summary>
        public void ShowCatchPredictor(CreatureInstance target, float trapModifier)
        {
            if (_catchPredictor == null) return;

            if (_combatController.EncounterType == EncounterType.Trainer || target == null)
            {
                _catchPredictorValue.text = "Cannot Capture";
                _catchPredictorValue.RemoveFromClassList("catch-predictor--good");
                _catchPredictorValue.RemoveFromClassList("catch-predictor--marginal");
                _catchPredictorValue.RemoveFromClassList("catch-predictor--low");
                _catchPredictorValue.AddToClassList("catch-predictor--disabled");
                _catchPredictor.style.display = DisplayStyle.Flex;
                return;
            }

            float statusBonus = CaptureCalculator.GetStatusBonus(target.ActiveStatusEffects);
            float catchRate = CaptureCalculator.CalculateCatchRate(
                target.Config, target.CurrentHP, target.MaxHP, trapModifier, statusBonus);

            int percent = Mathf.RoundToInt(catchRate * 100f);
            _catchPredictorValue.text = $"{percent}%";

            _catchPredictorValue.RemoveFromClassList("catch-predictor--good");
            _catchPredictorValue.RemoveFromClassList("catch-predictor--marginal");
            _catchPredictorValue.RemoveFromClassList("catch-predictor--low");
            _catchPredictorValue.RemoveFromClassList("catch-predictor--disabled");

            if (catchRate > 0.6f)
                _catchPredictorValue.AddToClassList("catch-predictor--good");
            else if (catchRate > 0.3f)
                _catchPredictorValue.AddToClassList("catch-predictor--marginal");
            else
                _catchPredictorValue.AddToClassList("catch-predictor--low");

            _catchPredictor.style.display = DisplayStyle.Flex;
        }

        /// <summary>Hide catch predictor.</summary>
        public void HideCatchPredictor()
        {
            if (_catchPredictor != null)
                _catchPredictor.style.display = DisplayStyle.None;
        }

        // ── Private ───────────────────────────────────────────────────────

        private void SetTarget(CreatureInstance creature)
        {
            _currentTarget = creature;
            Refresh();
        }

        private void Refresh()
        {
            if (_currentTarget == null)
            {
                _nameLabel.text = "—";
                _levelLabel.text = "";
                return;
            }

            var creature = _currentTarget;

            // Name + Level
            _nameLabel.text = creature.Nickname ?? creature.DisplayName ?? "Unknown";
            _levelLabel.text = $"LVL {creature.Level}";

            // Type badges
            RefreshTypeBadge(_primaryTypeBadge,
                creature.Config?.PrimaryType ?? CreatureType.None);

            var secondaryType = creature.ActiveSecondaryType;
            if (secondaryType != CreatureType.None)
            {
                RefreshTypeBadge(_secondaryTypeBadge, secondaryType);
                _secondaryTypeBadge.style.display = DisplayStyle.Flex;
            }
            else
            {
                _secondaryTypeBadge.style.display = DisplayStyle.None;
            }

            // HP bar
            int currentHP = creature.CurrentHP;
            int maxHP = creature.MaxHP;
            float ratio = maxHP > 0 ? (float)currentHP / maxHP : 0f;

            _hpValue.text = $"{currentHP} / {maxHP}";
            _hpBarFill.style.width = Length.Percent(ratio * 100f);

            _hpBarFill.RemoveFromClassList("hp-bar--healthy");
            _hpBarFill.RemoveFromClassList("hp-bar--caution");
            _hpBarFill.RemoveFromClassList("hp-bar--critical");

            if (ratio > 0.5f)
                _hpBarFill.AddToClassList("hp-bar--healthy");
            else if (ratio > 0.25f)
                _hpBarFill.AddToClassList("hp-bar--caution");
            else
                _hpBarFill.AddToClassList("hp-bar--critical");

            // Instability bar
            int instability = creature.Instability;
            float instabilityRatio = instability / 100f;
            _instabilityValue.text = instability.ToString();
            _instabilityBarFill.style.width = Length.Percent(instabilityRatio * 100f);

            // Instability color
            if (instability >= 75)
                _instabilityBarFill.style.backgroundColor = new Color(0.80f, 0.13f, 0.13f);
            else if (instability >= 50)
                _instabilityBarFill.style.backgroundColor = new Color(0.91f, 0.63f, 0.06f);
            else
                _instabilityBarFill.style.backgroundColor = new Color(0.18f, 0.80f, 0.35f);

            // Instability warning
            if (instability >= 50)
                _instabilityWarning.AddToClassList("instability-warning--visible");
            else
                _instabilityWarning.RemoveFromClassList("instability-warning--visible");

            // Body part slots
            RefreshBodyParts(creature);

            // Status effects (up to 4)
            RefreshStatusEffects(creature);
        }

        private void RefreshTypeBadge(Label badge, CreatureType type)
        {
            // Remove all type classes
            foreach (var kvp in TypeBadgeClasses)
                badge.RemoveFromClassList(kvp.Value);

            badge.text = type.ToString().ToUpper();

            if (TypeBadgeClasses.TryGetValue(type, out var cls))
                badge.AddToClassList(cls);
        }

        private void RefreshBodyParts(CreatureInstance creature)
        {
            var parts = creature.EquippedPartIds;
            var slots = new[] { BodySlot.Head, BodySlot.Back, BodySlot.Legs };
            var names = new[] { "body-slot-head", "body-slot-back", "body-slot-legs" };

            for (int i = 0; i < slots.Length; i++)
            {
                var slotElement = _root.Q(names[i]);
                if (slotElement == null) continue;

                if (parts != null && parts.TryGetValue(slots[i], out var partId) && !string.IsNullOrEmpty(partId))
                {
                    slotElement.tooltip = partId;
                    slotElement.style.borderBottomColor = new Color(0.18f, 0.80f, 0.35f); // equipped indicator
                    slotElement.style.borderBottomWidth = 2f;
                }
                else
                {
                    slotElement.tooltip = slots[i].ToString();
                    slotElement.style.borderBottomWidth = 0f;
                }
            }
        }

        private void RefreshStatusEffects(CreatureInstance creature)
        {
            if (_statusEffects == null) return;

            var effects = creature.ActiveStatusEffects;
            for (int i = 0; i < 4; i++)
            {
                var iconElement = _statusEffects.Q($"status-icon-{i}");
                if (iconElement == null) continue;

                if (effects != null && i < effects.Count)
                {
                    iconElement.AddToClassList("status-icon--active");
                    iconElement.tooltip = effects[i].ToString();

                    // Show visible overflow "+" badge on 4th icon (spec §6.4)
                    if (i == 3 && effects.Count > 4)
                    {
                        iconElement.tooltip = string.Join("\n",
                            effects.Skip(3).Select(e => e.ToString()));

                        // Add or update badge element
                        var badge = iconElement.Q("overflow-badge");
                        if (badge == null)
                        {
                            badge = new Label { name = "overflow-badge", text = "+" };
                            badge.AddToClassList("status-badge");
                            badge.style.backgroundColor = new Color(0.91f, 0.63f, 0.06f); // #E8A020
                            badge.style.color = Color.white;
                            iconElement.Add(badge);
                        }
                    }
                    else
                    {
                        var badge = iconElement.Q("overflow-badge");
                        if (badge != null) iconElement.Remove(badge);
                    }
                }
                else
                {
                    iconElement.RemoveFromClassList("status-icon--active");
                    iconElement.tooltip = "";
                }
            }
        }
    }
}
