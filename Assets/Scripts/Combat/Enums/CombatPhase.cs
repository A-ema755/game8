namespace GeneForge.Combat
{
    /// <summary>
    /// Ordered phases that make up one round of combat.
    /// Implements GDD Turn Manager §3.1 — Phase Sequence.
    /// </summary>
    public enum CombatPhase
    {
        /// <summary>Start-of-round effects (DoT, suppression rolls). Fires RoundStarted.</summary>
        RoundStart           = 0,

        /// <summary>Player submits one TurnAction per non-fainted party creature.</summary>
        PlayerCreatureSelect = 1,

        /// <summary>Player creatures execute in initiative order (split turn: move then act).</summary>
        PlayerAction         = 2,

        /// <summary>Enemy creatures execute in initiative order (split turn: move then act).</summary>
        EnemyAction          = 3,

        /// <summary>End-of-round effects, duration decrements, round counter increment. Fires RoundEnded.</summary>
        RoundEnd             = 4
    }
}
