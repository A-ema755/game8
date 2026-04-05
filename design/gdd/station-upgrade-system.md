# Station Upgrade System

## 1. Overview

Research Stations progress through five upgrade tiers purchased with Research Points. Each tier unlocks a new category of DNA operations, from basic stat boosts at the default Field Lab up to Forbidden Mod installation and fossil resurrection at the Apex Lab. Upgrades are global — purchasing a tier unlocks it at every Research Station in the game simultaneously. The system is driven by a single StationUpgradeConfig ScriptableObject and a StationUpgradeManager that persists the current tier in the save file.

## 2. Player Fantasy

The player feels their research career advancing in tangible ways. The first time they unlock the Gene Lab and graft a body part onto a creature, the world opens up. Splicing cross-species DNA at the Splice Lab feels like a significant milestone. Reaching the Apex Lab feels like earning forbidden knowledge — the UI subtly changes, the ambient audio darkens, and suddenly truly exotic modifications are possible. Each tier makes the Research Station feel like a more powerful tool, rewarding investment in the game's core DNA fantasy.

## 3. Detailed Rules

### Upgrade Tiers

| Level | Name | Cost | Unlocks |
|-------|------|------|---------|
| 1 | Field Lab | Default (free) | Heal party, party swap, storage access, basic stat boost DNA mods, standard Gene Traps |
| 2 | Gene Lab | 500 RP | Body part grafting, basic color modifications, uncommon Gene Traps |
| 3 | Splice Lab | 1500 RP | Cross-species DNA splicing, pattern extraction from donor species, zone permits |
| 4 | Mutation Lab | 3000 RP | Personality DNA trait modification, instability management tools, rare Gene Traps |
| 5 | Apex Lab | 5000 RP | Forbidden Mod installation, fossil creature resurrection, advanced crafting, legendary Gene Traps |

### Purchase Rules
- Tiers must be purchased in order. Cannot skip from Level 1 to Level 3.
- Cost is cumulative spending, not tiered individually — the costs listed are the unlock price per tier, not running totals.
- RP is deducted immediately on purchase confirmation.
- Insufficient RP shows a locked state with the deficit displayed: "Need 200 more RP."
- Upgrades apply globally. No per-station upgrade tracking.

### StationUpgradeConfig ScriptableObject
```csharp
[CreateAssetMenu(menuName = "GeneForge/StationUpgradeConfig")]
public class StationUpgradeConfig : ScriptableObject
{
    public List<StationTierData> tiers; // index 0 = Field Lab (Level 1)
}

[System.Serializable]
public class StationTierData
{
    public int level;                        // 1–5
    public string displayName;              // "Field Lab", "Gene Lab", etc.
    public int rpCost;                      // 0 for Field Lab
    public string description;             // shown in upgrade UI
    public List<StationFeature> features;  // list of unlocked features
    public Sprite tierIcon;
    public AudioClip unlockSfx;
}

public enum StationFeature
{
    HealParty,
    PartySwap,
    Storage,
    BasicStatMods,
    StandardTraps,
    PartGrafting,
    ColorMods,
    UncommonTraps,
    CrossSpeciesSplicing,
    PatternExtraction,
    ZonePermits,
    PersonalityMods,
    InstabilityManagement,
    RareTraps,
    ForbiddenMods,
    FossilResurrection,
    AdvancedCrafting,
    LegendaryTraps
}
```

### StationUpgradeManager
```csharp
public class StationUpgradeManager : MonoBehaviour
{
    public static StationUpgradeManager Instance { get; private set; }

    [SerializeField] private StationUpgradeConfig _config;

    public int CurrentTier { get; private set; } = 1;

    public event System.Action<int> TierUnlocked;

    public bool HasFeature(StationFeature feature)
    {
        for (int i = 0; i < CurrentTier; i++)
        {
            if (_config.tiers[i].features.Contains(feature)) return true;
        }
        return false;
    }

    public bool TryPurchaseTier(int targetTier, ref int playerRP)
    {
        if (targetTier != CurrentTier + 1) return false;
        int cost = _config.tiers[targetTier - 1].rpCost;
        if (playerRP < cost) return false;

        playerRP -= cost;
        CurrentTier = targetTier;
        TierUnlocked?.Invoke(CurrentTier);
        return true;
    }
}
```

### Feature Gating
- Every DNA modification UI button checks `StationUpgradeManager.Instance.HasFeature(feature)` before enabling.
- Locked features display with a padlock icon and the tier name required: "Requires Gene Lab (Level 2)."
- The DNA Vault system requires `ForbiddenMods` feature (Level 5 only).
- Fossil resurrection requires `FossilResurrection` feature (Level 5 only).

### Research Station UI Integration
- Station UI shows current tier name in the header: "Research Station — Splice Lab."
- An "Upgrade" button opens the upgrade panel, showing all 5 tiers, current level highlighted, and costs for locked tiers.
- Purchased tiers show a checkmark. The next available tier shows its cost with a "Purchase" button.
- Future tiers are visible but grayed with costs shown (players can plan RP investment).

### Institute Rank Gating
- Station Level 3 (Splice Lab) requires Institute Rank "Researcher" or higher.
- Station Level 5 (Apex Lab) requires Institute Rank "Lead Scientist."
- If rank requirement not met, upgrade button shows: "Requires [Rank] Institute rank."

## 4. Formulas

**Total RP Required to Reach Apex Lab:**
```
totalCost = sum(tier.rpCost for tier in tiers where tier.level > 1)
          = 500 + 1500 + 3000 + 5000 = 10,000 RP
```

**RP Deficit Display:**
```
deficit = requiredCost - currentPlayerRP
```
Displayed as: "Need [deficit] more RP" when deficit > 0.

**Upgrade Availability:**
```
canPurchase = (targetTier == currentTier + 1)
           AND (playerRP >= tier.rpCost)
           AND (playerInstituteRank >= tier.requiredRank)
```

## 5. Edge Cases

- **Purchasing mid-session:** If a player earns enough RP during a combat encounter, the upgrade button becomes active on their next Research Station visit. No retroactive grants.
- **Save corruption — tier mismatch:** If saved tier level exceeds the number of tiers in config (e.g., config changed), clamp to max valid tier and log a warning.
- **RP spent on upgrade then player continues without saving:** If the game crashes, RP is lost but tier not saved. On reload, player retains RP (since save had not persisted the deduction). Recovery: save immediately after any tier purchase.
- **Institute rank lost after black market:** If rank drops below a tier's requirement, the tier remains purchased and functional — rank only gates new purchases, not retroactive use.
- **Feature requested at non-station location:** `HasFeature` returns false in combat. DNA modifications are only accessible at Research Stations; the combat UI does not expose modification options.
- **Tier 5 purchased but Forbidden Mod not found yet:** Feature is unlocked at the station, but the Forbidden Mod items must be discovered in DNA Vaults. Apex Lab without vault clears shows the modification slot as "No Forbidden Mods in inventory."

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Save/Load System | Persists CurrentTier in save JSON |
| DNA Alteration System | Checks HasFeature() before enabling modification types |
| DNA Vault System | Requires Level 5 (ForbiddenMods) to install vault rewards |
| Fossil System | Requires Level 5 (FossilResurrection) to resurrect fossil creatures |
| Institute Rank System | Tiers 3 and 5 gated behind rank requirements |
| Capture System | Gene Trap tiers (uncommon/rare/legendary) gated behind station tiers |
| Party Management UI | Shows tier-appropriate options in the Creature Forge screen |
| Research Station (scene) | Reads HasFeature() to enable/disable UI buttons |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `tier2Cost` | 500 RP | Gene Lab unlock cost |
| `tier3Cost` | 1500 RP | Splice Lab unlock cost |
| `tier4Cost` | 3000 RP | Mutation Lab unlock cost |
| `tier5Cost` | 5000 RP | Apex Lab unlock cost |
| `tier3RequiredRank` | Researcher | Institute rank gate |
| `tier5RequiredRank` | Lead Scientist | Institute rank gate |
| `saveOnPurchase` | true | Auto-save after any tier purchase |

## 8. Acceptance Criteria

- [ ] Field Lab features (heal, swap, storage, basic mods) are available from game start without any purchase.
- [ ] Purchasing Gene Lab (500 RP) enables body part grafting and color mod buttons in the station UI.
- [ ] Attempting to purchase Splice Lab without first owning Gene Lab is rejected with an error message.
- [ ] Insufficient RP displays the correct deficit amount on the upgrade button.
- [ ] Tier upgrade persists correctly after save/load cycle.
- [ ] HasFeature() returns false for any feature belonging to a tier above CurrentTier.
- [ ] All five tier icons, names, and descriptions render correctly in the upgrade panel.
- [ ] Institute rank gate prevents Splice Lab purchase until Researcher rank is achieved.
- [ ] Upgrades apply at every Research Station immediately after purchase (not just the current one).
- [ ] TierUnlocked event fires with the correct tier number when a purchase succeeds.
