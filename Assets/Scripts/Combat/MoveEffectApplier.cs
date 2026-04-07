using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;

namespace GeneForge.Combat
{
    /// <summary>
    /// Applies individual move effects after damage resolution.
    /// Recoil and Drain are handled directly by TurnManager — this class handles the rest.
    /// Pure C# class — no MonoBehaviour (ADR-002). RNG injected for testability (ADR-003).
    /// Implements GDD turn-manager.md §3.8 step 9.
    /// </summary>
    public class MoveEffectApplier : IMoveEffectApplier
    {
        private readonly System.Random _rng;

        public MoveEffectApplier(System.Random rng = null)
        {
            _rng = rng ?? new System.Random();
        }

        /// <inheritdoc/>
        public void Apply(MoveEffect effect, CreatureInstance actor, CreatureInstance target, GridSystem grid)
        {
            // Chance roll — effect.Chance is [0.0, 1.0].
            if (effect.Chance < 1.0f && _rng.NextDouble() >= effect.Chance)
                return;

            var recipient = effect.AffectsSelf ? actor : target;
            if (recipient == null) return;

            switch (effect.EffectType)
            {
                case MoveEffectType.ApplyStatus:
                    recipient.ApplyStatusEffect(effect.StatusToApply);
                    break;

                case MoveEffectType.StatStage:
                    // MVP: stat stages not yet on CreatureInstance.
                    // Post-MVP: recipient.ModifyStatStage(effect.StatTarget, effect.Magnitude);
                    break;

                case MoveEffectType.HighCrit:
                    // Handled by DamageCalculator crit check — effect presence is the flag.
                    break;

                // Post-MVP effect types — no-op until systems exist.
                case MoveEffectType.ForcedMove:
                case MoveEffectType.IgnoreDefense:
                case MoveEffectType.MultiHit:
                case MoveEffectType.Flinch:
                case MoveEffectType.TerrainCreate:
                case MoveEffectType.PriorityNext:
                    break;
            }
        }
    }
}
