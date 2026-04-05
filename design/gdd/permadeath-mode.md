# Permadeath Mode

## 1. Overview

Permadeath Mode is an optional Nuzlocke-style difficulty mode where fainted creatures are permanently dead and removed from the party. Players can only attempt capture on the first creature encountered per area. A Memorial Wall at research stations displays all fallen creatures with full history (level, DNA mods, parts, scars, battles fought, cause of death). Posthumous DNA extraction allows one final genetic harvest from a fallen creature. An "In Memoriam" Pokedex section tracks all lost creatures across all permadeath runs. The mode is unlocked after completing the first zone in normal mode.

## 2. Player Fantasy

Permadeath creates genuine stakes. Every creature feels precious because death is final. Losing a favorite creature to a bad decision or unexpected move is emotionally impactful. The Memorial Wall becomes a tribute to fallen comrades. Extracting DNA from a dead creature feels like preserving their legacy. A permadeath run is a story worth telling.

## 3. Detailed Rules

### 3.1 Mode Activation

Permadeath is activated at the start of a new game, before the first encounter:

```
[ Start New Game ]
  [ Normal Mode ]
  [ Permadeath Mode ] (locked until zone 1 completed)
```

Can only be toggled at game start; cannot be changed mid-run.

### 3.2 Death Mechanics

When a creature faints in Permadeath Mode:

```csharp
public void OnCreatureFaint(CreatureInstance creature)
{
    if (gameMode != GameMode.Permadeath) return;
    
    // Creature is dead
    creature.isDead = true;
    
    // Record death data
    var deathRecord = new DeathRecord
    {
        creatureId = creature.id,
        speciesId = creature.speciesId,
        level = creature.level,
        causeOfDeath = GetLastDamageSource(),
        battleLocationId = currentBattleLocation,
        timestamp = System.DateTime.Now
    };
    
    gameState.memorialWall.Add(deathRecord);
    
    // Remove from party
    party.Remove(creature);
    
    // Offer posthumous DNA extraction
    ShowPosthumousDNAPrompt(creature);
}
```

### 3.3 Capture Restriction

Only the first creature encountered per area can be captured. Subsequent encounters cannot be caught:

```csharp
public bool CanCaptureCreature(EncounterData encounter)
{
    if (gameMode != GameMode.Permadeath) return true;
    
    string areaId = GetCurrentAreaId();
    
    if (firstEncountersPerArea.ContainsKey(areaId))
        return false;  // Already captured in this area
    
    // This is first encounter
    firstEncountersPerArea[areaId] = encounter.creatureSpeciesId;
    return true;
}
```

### 3.4 Memorial Wall

A permanent fixture at every research station displaying fallen creatures:

```csharp
[System.Serializable]
public class DeathRecord
{
    public string creatureId;
    public string speciesId;
    public int level;
    public List<string> modsApplied;      // DNA modifications equipped
    public List<string> partsEquipped;    // Body parts equipped
    public List<ScarData> scars;          // Battle scars earned
    public int battlesWon;
    public string causeOfDeath;           // "Defeated by Rival's Emberfox"
    public string battleLocationId;
    public System.DateTime deathTime;
}

public class MemorialWall
{
    public List<DeathRecord> allFallenCreatures = new();
    
    public void DisplayMemorial()
    {
        // Show full history per creature
        foreach (var record in allFallenCreatures)
        {
            Debug.Log($"{record.speciesId} (Lv {record.level}) - {record.causeOfDeath}");
            Debug.Log($"Mods: {string.Join(", ", record.modsApplied)}");
            Debug.Log($"Scars: {record.scars.Count}");
            Debug.Log($"Battles: {record.battlesWon}");
        }
    }
}
```

### 3.5 Posthumous DNA Extraction

When a creature dies, player can extract one final DNA material:

```csharp
public void ExtractPosthumousDNA(CreatureInstance deadCreature)
{
    // One guaranteed DNA material from the creature
    var dnaPool = GetDNAPoolForSpecies(deadCreature.speciesId);
    var extractedDNA = dnaPool[Random.Range(0, dnaPool.Count)];
    
    inventory.AddDNAMaterial(extractedDNA);
    
    Message($"Posthumously extracted {extractedDNA.displayName} from {deadCreature.displayName}.");
}
```

### 3.6 In Memoriam Pokedex Section

A separate Pokedex section for all dead creatures across all permadeath runs:

```csharp
public class InMemoriamEntry
{
    public string creatureId;
    public string speciesId;
    public int highestLevelReached;
    public int totalDeaths;             // If same species dies multiple times
    public System.DateTime firstDeathTime;
    public List<string> notableMods;    // DNA mods they carried
    public string epitaph;              // Optional player-written tribute
    
    public bool IsLegendary => highestLevelReached >= 35;
}
```

Entries are persistent across all permadeath runs; "In Memoriam" section grows over time.

## 4. Formulas

### Creature Value on Death

```
memorialValue = level + battleCount + modCount
legendary = level >= 35 OR battleCount >= 50
```

### Posthumous DNA Quality

```
extractedDNA rarity = baseRarity (no scaling)
cannot be Forbidden Mod (Apex Lab exclusive)
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Party fully fainted; game over | Return to last save (no resurrection) or reload |
| Last creature in party faints | Team wipe; memorial recorded for all |
| Creature dies to status effect at battle end | Status source recorded as cause |
| Player abandons permadeath run mid-zone | Run is saved; can continue later |
| Capture restriction: player encounters same species twice in one area | Second encounter cannot be captured |
| Posthumous DNA extraction: player declines | Creature is dead anyway; DNA offer is one-time |
| Memorial Wall is full (100+ entries) | Oldest entries scroll; newest shown first |
| In Memoriam entry duplicated (same species dies again) | Entry updated with new death time; deathCount increments |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Game State Manager | Tracks permadeath mode flag |
| Encounter System | Enforces first-capture-only rule |
| Party System | Removes fainted creatures |
| Creature Database | Species info for memorial |
| DNA Alteration System | Extracts DNA on death |
| Pokedex System | In Memoriam section |
| Research Station UI | Memorial Wall display |
| Save/Load System | Persists memorial wall and mode flag |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `unlockedAfterZoneCompletion` | Zone 1 | When permadeath mode becomes available |
| `capturesPerAreaLimit` | 1 | Only first encounter can be caught |
| `legendaryLevelThreshold` | 35 | Level for legendary fallen creature status |
| `memorialWallCapacity` | 100 | Max visible entries (oldest scroll off) |
| `posthumousDNAGuaranteed` | true | Always extract one material |
| `allowMidRunAbandon` | true | Can pause and return to run later |

## 8. Acceptance Criteria

- [ ] Permadeath mode is locked until zone 1 is completed in normal mode.
- [ ] Permadeath can be selected at game start before first encounter.
- [ ] Fainted creatures in permadeath are marked dead and removed from party.
- [ ] Death records are created with full creature history (level, mods, scars, battles, cause).
- [ ] Only the first creature per area can be captured in permadeath.
- [ ] Subsequent creatures in same area cannot be caught (capture button grayed out).
- [ ] Memorial Wall displays at research stations with full death history per creature.
- [ ] Posthumous DNA extraction offers one final material from dead creature.
- [ ] In Memoriam Pokedex section lists all fallen creatures across all runs.
- [ ] Fallen creature with level >= 35 marked as "legendary" in In Memoriam.
- [ ] Mode flag and memorial data persist through save/load.
- [ ] Game over/team wipe handled gracefully (no crash, memorial recorded).
