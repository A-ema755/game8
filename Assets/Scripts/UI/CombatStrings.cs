namespace GeneForge.UI
{
    /// <summary>
    /// Centralized combat UI string constants.
    /// MVP localization: constants here, full i18n post-MVP.
    /// </summary>
    public static class CombatStrings
    {
        // Combat end
        public const string Victory = "VICTORY";
        public const string Defeat = "DEFEAT";
        public const string Escaped = "ESCAPED";
        public const string Draw = "DRAW";

        // Type effectiveness
        public const string SuperEffective = "Super Effective!";
        public const string NotVeryEffective = "Not Very Effective...";

        // Switch overlay
        public const string NoOtherCreatures = "No other creatures available.";
        public const string Fainted = "FAINTED";
        public const string Active = "ACTIVE";

        // Turn order
        public const string PlayerTag = "[Player]";
        public const string EnemyTag = "[Enemy]";

        // Creature info
        public const string LevelFormat = "LVL {0}";
        public const string HpFormat = "{0} / {1}";

        // Move panel
        public const string TrapCountFormat = "x{0}";
        public const string Struggle = "STRUGGLE";
    }
}
