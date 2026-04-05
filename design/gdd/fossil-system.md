# Fossil System

## 1. Overview

The Fossil System allows players to discover fossilized DNA at dig sites and cave walls, then resurrect extinct creatures at advanced research stations. Fossil creatures have incomplete genomes — randomized stat gaps (one or two stats permanently lowered) but access to unique ancient parts unavailable on living species. Fossil creatures form a separate Pokedex section ("Paleo Archive") and add replayability through species recovery discovery.

## 2. Player Fantasy

Finding a fossil feels like unearthing a primordial secret. Resurrecting an extinct creature is tangible achievement — you've brought something back that no longer exists naturally. The creature's stat gaps feel like a trade-off: it's from another age, imperfect but authentic. Equipping ancient parts gives it a distinct appearance and moveset unavailable to modern species.

## 3. Detailed Rules

### 3.1 Fossil Discovery

Fossils are found at:
- **Dig sites** on the campaign map (interactive nodes)
- **Cave walls** (random breakable walls in zones)
- **Rare drops** from excavation-themed encounters

Dig sites yield 1-3 fossil shards. Collecting 3 shards = 1 complete fossil.

```csharp
[System.Serializable]
public class FossilShard
{
    public string speciesId;
    public int shardCount;  // 0–3
    public bool IsComplete => shardCount >= 3;
}
```

### 3.2 Resurrection at Apex Lab

Fossil resurrection requires:
- Station Level 5 (Apex Lab)
- 3 fossil shards of same species
- 300 RP for extraction and vitalization

```csharp
public void ResurrectFossil(string speciesId)
{
    if (shardCount < 3) return;  // Incomplete
    if (stationLevel < 5) return;  // Locked
    if (playerRP < 300) return;    // Insufficient funds
    
    // Resurrect with stat gaps
    CreatureInstance fossil = CreateFossilCreature(speciesId);
    fossil.statGaps = GenerateRandomStatGaps();
    
    party.AddCreature(fossil);
    PokedexManager.DiscoverFossil(speciesId);
}
```

### 3.3 Fossil Stat Gaps

A fossil creature has 1-2 randomly selected stats reduced by 20-30%:

```csharp
private List<StatGap> GenerateRandomStatGaps()
{
    List<(string statName, float reduction)> gaps = new();
    int gapCount = Random.Range(1, 3);  // 1–2 gaps
    
    var stats = new[] { "HP", "ATK", "DEF", "SPD", "ACC" };
    var selected = stats.OrderBy(_ => Random.value).Take(gapCount);
    
    foreach (var stat in selected)
    {
        float reduction = Random.Range(0.2f, 0.3f);  // 20–30% lower
        gaps.Add((stat, reduction));
    }
    
    return gaps;
}
```

### 3.4 Ancient Parts

Fossil creatures can equip unique ancient parts unavailable on living species:

| Ancient Part | Category | Effect | Fossil Only |
|--------------|----------|--------|-------------|
| Stone Heart | Defensive | +DEF, +ACC, no type affinity | Yes |
| Primordial Spines | Offensive | +ATK, pierces armor, neutral damage | Yes |
| Void Veil | Utility | Evasion +15%, negates weather effects | Yes |
| Ancestral Echo | Aura | Summons fossil spirit ally for 1 turn | Yes |

Ancient parts have no primary type — they deal neutral damage and provide utility without type-specific bonuses.

### 3.5 Paleo Archive (Pokedex Section)

Discovered fossil species have their own Pokedex section distinct from living creatures:

```csharp
[System.Serializable]
public class FossilPokedexEntry
{
    public string fossilSpeciesId;
    public int dexNumber;  // Separate numbering from living creatures
    public string description;
    public List<StatGap> typicalStatGaps;  // Example gaps
    public List<string> ancientPartPool;   // Parts available to this fossil
    public bool isResurrected;             // Player has created one
}
```

## 4. Formulas

### Stat Reduction

```
reducedStat = baseStat * (1.0 - reduction)
reduction = Random(0.2, 0.3)  [20–30% lower]
```

### Resurrection Cost

```
cost = 300 RP fixed
shardsRequired = 3
```

### Fossil Rarity

```
commonFossils = species found in multiple zones
rareFossils = species found in 1 zone only
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Player finds 2 shards of Species A and 1 shard of Species B | Store separately; incomplete fossils remain as shards until 3 collected |
| Fossil is resurrected, then player finds another fossil of same species | Both are valid; player can have multiple fossil instances of same species |
| Fossil with stat gap (low HP) uses Regeneration perk | Perk heals based on max HP (lowered); effective healing is reduced |
| Ancient part is equipped then unequipped | Part remains in inventory; creature reverts to default parts |
| Fossil creature is used in battle and gains XP | XP increases level normally; stat gaps remain permanent |
| Player has incomplete fossil (2/3 shards) in inventory indefinitely | Shards persist in save; no expiration |
| Fossil is captured/reset at station | Fossil is no longer recoverable (no fossil restore mechanic) |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Creature Database | Reads species data for fossil creation |
| Station Upgrade System | Resurrection locked to Level 5 (Apex Lab) |
| Body Part System | Ancient parts equip like standard parts |
| Pokedex System | Fossil creatures in separate Paleo Archive section |
| Campaign Map | Dig site locations |
| Encounter System | Excavation encounters yield fossil shards |
| Save/Load System | Persists fossil shards and resurrected creatures |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `shardsPerFossil` | 3 | Shards needed to complete a fossil |
| `resurrectionCost` | 300 RP | Fixed cost at Apex Lab |
| `statGapLow` | 0.20 | 20% minimum reduction |
| `statGapHigh` | 0.30 | 30% maximum reduction |
| `statGapCount` | 1–2 | Number of stats affected |
| `requiredStationLevel` | 5 | Apex Lab required |
| `ancientPartsPerSpecies` | 2–4 | Available ancient parts per fossil species |

## 8. Acceptance Criteria

- [ ] Fossil shards are discoverable at dig sites and from encounters.
- [ ] Collecting 3 shards of same species enables resurrection.
- [ ] Resurrection requires Station Level 5 and 300 RP.
- [ ] Fossil creatures have 1-2 stats reduced by 20-30%.
- [ ] Stat gaps are permanent and calculated on resurrection.
- [ ] Fossil creatures appear in separate Pokedex section (Paleo Archive).
- [ ] Ancient parts are equippable only on fossil creatures.
- [ ] Ancient parts have no primary type and provide utility bonuses.
- [ ] Multiple fossil instances of same species can coexist.
- [ ] Fossil creature data persists through save/load.
- [ ] Resurrected fossils can battle and level like normal creatures.
