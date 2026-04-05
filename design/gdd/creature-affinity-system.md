# Creature Affinity System

## 1. Overview

Creature Affinity tracks bond levels (0–5) between creatures that battle together. Higher affinity unlocks combo moves, passive affinity perks, lore entries, and reduced DNA instability. Affinity is stored per creature pair in an AffinityState ScriptableObject and increments by 1 point per battle where both creatures actively participated (dealt or took damage). The system rewards loyal team composition and encourages replaying with favorite creatures.

## 2. Player Fantasy

Your core team feels special because you've fought 50+ battles with them together. A pair of creatures at Affinity 5 can execute devastating combo moves that feel earned. Reading lore entries about how your creatures grew bonded creates emotional investment. The system subtly encourages "main team" playstyles without punishing experimentation.

## 3. Detailed Rules

### 3.1 Affinity Levels

| Level | Requirement | Unlocks |
|-------|-------------|---------|
| 0 | Never battled together | Baseline |
| 1 | Battled 1+ times together | Lore entry tier 1 |
| 2 | Battled 5+ times together | Affinity perk slot 1 |
| 3 | Battled 10+ times together | Combo move unlock begins, reduced instability -5% |
| 4 | Battled 20+ times together | Lore entry tier 2, reduced instability -10% |
| 5 | Battled 30+ times together | Full combo move power, lore complete, reduced instability -15% |

### 3.2 AffinityState

```csharp
[System.Serializable]
public class AffinityState
{
    public string creature1Id;
    public string creature2Id;
    public int affinityLevel;  // 0–5
    public int battlesSharedCount;
    
    public bool CanUseComboMove => affinityLevel >= 3;
    public float InstabilityReduction => affinityLevel * 0.05f;  // 0, 5%, 10%, 15%, 20%, 25%
}
```

### 3.3 Affinity Perk Slots

At Affinity 2+, each creature in the pair can equip one of two preset affinity perks:

- **Loyal Bond:** -5% damage taken when pair member is alive
- **Synchronized Strike:** +10% damage when both are in battle same turn

Players choose which perk (if any) to equip per creature; it's not automatic.

### 3.4 Affinity Gain

Affinity increases by 1 after a battle if both creatures:
- Participated (dealt or took damage)
- Survived to battle end (optional: include fainted creatures)

```csharp
public void OnBattleEnd(List<CreatureInstance> participants)
{
    for (int i = 0; i < participants.Count; i++)
    {
        for (int j = i + 1; j < participants.Count; j++)
        {
            var pair = new CreaturePair(participants[i], participants[j]);
            AffinityState state = GetOrCreateAffinityState(pair);
            state.battlesSharedCount++;
            
            if (state.battlesSharedCount % GetAffinityThreshold(state.affinityLevel) == 0)
            {
                state.affinityLevel = Min(state.affinityLevel + 1, 5);
            }
        }
    }
}

private int GetAffinityThreshold(int currentLevel) => currentLevel switch
{
    0 => 1,   // Reach level 1 after 1 battle
    1 => 5,   // Reach level 2 after 5 total
    2 => 10,  // Reach level 3 after 10 total
    3 => 20,  // Reach level 4 after 20 total
    4 => 30,  // Reach level 5 after 30 total
    _ => int.MaxValue
};
```

## 4. Formulas

### Affinity Instability Reduction

```
reductionPercentage = affinityLevel * 5%
totalInstability = appliedModInstability * (1.0 - reductionPercentage)
```

### Combo Move Unlock

```
canUseCombo = affinityLevel >= 3 AND creatures are adjacent on grid
```

### Affinity Perk Effect

```
loyalBondReduction = 0.05 (5% damage reduction)
synchronizedStrikeBonus = 0.10 (10% damage increase)
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Creature faints mid-battle; pair member survives | Battle complete: does creature count toward affinity gain? (Configurable: yes/no) |
| Three creatures in battle together | Affinity applies to all pairs (creature A-B, A-C, B-C) |
| Creature reaches Affinity 5, then is permanently removed from party | Affinity state persists in save; can be recovered if creature is revisited |
| Player uses same creature twice in one battle slot (not possible in team slot system) | N/A; team slots are unique per active party |
| Affinity perk equipped then unequipped before battle | Perk is disabled; no bonus applied for that battle |
| Affinity combo move used; one creature faints during execution | Combo still completes; faint happens after |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Turn Manager | Detects creature participation in battle |
| Creature Instance | Reads creature ID for pair matching |
| Combo Move System | Checks affinity level >= 3 for combo unlock |
| DNA Alteration System | Applies instability reduction to mods |
| Pokedex System | Displays affinity level and lore entries |
| Save/Load System | Persists AffinityState for all pairs |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `affinityPerBattle` | 1 | Affinity gained per qualifying battle |
| `affinityThreshold1` | 1 | Battles to reach level 1 |
| `affinityThreshold2` | 5 | Battles to reach level 2 |
| `affinityThreshold3` | 10 | Battles to reach level 3 |
| `affinityThreshold4` | 20 | Battles to reach level 4 |
| `affinityThreshold5` | 30 | Battles to reach level 5 |
| `instabilityReductionPerLevel` | 0.05 | 5% per affinity level |
| `affinityPerkSlotUnlockLevel` | 2 | Affinity level when perks become available |
| `maxAffinity` | 5 | Affinity cap |

## 8. Acceptance Criteria

- [ ] Affinity level increases by 1 after each battle where both creatures participated.
- [ ] Affinity level caps at 5 and cannot increase further.
- [ ] Affinity perks unlock at level 2 and are selectable in party management UI.
- [ ] Instability is reduced by correct percentage per affinity level (unit tested).
- [ ] Combo moves can only be used when affinity >= 3.
- [ ] Lore entries progressively unlock at levels 1, 4, and 5.
- [ ] Affinity state persists through save/load.
- [ ] Multiple creature pairs tracked independently and correctly.
- [ ] Battle participation is correctly detected (creature dealt or took damage).
- [ ] Affinity perks apply in combat (Loyal Bond damage reduction, Synchronized Strike bonus).
