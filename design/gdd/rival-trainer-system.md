# Rival Trainer System

## 1. Overview

The Rival Trainer System creates recurring enemies with names, personalities, and adaptive team composition. Rivals appear 3-4 times per zone and grow stronger as they observe the player's most-used creature types. Each rival has a RivalState that tracks player battle history, current difficulty tier, and personality quirks (boastful, cautious, scientific). Rivalry feels personal — rivals acknowledge their defeats, tease the player about using the same creatures repeatedly, and change tactics on rematches.

## 2. Player Fantasy

Your first rival feels like a real opponent, not a random trainer. They remember you beat them last time and come back angrier. A rival might taunt: "Still relying on that fire creature, huh?" if you've used fire types in 70% of battles. Learning a rival's team composition and countering it feels like outmaneuvering a real person. The story of your rivalry evolves through encounters.

## 3. Detailed Rules

### 3.1 RivalState

```csharp
[System.Serializable]
public class RivalState
{
    public string rivalId;
    public string displayName;
    public string personality;  // "Boastful", "Cautious", "Scientific", "Honorable"
    
    public int encounteredCount;
    public List<string> previousTeamIds;  // Team compositions used historically
    public List<CreatureType> playerObservedTypes;  // Frequency tracking
    
    public int difficultyTier = 1;
    public int avgCreatureLevel;
    
    public event System.Action OnDefeated;
    public event System.Action OnRematched;
}
```

### 3.2 Rival Encounters

Rivals appear 3-4 times per zone:

| Encounter | Trigger | Difficulty Tier | Incentive |
|-----------|---------|-----------------|-----------|
| First encounter | Zone entry | 1 (base) | Introduction, story buildup |
| Mid-zone | 50% zone progress | 2 (medium) | Heated rematch |
| Late-zone | 80% zone progress | 3 (hard) | High-stakes final battle |
| Optional | Boss area | 4 (extreme) | Championship match |

### 3.3 Adaptive Team Building

```csharp
public void BuildAdaptiveTeam(RivalState rival, List<CreatureType> playerTypes)
{
    // Count player type usage
    Dictionary<CreatureType, int> typeFreq = new Dictionary<CreatureType, int>();
    foreach (var type in playerTypes)
    {
        typeFreq[type] = typeFreq.TryGetValue(type, out int count) ? count + 1 : 1;
    }
    
    // Find most-used type
    CreatureType playerFavorite = typeFreq.OrderByDescending(x => x.Value).First().Key;
    
    // Build counter-team
    List<CreatureType> counterTypes = GetCounterTypes(playerFavorite);
    
    // Build team with difficulty scaling
    team = BuildTeamFromTypes(counterTypes, rival.difficultyTier, rival.avgCreatureLevel);
}
```

### 3.4 Rival Dialogue & Personality

Personalities affect battle dialogue and team choices:

- **Boastful:** Picks powerful creatures, taunts before/after battle, compliments powerful moves
- **Cautious:** Prefers type-advantage matchups, compliments opponent's strategy on loss
- **Scientific:** Uses unexpected type combinations, asks questions about your team
- **Honorable:** Respects the player's progress, offers fair fights, mentions learning from defeats

### 3.5 Rival Progression

After each encounter, RivalState updates:

```csharp
public void OnBattleResolved(bool playerWon, List<CreatureInstance> playerTeam)
{
    encounteredCount++;
    
    if (playerWon)
    {
        // Rival learns and upgrades
        difficultyTier = Min(difficultyTier + 1, 4);
        avgCreatureLevel += 2;
    }
    else
    {
        // Player loses; rival is satisfied for now
        difficultyTier = Min(difficultyTier + 1, 3);
    }
    
    // Record player types for future adaptation
    foreach (var creature in playerTeam)
        playerObservedTypes.Add(creature.primaryType);
}
```

## 4. Formulas

### Team Power Level

```
powerLevel = avgCreatureLevel * (1.0 + difficultyTier * 0.2)
teamSize = 4 + (difficultyTier - 1)  [tier 1 = 4, tier 4 = 7]
```

### Counter Type Probability

```
counterTypeChance = (playerTypeFrequency / totalPlayerBattles) * weightFactor
mostCounteredType = argMax(counterTypeChance for all types)
```

### Personality Dialogue Selection

```
dialogueTree = PickDialogueTree(personality, battleOutcome, encounter#)
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Player uses diverse team (no dominant type) | Rival builds balanced team with mixed types |
| Rival encountered at different levels in different playthroughs | Rival level scales to current zone average (configurable per zone) |
| Player beats rival 3 times; they don't reappear | Rival is marked "Defeated" in this zone; no further encounters (post-zone story) |
| Rival team has Pokemon that player hasn't seen yet | Pokedex discovers new species from trainer battle (standard) |
| Difficulty tier maxes at 4; player keeps winning | Avg level continues to increase instead |
| Rival dialogue is personality-based but player defeats them first encounter | Dialogue shown post-battle includes humble acknowledgment of defeat |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Encounter System | Triggers rival encounters at zone progress points |
| Creature Database | Reads creature stats and types for team building |
| Type Chart System | Determines counter types |
| AI Decision System | Uses rival personality to drive move choices |
| Pokedex System | Discovers species from rival teams |
| Campaign Map | Defines rival spawn locations per zone |
| Save/Load System | Persists RivalState (encounteredCount, difficultyTier, type frequencies) |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `rivalEncountersPerZone` | 3 | Number of rival battles per zone |
| `difficultyTierMax` | 4 | Maximum rival difficulty |
| `difficultyTierOnWin` | +1 | Increment on player victory |
| `difficultyTierOnLoss` | +1 | Increment on player loss |
| `rivalLevelScaling` | +2 | Level increase per rematch |
| `counterTypeWeighting` | 1.5 | How aggressively rival counters dominant type |
| `personalityDialogueVariance` | High | How much personality affects dialogue |

## 8. Acceptance Criteria

- [ ] Rivals appear 3-4 times per zone at designated progress points.
- [ ] Rival team composition adapts based on player's most-used creature types.
- [ ] Difficulty tier increases after each encounter (winning or losing).
- [ ] Rival team size scales with difficulty tier.
- [ ] Personality affects dialogue and team-building strategy.
- [ ] Rival state (encounters, type frequencies, difficulty) persists through save/load.
- [ ] Rivals reference previous encounters in dialogue (e.g., "Last time you beat me...").
- [ ] Counter-type selection is weighted toward player's dominant types.
- [ ] Rivals unlock new Pokedex entries when using unseen species.
- [ ] Optional extreme-difficulty final encounter available per zone.
