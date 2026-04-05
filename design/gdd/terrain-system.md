# Terrain System

## 1. Overview

The Terrain System defines tile types and their mechanical effects on the grid. Nine terrain types exist (Open, Forest, Water, Lava, Ice, Rock, Sand, Crystal, Toxic), each with distinct movement costs, line-of-sight rules, and type synergy bonuses. Creatures matching a terrain type gain +20% damage and passive healing on that tile. The system is tile-agnostic (works on any grid) and integrates with creature pathfinding and combat damage calculations.

## 2. Player Fantasy

The grid is more than empty space — it's a strategic canvas. Positioning your water creature on water grants both damage boost and healing, turning a defending tile into a fortress. Using terrain to block enemy line-of-sight forces opponents into unfavorable positions. A lava tile damages fire creatures standing on it normally, but a fire creature thrives there. Understanding terrain becomes as important as type matchups.

## 3. Detailed Rules

### 3.1 Terrain Types & Properties

| Terrain | Movement Cost | LoS Blocking | Type Synergy | Special Notes |
|---------|---------------|-------------|-------------|---------------|
| Open | 1 | No | None | Default; allows flight over |
| Forest | 2 | Partial (0.5x range) | Organic (+20% dmg, regen) | Cover; Energy attacks reduced by cover penalty |
| Water | 2 | No | Aqua (+20% dmg, regen) | Non-fliers slow; Aqua creatures swim freely |
| Lava | 3 | No | Thermal (+20% dmg, regen) | Non-Thermal creatures take 5% max HP damage per turn |
| Ice | 2 | No | Cryo (+20% dmg, regen) | Slippery; movement cost += 1 for non-Cryo creatures |
| Rock | 2 | Partial (0.75x range) | Mineral (+20% dmg, regen) | Heavy; blocks Physical and Energy pathing |
| Sand | 2 | Partial (0.5x range) | Kinetic (+20% dmg, regen) | Windy; Aero creatures take -25% accuracy |
| Crystal | 2 | Partial (1.0x range) | Neural (+20% dmg, regen) | Reflective; 10% chance to redirect Energy projectiles |
| Toxic | 3 | No | Toxic (+20% dmg, regen) | Poisonous; non-Toxic creatures apply poison on end of turn |

### 3.1b Terrain Interaction by Damage Form

Terrain cover and blocking interact differently with each damage form:

| Terrain Feature | Physical | Energy | Bio |
|----------------|----------|--------|-----|
| Cover (Forest, partial) | Blocks targeting entirely | 50% damage reduction | Ignored — Bio bypasses cover |
| Wall/Rock blocking | Blocks targeting entirely | Blocks LoS (cannot target) | Ignored — Bio bypasses walls |
| Height advantage | +10% per level | +10% per level | No effect |
| Crystal redirect | No redirect (melee) | 10% redirect chance | No redirect |

See `damage-health-system.md` Section 3.6 for full form-terrain interaction rules.

### 3.2 Type Synergy Mechanics

When a creature whose primary genome type matches a terrain's synergy type stands on that tile:

```csharp
public float GetSynergyBonus(CreatureInstance creature, TerrainType tileType)
{
    if (creature.primaryType == GetSynergyType(tileType))
    {
        return 1.2f;  // +20% damage multiplier
    }
    return 1.0f;
}

public void ApplySynergyHealing(CreatureInstance creature, TerrainType tileType)
{
    if (creature.primaryType == GetSynergyType(tileType))
    {
        int healAmount = Mathf.RoundToInt(creature.maxHP * 0.05f);  // 5% max HP per turn
        creature.Heal(healAmount);
    }
}
```

### 3.3 Line-of-Sight & Range

LoS blocking types (Forest, Rock, Sand, Crystal) interact with damage forms differently:

- **Energy form**: LoS blocking reduces effective range or blocks targeting entirely (see form-terrain table above)
- **Physical form**: Cover and walls block targeting entirely (cannot target through)
- **Bio form**: Ignores all cover and wall blocking — spores/parasites bypass physical barriers
- LoS blocking is most relevant for Energy moves; Bio moves ignore it completely

### 3.4 Terrain Config

```csharp
[CreateAssetMenu(menuName = "GeneForge/TerrainConfig")]
public class TerrainConfig : ScriptableObject
{
    [System.Serializable]
    public class TerrainTypeData
    {
        public TerrainType terrainType;
        public string displayName;
        public int movementCost;
        public float losBlockingMultiplier;    // 1.0 = no block, 0.5 = 50% range
        public CreatureType synergyType;
        public Color tileColor;
        public Sprite tileIcon;
        public AudioClip stepSFX;
        public bool isDamaging;
        public int damagePerTurn;              // e.g., 5% max HP
    }
    
    public List<TerrainTypeData> terrainTypes;
}
```

## 4. Formulas

### Movement Cost Calculation

```
baseCost = terrainConfig.movementCost
extraCost = (creature.type != terrainType && terrainType == Ice) ? 1 : 0
finalCost = baseCost + extraCost
```

### Type Synergy Damage Multiplier

```
synergyBonus = (creature.type == terrain.synergyType) ? 1.2 : 1.0
finalDamage = baseDamage * synergyBonus
```

### Synergy Healing per Turn

```
healAmount = Max(1, Round(creature.maxHP * 0.05))  [if creature is on synergy terrain]
```

### Damage from Hazardous Terrain

```
damagePerTurn = Round(creature.maxHP * 0.05)  [for Lava and Toxic if type doesn't match]
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Creature with dual type (e.g., Thermal/Aqua) steps on Water tile | Synergy applies if either genome type matches Aqua; gain bonus |
| Creature moves into Toxic tile with Toxic type | No damage taken; healing applies instead |
| Energy move targets creature behind Rock with LoS blocking | LoS calculated before move; if blocked, Energy move cannot target |
| Bio move targets creature behind Rock cover | Bio ignores cover — full damage, can target through |
| Physical move targets creature behind cover | Cannot target — Physical is blocked by cover |
| Creature on healing terrain but at max HP | Healing is wasted; no overflow |
| Non-flying creature steps on Open tile (default) | Normal movement cost (1); can move freely |
| Healing and damage both apply (dual-effect terrain) | Healing takes precedence; damage cancels if creature is synergy type |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Grid Tile System | Reads terrain type of each tile |
| Creature Instance | Reads creature genome type for synergy matching |
| Damage & Health System | Applies synergy multiplier to damage calculation; form-terrain interactions |
| Turn Manager | Applies hazard damage and synergy healing at end of turn |
| Pathfinding (A*) | Uses movement cost for path cost calculation |
| Combat UI | Displays terrain type and synergy bonus on hover |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `synergyDamageMultiplier` | 1.2 | +20% damage on matching terrain |
| `synergyHealingPercentage` | 0.05 | 5% max HP per turn on synergy tile |
| `hazardDamagePercentage` | 0.05 | 5% max HP per turn on hazardous tile |
| `forestRangeMultiplier` | 0.5 | 50% range reduction |
| `rockyRangeMultiplier` | 0.75 | 75% range (25% reduction) |
| `sandRangeMultiplier` | 0.5 | 50% range reduction |
| `crystalRangeMultiplier` | 1.0 | No range reduction (full range, 10% redirect chance) |
| `iceExtraMovementCost` | 1 | Additional cost for non-ice creatures |
| `sandFlyingAccuracyPenalty` | -0.25 | -25% accuracy for flying creatures |

## 8. Acceptance Criteria

- [ ] All 9 terrain types can be placed on the grid and display correctly.
- [ ] Movement pathfinding respects terrain movement costs.
- [ ] Creatures on synergy terrain gain +20% damage (unit tested).
- [ ] Synergy healing applies at end of turn and heals 5% max HP.
- [ ] Hazardous terrain (Lava, Toxic) applies 5% max HP damage to non-matching types.
- [ ] LoS blocking terrains reduce attack range by correct multiplier.
- [ ] Melee attacks ignore LoS blocking; ranged attacks respect it.
- [ ] Ice terrain applies extra movement cost to non-ice creatures.
- [ ] Terrain data persists in save files.
- [ ] Combat UI displays terrain type and synergy status on hover.
