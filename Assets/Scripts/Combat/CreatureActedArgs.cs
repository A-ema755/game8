using GeneForge.Creatures;

namespace GeneForge.Combat
{
    /// <summary>
    /// Event data emitted by TurnManager.CreatureActed after each creature's turn resolves.
    /// Includes enough information for UI, audio, VFX, and analytics to react
    /// without querying TurnManager state directly.
    /// Implements GDD Turn Manager §6.3.
    /// </summary>
    public readonly struct CreatureActedArgs
    {
        /// <summary>The creature whose turn just resolved.</summary>
        public readonly CreatureInstance Actor;

        /// <summary>The action that was attempted.</summary>
        public readonly ActionType Action;

        /// <summary>
        /// True if the creature was suppressed (Sleep, Freeze, or Paralysis proc)
        /// and skipped both movement and action. PP was not consumed.
        /// </summary>
        public readonly bool WasSuppressed;

        /// <summary>
        /// True if the move missed (failed hit check). PP was consumed.
        /// Only meaningful when Action == UseMove and WasSuppressed == false.
        /// </summary>
        public readonly bool WasMiss;

        /// <summary>
        /// True if the actor hit itself due to Confusion.
        /// Original action did not execute. PP was not consumed.
        /// Only meaningful when Action == UseMove and WasSuppressed == false.
        /// </summary>
        public readonly bool WasConfusionSelfHit;

        /// <summary>Final damage dealt this action step (0 if no damage, miss, or suppressed).</summary>
        public readonly int DamageDealt;

        /// <summary>The round number during which this action occurred.</summary>
        public readonly int Round;

        /// <summary>Creates a CreatureActedArgs.</summary>
        public CreatureActedArgs(
            CreatureInstance actor,
            ActionType action,
            int round,
            bool wasSuppressed      = false,
            bool wasMiss            = false,
            bool wasConfusionSelfHit = false,
            int damageDealt         = 0)
        {
            Actor               = actor;
            Action              = action;
            Round               = round;
            WasSuppressed       = wasSuppressed;
            WasMiss             = wasMiss;
            WasConfusionSelfHit = wasConfusionSelfHit;
            DamageDealt         = damageDealt;
        }
    }
}
