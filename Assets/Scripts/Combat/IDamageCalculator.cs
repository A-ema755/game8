using GeneForge.Creatures;
using GeneForge.Grid;

namespace GeneForge.Combat
{
    /// <summary>
    /// Contract for damage resolution.
    /// Implemented by DamageCalculator and injected into TurnManager via constructor.
    /// Implements GDD Turn Manager §6.2.
    /// </summary>
    public interface IDamageCalculator
    {
        /// <summary>
        /// Calculate damage dealt by <paramref name="attacker"/> using
        /// <paramref name="move"/> against <paramref name="target"/>.
        /// </summary>
        /// <param name="move">The move being used.</param>
        /// <param name="attacker">The attacking creature.</param>
        /// <param name="target">The defending creature.</param>
        /// <param name="grid">The combat grid (used for height and flanking bonuses).</param>
        /// <returns>Final damage value. Always >= 1 for damaging moves (min damage enforced).</returns>
        int Calculate(MoveConfig move, CreatureInstance attacker, CreatureInstance target, GridSystem grid);
    }
}
