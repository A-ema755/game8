using System.Collections.Generic;
using GeneForge.Core;
using GeneForge.Creatures;
using UnityEngine;
using UnityEngine.UIElements;

namespace GeneForge.UI
{
    /// <summary>
    /// Manages the turn order icon strip at the top of the Combat HUD.
    /// Displays creature portrait placeholders, HP pips, and status icons
    /// in initiative order.
    ///
    /// Implements design/ux/combat-ui-ux-spec.md §4.1.
    /// </summary>
    public class TurnOrderBarController
    {
        private readonly VisualElement _container;
        private readonly List<VisualElement> _iconElements = new();
        private readonly Dictionary<CreatureInstance, VisualElement> _creatureToIcon = new();
        private VisualElement _activeIcon;

        // Type color lookup matching visual spec §1.6
        internal static readonly Dictionary<CreatureType, Color> TypeColors = new()
        {
            { CreatureType.None, new Color(0.11f, 0.15f, 0.19f) },
            { CreatureType.Thermal, new Color(0.78f, 0.29f, 0.10f) },
            { CreatureType.Aqua, new Color(0.10f, 0.43f, 0.80f) },
            { CreatureType.Organic, new Color(0.16f, 0.48f, 0.18f) },
            { CreatureType.Bioelectric, new Color(0.72f, 0.63f, 0.06f) },
            { CreatureType.Cryo, new Color(0.23f, 0.56f, 0.71f) },
            { CreatureType.Mineral, new Color(0.42f, 0.48f, 0.35f) },
            { CreatureType.Toxic, new Color(0.50f, 0.19f, 0.75f) },
            { CreatureType.Neural, new Color(0.38f, 0.31f, 0.66f) },
            { CreatureType.Ferro, new Color(0.29f, 0.38f, 0.47f) },
            { CreatureType.Kinetic, new Color(0.63f, 0.31f, 0.13f) },
            { CreatureType.Aero, new Color(0.16f, 0.60f, 0.69f) },
            { CreatureType.Sonic, new Color(0.57f, 0.16f, 0.44f) },
            { CreatureType.Ark, new Color(0.78f, 0.60f, 0.06f) },
            { CreatureType.Blight, new Color(0.35f, 0.44f, 0.06f) }
        };

        /// <summary>Creates a TurnOrderBarController bound to the icon container element.</summary>
        public TurnOrderBarController(VisualElement container)
        {
            _container = container;
        }

        /// <summary>
        /// Rebuild the turn order bar with all creatures in initiative order.
        /// Called on RoundStarted.
        /// </summary>
        public void Refresh(
            List<CreatureInstance> allCreatures,
            CreatureInstance activeCreature,
            HashSet<CreatureInstance> playerSet)
        {
            _container.Clear();
            _iconElements.Clear();
            _creatureToIcon.Clear();

            foreach (var creature in allCreatures)
            {
                if (creature.IsFainted) continue;

                var icon = CreateIcon(creature,
                    playerSet.Contains(creature),
                    creature == activeCreature);

                _container.Add(icon);
                _iconElements.Add(icon);
                _creatureToIcon[creature] = icon;
            }
        }

        /// <summary>Set the active creature indicator (gold border + enlarged).</summary>
        public void SetActiveCreature(CreatureInstance creature)
        {
            // O(1): remove from previous active only
            _activeIcon?.RemoveFromClassList("icon--active");
            _activeIcon = null;

            if (creature != null && _creatureToIcon.TryGetValue(creature, out var icon))
            {
                icon.AddToClassList("icon--active");
                _activeIcon = icon;
            }
        }

        /// <summary>Update HP pips and status icon for a single creature.</summary>
        public void UpdateCreature(CreatureInstance creature)
        {
            if (!_creatureToIcon.TryGetValue(creature, out var icon)) return;
            UpdateHPPips(icon, creature);
            UpdateStatusIcon(icon, creature);
        }

        /// <summary>Remove a creature's icon (on faint).</summary>
        public void RemoveCreature(CreatureInstance creature)
        {
            if (!_creatureToIcon.TryGetValue(creature, out var icon)) return;
            _container.Remove(icon);
            _iconElements.Remove(icon);
            _creatureToIcon.Remove(creature);
        }

        // ── Icon Creation ─────────────────────────────────────────────────

        private VisualElement CreateIcon(CreatureInstance creature, bool isPlayer, bool isActive)
        {
            var icon = new VisualElement();
            icon.AddToClassList("turn-order-icon");
            icon.AddToClassList(isPlayer ? "icon--player" : "icon--enemy");
            icon.tooltip = $"{(isPlayer ? CombatStrings.PlayerTag : CombatStrings.EnemyTag)} {creature.Nickname ?? creature.DisplayName}";

            if (isActive)
                icon.AddToClassList("icon--active");

            // Base background is panel-bg-raised; team tint applied via CSS class
            // Portrait placeholder: type-colored overlay + 2-letter initials
            var type = creature.Config != null ? creature.Config.PrimaryType : CreatureType.None;
            if (TypeColors.TryGetValue(type, out var color))
            {
                // Add type-color overlay as a child element so it doesn't stomp team tint
                var typeOverlay = new VisualElement();
                typeOverlay.style.position = Position.Absolute;
                typeOverlay.style.width = Length.Percent(100);
                typeOverlay.style.height = Length.Percent(100);
                typeOverlay.style.backgroundColor = new Color(color.r, color.g, color.b, 0.5f);
                typeOverlay.pickingMode = PickingMode.Ignore;
                icon.Add(typeOverlay);
            }

            // TODO: Replace with CreatureConfig.PortraitSprite when art pipeline delivers portraits
            var initials = new Label();
            initials.AddToClassList("icon-initials");
            string name = creature.Nickname ?? creature.DisplayName ?? "??";
            initials.text = name.Length >= 2
                ? name.Substring(0, 2).ToUpper()
                : name.ToUpper();
            icon.Add(initials);

            // HP pips
            var pipContainer = new VisualElement();
            pipContainer.AddToClassList("icon-hp-pips");
            int pipCount = GetPipCount(creature.MaxHP);
            int filledPips = GetFilledPips(pipCount, creature.CurrentHP, creature.MaxHP);

            for (int i = 0; i < pipCount; i++)
            {
                var pip = new VisualElement();
                pip.AddToClassList("hp-pip");
                if (i < filledPips)
                    pip.AddToClassList("hp-pip--filled");
                pipContainer.Add(pip);
            }
            icon.Add(pipContainer);

            // Status icon (first active status)
            var statusIcon = new VisualElement();
            statusIcon.AddToClassList("icon-status");
            if (creature.ActiveStatusEffects != null && creature.ActiveStatusEffects.Count > 0)
            {
                statusIcon.style.display = DisplayStyle.Flex;
                statusIcon.tooltip = creature.ActiveStatusEffects[0].ToString();
            }
            else
            {
                statusIcon.style.display = DisplayStyle.None;
            }
            icon.Add(statusIcon);

            return icon;
        }

        private void UpdateHPPips(VisualElement icon, CreatureInstance creature)
        {
            var pipContainer = icon.Q(className: "icon-hp-pips");
            if (pipContainer == null) return;

            int pipCount = GetPipCount(creature.MaxHP);
            int filledPips = GetFilledPips(pipCount, creature.CurrentHP, creature.MaxHP);

            var pips = pipContainer.Children();
            int i = 0;
            foreach (var pip in pips)
            {
                if (i < filledPips)
                    pip.AddToClassList("hp-pip--filled");
                else
                    pip.RemoveFromClassList("hp-pip--filled");
                i++;
            }
        }

        private void UpdateStatusIcon(VisualElement icon, CreatureInstance creature)
        {
            var statusIcon = icon.Q(className: "icon-status");
            if (statusIcon == null) return;

            if (creature.ActiveStatusEffects != null && creature.ActiveStatusEffects.Count > 0)
            {
                statusIcon.style.display = DisplayStyle.Flex;
                statusIcon.tooltip = creature.ActiveStatusEffects[0].ToString();
            }
            else
            {
                statusIcon.style.display = DisplayStyle.None;
            }
        }

        // ── HP Pip Formulas (UX spec §4.1) ────────────────────────────────

        private static int GetPipCount(int maxHP)
        {
            if (maxHP <= 30) return 3;
            if (maxHP <= 60) return 4;
            return 5;
        }

        private static int GetFilledPips(int pipCount, int currentHP, int maxHP)
        {
            if (maxHP <= 0) return 0;
            float ratio = (float)currentHP / maxHP;
            return Mathf.CeilToInt(pipCount * ratio);
        }
    }
}
