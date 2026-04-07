using System.Collections.Generic;
using GeneForge.Core;
using GeneForge.Gameplay;

namespace GeneForge.Combat
{
    /// <summary>
    /// Interface for encounter initialization.
    /// Matches existing combat system pattern (IDamageCalculator, ICaptureSystem, etc.).
    /// </summary>
    public interface IEncounterManager
    {
        /// <summary>
        /// Initialize a battle from config and player party.
        /// Returns a fully populated BattleContext ready for TurnManager.
        /// </summary>
        BattleContext InitializeEncounter(EncounterConfig config, PartyState playerParty);

        /// <summary>
        /// Validate config integrity. Returns list of error strings.
        /// Empty list means config is valid.
        /// </summary>
        List<string> ValidateConfig(EncounterConfig config);
    }
}
