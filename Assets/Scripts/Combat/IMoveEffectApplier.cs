using GeneForge.Creatures;
using GeneForge.Grid;

namespace GeneForge.Combat
{
    /// <summary>
    /// Contract for applying individual move effects after damage resolution.
    /// Handles status application, stat stage changes, terrain creation, etc.
    /// Does NOT handle Recoil or Drain — those are handled directly in TurnManager
    /// before this interface is called. Implements GDD Turn Manager §6.2 and §3.8.
    /// </summary>
    public interface IMoveEffectApplier
    {
        /// <summary>
        /// Apply a single move effect.
        /// </summary>
        /// <param name="effect">The effect to apply.</param>
        /// <param name="actor">The creature that used the move.</param>
        /// <param name="target">The primary target (may be null for self-targeting effects).</param>
        /// <param name="grid">The combat grid.</param>
        void Apply(MoveEffect effect, CreatureInstance actor, CreatureInstance target, GridSystem grid);
    }
}
