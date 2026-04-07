using GeneForge.Creatures;

namespace GeneForge.Combat
{
    /// <summary>
    /// Resolves capture attempts by delegating math to <see cref="CaptureCalculator"/>.
    /// Pure C# class — no MonoBehaviour (ADR-002). RNG injected for testability (ADR-003).
    /// MVP uses trapModifier=1.0 (Standard Gene Trap); inventory system not yet implemented.
    /// Implements GDD capture-system.md §3.2.
    /// </summary>
    public class CaptureSystem : ICaptureSystem
    {
        private readonly System.Random _rng;

        public CaptureSystem(System.Random rng = null)
        {
            _rng = rng ?? new System.Random();
        }

        /// <inheritdoc/>
        public bool Attempt(CreatureInstance target, CreatureInstance actor)
        {
            // MVP: Standard Gene Trap (1.0x modifier). Post-MVP: read from inventory.
            const float trapModifier = 1.0f;

            float statusBonus = CaptureCalculator.GetStatusBonus(target.ActiveStatusEffects);
            float catchRate = CaptureCalculator.CalculateCatchRate(
                target.Config, target.CurrentHP, target.MaxHP, trapModifier, statusBonus);

            return CaptureCalculator.AttemptCapture(catchRate, _rng);
        }
    }
}
