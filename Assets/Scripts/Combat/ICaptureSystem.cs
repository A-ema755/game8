using GeneForge.Creatures;

namespace GeneForge.Combat
{
    /// <summary>
    /// Contract for capture attempt resolution.
    /// Implemented by CaptureSystem and injected into TurnManager via constructor.
    /// Implements GDD Turn Manager §6.2.
    /// </summary>
    public interface ICaptureSystem
    {
        /// <summary>
        /// Attempt to capture <paramref name="target"/> using <paramref name="actor"/>'s Gene Trap.
        /// Always returns false for trainer-owned creatures regardless of formula outcome.
        /// </summary>
        /// <param name="target">The creature being targeted for capture.</param>
        /// <param name="actor">The creature performing the capture action.</param>
        /// <returns>True if capture succeeded and the target should be removed from the enemy party.</returns>
        bool Attempt(CreatureInstance target, CreatureInstance actor);
    }
}
