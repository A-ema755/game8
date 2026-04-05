# Pokedex System

## 1. Overview

The Pokedex System is Gene Forge's progressive discovery journal. Information about each species unlocks across four research tiers as the player observes, fights, captures, and repeatedly battles creatures. Tier 0 (Silhouette) is granted on sight; Tier 1 (Basic) after one battle; Tier 2 (Full Profile) after capture; Tier 3 (Research Complete) after 10+ battles. A separate Paleo Archive section tracks fossil/extinct creatures. Pokedex completion contributes to Institute Rank. The full `PokedexState` persists in the player's save file.

## 2. Player Fantasy

Your Pokedex fills in through genuine play, not a menu sweep. The first time you see a creature's silhouette lurking in tall grass, you want to find it. Fighting it reveals its type and stats. Catching it unlocks its move pool and habitats. Battling it again and again until the Research Complete banner fires — and suddenly every DNA recipe involving that species is laid out in front of you — rewards dedication. The Pokedex is a living record of what you've learned, not a checklist handed to you.

## 3. Detailed Rules

### 3.1 Research Tiers

| Tier | Name | Trigger | Information Unlocked |
|------|------|---------|---------------------|
| 0 | Silhouette | Creature appears on campaign map or combat grid | Species name, silhouette image only |
| 1 | Basic | Player's creature dealt or received damage from this species at least once | Type(s), base stat totals, habitat zone(s) |
| 2 | Full Profile | Successful capture of this species | Full base stats, move pool, DNA compatibility list, lore entry, creature call (if Creature Call System active) |
| 3 | Research Complete | Cumulative battle count against this species >= `researchBattleThreshold` (default 10) | All DNA recipes involving this species revealed, affinity perk list, breeding data (post-MVP) |

Tiers are strictly sequential — Tier 2 requires Tier 1 to already be unlocked, etc. However, triggers can stack in a single session: capturing a creature you've never fought before grants Tier 1 and Tier 2 simultaneously.

### 3.2 PokedexEntry Class

```csharp
[System.Serializable]
public class PokedexEntry
{
    public string speciesId;
    public PokedexTier tier;               // 0–3
    public int battleCount;                // Cumulative battles against this species
    public bool hasBeenCaptured;
    public System.DateTime firstSeenDate;
    public System.DateTime researchCompleteDate;  // Default: DateTime.MinValue if not reached
    public List<string> discoveredRecipes; // Recipe IDs unlocked at Tier 3
    public List<ScarRecord> observedScars; // Scars seen on wild specimens of this species
}

public enum PokedexTier
{
    Unseen      = -1,  // Not in Pokedex at all; creature has never been encountered
    Silhouette  =  0,
    Basic       =  1,
    FullProfile =  2,
    ResearchComplete = 3
}
```

### 3.3 PokedexState

```csharp
[System.Serializable]
public class PokedexState
{
    public Dictionary<string, PokedexEntry> entries;  // Keyed by speciesId
    public PaleoArchiveState paleoArchive;

    public int TotalSeen    => entries.Count(e => e.Value.tier >= PokedexTier.Silhouette);
    public int TotalFought  => entries.Count(e => e.Value.tier >= PokedexTier.Basic);
    public int TotalCaptured => entries.Count(e => e.Value.hasBeenCaptured);
    public int TotalComplete => entries.Count(e => e.Value.tier == PokedexTier.ResearchComplete);
}
```

### 3.4 Tier Unlock Events

The Pokedex System subscribes to game events and upgrades tiers reactively:

| Event | Handler |
|-------|---------|
| `CreatureSpottedOnMap(speciesId)` | Unlock Tier 0 if not already seen |
| `CombatDamageDealt(attackerOwner, targetSpeciesId)` | Unlock Tier 1 for `targetSpeciesId` |
| `CombatDamageReceived(playerCreature, sourceSpeciesId)` | Unlock Tier 1 for `sourceSpeciesId` |
| `CreatureCaptured(speciesId)` | Set `hasBeenCaptured = true`; unlock Tier 2 |
| `BattleEnded(enemySpeciesIds)` | Increment `battleCount` for each species that participated; check Tier 3 threshold |

Battle count increments once per species per battle (not per creature — a horde of 10 Leaflings counts as 1 battle-count increment for Leafling).

### 3.5 Displayed Information by Tier

**Tier 0 — Silhouette:**
- Species name displayed.
- Silhouette image (grey fill of model outline).
- "???" shown for all other fields.
- Map habitat dot shown as a question mark.

**Tier 1 — Basic:**
- Primary and secondary type badges.
- Total base stat bar (not individual stats).
- Habitat zone name(s).
- Flavor text line 1 (vague description).

**Tier 2 — Full Profile:**
- Individual base stats (HP, ATK, DEF, SPD, ACC).
- Full move pool with level-learn thresholds.
- DNA compatibility list (which species' DNA can be spliced onto this one).
- Full lore entry (2–3 paragraphs).
- Capture rate displayed.
- Known habitat tiles on zone map.

**Tier 3 — Research Complete:**
- All DNA recipes involving this species (donor + recipient + result).
- Affinity perk list (perks unlocked by bonding with this species).
- Battle scar history from observed wild specimens.
- "Research Complete" banner and Institute Rank contribution logged.

### 3.6 Paleo Archive

Fossil creatures have their own Pokedex section. Rules are identical to the main Pokedex but:
- Silhouette is unlocked by discovering a fossil item (not by visual sighting).
- Full Profile requires successful resurrection at an Apex Lab (Station Level 5), not standard capture.
- Paleo Archive entries track `reconstructionQuality` (0–100%) based on genome completeness.
- Ancient parts unique to fossil creatures are revealed at Tier 2.

```csharp
[System.Serializable]
public class PaleoArchiveState
{
    public Dictionary<string, PaleoEntry> entries;
}

[System.Serializable]
public class PaleoEntry : PokedexEntry
{
    public float reconstructionQuality;   // 0.0–1.0; affects stat completeness
    public List<string> ancientPartIds;   // Revealed at Tier 2
}
```

### 3.7 Institute Rank Contribution

Each tier unlock contributes to Institute Rank scoring:

| Event | Rank Score |
|-------|-----------|
| Tier 0 unlock (new species seen) | +1 |
| Tier 1 unlock (fought) | +2 |
| Tier 2 unlock (captured) | +5 |
| Tier 3 unlock (research complete) | +10 |
| Paleo Archive Tier 2 (resurrected) | +15 |

Total Rank Score thresholds are defined in the Institute Rank System.

### 3.8 UI Layout

The Pokedex UI has three tabs:
1. **Main Index** — grid of all species, filterable by type, tier, zone, and name. Silhouettes shown as grey cards.
2. **Species Detail** — single-species view with all unlocked information; tier progress bar.
3. **Paleo Archive** — identical layout to Main Index but scoped to fossil species.

A "Research Progress" bar at the top of the screen shows total Tier 3 completions vs. total species count.

## 4. Formulas

### Research Complete Threshold

```
researchComplete = (entry.battleCount >= researchBattleThreshold)
researchBattleThreshold = 10    // Tunable
```

### Institute Rank Score from Pokedex

```
totalRankScore = (tier0Count * 1) + (tier1Count * 2) + (tier2Count * 5) + (tier3Count * 10)
              + (paleoTier2Count * 15)
```

Where each count is the number of species at exactly that tier or above (cumulative).

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Player captures a species they've never fought (e.g. trap set before battle) | Tier 1 and Tier 2 granted simultaneously in one unlock event |
| Player reaches battle count 10 before capturing | Tier 1 and Tier 3 unlocked; Tier 2 remains locked until capture occurs |
| Same species fought in multiple waves of a single Horde encounter | Battle count increments by 1 for that encounter, not by wave count |
| Species reappears after being wiped from ecosystem (over-capture consequence) | Pokedex entry is never removed; tier data is permanent |
| Fossil creature resurrected but dies in battle before capture | Tier 1 unlocks from the battle damage event; Tier 2 still requires resurrection (recapture) |
| Player loads an old save that predate a new species added in a patch | New species added with Tier -1 (Unseen); no corruption |
| Two creatures of same species in one battle | Battle count increments once per battle, not per creature |
| Pokedex entry for a species the player will never encounter (zone-locked, future content) | Entry exists in database but shows as Unseen; not counted toward rank completion % |
| `discoveredRecipes` list is empty at Tier 3 (species has no DNA recipes yet) | Tier 3 still unlocks; "No recipes discovered" message shown instead of empty list |

## 6. Dependencies

| System | Dependency Type | Notes |
|--------|----------------|-------|
| Creature Database | Read | Species data per tier: stats, moves, lore, DNA compatibility |
| Capture System | Write | Successful capture event triggers Tier 2 |
| Encounter System | Write | Battle damage events trigger Tier 1; battle end triggers battle count |
| Campaign Map | Write | Creature spotted on map triggers Tier 0 |
| DNA Alteration System | Read | Tier 3 reveals DNA recipes involving this species |
| Institute Rank System | Write | Tier unlocks contribute rank score |
| Body Part System | Read | Paleo Archive entries reference ancient part IDs |
| Save/Load System | Read/Write | Full `PokedexState` persisted in save file |
| Party Management UI | Read | Species detail view reads Pokedex entry |
| Combat UI | Read | Tier 1 data shown during combat (type badge on enemy health bar) |

## 7. Tuning Knobs

| Parameter | Default | Range | Notes |
|-----------|---------|-------|-------|
| `researchBattleThreshold` | 10 | 5–25 | Battles needed for Tier 3 |
| Rank score per Tier 0 | 1 | 0–5 | |
| Rank score per Tier 1 | 2 | 1–10 | |
| Rank score per Tier 2 | 5 | 2–20 | |
| Rank score per Tier 3 | 10 | 5–30 | |
| Rank score per Paleo Tier 2 | 15 | 5–40 | Rarer; worth more |
| `paleoResurrectionRequired` | true | bool | If false, capture counts for Paleo Tier 2 |
| Battle count per horde encounter | 1 | 1–3 | Set to 2–3 to speed up Tier 3 via hordes |

## 8. Acceptance Criteria

- [ ] Tier 0 unlocks when a species is spotted on the campaign map; only name and silhouette are visible.
- [ ] Tier 1 unlocks after any combat damage event involving the species; type badges and stat total appear.
- [ ] Tier 2 unlocks after successful capture; full stats, move pool, and DNA compatibility list appear.
- [ ] Tier 3 unlocks when `battleCount >= researchBattleThreshold`; all DNA recipes are revealed.
- [ ] Capturing a never-fought species grants Tier 1 and Tier 2 simultaneously in a single session.
- [ ] Battle count increments once per battle per species regardless of how many of that species were present.
- [ ] Horde encounters count as a single battle-count increment per species.
- [ ] Paleo Archive entries are separate from the main index and require resurrection for Tier 2.
- [ ] Institute Rank score contributions are correctly calculated from Pokedex tier counts (unit tested).
- [ ] Pokedex UI correctly filters by type, tier, zone, and name without errors.
- [ ] All `PokedexState` data persists correctly through a save/load cycle.
- [ ] Adding a new species to the database in a patch does not corrupt existing save data.
- [ ] Enemy type badge in Combat UI shows correct type after Tier 1 unlock for that species.
