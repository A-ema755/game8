using System.Collections.Generic;
using GeneForge.Combat;
using UnityEngine;
using UnityEngine.UIElements;

namespace GeneForge.UI
{
    /// <summary>
    /// Manages screen-space type effectiveness callout labels.
    /// Uses a small pool of reusable Label elements to avoid per-hit allocation.
    ///
    /// Implements design/ux/combat-ui-ux-spec.md §4.5 and
    /// design/art/combat-ui-visual-spec.md §5.2.
    /// </summary>
    public class TypeEffectivenessCallout
    {
        private readonly VisualElement _container;
        private readonly List<Label> _pool = new();
        private float _lastCalloutTime;
        private int _queueOffset;

        private const int PoolSize = 3;
        private const float CalloutQueueWindow = 0.5f;
        private const float CalloutYOffset = 40f;

        /// <summary>Creates a TypeEffectivenessCallout bound to the callout container.</summary>
        public TypeEffectivenessCallout(VisualElement container)
        {
            _container = container;

            // Pre-allocate pool
            for (int i = 0; i < PoolSize; i++)
            {
                var label = new Label();
                label.AddToClassList("type-callout");
                label.pickingMode = PickingMode.Ignore;
                label.style.display = DisplayStyle.None;
                _container.Add(label);
                _pool.Add(label);
            }
        }

        /// <summary>
        /// Show a type effectiveness callout at the given screen position.
        /// EffectivenessLabel.Neutral produces no callout.
        /// </summary>
        /// <param name="label">The effectiveness label from TypeChart.GetLabel().</param>
        /// <param name="screenX">Screen X position (from WorldToScreenPoint).</param>
        /// <param name="screenY">Screen Y position.</param>
        public void ShowCallout(EffectivenessLabel label, float screenX, float screenY)
        {
            if (label == EffectivenessLabel.Neutral) return;

            // Queue offset: if previous callout still visible, offset Y
            float currentTime = Time.time;
            if (currentTime - _lastCalloutTime < CalloutQueueWindow)
                _queueOffset++;
            else
                _queueOffset = 0;

            _lastCalloutTime = currentTime;
            float yOffset = _queueOffset * CalloutYOffset;

            // Acquire label from pool (reuse oldest hidden, or force-recycle first)
            var calloutLabel = AcquireFromPool();
            if (calloutLabel == null) return;

            // Configure
            calloutLabel.RemoveFromClassList("type-callout--super-effective");
            calloutLabel.RemoveFromClassList("type-callout--not-very-effective");

            if (label == EffectivenessLabel.SuperEffective)
            {
                calloutLabel.text = "Super Effective!";
                calloutLabel.AddToClassList("type-callout--super-effective");
            }
            else
            {
                calloutLabel.text = "Not Very Effective...";
                calloutLabel.AddToClassList("type-callout--not-very-effective");
            }

            // Position
            calloutLabel.style.left = screenX;
            calloutLabel.style.top = screenY - yOffset;

            // Initial state
            float startScale = label == EffectivenessLabel.SuperEffective ? 0.6f : 1.0f;
            calloutLabel.style.scale = new Scale(new Vector3(startScale, startScale, 1f));
            calloutLabel.style.opacity = 1f;
            calloutLabel.style.display = DisplayStyle.Flex;

            // Animation
            if (label == EffectivenessLabel.SuperEffective)
            {
                calloutLabel.schedule.Execute(() =>
                    calloutLabel.style.scale = new Scale(new Vector3(1.1f, 1.1f, 1f))
                ).ExecuteLater(10);

                calloutLabel.schedule.Execute(() =>
                    calloutLabel.style.scale = new Scale(Vector3.one)
                ).ExecuteLater(200);

                calloutLabel.schedule.Execute(() =>
                    calloutLabel.style.opacity = 0f
                ).ExecuteLater(1400);

                calloutLabel.schedule.Execute(() =>
                    ReturnToPool(calloutLabel)
                ).ExecuteLater(1700);
            }
            else
            {
                calloutLabel.schedule.Execute(() =>
                    calloutLabel.style.scale = new Scale(new Vector3(0.9f, 0.9f, 1f))
                ).ExecuteLater(10);

                calloutLabel.schedule.Execute(() =>
                    calloutLabel.style.opacity = 0f
                ).ExecuteLater(1400);

                calloutLabel.schedule.Execute(() =>
                    ReturnToPool(calloutLabel)
                ).ExecuteLater(1700);
            }
        }

        /// <summary>Clear all active callouts (e.g., on combat end).</summary>
        public void ClearAll()
        {
            foreach (var label in _pool)
                ReturnToPool(label);
        }

        private Label AcquireFromPool()
        {
            // Find first hidden label
            foreach (var label in _pool)
            {
                if (label.style.display == DisplayStyle.None)
                    return label;
            }

            // All in use — force-recycle the first one
            if (_pool.Count > 0)
            {
                var recycled = _pool[0];
                ReturnToPool(recycled);
                return recycled;
            }

            return null;
        }

        private static void ReturnToPool(Label label)
        {
            label.style.display = DisplayStyle.None;
            label.style.opacity = 0f;
        }
    }
}
