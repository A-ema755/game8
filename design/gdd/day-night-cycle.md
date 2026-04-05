# Day-Night Cycle

## 1. Overview

The Day-Night Cycle progresses time forward as the player completes encounters. A session counter increments after each battle; every 4 encounters = one day-night cycle. Dark-type creatures gain +10% effectiveness at night; Psychic and Fairy types gain +10% during day. Visual lighting on the isometric grid shifts — brighter at day, darker and moodier at night. Players can toggle between day/night at research stations for strategy testing.

## 2. Player Fantasy

Time feels like it's passing. Returning to a zone at a different time reveals different creatures or hints at a living world. Nocturnal creatures appear only at night, adding replay value. Understanding creature spawn tables by time-of-day becomes part of mastery. The visual atmosphere shift (day bright, night dark) makes the world feel alive without requiring a full day-cycle simulation.

## 3. Detailed Rules

### 3.1 Time Progression

Time advances per encounter completion:

```csharp
public enum TimeOfDay { Day, Night }

public class TimeManager : MonoBehaviour
{
    public TimeOfDay CurrentTime { get; private set; } = TimeOfDay.Day;
    
    private int _encounterCounter;
    
    public void OnEncounterComplete()
    {
        _encounterCounter++;
        if (_encounterCounter % 4 == 0)
        {
            CurrentTime = CurrentTime == TimeOfDay.Day ? TimeOfDay.Night : TimeOfDay.Day;
            OnTimeChanged?.Invoke(CurrentTime);
        }
    }
    
    public void SetTimeManually(TimeOfDay newTime)
    {
        CurrentTime = newTime;
        OnTimeChanged?.Invoke(CurrentTime);
    }
}
```

### 3.2 Type Effectiveness Shifts

| Type | Day Bonus | Night Bonus |
|------|-----------|-------------|
| Dark | None | +10% effective |
| Psychic | +10% effective | None |
| Fairy | +10% effective | None |
| Ghost | None | +10% effective |

All other types unaffected by time.

### 3.3 Creature Spawning

CreatureConfig has optional `nightOnlySpawn` and `dayOnlySpawn` flags. Wild encounter generation checks current time:

```csharp
public bool CanSpawn(CreatureConfig config, TimeOfDay currentTime)
{
    if (currentTime == TimeOfDay.Night && config.dayOnlySpawn)
        return false;
    if (currentTime == TimeOfDay.Day && config.nightOnlySpawn)
        return false;
    return true;
}
```

### 3.4 Visual Implementation

```csharp
public class LightingController : MonoBehaviour
{
    [SerializeField] private Light _globalLight;
    [SerializeField] private Color _dayColor = Color.white;
    [SerializeField] private Color _nightColor = new Color(0.3f, 0.3f, 0.4f);
    
    public void SetTimeOfDay(TimeOfDay time)
    {
        Color targetColor = time == TimeOfDay.Day ? _dayColor : _nightColor;
        _globalLight.color = Color.Lerp(_globalLight.color, targetColor, Time.deltaTime * 2f);
    }
}
```

### 3.5 Station Toggle

At research stations, a UI toggle allows manual day/night switching for testing:

```
[ Day ] [ Night ] (buttons)
Current time: Day
```

Switching does not cost resources or reset encounters; it's purely for strategy testing.

## 4. Formulas

### Time Progression

```
encountersPerCycle = 4
onCycleComplete: timeOfDay = toggle(timeOfDay)
```

### Type Effectiveness with Time

```
dayTimeBonus = (type == Psychic OR type == Fairy) ? 1.1 : 1.0
nightTimeBonus = (type == Dark OR type == Ghost) ? 1.1 : 1.0

finalMultiplier = baseDamage * dayTimeBonus OR nightTimeBonus
```

### Lighting Interpolation

```
targetColor = (currentTime == Day) ? dayColor : nightColor
currentColor = Lerp(currentColor, targetColor, deltaTime * transitionSpeed)
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Player manually toggles day/night then encounters wild creature | Time change applies immediately; wild spawns re-evaluate for this new time |
| Player is mid-battle when 4th encounter completes | Time doesn't change until battle ends and next encounter starts |
| Creature has both Psychic and Dark types | Applies whichever time matches; tie = no bonus |
| Creature captured at day, reviewed at night | Pokedex shows stats as if it were day (captured stats don't change with time) |
| Save file loaded with corrupted time data | Default to Day; encounter counter resets to 0 |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Encounter System | Increments encounter counter; triggers time check |
| Creature Database | Reads dayOnlySpawn/nightOnlySpawn flags |
| Type Chart System | Applies day/night effectiveness modifiers |
| Camera/Lighting System | Updates light colors on time change |
| Combat UI | Displays current time of day |
| Save/Load System | Persists current time and encounter counter |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `encountersPerCycle` | 4 | Encounters before day/night toggle |
| `dayTypeBonus` | 1.1 | +10% for Psychic/Fairy |
| `nightTypeBonus` | 1.1 | +10% for Dark/Ghost |
| `lightTransitionSpeed` | 2.0 | Smoothness of color fade (units/sec) |
| `dayLightColor` | (1, 1, 1) | White ambient light |
| `nightLightColor` | (0.3, 0.3, 0.4) | Dim blue-tinted light |

## 8. Acceptance Criteria

- [ ] Time toggles between Day and Night after every 4 completed encounters.
- [ ] Psychic and Fairy types gain +10% effectiveness during Day.
- [ ] Dark and Ghost types gain +10% effectiveness during Night.
- [ ] Creatures with dayOnlySpawn flag do not appear at night.
- [ ] Creatures with nightOnlySpawn flag do not appear during day.
- [ ] Manual day/night toggle at research stations works without cost.
- [ ] Lighting color transitions smoothly when time changes.
- [ ] Current time is displayed in combat UI.
- [ ] Time and encounter counter persist through save/load.
- [ ] Time change does not interrupt ongoing battle.
