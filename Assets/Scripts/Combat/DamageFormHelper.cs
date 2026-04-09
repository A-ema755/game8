using GeneForge.Creatures;

namespace GeneForge.Combat
{
    /// <summary>
    /// Shared damage form utilities used by DamageCalculator and AIActionScorer.
    /// Eliminates DRY violation between the two stat-pairing implementations.
    /// </summary>
    public static class DamageFormHelper
    {
        /// <summary>
        /// Select offensive and defensive stats based on damage form.
        /// Physical: ATK vs DEF. Energy: ATK vs SPD. Bio: ACC vs DEF.
        /// </summary>
        /// <param name="form">The damage form of the move being evaluated.</param>
        /// <param name="attacker">Creature performing the attack.</param>
        /// <param name="defender">Creature receiving the attack.</param>
        /// <param name="offStat">Output: the offensive stat value.</param>
        /// <param name="defStat">Output: the defensive stat value.</param>
        public static void GetStatPairing(
            DamageForm form,
            CreatureInstance attacker,
            CreatureInstance defender,
            out int offStat,
            out int defStat)
        {
            switch (form)
            {
                case DamageForm.Energy:
                    offStat = attacker.ComputedStats.ATK;
                    defStat = defender.ComputedStats.SPD;
                    break;
                case DamageForm.Bio:
                    offStat = attacker.ComputedStats.ACC;
                    defStat = defender.ComputedStats.DEF;
                    break;
                case DamageForm.Physical:
                default:
                    offStat = attacker.ComputedStats.ATK;
                    defStat = defender.ComputedStats.DEF;
                    break;
            }
        }
    }
}
