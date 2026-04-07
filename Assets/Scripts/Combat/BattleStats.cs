namespace GeneForge.Combat
{
    /// <summary>
    /// Aggregate combat metrics accumulated during a single combat session.
    /// Queryable after combat ends via TurnManager.Stats.
    /// Implements GDD Turn Manager §3.12.
    ///
    /// Per-round counters (FaintsThisRound, ActionsThisRound, CapturesThisRound)
    /// reset at the start of each round during ApplyStartOfRoundEffects.
    /// Cumulative counters (TotalDamageDealt, TotalDamageTaken, TotalCaptures,
    /// RoundsElapsed) accumulate across all rounds.
    /// </summary>
    public class BattleStats
    {
        /// <summary>Total damage dealt to enemy creatures across all rounds.</summary>
        public int TotalDamageDealt { get; internal set; }

        /// <summary>Total damage taken by player creatures across all rounds.</summary>
        public int TotalDamageTaken { get; internal set; }

        /// <summary>Number of complete rounds elapsed. Incremented at RoundEnd.</summary>
        public int RoundsElapsed { get; internal set; }

        /// <summary>Number of faints that occurred during the current round. Resets at RoundStart.</summary>
        public int FaintsThisRound { get; internal set; }

        /// <summary>Number of actions executed during the current round. Resets at RoundStart.</summary>
        public int ActionsThisRound { get; internal set; }

        /// <summary>Number of successful captures during the current round. Resets at RoundStart.</summary>
        public int CapturesThisRound { get; internal set; }

        /// <summary>Total successful captures across all rounds.</summary>
        public int TotalCaptures { get; internal set; }

        /// <summary>Terminal result of the encounter. Ongoing until combat ends.</summary>
        public CombatResult Result { get; internal set; } = CombatResult.Ongoing;

        /// <summary>Resets per-round counters. Called at the start of each round.</summary>
        internal void ResetRoundCounters()
        {
            FaintsThisRound   = 0;
            ActionsThisRound  = 0;
            CapturesThisRound = 0;
        }
    }
}
