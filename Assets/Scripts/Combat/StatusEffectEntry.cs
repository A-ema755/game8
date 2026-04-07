using GeneForge.Core;

namespace GeneForge.Combat
{
    /// <summary>
    /// Pairs a status effect with its remaining duration.
    /// Owned by TurnManager's internal combat state — not serialized on CreatureInstance.
    /// Implements GDD Turn Manager §3.7 — Status Effect Tick Timing.
    ///
    /// Duration semantics:
    ///   -1 = indefinite (Burn, Poison — persist until cured or faint).
    ///    0 = expired (will be removed at end of tick).
    ///   >0 = rounds remaining.
    ///
    /// MVP fixed durations per GDD §4.7:
    ///   Sleep     = 3 rounds
    ///   Freeze    = 2 rounds
    ///   Confusion = 3 rounds
    ///   Paralysis = indefinite (-1)
    ///   Burn      = indefinite (-1)
    ///   Poison    = indefinite (-1)
    /// </summary>
    public struct StatusEffectEntry
    {
        /// <summary>The status effect.</summary>
        public StatusEffect Effect;

        /// <summary>
        /// Rounds remaining. -1 = indefinite.
        /// Do not read 0 as "one round left" — 0 means expired.
        /// </summary>
        public int RemainingRounds;

        /// <summary>
        /// Creates a StatusEffectEntry with the given effect and duration.
        /// </summary>
        /// <param name="effect">The status condition.</param>
        /// <param name="remainingRounds">Duration in rounds. Pass -1 for indefinite.</param>
        public StatusEffectEntry(StatusEffect effect, int remainingRounds)
        {
            Effect         = effect;
            RemainingRounds = remainingRounds;
        }

        /// <summary>True when this entry has expired and should be removed.</summary>
        public bool IsExpired => RemainingRounds == 0;

        /// <summary>True when this entry has no duration limit (Burn, Poison, Paralysis).</summary>
        public bool IsIndefinite => RemainingRounds == -1;
    }
}
