namespace GeneForge.Combat
{
    /// <summary>
    /// The action a creature performs during its action step.
    /// Movement is always available as part of the split turn via TurnAction.MovementTarget —
    /// it is NOT an ActionType. See GDD Turn Manager §3.2 and §3.3.
    /// </summary>
    public enum ActionType
    {
        /// <summary>Use a learned move against a target or tile.</summary>
        UseMove  = 0,

        /// <summary>Throw a Gene Trap at a target creature (wild encounters only).</summary>
        Capture  = 1,

        /// <summary>Use a held item. Post-MVP only — silently treated as Wait in MVP.</summary>
        Item     = 2,

        /// <summary>
        /// Attempt to flee combat. Consumes the entire turn — MovementTarget must be null.
        /// Only valid in wild encounters; no-ops in trainer battles.
        /// </summary>
        Flee     = 3,

        /// <summary>Pass the action step. No state changes.</summary>
        Wait     = 4
    }
}
