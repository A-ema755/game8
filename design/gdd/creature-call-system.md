# Creature Call System

## 1. Overview

The Creature Call System allows players to learn and use creature vocalizations from the Pokedex to interact with the world. Calls are unlocked when a creature reaches "Full Profile" research tier and can lure specific species, scare away unwanted encounters, or trigger environmental reactions. Calling wrong creatures in wrong zones may backfire, attracting aggressive swarms. Calls are a non-combat utility system that rewards Pokedex completion and creature knowledge.

## 2. Player Fantasy

Learning to mimic your favorite creature's vocalization feels like genuine connection. Using a water creature's call to raise a water level and unlock a passage feels like the creature is actively helping outside combat. A dragon's call clears rockslides like it's genuinely powerful. Calling wrong creatures and accidentally attracting predators creates memorable mishaps and teaches zone knowledge.

## 3. Detailed Rules

### 3.1 Call Types

| Call Type | Effect | Learning Condition | Usage |
|-----------|--------|-------------------|-------|
| Lure | Attract specific species to current location | Full Profile + 10+ encounters | Campaign map; draw rare creatures |
| Scare | Repel specific predator species | Full Profile + capture once | Campaign map; avoid encounters |
| Trigger | Cause environmental reaction | Full Profile + observation | Campaign map; solve environmental puzzles |

### 3.2 CallConfig

```csharp
[CreateAssetMenu(menuName = "GeneForge/CallConfig")]
public class CreatureCallConfig : ScriptableObject
{
    [System.Serializable]
    public class Call
    {
        public string callId;
        public string creatureSourceId;
        public string displayName;     // "Dragon's Roar", "Water's Whisper"
        public CallType callType;       // Lure, Scare, Trigger
        
        public List<string> targetCreatureIds;  // Species lured/scared
        public float effectDuration = 5.0f;
        public float effectRadius = 20.0f;      // Distance in world units
        
        public EnvironmentalEffectType triggerEffect;  // For Trigger calls
        public AudioClip callSound;
    }
    
    public List<Call> calls;
}

public enum CallType { Lure, Scare, Trigger }
public enum EnvironmentalEffectType { ClearRocks, RaiseWater, Thaw, etc. }
```

### 3.3 Learning Calls

```csharp
public void UnlockCall(string creatureSpeciesId)
{
    var pokedexEntry = PokedexManager.Get(creatureSpeciesId);
    if (pokedexEntry.researchTier < ResearchTier.FullProfile)
        return;  // Not yet discovered
    
    var callConfig = CallDatabase.GetCallForSpecies(creatureSpeciesId);
    if (callConfig != null)
        playerInventory.unlockedCalls.Add(callConfig.callId);
}
```

### 3.4 Using Calls

On the campaign map, player can select an unlocked call:

```csharp
public void UseCall(string callId)
{
    var call = CallDatabase.Get(callId);
    
    // Play vocalization
    AudioManager.PlayCreatureVocalization(call.callSound);
    VFXPoolManager.PlayEffect("call-aura", playerPos);
    
    // Apply effect
    switch (call.callType)
    {
        case CallType.Lure:
            SpawnTargetCreatures(call.targetCreatureIds, call.effectRadius);
            break;
        case CallType.Scare:
            RepelCreatures(call.targetCreatureIds, call.effectRadius);
            break;
        case CallType.Trigger:
            ApplyEnvironmentalEffect(call.triggerEffect, playerPos);
            break;
    }
}
```

### 3.5 Backfire: Wrong Call

Using wrong call in wrong zone may trigger:
- Lure call for rare creature attracts common predators instead
- Scare call for predator attracts aggressive swarm (4-6 creatures)
- Trigger call for environmental effect in incompatible zone (no effect, wasted)

## 4. Formulas

### Call Learning Requirement

```
canLearnCall = pokedexResearchTier == FullProfile
encountersRequirement = 10 (for Lure/Scare calls)
```

### Call Effect Radius

```
effectRadius = 20.0 units (adjustable per call)
spawnCount = 1-3 creatures (lure) or 4-6 (swarm backfire)
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Player learns two calls from same species | Both calls are available (unlikely, but possible if designed) |
| Lure call succeeds but no creatures of that species exist in zone | No creatures spawn; call is wasted but no penalty |
| Scare call successfully repels predators; player continues into area | Creatures stay repelled for duration (5 seconds) then may respawn |
| Trigger call used in incompatible zone (e.g., water creature's call in desert) | No effect; call is consumed; message says "Nothing happened" |
| Player uses call but no creatures in PokedEx yet | Calls are locked; Pokedex requirement prevents this |
| Call is used in trainer battle (only available on map) | Calls are disabled in combat UI; only available on campaign map |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Pokedex System | Calls unlocked at Full Profile tier |
| Creature Database | Source species for each call |
| Audio System | Plays vocalization audio |
| Campaign Map | Call usage context and locations |
| Encounter System | Lure/Scare calls spawn or prevent encounters |
| Environmental Puzzle System | Trigger calls solve environmental interactions |
| Save/Load System | Persists unlocked calls list |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `callEffectDuration` | 5.0s | How long call effect lasts |
| `callEffectRadius` | 20.0 | Distance for call to affect area |
| `lureSpawnCount` | 1–3 | Creatures spawned by lure call |
| `scareSwarmCount` | 4–6 | Backfire swarm size on wrong scare |
| `encountersToLearnCall` | 10 | Minimum encounters before call unlocks |
| `callCooldown` | 3.0s | Time before same call can be used again |
| `backfireChance` | 0.3 | 30% chance wrong call triggers backfire |

## 8. Acceptance Criteria

- [ ] Calls are unlocked when creature reaches Full Profile tier and 10+ encounters.
- [ ] Lure call spawns 1-3 creatures of target species near player.
- [ ] Scare call repels specified predator types for duration.
- [ ] Trigger call causes environmental effect (clear rocks, raise water, etc.).
- [ ] Wrong call in incompatible zone triggers backfire (swarm or no effect).
- [ ] Calls are only usable on campaign map, not in combat.
- [ ] Call effects display appropriate VFX and audio.
- [ ] Unlocked calls list persists through save/load.
- [ ] Multiple calls per species supported (if designed).
- [ ] Call cooldown prevents rapid-fire usage.
