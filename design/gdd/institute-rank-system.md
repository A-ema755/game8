# Institute Rank System

## 1. Overview

The Institute Rank System tracks the player's standing with the Gene Research Institute — their employer and sponsor. Rank advances by completing Pokedex entries, discovering DNA recipes, maintaining conservation scores, and exploring all habitat zones. Five ranks exist: Intern, Field Agent, Researcher, Senior Researcher, and Lead Scientist. Higher rank unlocks better Gene Traps, restricted habitat zones, grant funding, and exclusive research station access. Using black market DNA decreases rank. The `InstituteRankManager` class evaluates rank thresholds at session end and after significant events, adjusting access and perks accordingly.

## 2. Player Fantasy

Your rank is a visible measure of how good a scientist you are. Climbing from Intern to Field Agent after filling your first ten Pokedex entries feels like a promotion you earned. Reaching Senior Researcher and receiving passive grant funding each session changes how you play — the economy opens up. Lead Scientist is a long-term goal that requires mastering the whole game, not just the combat. And the consequence of black market dealings — watching your hard-earned rank slip — gives ethical choices real weight.

## 3. Detailed Rules

### 3.1 Rank Tiers

| Rank | ID | Requirement | Perks |
|------|----|-------------|-------|
| Intern | `intern` | Starting rank | Basic station access, Standard Gene Traps |
| Field Agent | `field-agent` | 10 Pokedex entries (Tier 1+) | Enhanced Gene Traps unlocked; +10% RP from all battles |
| Researcher | `researcher` | 30 Pokedex entries + 5 DNA recipes discovered | Station Level 3 access; exclusive zone permits for restricted areas |
| Senior Researcher | `senior-researcher` | 60 Pokedex entries + conservation score > 50 | Rare Gene Traps unlocked; grant funding (500 RP per session start); rival trainer intel reports |
| Lead Scientist | `lead-scientist` | 100 Pokedex entries + all habitat zones fully explored | Station Level 5 access; Legendary Gene Trap access; Forbidden Mod clearance |

"Pokedex entries" for rank purposes counts any species at Tier 1 (Basic) or higher — species seen but not fought (Tier 0) do not count.

### 3.2 InstituteRankManager

```csharp
public class InstituteRankManager
{
    private InstituteRankState _state;
    private PokedexState _pokedex;
    private EcosystemState _ecosystem;

    /// <summary>
    /// Evaluates all rank thresholds and applies the highest earned rank.
    /// Called at session start, after Pokedex updates, and after DNA discoveries.
    /// </summary>
    public void EvaluateRank()
    {
        InstituteRank earned = CalculateEarnedRank();
        if (earned > _state.currentRank)
        {
            _state.currentRank = earned;
            OnRankUp?.Invoke(earned);
        }
        // Rank can decrease from black market penalty — always recalculate
        if (earned < _state.currentRank)
        {
            _state.currentRank = earned;
            OnRankDown?.Invoke(earned);
        }
    }

    private InstituteRank CalculateEarnedRank()
    {
        int entries    = _pokedex.TotalFought;           // Tier 1+
        int recipes    = _state.discoveredRecipeCount;
        float conservation = _ecosystem.ConservationScore;
        bool allZones  = _state.allZonesFullyExplored;
        int blackMarketPenalty = _state.blackMarketStrikes;

        InstituteRank rank = InstituteRank.Intern;

        if (entries >= 10)
            rank = InstituteRank.FieldAgent;
        if (entries >= 30 && recipes >= 5)
            rank = InstituteRank.Researcher;
        if (entries >= 60 && conservation > 50f)
            rank = InstituteRank.SeniorResearcher;
        if (entries >= 100 && allZones)
            rank = InstituteRank.LeadScientist;

        // Black market strikes reduce rank floor
        rank = ApplyBlackMarketPenalty(rank, blackMarketPenalty);
        return rank;
    }

    private InstituteRank ApplyBlackMarketPenalty(InstituteRank earned, int strikes)
    {
        // Each strike drops earned rank by 1 tier, floor at Intern
        int penaltyLevels = Mathf.Clamp(strikes, 0, (int)earned);
        return (InstituteRank)Mathf.Max(0, (int)earned - penaltyLevels);
    }

    public event System.Action<InstituteRank> OnRankUp;
    public event System.Action<InstituteRank> OnRankDown;
}

public enum InstituteRank
{
    Intern          = 0,
    FieldAgent      = 1,
    Researcher      = 2,
    SeniorResearcher = 3,
    LeadScientist   = 4
}
```

### 3.3 Black Market Penalty

Each black market DNA purchase or use increments `blackMarketStrikes` by 1. Strikes are permanent and cannot be reversed. Each strike reduces the player's effective rank by one tier from what they have otherwise earned.

Examples:
- Player earns Researcher (tier 2) but has 1 black market strike → effective rank is Field Agent (tier 1).
- Player earns Lead Scientist (tier 4) but has 2 strikes → effective rank is Researcher (tier 2).
- Player earns Field Agent (tier 1) but has 2 strikes → effective rank is Intern (tier 0, floor).

The UI displays the player's current effective rank and, separately, a note showing "Ethics Penalty: N strikes" if strikes > 0, so the player understands why their rank is lower than their research achievements warrant.

### 3.4 Conservation Score

Conservation Score (0–100) is maintained by the Living Ecosystem System. It reflects the health of creature populations across all explored habitat zones:

- Score starts at 50 (neutral).
- Rises when: creature populations in explored zones remain diverse, predator/prey ratios are balanced, and the player completes conservation-tagged bounties.
- Falls when: a species is over-captured to near-extinction levels in a zone, ecosystem imbalances go unaddressed for multiple sessions.

Conservation Score > 50 is required for Senior Researcher rank (alongside 60 entries). A player who aggressively over-captures will block themselves from Senior Researcher until the ecosystem recovers.

### 3.5 Zone Exploration Requirement (Lead Scientist)

"All habitat zones fully explored" means:
- Every zone on the campaign map has been entered at least once.
- Every named sub-area within each zone has had at least one encounter completed.
- This flag is set in `InstituteRankState.allZonesFullyExplored` by the Campaign Map when all zone nodes are marked visited.

For MVP (single Verdant Basin zone), this flag is set to true when all 10 Verdant Basin encounters have been completed.

### 3.6 Rank Perks in Detail

**Intern:**
- Standard Gene Trap purchases available at stations.
- Basic research station functions (heal, party swap, common stat boosts).

**Field Agent (+10% RP from battles):**
```csharp
int rpAward = Mathf.FloorToInt(baseRp * (1f + GetRankRpBonus()));

float GetRankRpBonus() => _rankManager.CurrentRank >= InstituteRank.FieldAgent ? 0.10f : 0.0f;
```
- Enhanced Gene Traps available for purchase at stations.

**Researcher (Station Level 3 access, zone permits):**
- Restricted habitat zone nodes become accessible on the campaign map.
- Station Level 3 (Splice Lab) features unlock: cross-species DNA splicing, pattern extraction.

**Senior Researcher (Grant Funding, Rare Traps, Rival Intel):**
- 500 RP deposited to player inventory at each session start.
- Rare Gene Traps (3x modifier on matching type) available for purchase.
- Rival trainer intel: a report appears at research stations summarizing the rival's current team composition before the next trainer encounter.

**Lead Scientist (Station Level 5, Legendary Traps, Forbidden Mods):**
- Station Level 5 (Apex Lab): Forbidden Mod installation, fossil resurrection, advanced crafting.
- Legendary Gene Trap (4x modifier) available at select stations only.
- Forbidden Mod clearance: the Institute no longer flags Forbidden Mod use as an ethics violation.

### 3.7 NPC Reactions

Station staff and NPCs address the player differently per rank:
- Intern: "New recruit, huh? Welcome to the field."
- Field Agent: "Agent [name]. Good work out there."
- Researcher: "Dr. [name]. Your data has been invaluable."
- Senior Researcher: "Senior Researcher [name]. The Institute sends its regards and funding."
- Lead Scientist: "Lead Scientist [name]. You've rewritten the textbooks."

Rival trainers also reference rank: commenting on promotions, mocking demotions, and noting ethics penalties if strikes > 0.

### 3.8 Rank State Persistence

```csharp
[System.Serializable]
public class InstituteRankState
{
    public InstituteRank currentRank;
    public int discoveredRecipeCount;
    public int blackMarketStrikes;
    public bool allZonesFullyExplored;
    public float lastSessionGrantAwarded;   // Timestamp to prevent double-grant on load
}
```

## 4. Formulas

### Rank Threshold Summary

```
Intern:            always
FieldAgent:        pokedexEntries >= 10
Researcher:        pokedexEntries >= 30 AND recipes >= 5
SeniorResearcher:  pokedexEntries >= 60 AND conservationScore > 50
LeadScientist:     pokedexEntries >= 100 AND allZonesExplored == true
```

### Black Market Penalty

```
effectiveRank = Max(0, earnedRankTier - blackMarketStrikes)
```

### Battle RP Bonus (Field Agent+)

```
finalRp = Floor(baseRp * (1.0 + rankRpBonus))
rankRpBonus = 0.10   // Field Agent and above
```

### Session Grant (Senior Researcher+)

```
grantRp = 500   // Awarded once per session start, not per battle
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Player earns enough entries for Researcher but conservation drops below 50 | Senior Researcher not available; Researcher rank held; conservation must recover |
| Black market strikes exceed earned rank tier | Rank floors at Intern; additional strikes have no further mechanical effect but are still recorded |
| Player reaches 100 entries but hasn't explored all zones | Lead Scientist locked; `allZonesFullyExplored` flag must also be true |
| Session grant fires twice on a single session (load/save edge case) | `lastSessionGrantAwarded` timestamp compared to session start time; grant skipped if same session |
| Player achieves Senior Researcher, then over-captures and conservation drops to 48 | Rank recalculated on next `EvaluateRank()` call; drops to Researcher; perks revoked accordingly |
| Player uses black market DNA mid-campaign after reaching Lead Scientist with 0 strikes | 1 strike drops effective rank to Senior Researcher; Forbidden Mod clearance revoked |
| Pokedex entries decremented (bug scenario) | Entries never decrement in normal play; if count decreases due to a bug, `EvaluateRank()` re-evaluates and drops rank if threshold no longer met |
| MVP has only 1 zone; Lead Scientist requires all zones | `allZonesFullyExplored` is set true when all 10 Verdant Basin encounters are completed; Lead Scientist achievable in MVP |
| Rival intel report references a trainer encounter already completed | Report shows "Rival team data: no upcoming encounter detected" |

## 6. Dependencies

| System | Dependency Type | Notes |
|--------|----------------|-------|
| Pokedex System | Read | `TotalFought` (Tier 1+ entries) for rank thresholds |
| Living Ecosystem System | Read | `ConservationScore` for Senior Researcher threshold |
| Campaign Map | Read | Zone exploration flags for Lead Scientist threshold |
| DNA Alteration System | Read | Discovered recipe count; Forbidden Mod clearance flag |
| Black Market System (post-MVP) | Write | Increments `blackMarketStrikes` on each purchase/use |
| Research Station Upgrade System | Read | Rank gates station level access |
| Capture System | Read | Trap tier availability gated by rank |
| Save/Load System | Read/Write | `InstituteRankState` persisted in save file |
| Party Management UI | Read | Displays current rank and ethics penalty in player profile |
| Encounter System | Read | Rival intel report reads from upcoming trainer encounter config |

## 7. Tuning Knobs

| Parameter | Default | Range | Notes |
|-----------|---------|-------|-------|
| Field Agent entry threshold | 10 | 5–20 | Pokedex Tier 1+ entries required |
| Researcher entry threshold | 30 | 20–50 | |
| Researcher recipe threshold | 5 | 3–10 | DNA recipes discovered required |
| Senior Researcher entry threshold | 60 | 40–80 | |
| Senior Researcher conservation threshold | 50 | 30–70 | Conservation score floor |
| Lead Scientist entry threshold | 100 | 80–120 | |
| Field Agent RP bonus | 0.10 | 0.05–0.25 | Fraction of base RP added per battle |
| Senior Researcher session grant | 500 | 100–1000 | RP awarded per session start |
| Black market penalty per strike | 1 rank tier | 1–2 | Ranks dropped per strike |
| `allZonesFullyExplored` MVP definition | All 10 VB encounters cleared | Boolean | Expands to multi-zone in vertical slice |

## 8. Acceptance Criteria

- [ ] Player starts at Intern rank with Standard Gene Traps available and no RP bonus.
- [ ] Reaching 10 Pokedex Tier 1+ entries triggers Field Agent rank-up notification; Enhanced Gene Traps become purchasable.
- [ ] Field Agent rank applies +10% RP to all battle rewards (verified against base RP formula).
- [ ] Researcher rank requires both 30 entries AND 5 discovered recipes; missing either keeps rank at Field Agent.
- [ ] Station Level 3 features are inaccessible below Researcher rank; accessible at Researcher and above.
- [ ] Senior Researcher requires conservation score > 50; dropping to 50 or below after achieving it revokes the rank on next evaluation.
- [ ] 500 RP grant is deposited exactly once per session start at Senior Researcher+; not awarded again on save/load in same session.
- [ ] Lead Scientist requires 100 entries AND `allZonesFullyExplored`; neither condition alone is sufficient.
- [ ] 1 black market strike drops effective rank by exactly 1 tier; 2 strikes by 2 tiers; floors at Intern.
- [ ] Ethics penalty count is displayed separately from earned rank in the player profile UI.
- [ ] NPC dialogue references correct rank title for each of the 5 rank tiers.
- [ ] `InstituteRankState` (rank, strikes, recipes, zone flags) persists correctly through a save/load cycle.
- [ ] `EvaluateRank()` is called after each Pokedex update and DNA recipe discovery without perceptible frame lag.
- [ ] Rank drop (from conservation fall or black market) correctly revokes associated perks.
