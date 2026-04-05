# Sound Mutation System

## 1. Overview

The Sound Mutation System layers creature vocalizations based on DNA modifications. Base cry is unique per species. DNA from different sources adds distinct audio layers: fire adds rumble and crackle, crystal adds chime and resonance, poison adds hissing and bubbling, electric adds crackling, instability adds distortion and static. The resulting "voice" becomes an audio fingerprint of every modification applied. High-instability creatures sound glitchy and unpredictable. The system makes every creature sonically unique.

## 2. Player Fantasy

Your modified creature doesn't just look different — it sounds different. A fire-infused creature has a crackling undertone. A heavily instable creature sounds warped and strange. The creature's vocalization tells a story of what you've done to it. No two customized teams sound the same.

## 3. Detailed Rules

### 3.1 Base Cry

Each creature species has a base vocalization:

```csharp
public class CreatureConfig : ScriptableObject
{
    public AudioClip baseCryClip;
    public float baseCryPitch = 1.0f;
    public float baseCryVolume = 0.8f;
}
```

Base cries are 0.5–2.0 seconds long and unique per species.

### 3.2 DNA Mutation Layers

| DNA Source | Audio Layer | Effect |
|-----------|------------|--------|
| Fire | Rumble + Crackle | Deep bass undertone + high-frequency pop |
| Ice | Chime + Resonance | Bell-like tone, sustain |
| Grass | Whistle + Rustle | Organic whoosh, leaf sounds |
| Water | Splash + Echo | Wet reverb, dripping |
| Electric | Crackle + Zap | High-pitched sparkle, electrical buzz |
| Rock | Boom + Grind | Low bass, grinding texture |
| Poison | Hiss + Bubble | Serpentine hiss, acidic burble |
| Psychic | Shimmer + Tone | Ethereal chime, vocal harmonics |
| Dark | Growl + Distortion | Deep growl, slight pitch bend down |

### 3.3 Sound Mixing

```csharp
public void PlayCreatureVocalization(CreatureInstance creature)
{
    var baseClip = _creatureDatabase.Get(creature.speciesId).baseCryClip;
    var audioSource = _audioPool.GetSource();
    
    audioSource.PlayOneShot(baseClip, creature.baseVolume);
    
    // Apply DNA layers
    float fireIntensity = 0.0f;
    float iceIntensity = 0.0f;
    float poisonIntensity = 0.0f;
    // ... etc for each DNA source
    
    foreach (var mod in creature.appliedMods)
    {
        switch (GetTypeFromDNASource(mod.sourceSpeciesId))
        {
            case CreatureType.Fire:
                fireIntensity += 0.3f;
                break;
            case CreatureType.Poison:
                poisonIntensity += 0.25f;
                break;
            // ... etc
        }
    }
    
    // Clamp intensities
    fireIntensity = Mathf.Min(fireIntensity, 1.0f);
    poisonIntensity = Mathf.Min(poisonIntensity, 1.0f);
    
    // Layer sounds
    PlayDNALayer("fire-rumble", fireIntensity);
    PlayDNALayer("poison-hiss", poisonIntensity);
    
    // Instability distortion (highest intensity at 100)
    float distortion = creature.instability / 100f * 0.5f;  // Up to 50% distortion
    ApplyDistortionEffect(audioSource, distortion);
}
```

### 3.4 Sound Config

```csharp
[CreateAssetMenu(menuName = "GeneForge/SoundMutationConfig")]
public class SoundMutationConfig : ScriptableObject
{
    [System.Serializable]
    public class DNALayerSound
    {
        public CreatureType sourceType;
        public AudioClip layerClip;
        public float volume = 0.4f;
        public float pitch = 1.0f;
    }
    
    public List<DNALayerSound> dnaLayers;
    public AudioClip instabilityDistortionEffect;
}
```

### 3.5 Instability Distortion

At instability >= 50, the creature's vocalization becomes warped:

```csharp
public void ApplyInstabilityDistortion(AudioSource source, float instability)
{
    if (instability < 50) return;
    
    float distortionAmount = (instability - 50) / 50f * 0.5f;  // 0–50%
    
    // Pitch shifting (slight detune)
    source.pitch *= (1.0f - distortionAmount * 0.2f);  // Up to -10% pitch
    
    // Add distortion filter
    var distortionFilter = source.GetComponent<AudioDistortionFilter>();
    distortionFilter.distortionLevel = distortionAmount;
}
```

## 4. Formulas

### DNA Layer Intensity

```
layerIntensity = modCount * 0.3  [per DNA source type]
layerIntensity = Clamp(layerIntensity, 0.0, 1.0)
```

### Distortion Amount

```
distortionAmount = (instability - 50) / 50  [when instability >= 50]
distortionAmount = Clamp(distortionAmount, 0.0, 1.0)
pitchShift = distortionAmount * -0.10  [-10% pitch max]
```

### Final Vocalization Mix

```
finalVolume = baseVolume * (1.0 - distortionAmount * 0.1)  [distortion reduces clarity]
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Creature with same DNA from multiple sources | Layer intensity increases per source count; may sound muddy |
| High instability + many DNA layers | Distortion + layers blend; sound remains intelligible but heavily modified |
| Creature has no DNA mods | Base cry plays cleanly with no layers |
| Instability drops rapidly (100→50); distortion fades | Pitch and distortion smoothly transition over 0.5 seconds |
| DNA layer clip not found in config | Silent fallback; layer is skipped; no error |
| Base cry missing from creature config | Fallback to generic creature vocalization |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Creature Instance | Reads appliedMods and instability |
| Creature Database | Loads baseCryClip per species |
| DNA Alteration System | DNA mods determine layer selection |
| Audio Manager | Manages audio source pooling and playback |
| Audio Filters | Distortion filter applied per instability |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `dnaLayerIntensityPerMod` | 0.30 | Each DNA mod contributes 30% to layer |
| `maxDNALayerIntensity` | 1.0 | Capped at 100% |
| `instabilityDistortionThreshold` | 50 | When distortion starts |
| `instabilityDistortionMax` | 0.5 | 50% distortion at instability 100 |
| `instabilityPitchShiftMax` | -0.10 | -10% pitch shift max |
| `distortionFadeTime` | 0.5s | Smooth transition duration |
| `dnaLayerBaseVolume` | 0.4 | Default layer volume |
| `instabilityVolumeReduction` | 0.1 | Distortion reduces clarity |

## 8. Acceptance Criteria

- [ ] Base creature cry plays correctly for each species.
- [ ] DNA layers are added to vocalization based on applied mods.
- [ ] Fire DNA adds rumble/crackle layer.
- [ ] Poison DNA adds hiss/bubble layer.
- [ ] Electric DNA adds crackle/zap layer.
- [ ] Multiple DNA sources layer together without canceling out.
- [ ] Instability >= 50 applies distortion effect.
- [ ] Distortion pitch-shifts vocalization by up to -10%.
- [ ] Distortion fades smoothly when instability changes.
- [ ] Heavily modified creatures have distinctly different "voices."
- [ ] Audio data plays correctly in battle and on campaign map.
- [ ] Creature vocalization data persists through save/load (state determines sound).
