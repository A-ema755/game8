# Terrain Alteration System

## 1. Overview

Select creatures with a `canAlterTerrain` trait can modify battlefield tiles mid-combat through ability use. Fire creatures scorch tiles to lava, ice creatures freeze water to ice, grass creatures grow vine walls, water creatures flood low areas, electric creatures charge tiles, and poison creatures corrode with toxic puddles. Alterations persist until the battle ends or are reverted by opposing creatures. A `CanAlterTerrain` trait on CreatureConfig enables the feature per species.

## 2. Player Fantasy

Discovering that your fire creature can turn a water tile into lava creates a tactical "aha!" moment. Positioning creatures to maximize terrain alteration feels like environmental mastery. An opponent's ice creature freezes a path, but you have a fire creature that can melt it back — the terrain becomes a contested resource, not a static backdrop.

## 3. Detailed Rules

### 3.1 Alteration Traits

| Source Type | Effect | Result Terrain | Trigger |
|-------------|--------|----------------|---------|
| Fire | Scorch | Lava | Fire ability lands or hits terrain |
| Ice | Freeze | Ice | Ice ability lands on water tile |
| Grass | Grow | Forest | Grass ability casts; creates vine wall |
| Water | Flood | Water | Water ability used on empty/low tiles |
| Electric | Charge | Crystal | Electric ability hits tile |
| Poison | Corrode | Toxic | Poison ability hits tile |

### 3.2 CreatureConfig Trait

```csharp
public class CreatureConfig : ScriptableObject
{
    // ... existing fields ...
    
    public bool canAlterTerrain;
    public List<TerrainAlterationRule> terrainAlterations;
}

[System.Serializable]
public class TerrainAlterationRule
{
    public string moveId;              // Trigger move; if empty, any move of matching type triggers
    public CreatureType triggerType;   // Fire, Ice, Grass, etc.
    public TerrainType targetTerrain;  // Tile type to alter (e.g., Normal -> Lava)
    public TerrainType resultTerrain;  // Result after alteration
    public bool requiresDirectHit;     // True = only alters if move hits this tile directly
}
```

### 3.3 Alteration Resolution

When a creature with `canAlterTerrain` uses a move:

```csharp
public void OnMoveResolved(MoveInstance move, CreatureInstance caster)
{
    if (!caster.config.canAlterTerrain) return;
    
    foreach (var altRule in caster.config.terrainAlterations)
    {
        if (altRule.triggerType == move.config.type || altRule.moveId == move.config.id)
        {
            // Find affected tiles in move AoE
            List<Vector3Int> affectedTiles = move.GetAffectedTiles();
            
            foreach (var tile in affectedTiles)
            {
                TerrainType currentTerrain = GridSystem.GetTileType(tile);
                if (currentTerrain == altRule.targetTerrain)
                {
                    GridSystem.SetTileType(tile, altRule.resultTerrain);
                    VFXPoolManager.PlayEffect("terrain-alter-" + altRule.resultTerrain, tile);
                }
            }
        }
    }
}
```

### 3.4 Reversion & Stacking

- Alterations persist until battle end; no reversion within a single battle.
- A tile can be altered multiple times (e.g., lava → ice → lava).
- Weather alteration (rare) is tracked separately and takes precedence in visual rendering.

## 4. Formulas

### Alteration Trigger Condition

```
canAlter = creature.config.canAlterTerrain
        AND move.type matches altRule.triggerType OR moveId matches altRule.moveId
        AND currentTerrainType == altRule.targetTerrain
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Move hits multiple tiles; some match alterable terrain, some don't | Only matching terrain tiles are altered |
| Creature tries to alter tile already at result terrain | Tile remains unchanged; no error |
| Lava tile is frozen by ice creature | Tile changes to Ice; fire creature can scorch it back to Lava |
| Creature alters terrain then moves away | Alteration persists; tile remains altered |
| Battle ends with altered terrain | Terrain reverts to original state when battle concludes |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Creature Database | Reads `canAlterTerrain` trait and alteration rules |
| Move Database | Checks move type and area-of-effect |
| Turn Manager | Triggers alteration logic after move resolution |
| Terrain System | Modifies tile type; recalculates synergy/hazards |
| Grid Tile System | Stores and retrieves terrain state |
| VFX System | Displays alteration effects |
| Combat UI | Shows altered terrain visually |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `terrainAlterationChance` | 1.0 | Probability alteration succeeds (1.0 = guaranteed) |
| `allowMultipleAlterationsPerTurn` | true | Can alter same tile twice in one turn |
| `revertOnBattleEnd` | true | Terrain reverts after combat |

## 8. Acceptance Criteria

- [ ] Creatures with `canAlterTerrain = true` have alteration rules displayed in Pokedex.
- [ ] Using a fire ability on an Open tile does not alter terrain (no matching rule).
- [ ] Using a fire ability that results in Lava correctly alters all hit tiles.
- [ ] Altered terrain persists until battle end.
- [ ] Terrain reverts to original state when battle concludes.
- [ ] Tiles can be altered multiple times (lava → ice → lava chain).
- [ ] Alteration rules are configurable per creature in CreatureConfig.
- [ ] Combat UI displays newly altered terrain with appropriate color/icon.
