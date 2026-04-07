using GeneForge.Creatures;

namespace GeneForge.Combat
{
    /// <summary>
    /// Event data emitted by TurnManager.CreatureCaptured after a capture attempt resolves.
    /// Implements GDD Turn Manager §6.3.
    /// </summary>
    public readonly struct CaptureResultArgs
    {
        /// <summary>The creature that was targeted by the capture attempt.</summary>
        public readonly CreatureInstance Target;

        /// <summary>
        /// True if the capture succeeded and the target has been removed from the enemy party.
        /// False if it failed (e.g., trainer creature, low catch rate, or bad RNG).
        /// </summary>
        public readonly bool Success;

        /// <summary>The round number during which this capture was attempted.</summary>
        public readonly int Round;

        /// <summary>Creates a CaptureResultArgs.</summary>
        public CaptureResultArgs(CreatureInstance target, bool success, int round)
        {
            Target  = target;
            Success = success;
            Round   = round;
        }
    }
}
