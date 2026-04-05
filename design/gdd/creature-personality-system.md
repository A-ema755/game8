# Creature Personality System

## 1. Overview

The Creature Personality System assigns one behavioral trait per creature that affects combat and out-of-combat behavior. Traits (Aggressive, Cautious, Loyal, Feral, Curious, Territorial) are DNA-derived and can be swapped at research stations. Each trait provides mechanical bonuses/penalties and visual indicators. Personality affects AI decision-making in trainer battles and influences Pokedex lore entries.

## 2. Player Fantasy

Your creatures feel like individuals, not generic team members. An Aggressive creature attacks first in ties and hits harder. A Loyal creature becomes more reliable and loses instability slower. Personalizing team composition with personality traits adds another layer of build crafting beyond type and moves.

## 3. Detailed Rules

### 3.1 Personality Traits

| Trait | Effect | Visual Indicator | Mechanics |
|-------|--------|------------------|-----------|
| Aggressive | +10% damage, attacks first in speed tie | Red-tinted eyes, battle-ready stance | Higher priority in action order |
| Cautious | +10% evasion, auto-retreat at <25% HP | Lowered posture, defensive stance | May prioritize switching out |
| Loyal | -5% instability gain, combos cost 20% less | Warm aura, close bond visualization | Reduced DNA instability grant |
| Feral | +20% damage, +instability gain (risk/reward) | Glitch particles, aggressive posture | Unpredictable behavior possible |
| Curious | +20% XP gain, reveals Pokedex info faster | Inquisitive head tilt, scanning animation | Faster Pokedex research |
| Territorial | +Defense when holding position 2+ turns | Expanded size, dominant posture | Defensive bonus on sustained position |

### 3.2 PersonalityConfig

```csharp
[CreateAssetMenu(menuName = "GeneForge/PersonalityConfig")]
public class CreaturePersonality : ScriptableObject
{
    public string personalityId;
    public string displayName;
    public string description;
    
    public float damageMultiplier = 1.0f;
    public float evasionMultiplier = 1.0f;
    public float xpGainMultiplier = 1.0f;
    public float instabilityModifier = 0.0f;      // -0.05 for Loyal, +0.20 for Feral
    public float comboMoveCostReduction = 0.0f;   // 0.20 for Loyal
    
    public string visualIndicator;   // Sprite/particle effect reference
    public AudioClip personalitySFX;
    public bool actFirstInTies;      // Aggressive
    public bool autoRetreatsAtLowHP; // Cautious
}
```

### 3.3 Personality Mechanics

**Aggressive:**
```csharp
if (personality == Aggressive && speedTie)
{
    actFirst = true;  // Acts before opponent in same-speed tie
}
damageMultiplier *= 1.10f;
```

**Cautious:**
```csharp
if (currentHpPercent < 0.25f && personality == Cautious)
{
    preferSwitch = true;  // AI prioritizes switching out
}
evasionMultiplier *= 1.10f;
```

**Loyal:**
```csharp
modInstability = Mathf.RoundToInt(modInstability * 0.95f);  // -5% instability
comboMoveCost *= 0.80f;  // Combos cost 20% less
```

**Feral:**
```csharp
damageMultiplier *= 1.20f;
modInstability = Mathf.RoundToInt(modInstability * 1.20f);  // +20% instability
// Possible: random disobedience at very high instability
```

**Curious:**
```csharp
xpGainMultiplier *= 1.20f;
pokedexResearchSpeed *= 1.20f;  // Reveals info 20% faster
```

**Territorial:**
```csharp
int turnsHeld = turnsSinceLastMove;
if (turnsHeld >= 2 && personality == Territorial)
{
    defenseMultiplier *= 1.15f;  // +15% DEF
}
```

### 3.4 Changing Personality

Personalities can be changed at research stations (Station Level 4+, Mutation Lab):

```csharp
public bool TryChangePersonality(CreatureInstance creature, string newPersonalityId)
{
    if (stationLevel < 4) return false;  // Requires Mutation Lab
    
    creature.personality = newPersonalityId;
    // Cost: 100 RP + 1 turn at station
    
    return true;
}
```

## 4. Formulas

### Personality Damage Modifier

```
damageMultiplier = personalityConfig.damageMultiplier
finalDamage = baseDamage * damageMultiplier
```

### Instability Modification

```
personalityInstabilityMod = personalityConfig.instabilityModifier
finalInstability = modInstability + personalityInstabilityMod
Loyal: -0.05 (5% reduction)
Feral: +0.20 (20% increase)
Others: 0.0
```

### XP Gain with Curious

```
curioXPBonus = 1.20 (20% increase)
finalXP = baseXP * curioXPBonus
```

### Territorial Defense Bonus

```
turnsHeld = currentTurn - lastMoveExecutedTurn
defensBonus = (turnsHeld >= 2) ? 1.15 : 1.0  [+15% DEF]
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Aggressive personality in double battle; both creatures same speed | Aggressive acts first among ties (priority order) |
| Cautious creature at 25% HP; auto-retreat triggers but no switch available | Creature stays in battle; can still attack or use other moves |
| Feral personality + high instability causing extra risk | Possible random disobedience; damage boost is guaranteed |
| Loyal creature with combo move; cost reduction applies | Combo still uses both creatures' turns but reduced RP/resource cost |
| Territorial creature loses position mid-battle; defense bonus resets | Bonus recalculates next time creature is in sustained position 2+ turns |
| Personality changed mid-session (at station); immediate effect | Personality applies to next battle; current battle unaffected |
| Curious personality gains XP at 1.20x; level-up threshold unchanged | Creature levels up normally; XP bar progresses faster visually |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Creature Instance | Stores assigned personality |
| DNA Alteration System | Personality is DNA-derived trait |
| Station Upgrade System | Personality change locked to Level 4 (Mutation Lab) |
| Turn Manager | Acts first in ties (Aggressive), auto-retreat logic (Cautious) |
| Damage & Health System | Damage multipliers applied |
| Leveling / XP System | XP gain multipliers (Curious) |
| AI Decision System | Personality affects trainer battle AI behavior |
| Pokedex System | Personality shown in creature lore |
| Save/Load System | Persists creature personality choice |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `aggressiveDamageBonus` | 1.10 | +10% damage |
| `cautiousEvasionBonus` | 1.10 | +10% evasion |
| `loyalInstabilityReduction` | 0.95 | -5% instability |
| `loyalComboCostReduction` | 0.80 | -20% combo cost |
| `feralDamageBonus` | 1.20 | +20% damage |
| `feralInstabilityPenalty` | 1.20 | +20% instability |
| `curiousXPBonus` | 1.20 | +20% XP gain |
| `territorialDefenseBonus` | 1.15 | +15% DEF when holding 2+ turns |
| `cautiosAutoRetreathreshold` | 0.25 | 25% HP triggers retreat |
| `personalityChangeCost` | 100 RP | Cost at Mutation Lab |
| `minStationLevelForChange` | 4 | Mutation Lab required |

## 8. Acceptance Criteria

- [ ] Creatures can be assigned one of six personality traits.
- [ ] Aggressive creatures gain +10% damage and act first in speed ties.
- [ ] Cautious creatures gain +10% evasion and prioritize retreat at <25% HP.
- [ ] Loyal creatures reduce DNA instability by 5%.
- [ ] Feral creatures gain +20% damage but increase instability by 20%.
- [ ] Curious creatures gain +20% XP and faster Pokedex research.
- [ ] Territorial creatures gain +15% DEF after holding position 2+ turns.
- [ ] Personality can be changed at Station Level 4 for 100 RP.
- [ ] Personality traits persist through save/load.
- [ ] Visual indicators (eyes, stance, particles) display correctly per personality.
- [ ] AI trainer battles respect personality behavior (Cautious retreat, Aggressive early attack).
- [ ] Personality shown in Pokedex lore entries.
