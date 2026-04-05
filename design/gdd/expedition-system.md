# Expedition System

## 1. Overview

The Expedition System allows players to send idle creatures (not in active party) on autonomous missions lasting 1-8 real-time hours. Creatures return with DNA materials, eggs, discovered creatures, map intel, or rare fossils. Success depends on creature level, type match to expedition zone, and affinity level. Players can send up to 3 expeditions simultaneously. An expedition log summarizes what creatures encountered and items collected. The system rewards players for maintaining diverse collections.

## 2. Player Fantasy

Sending your creature off on an adventure feels rewarding — they come back with trophies and stories. Checking back after several hours to find your creatures brought back rare DNA or an egg feels like a bonus surprise. Strategic team building includes who's best suited to expedition zones, rewarding Pokedex breadth over narrow optimization.

## 3. Detailed Rules

### 3.1 Expedition Types

| Expedition | Duration | Zone | Base Success % | Rewards |
|-----------|----------|------|----------------|---------|
| Grasslands Foray | 1 hour | Verdant Basin | 70% | DNA materials (grass), common creatures |
| Mountain Trek | 2 hours | Ember Peaks | 60% | Rock DNA, fire DNA, fossils (rare) |
| Frozen Expedition | 3 hours | Frozen Reach | 55% | Ice DNA, rare eggs, crystals |
| Storm Sweep | 2 hours | Storm Coast | 65% | Electric DNA, water DNA, intel |
| Shadow Delve | 4 hours | Shadow Depths | 50% | Dark DNA, poison DNA, legendary drops (rare) |
| Lost Lands | 6–8 hours | Random | 40% | Unique creatures, ancient parts, fossils |

### 3.2 ExpeditionConfig

```csharp
[CreateAssetMenu(menuName = "GeneForge/ExpeditionConfig")]
public class ExpeditionConfig : ScriptableObject
{
    [System.Serializable]
    public class ExpeditionType
    {
        public string expeditionId;
        public string displayName;
        public int durationMinutes;
        public float baseSuccessRate = 0.7f;
        public List<string> rewardCreatureIds;
        public List<string> rewardDNAIds;
        public bool canYieldEgg = false;
        public bool canYieldFossil = false;
        public bool canDiscoverRareCreature = false;
    }
    
    public List<ExpeditionType> expeditions;
}
```

### 3.3 Expedition Success Formula

```csharp
public float CalculateExpeditionSuccess(
    CreatureInstance creature,
    ExpeditionType expedition,
    float affinityBonus)
{
    float baseSuccess = expedition.baseSuccessRate;
    
    // Level bonus: creatures level 30+ get +20%
    float levelBonus = creature.level >= 30 ? 0.2f : (creature.level - 1) / 29f * 0.2f;
    
    // Type match bonus: +25% if creature type matches zone
    float typeBonus = IsTypeMatchToExpedition(creature, expedition) ? 0.25f : 0.0f;
    
    // Affinity bonus: +5% per affinity level
    float affinityBonus = creature.affinityLevelAverage * 0.05f;
    
    return Mathf.Clamp01(baseSuccess + levelBonus + typeBonus + affinityBonus);
}
```

### 3.4 Expedition Log

After expedition completes, log shows:

```
=== Mountain Trek Complete ===
Duration: 2 hours
Success: YES
Creatures Encountered:
  - Emberfox (x2)
  - Magma Turtle (x1)
  - Wild Cinder Dragon (x1) [NEW!]

Rewards:
  + Fire DNA Material (Uncommon) x3
  + Rock DNA Material (Common) x2
  + Fossil Shard: Ancient Dragon x1
  + Research Points: +150 RP
```

### 3.5 Concurrent Expeditions

```csharp
public class ExpeditionManager
{
    public int MaxConcurrentExpeditions = 3;
    
    public bool CanStartExpedition(CreatureInstance creature)
    {
        int activeCount = activeExpeditions.Count(e => !e.IsComplete);
        if (activeCount >= MaxConcurrentExpeditions)
            return false;
        
        return true;
    }
    
    public void CompleteExpedition(ExpeditionData data)
    {
        if (Random.value < data.successRate)
        {
            // Success: apply rewards
            GiveRewards(data);
        }
        else
        {
            // Failure: creature returns empty-handed
            Message($"{data.creature.displayName} returned from expedition empty-handed.");
        }
        
        data.creature.isOnExpedition = false;
    }
}
```

## 4. Formulas

### Base Success Rate

```
successRate = baseRate + levelBonus + typeBonus + affinityBonus
baseRate = 0.40–0.70 (per expedition type)
levelBonus = (creatureLevel / 50) * 0.20  [capped at 0.20]
typeBonus = 0.25 (if creature.type matches expedition zone)
affinityBonus = creature.avgAffinityLevel * 0.05
finalSuccess = Clamp(successRate, 0.0, 1.0)
```

### Reward Scaling

```
rewardQuality = baseReward * (1.0 + successRate * 0.5)
rareRewardChance = 0.10 + (creatureLevel / 50) * 0.15  [egg/fossil/legend]
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Creature is sent on expedition, then player swaps it to active party | Expedition completes; creature is retrieved and added to party (not lost) |
| Expedition succeeds but rewards inventory is full | Rewards are held in "pending claims" until player frees space |
| Player saves during ongoing expedition | Expedition continues in background; completes on load |
| Expedition fails; player doesn't check log | Log is available anytime in menu; doesn't disappear |
| Rare creature discovered on expedition; player has max party | Rare creature goes to storage (auto-storage if available) |
| Expedition duration is 8 hours; player only plays 4 hours | Expedition timer still counts real-time; will complete after actual 8 hours |
| Creature on expedition; battle saves and loads | Expedition persists; no interruption |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Creature Instance | Reads creature level, type, affinity |
| Party System | Creature must be in storage (not active party) |
| Creature Affinity System | Affinity level boosts success rate |
| Pokedex System | Rare creatures discovered from expeditions |
| Save/Load System | Persists active expeditions and timers |
| Campaign Map | Expedition zones linked to habitat zones |
| Encounter System | Expedition results add creatures/DNA to player inventory |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `maxConcurrentExpeditions` | 3 | Max simultaneous expeditions |
| `levelBonusPerLevel` | 0.0004 | Level contribution to success (capped at 0.20) |
| `typeMatchBonus` | 0.25 | +25% for matching type |
| `affinityBonusPerLevel` | 0.05 | +5% per affinity level |
| `successRateMin` | 0.40 | Lowest possible success rate |
| `successRateMax` | 1.0 | Highest possible success rate |
| `rareRewardBaseChance` | 0.10 | Base 10% chance for rare drops |
| `rareRewardLevelScaling` | 0.003 | 0.3% per creature level |

## 8. Acceptance Criteria

- [ ] Expeditions are accessible from menu and show available options.
- [ ] Creatures can only be sent if in storage (not active party).
- [ ] Up to 3 expeditions can run simultaneously.
- [ ] Success rate is calculated correctly (level + type + affinity bonuses).
- [ ] Expedition timer counts in real-time.
- [ ] Expedition completion triggers reward distribution.
- [ ] Expedition log displays creatures encountered and items collected.
- [ ] Rare creatures discovered on expedition are added to Pokedex.
- [ ] Fossils and eggs from expeditions appear in appropriate inventories.
- [ ] Full inventory prevents reward collection (held in pending).
- [ ] Expedition data persists through save/load.
- [ ] Failed expeditions return creature with no rewards (logged).
