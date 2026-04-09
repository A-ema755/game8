using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Gameplay;
using UnityEngine;

namespace GeneForge.UI
{
    /// <summary>
    /// Test bootstrap that auto-starts a combat encounter on scene load.
    /// Attach to a GameObject in the Combat scene for quick playtesting.
    /// NOT for production use — bypasses normal game flow.
    /// </summary>
    public class CombatTestBootstrap : MonoBehaviour
    {
        [SerializeField] private CombatController combatController;
        [SerializeField] private CombatHUDController combatHUD;
        [SerializeField] private string encounterConfigId = "test-wild-encounter";
        [SerializeField] private int seed = 12345;

        [Header("Player Party (creature IDs from Resources/Data/Creatures)")]
        [SerializeField] private string[] partyCreatureIds = { "emberfox", "voltfin" };
        [SerializeField] private int partyLevel = 10;

        private void Start()
        {
            // Ensure ConfigLoader is initialized
            ConfigLoader.Initialize();

            // Build player party
            var party = new PartyState();
            foreach (var id in partyCreatureIds)
            {
                var config = ConfigLoader.GetCreature(id) as CreatureConfig;
                if (config == null)
                {
                    Debug.LogError($"[CombatTestBootstrap] Creature '{id}' not found.");
                    continue;
                }
                var creature = CreatureInstance.Create(config, partyLevel);
                party.AddToParty(creature);
            }

            if (party.ActiveParty.Count == 0)
            {
                Debug.LogError("[CombatTestBootstrap] No valid creatures in party. Cannot start combat.");
                return;
            }

            // Load encounter config
            var encounter = ConfigLoader.GetEncounter(encounterConfigId) as EncounterConfig;
            if (encounter == null)
            {
                Debug.LogError($"[CombatTestBootstrap] Encounter '{encounterConfigId}' not found.");
                return;
            }

            // Initialize HUD before starting combat (must happen after UIDocument is ready)
            if (combatHUD != null)
                combatHUD.Initialize(combatController);

            Debug.Log($"[CombatTestBootstrap] Starting combat: {encounter.DisplayName} with {party.ActiveParty.Count} creatures at level {partyLevel}.");
            combatController.StartCombat(encounter, party, seed);
        }
    }
}
