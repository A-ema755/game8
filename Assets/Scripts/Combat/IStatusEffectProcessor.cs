using System.Collections.Generic;
using GeneForge.Creatures;

namespace GeneForge.Combat
{
    /// <summary>
    /// Contract for status effect tick processing.
    /// Implemented by StatusEffectProcessor and injected into TurnManager via constructor.
    /// TurnManager calls these methods at the appropriate phase boundaries.
    /// Implements GDD Turn Manager §6.2 and §3.7.
    /// </summary>
    public interface IStatusEffectProcessor
    {
        /// <summary>
        /// Apply start-of-round status effects for <paramref name="creature"/>.
        /// Handles: Burn/Poison DoT damage, Sleep/Freeze unconditional suppression,
        /// Paralysis suppression roll.
        /// Returns true if the creature is suppressed this round (Sleep, Freeze, or Paralysis proc).
        /// </summary>
        /// <param name="creature">The creature being processed.</param>
        /// <param name="statusEntries">
        /// Mutable status entry list for this creature owned by TurnManager.
        /// Processor may remove entries (e.g. Sleep expiry) but must not add new ones.
        /// </param>
        /// <param name="rngRoll">
        /// Pre-rolled value in [0.0, 1.0) for the Paralysis suppression check.
        /// Passed in to keep RNG ownership in TurnManager.
        /// </param>
        /// <returns>True if the creature is suppressed and should skip movement and action.</returns>
        bool ApplyStartOfRound(
            CreatureInstance creature,
            List<StatusEffectEntry> statusEntries,
            double rngRoll);

        /// <summary>
        /// Decrement duration counters for time-limited effects at end of round.
        /// Removes entries that reach duration 0.
        /// Does NOT handle Burn and Poison (indefinite, no decrement needed).
        /// </summary>
        /// <param name="creature">The creature being processed.</param>
        /// <param name="statusEntries">
        /// Mutable status entry list for this creature owned by TurnManager.
        /// </param>
        void DecrementDurations(
            CreatureInstance creature,
            List<StatusEffectEntry> statusEntries);
    }
}
