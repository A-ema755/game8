# Weather System

## 1. Overview

The Weather System defines region-specific weather patterns on the combat grid. Six weather types (Rain, Sun, Sandstorm, Blizzard, Fog, Thunder) apply zone-wide or region-specific type effectiveness shifts (±20%), alter terrain behavior, and trigger weather-dependent abilities. Weather can be environmental (set at battle start per zone) or dynamically created by rare creature abilities. Creatures with matching types gain combat bonuses in their weather.

## 2. Player Fantasy

A rainy zone favors water creatures and nerfs fire. A sandstorm blocks accuracy for fliers but powers ground creatures. The weather feels like a dynamic opponent — adapting your team or finding weather-manipulating creatures becomes a viable strategy. Entering a zone with unfamiliar weather forces tactical rethinking.

## 3. Detailed Rules

### 3.1 Weather Types & Effects

| Weather | Effectiveness Shifts | Terrain Interaction | Ability Bonus |
|---------|---------------------|-------------------|--------------|
| Rain | +Water, -Fire, +Electric | Water tiles heal allies, Lava dmg doubled | Water abilities +25% power |
| Sun | +Fire, +Grass, -Water, -Ice | Grass tiles regenerate, Ice melts | Fire/Grass abilities +25% power |
| Sandstorm | +Rock, +Ground, -Flying, -Accuracy -20% | Sand terrain blocks vision, Rock tiles boost | Rock/Ground abilities +25% power |
| Blizzard | +Ice, -Fire, -Grass | Ice tiles spread, slippery movement | Ice abilities +25% power |
| Fog | -Accuracy -30%, blocks LoS | All LoS blocked beyond 1 tile | Ghost abilities +25% power |
| Thunder | +Electric, -Accuracy -15% (except Electric) | Electric tiles damage on step, attract lightning strikes | Electric abilities +25% power |

### 3.2 Weather Application

```csharp
[System.Serializable]
public class WeatherEffect
{
    public string weatherId;
    public CreatureType affectedType;
    public float damageMultiplier;     // e.g., 1.2 for +20%
    public float accuracyModifier;     // e.g., -0.2 for -20%
    public List<CreatureType> immuneTypes;
    public string particleEffect;
    public AudioClip ambientLoop;
}

public class WeatherSystem
{
    public void ApplyWeatherBonus(MoveInstance move, WeatherEffect weather)
    {
        if (weather.affectedType == move.config.type)
        {
            move.power *= weather.damageMultiplier;
        }
    }
}
```

### 3.3 Region-Specific Weather

Combat grids can have multiple weather regions. The left side of a 4x4 grid might be rainy while the right is sunny. Creatures moving between regions experience different bonuses.

```csharp
public class CombatGrid
{
    public Dictionary<Vector3Int, string> tileWeatherMap;  // Tile -> weatherId
    
    public WeatherEffect GetWeatherAtTile(Vector3Int tilePos)
    {
        if (tileWeatherMap.TryGetValue(tilePos, out string weatherId))
            return WeatherDatabase.Get(weatherId);
        return WeatherEffect.None;
    }
}
```

### 3.4 Weather Config

```csharp
[CreateAssetMenu(menuName = "GeneForge/WeatherConfig")]
public class WeatherConfig : ScriptableObject
{
    [System.Serializable]
    public class WeatherEntry
    {
        public string weatherId;
        public string displayName;
        public float duration;          // Duration in turns (0 = permanent)
        public List<WeatherEffect> effects;
    }
    
    public List<WeatherEntry> weatherTypes;
}
```

## 4. Formulas

### Weather Damage Modification

```
modifiedPower = movePower * weatherMultiplier
weatherMultiplier = affectedType matches weather ? 1.2 : 1.0
```

### Accuracy Modification

```
finalAccuracy = baseAccuracy * (1.0 + weatherAccuracyMod)
weatherAccuracyMod = -0.20 for Sandstorm, -0.30 for Fog, -0.15 for Thunder
```

### Terrain Synergy in Weather

```
synergyBonus = 1.2 if creature.type == terrain.synergyType AND weather matches
combinedBonus = synergyBonus * weatherBonus  [multiplicative]
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Fire creature in Rain; uses fire move | Move power = basePower * 0.8 (negative weather modifier) |
| Water creature in Rain on Water tile | Damage multiplier = 1.2 (weather) * 1.2 (terrain) = 1.44x |
| Region weather changes mid-battle | Ongoing effects recalculate; new moves use new weather bonus |
| Fog weather blocks all LoS beyond 1 tile but creature has Move with ignore-LoS flag | LoS ignore flag takes precedence; move targets normally |
| Weather duration is 0 (permanent) then battle ends | Weather persists into next battle if both zones have same weather |
| Multiple weather regions but creature ability affects "all weather" | Ability applies to every region independently |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Combat System | Applies weather modifiers to damage and accuracy |
| Creature Instance | Checks creature type for weather matching |
| Terrain System | Combines weather and terrain bonuses |
| Move Database | Weather modifies move power |
| Turn Manager | Tracks weather duration; decrements per turn |
| Grid Tile System | Stores per-tile weather regions |
| Combat UI | Displays weather icon and current effects |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `weatherDamageBonus` | 1.2 | +20% power for matching type |
| `weatherDamageNegative` | 0.8 | -20% power for opposed type |
| `sandstormAccuracyPenalty` | -0.20 | -20% accuracy |
| `fogAccuracyPenalty` | -0.30 | -30% accuracy |
| `thunderAccuracyPenalty` | -0.15 | -15% accuracy (except Electric type) |
| `abilityWeatherBonus` | 1.25 | +25% power for weather-matched ability |

## 8. Acceptance Criteria

- [ ] Weather is displayed on combat grid with icon and name.
- [ ] Weather modifiers apply correctly to move power (+20% for matching type).
- [ ] Accuracy penalties apply correctly (Sandstorm -20%, Fog -30%).
- [ ] Region-specific weather allows multiple weather zones on same grid.
- [ ] Creatures moving between weather regions experience correct bonuses.
- [ ] Weather duration countdown works; temporary weather expires after N turns.
- [ ] Weather data persists in save (if dynamic weather created mid-session).
- [ ] Combat UI shows active weather and its effects clearly.
