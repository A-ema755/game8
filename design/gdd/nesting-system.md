# Nesting System

## 1. Overview

The Nesting System allows players to discover and hatch eggs at nesting sites found in the world. Eggs contain randomized innate DNA perks that are permanent and cannot be modified — making nest finds valuable even for species already in the player's collection. Eggs hatch after completing a configurable number of encounters. Nesting sites can trigger defend-the-nest encounters against predator creatures. A NestConfig ScriptableObject defines egg rarity and perk pools per species.

## 2. Player Fantasy

Finding a nesting site feels like discovering treasure. A rare egg with a unique innate perk (e.g., "Regeneration") is a prize worth protecting. Hatching an egg after 5-10 encounters feels like raising a creature from birth. An encounter that forces you to defend a nest against predators adds dramatic stakes. You might catch an ordinary specimen, but a wild egg version of the same species with a perfect perk feels special.

## 3. Detailed Rules

### 3.1 Nesting Sites

Nesting sites are fixed locations on the campaign map that can be revisited. Each site holds 1-3 eggs at a time. Eggs can be left behind (cached in world) or taken.

### 3.2 Egg Properties

```csharp
[System.Serializable]
public class EggData
{
    public string creatureSpeciesId;
    public int encounteredRequiredToHatch;  // e.g., 8
    public List<PerkConfig> innatePerks;   // 1–3 perks; permanent
    public DnaRarity rarity;                // Common, Uncommon, Rare
    public int encountersCompleted;
    
    public bool IsHatched => encountersCompleted >= encounteredRequiredToHatch;
}
```

### 3.3 Egg Rarity & Perk Quality

| Rarity | Encounters to Hatch | Perk Pool | Example Perks |
|--------|-------------------|-----------|-------------|
| Common | 4 | Basic (1 perk) | +5% HP, +1 ATK, Poison immunity |
| Uncommon | 6 | Standard (2 perks) | Regeneration, Thick Fat, Adaptability |
| Rare | 8 | Advanced (3 perks) | Breakthrough on critical hit, Perpetual mutation, Symbiosis |

Rarity is determined when the egg is laid; players can identify it by appearance.

### 3.4 Hatching Mechanics

When an egg is in the party:

```csharp
public void OnEncounterComplete()
{
    foreach (var egg in party.eggs)
    {
        egg.encountersCompleted++;
        if (egg.IsHatched)
        {
            // Transform egg to creature
            CreatureInstance hatched = new CreatureInstance(egg.creatureSpeciesId);
            hatched.innatePerks = egg.innatePerks;
            party.AddCreature(hatched);
            party.RemoveEgg(egg);
        }
    }
}
```

### 3.5 Defend-the-Nest Encounters

When approaching a nesting site with unclaimed eggs, a defend encounter may trigger (configurable probability per site):

- Wild predator creatures (configured per nest) attack the nest
- Player must defeat predators or they consume eggs
- Victory: earn bonus RP and eggs remain safe
- Defeat: lose 1-2 eggs permanently; no further penalty

## 4. Formulas

### Innate Perk Selection

```
rarity determines perk pool
eggPerks = randomSelect(rarityPool, perkCount)
perkCount = 1 (Common), 2 (Uncommon), 3 (Rare)
```

### Encounters to Hatch

```
encounteredRequiredToHatch = rarity switch:
  Common => 4,
  Uncommon => 6,
  Rare => 8
```

### Defend Encounter Success

```
if winCondition met (all predators defeated):
  eggsSaved = all eggs
  bonusRP = eggsCount * 50
else:
  eggsSaved = random(0, eggsCount - 1)
  eggsLost = eggsCount - eggsSaved
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Party is full (max creatures) and egg hatches | New creature enters party; if inventory also full, oldest egg is replaced |
| Player catches a creature of same species that they have an egg of | Both are valid; player now has duplicate species (one hatched, one caught) |
| Defend-the-nest encounter fails; 1 egg lost, player respawns at station | Lost eggs are permanently gone; remaining eggs stay in party or nest |
| Egg is in party during a trainer battle (no experience gain for eggs) | Egg still counts as "in party" for encounter counter; hatches normally |
| Player finds rare egg, immediately gets another rare egg from same nest | Both eggs are in party; can hatch separately |
| Egg has a perk that conflicts with another party member's perk | No conflict resolution; both perks are independent |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Encounter System | Increments egg hatch counter on completion |
| Party System | Manages eggs alongside creatures in party |
| Creature Database | Reads species info for egg hatching |
| Campaign Map | Nesting site locations and spawning |
| Save/Load System | Persists egg data (species, hatch progress, innate perks) |
| Pokedex System | Egg entries register when hatched creature is added |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `commonEncountersToHatch` | 4 | Common eggs hatch after 4 encounters |
| `uncommonEncountersToHatch` | 6 | Uncommon eggs hatch after 6 |
| `rareEncountersToHatch` | 8 | Rare eggs hatch after 8 |
| `commonPerkCount` | 1 | Common eggs get 1 innate perk |
| `uncommonPerkCount` | 2 | Uncommon eggs get 2 perks |
| `rarePerkCount` | 3 | Rare eggs get 3 perks |
| `defendNestTriggerChance` | 0.5 | 50% chance to trigger predator encounter at nest |
| `bonusRpPerEgg` | 50 | RP awarded for defending eggs |

## 8. Acceptance Criteria

- [ ] Nesting sites can be discovered on the campaign map.
- [ ] Eggs have rarity, hatching requirement, and innate perk list.
- [ ] Common eggs hatch after 4 encounters; Uncommon after 6; Rare after 8.
- [ ] Hatching transforms egg to creature and adds to party.
- [ ] Innate perks are permanent and cannot be removed.
- [ ] Defend-the-nest encounters can be triggered with configurable probability.
- [ ] Losing a defend-the-nest encounter removes 1-2 eggs permanently.
- [ ] Egg data (species, hatch progress, perks) persists through save/load.
- [ ] Multiple eggs in party hatch independently on schedule.
- [ ] Pokedex updates when egg hatches.
