namespace GeneForge.Combat
{
    /// <summary>
    /// Terminal outcome of a combat encounter.
    /// Implements GDD Turn Manager §3.2 — Enums.
    /// </summary>
    public enum CombatResult
    {
        /// <summary>Combat is still in progress.</summary>
        Ongoing = 0,

        /// <summary>All enemy creatures fainted. Player wins.</summary>
        Victory = 1,

        /// <summary>All player creatures fainted. Player loses.</summary>
        Defeat  = 2,

        /// <summary>Player executed a successful Flee action in a wild encounter.</summary>
        Fled    = 3,

        /// <summary>Max rounds exceeded (post-MVP; requires Draw implementation in TurnManager).</summary>
        Draw    = 4
    }
}
