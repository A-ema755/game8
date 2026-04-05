# Audio System

## 1. Overview

The Audio System manages all sound in Gene Forge: combat SFX (attack hits, creature abilities), creature vocalizations (base cries layered with DNA mutation audio), UI sounds, music themes (menu, zone exploration, boss encounters), and dynamic combat music that intensifies as battles escalate. The system uses an AudioManager singleton with pooled AudioSources for efficient playback and memory management. All audio events fire through the turn manager and UI systems.

## 2. Player Fantasy

Every creature has a unique voice shaped by its DNA modifications. A fire creature with a resonant crackle undertone sounds different from a poisoned variant with a hissing layer. Combat feels impactful — claw strikes have sharp attack SFX, capture moments have distinctive triumph chimes. The soundtrack dynamically builds tension as creatures' HP drops, making high-stakes moments feel cinematic without requiring player attention to music.

## 3. Detailed Rules

### 3.1 Audio Manager

```csharp
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private int _sfxPoolSize = 16;
    [SerializeField] private int _musicLayerCount = 4;

    private Queue<AudioSource> _sfxPool;
    private AudioSource _musicSource;
    private AudioSource _ambientSource;

    public void PlaySFX(string soundId, float volume = 1.0f);
    public void PlayCreatureVocalization(CreatureInstance creature);
    public void SetMusicIntensity(float normalizedIntensity);  // 0.0–1.0
    public void StopMusic();
}
```

### 3.2 Sound Categories

**Combat SFX:**
- Attack impacts (type-specific): flame-hit, frost-hit, slash-hit, crush-hit
- Ability casting: charge-up, release, impact
- Status effects: poison-applied, burn-tick, paralyze-zap
- Capture: trap-throw, trap-shake (x3), trap-capture-success, trap-capture-failure

**Creature Vocalizations:**
- Base cry (unique per species, loaded from CreatureConfig)
- DNA mutation layers applied on top: fire-rumble, crystal-chime, poison-hiss, electric-crackle, instability-distortion

**UI Sounds:**
- Menu click, selection, back, error, confirmation, level-up chime

**Music:**
- Menu theme (looping)
- Zone themes per habitat (looping, 2-minute loops)
- Boss encounter theme (looping)
- Combat theme (dynamic, 4-layer mixing)
- Station ambient (looping, subtle)

### 3.3 Creature Vocalization Mixing

When a creature is encountered or attacks:

```csharp
public void PlayCreatureVocalization(CreatureInstance creature)
{
    // Load base cry from CreatureConfig
    AudioClip baseCry = _creatureDatabase.Get(creature.speciesId).baseCryClip;
    
    // Layer DNA mutation sounds
    float fireLayer = creature.HasFireDNA ? 0.3f : 0.0f;
    float crystalLayer = creature.HasCrystalDNA ? 0.25f : 0.0f;
    float poisonLayer = creature.HasPoisonDNA ? 0.2f : 0.0f;
    float instabilityLayer = Mathf.Clamp01(creature.instability / 100f) * 0.4f;
    
    PlaySFXWithMix(baseCry, fireLayer, crystalLayer, poisonLayer, instabilityLayer);
}
```

### 3.4 Dynamic Combat Music

Combat music has 4 independent layers (base, drums, melody, orchestral). Intensity is driven by:

```
combatIntensity = (1.0 - (playerTeamAvgHP / playerTeamMaxHP)) * 0.5f  // enemy health
                + (1.0 - (enemyTeamAvgHP / enemyTeamMaxHP)) * 0.5f     // player health
```

- 0.0–0.3 intensity: base layer only (calm)
- 0.3–0.6 intensity: base + drums
- 0.6–0.9 intensity: base + drums + melody
- 0.9–1.0 intensity: all layers (climax)

Transitions are smooth crossfades over 0.5 seconds.

### 3.5 Audio Configuration

```csharp
[CreateAssetMenu(menuName = "GeneForge/AudioConfig")]
public class AudioConfig : ScriptableObject
{
    public AudioClip menuTheme;
    public Dictionary<string, ZoneAudioSet> zoneThemes;
    public AudioClip bossTheme;
    
    public Dictionary<string, AudioClip> sfxClips;  // keyed by soundId
    public Dictionary<string, CreatureCrySet> creatureCries;
}

[System.Serializable]
public class ZoneAudioSet
{
    public AudioClip explorationTheme;
    public AudioClip ambientLoop;
    public float volume = 1.0f;
}

[System.Serializable]
public class CreatureCrySet
{
    public AudioClip baseCry;
    public float pitch = 1.0f;
}
```

## 4. Formulas

### Combat Intensity Calculation

```
playerHealthRatio = currentPlayerTeamHP / maxPlayerTeamHP
enemyHealthRatio = currentEnemyTeamHP / maxEnemyTeamHP

combatIntensity = (1.0 - playerHealthRatio) * 0.5 + (1.0 - enemyHealthRatio) * 0.5
combatIntensity = Clamp(combatIntensity, 0.0, 1.0)
```

### Vocalization Layer Mix

```
DNA layer volume = baseLayerVolume + (layerIntensity * maxLayerVolume)
instabilityLayer = (creature.instability / 100.0) * 0.4
totalVolume = baseCry + fireLayer + crystalLayer + poisonLayer + instabilityLayer
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Combat intensity reaches 1.0 but creature wins before climax finishes | Music sustains climax for 2 seconds, then fade to victory stinger |
| Creature vocalization clips are missing from AudioConfig | Silent fallback; no error thrown |
| Player mutes SFX in settings but combat intensity changes | Music continues normally; only SFX are suppressed |
| Multiple creatures vocalize simultaneously | AudioSource pool handles queuing; older vocalizations cut off after 0.3s |
| Zone theme file is not found at startup | Menu theme plays as fallback for entire session |
| Station ambient loop should fade in on first station visit | 1-second fade-in from 0.0 to target volume |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Turn Manager | Fires audio events on attack, ability, capture |
| Creature Instance | Reads DNA mods for vocalization layer mixing |
| Settings System | Respects master volume and SFX/music toggles |
| Combat Feedback | Coordinates SFX timing with damage popups |
| UI Shell | Menu theme playback |
| Combat UI | Capture attempt SFX |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `sfxPoolSize` | 16 | Concurrent SFX sources allowed |
| `musicIntensitySmoothing` | 0.5s | Fade duration for layer transitions |
| `maxCreatureCryVolume` | 0.8 | Normalized 0–1 |
| `instabilityLayerMaxVolume` | 0.4 | At instability 100 |
| `combatIntensityUpdateRate` | Every turn | Recalculate intensity at turn start |
| `victoryFadeDuration` | 2.0s | Music sustain after win before fade |
| `masterVolume` | 0.8 | Default playback volume |

## 8. Acceptance Criteria

- [ ] AudioManager singleton initializes without error and pools AudioSources correctly.
- [ ] PlaySFX() retrieves sounds from AudioConfig dictionary by soundId and plays with correct volume.
- [ ] Creature vocalization layers (fire, crystal, poison, instability) mix correctly and do not exceed max volume.
- [ ] Combat music intensity transitions smoothly between layers every 0.5 seconds.
- [ ] Zone music changes when entering a new habitat and loops correctly.
- [ ] Master volume setting affects all audio playback proportionally.
- [ ] Missing audio clips do not crash the game; fallback to silence.
- [ ] Audio data persists through save/load (volume settings stored in GameSettings).
