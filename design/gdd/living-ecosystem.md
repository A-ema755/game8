# Living Ecosystem

## 1. Overview

The living ecosystem system simulates a dynamic predator-prey ecology on the campaign map. Each habitat zone tracks population counts per species, and those populations shift based on player capture behavior, natural predation cycles, and migration events. Over-capturing prey species causes predators to expand their territory and rare species to vanish. Maintaining a healthy balance rewards players with rare spawns and Research Point grants. The entire ecosystem state serializes into the save file and persists across sessions.

## 2. Player Fantasy

The player feels like a genuine field researcher whose presence has real consequences. Aggressively farming a single species eventually backfires — the forest feels emptier, predators get aggressive, and a previously abundant egg source dries up. Conversely, a careful researcher who diversifies captures and monitors population bars discovers rare creatures that only emerge when the ecosystem is thriving. The ecosystem reacts to you; it's not just a backdrop.

## 3. Detailed Rules

### EcosystemState Data Model
```csharp
[System.Serializable]
public class SpeciesPopulation
{
    public string speciesId;
    public int currentPopulation;   // 0–100 normalized units
    public int baselinePopulation;  // target healthy level per zone config
    public EcologyRole role;        // Prey, Predator, Apex, Neutral
}

[System.Serializable]
public class EcosystemState
{
    public string zoneId;
    public List<SpeciesPopulation> populations;
    public int conservationScore;       // 0–100
    public int cycleIndex;              // increments per encounter completed
    public bool rareSpawnActive;
    public bool researchGrantActive;
}
```

### Population Units
- Population is normalized to 0–100 per species per zone. 50 = healthy baseline.
- Populations are not real creature counts; they represent relative ecological density.

### Population Change Events

| Event | Population Delta |
|-------|-----------------|
| Player captures 1 creature of species X | -5 to species X |
| Player defeats (no capture) species X | -2 to species X |
| Species X predates species Y (cycle event) | -3 to species Y, +2 to species X |
| Migration cycle fires | ±5 to migrating species (see Migration) |
| Player releases a stored creature of species X | +3 to species X |
| Encounter node completed without targeting species X | +1 to species X (passive recovery) |

### Predation Cycle
- Every `predationCycleInterval` encounters completed (default: 3), the system runs one predation pass.
- For each Predator species: find all Prey species it hunts (defined in `EcologyConfig.preyList`).
- If the Predator population > `predationThreshold` (default: 60) AND any Prey population > 20, apply predation delta.
- If a Prey species drops to 0, it is locally extinct in that zone: its encounter slot is replaced by its Predator species in wild encounter pools.

```csharp
public void RunPredationCycle(EcosystemState state, EcologyConfig config)
{
    foreach (var predator in config.predators)
    {
        var predPop = GetPopulation(state, predator.speciesId);
        if (predPop.currentPopulation < config.predationThreshold) continue;

        foreach (var preyId in predator.preyList)
        {
            var preyPop = GetPopulation(state, preyId);
            if (preyPop.currentPopulation <= 0) continue;

            preyPop.currentPopulation = Mathf.Max(0, preyPop.currentPopulation - config.predationDelta);
            predPop.currentPopulation = Mathf.Min(100, predPop.currentPopulation + config.predationGain);
        }
    }
}
```

### Migration
- Each zone has 1–3 migration routes defined in `EcologyConfig.migrationRoutes`.
- A route has: speciesId, sourceZoneId, destinationZoneId, cycleInterval, delta.
- Every `cycleInterval` encounters, the species population in sourceZone decreases by delta and destinationZone increases by delta.
- Migration routes are one-way. Round-trip migration uses two opposing routes.

### Wild Encounter Pool Modification
- Wild encounter nodes sample their creature pool from `EncounterConfig.speciesPool`.
- At runtime, the pool is filtered: species with population = 0 are removed. Species with population > 70 get a weight bonus of +30%.
- Predators whose prey has gone locally extinct gain a +20% appearance weight in that zone.

### Conservation Score
```
conservationScore = (sum of all species currentPopulation / sum of all species baselinePopulation) * 100
conservationScore = Clamp(conservationScore, 0, 100)
```

### Conservation Thresholds

| Score | Effect |
|-------|--------|
| 80–100 | Rare spawn active: 1 rare species enters wild encounter pool. Research grant: +50 RP per session. |
| 50–79 | Healthy. No special effects. |
| 20–49 | Degraded. Rare species absent. Some encounter nodes show "scarce" warning. |
| 0–19 | Collapsed. Predator encounters become aggressive (higher threat). 2 encounter nodes replaced with Predator Surge events. |

### Rare Spawn Logic
- When conservation score crosses 80, `rareSpawnActive = true`.
- The zone's `EcologyConfig.rareSpeciesId` is added to wild encounter pools with weight 10%.
- If conservation drops below 70, `rareSpawnActive = false` and the rare species is removed.
- Hysteresis of 10 points prevents rapid toggling.

### Research Grant
- When `researchGrantActive = true`, each Research Station visit awards `researchGrantBonus` RP (default 50).
- Flag clears each session start and re-evaluates on first station visit.

### EcologyConfig ScriptableObject
```csharp
[CreateAssetMenu(menuName = "GeneForge/EcologyConfig")]
public class EcologyConfig : ScriptableObject
{
    public string zoneId;
    public List<PredatorEntry> predators;
    public List<MigrationRoute> migrationRoutes;
    public string rareSpeciesId;
    public int predationCycleInterval;   // encounters between predation passes
    public int predationThreshold;       // predator pop must exceed this to hunt
    public int predationDelta;           // prey pop reduction per pass
    public int predationGain;            // predator pop gain per pass
    public int researchGrantBonus;       // RP per station visit when active
}

[System.Serializable]
public class PredatorEntry
{
    public string speciesId;
    public List<string> preyList;
}

[System.Serializable]
public class MigrationRoute
{
    public string speciesId;
    public string sourceZoneId;
    public string destinationZoneId;
    public int cycleInterval;
    public int delta;
}
```

## 4. Formulas

**Conservation Score:**
```
conservationScore = Clamp(
    (Σ population[i] / Σ baseline[i]) * 100,
    0, 100
)
```

**Species Recovery Rate (per encounter where species not targeted):**
```
recovery = passiveRecoveryRate   // default: +1 per encounter
```

**Encounter Pool Weight:**
```
baseWeight = speciesEncounterWeight (from EncounterConfig)
if population > 70: weight *= 1.3
if population == 0: weight = 0 (excluded)
if predator and all prey extinct: weight *= 1.2
finalWeight = baseWeight (normalized across pool)
```

**Capture Impact:**
```
captureImpact = -capturePopulationCost   // default: -5
defeatImpact = -defeatPopulationCost     // default: -2
releaseGain = +releasePopulationGain     // default: +3
```

## 5. Edge Cases

- **Species at 0 population:** Cannot be captured (no encounters generated). Remains in Pokedex as "locally extinct" with a distinct icon. Can return if player releases captured specimens (population recovers to 10 minimum after 1 release).
- **All prey species extinct:** Predator population decays at `apexDecayRate` (default: -2/cycle) since no food source. This prevents permanent predator dominance.
- **Player releases creature of species that never existed in this zone:** Population initializes at 5 for that species. It can persist but won't appear in encounter pools unless population reaches 20+.
- **Conservation score stuck at 0:** At least one "recovery event" fires every 10 encounters if all species are at 0 — adds +10 to each species (ecosystem rebounds slowly without player intervention).
- **Migration overflows cap:** Population is clamped at 100. Surplus migration is lost. No overflow to adjacent zones.
- **Zone not yet visited:** Ecosystem state initializes from `EcologyConfig.baselinePopulation` values when the zone is first unlocked.
- **Permadeath mode:** Creature deaths (party members) do NOT affect ecosystem population. Only captured wild creatures and defeated wild encounters count.

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Save/Load System | Serializes EcosystemState per zone into save JSON |
| Campaign Map | Triggers ecosystem cycle update after each node completion |
| Encounter System | Reads ecosystem-modified encounter pools for wild nodes |
| Party System | Notifies ecosystem when creatures are captured or released |
| Research Station | Reads researchGrantActive flag to award bonus RP |
| Pokedex System | Reads population = 0 to display "locally extinct" status |
| Institute Rank | Conservation score > 50 contributes to Senior Researcher rank requirement |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `capturePopulationCost` | 5 | Population reduction per capture |
| `defeatPopulationCost` | 2 | Population reduction per defeat (no capture) |
| `releasePopulationGain` | 3 | Population gain per release |
| `passiveRecoveryRate` | 1 | Population gain per encounter (species not targeted) |
| `predationCycleInterval` | 3 | Encounters between predation passes |
| `predationThreshold` | 60 | Predator population required to hunt |
| `predationDelta` | 3 | Prey population reduction per predation pass |
| `predationGain` | 2 | Predator population gain per predation pass |
| `apexDecayRate` | 2 | Population decay per cycle when no prey exists |
| `rareSpawnThreshold` | 80 | Conservation score to activate rare spawns |
| `rareSpawnHysteresis` | 10 | Score buffer before deactivating rare spawns |
| `rareSpeciesPoolWeight` | 10% | Encounter weight when rare spawn active |
| `researchGrantBonus` | 50 RP | Per station visit when conservation high |
| `collapseThreshold` | 20 | Conservation score below which ecosystem collapses |
| `extinctionRecoveryInterval` | 10 | Encounters before forced recovery event at score 0 |

## 8. Acceptance Criteria

- [ ] Capturing 5 consecutive creatures of the same species visibly reduces that species' encounter frequency in subsequent wild nodes.
- [ ] If a prey species population reaches 0, its predator replaces it in wild encounter pools within 1 predation cycle.
- [ ] Conservation score updates after every encounter node completion and persists in save.
- [ ] When conservation score >= 80, a rare species appears in wild encounter pools at ~10% weight.
- [ ] Research Station awards bonus RP when researchGrantActive is true; no bonus when false.
- [ ] Releasing a captured creature of a locally extinct species (population = 0) restores its population to at least 10.
- [ ] EcosystemState survives save/load cycle with no data loss.
- [ ] Predator population does not grow indefinitely when all prey are extinct (decay rate applies).
- [ ] Collapsed ecosystem (score < 20) shows Predator Surge encounter nodes on the campaign map.
- [ ] Zone ecosystem initializes correctly from EcologyConfig baseline values on first zone unlock.
