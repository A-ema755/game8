# Unity 6.3 — Audio Module Reference

**Last verified:** 2026-02-13
**Knowledge Gap:** Unity 6 audio mixer improvements

**Gene Forge context:** Isometric 3D creature combat. Audio includes creature SFX (attacks,
abilities, hit reactions, death), UI sounds (menu nav, stat changes, DNA unlock stings),
and adaptive battle music. Spatial audio for creature positions on the iso grid.

---

## Overview

Unity 6.3 audio systems:
- **AudioSource**: Play sounds on GameObjects
- **Audio Mixer**: Mix, effect processing, dynamic mixing
- **Spatial Audio**: 3D positioned sound

---

## Basic Audio Playback

### AudioSource Component

```csharp
AudioSource audioSource = GetComponent<AudioSource>();

// ✅ Play
audioSource.Play();

// ✅ Play with delay
audioSource.PlayDelayed(0.5f);

// ✅ Play one-shot (doesn't interrupt current sound)
audioSource.PlayOneShot(clip);

// ✅ Stop
audioSource.Stop();

// ✅ Pause/Resume
audioSource.Pause();
audioSource.UnPause();
```

### Play Sound at Position (Static Method)

```csharp
// ✅ Quick 3D sound playback (auto-destroys when done)
// Gene Forge: play creature hit SFX at tile world position
AudioSource.PlayClipAtPoint(clip, creatureTileWorldPos);
AudioSource.PlayClipAtPoint(clip, creatureTileWorldPos, 0.7f);
```

---

## 3D Spatial Audio

### AudioSource 3D Settings

```csharp
AudioSource source = GetComponent<AudioSource>();

// Gene Forge: set per-creature AudioSource for positional combat audio
source.spatialBlend = 1.0f; // Fully 3D

source.dopplerLevel = 0f;    // Gene Forge: creatures don't move fast; disable doppler
source.minDistance = 2f;
source.maxDistance = 30f;    // Roughly the visible isometric grid width
source.rolloffMode = AudioRolloffMode.Logarithmic; // Natural falloff (RECOMMENDED)
```

### Volume Rolloff Curves
- **Logarithmic**: Natural, realistic (RECOMMENDED)
- **Linear**: Steady decrease
- **Custom**: Define your own curve

---

## Audio Mixer (Advanced Mixing)

### Gene Forge Mixer Hierarchy

```
Master
├── SFX
│   ├── CreatureSFX   (attacks, abilities, hit reactions, death)
│   └── UISounds      (menu nav, button clicks, stat pop-ups)
├── Music             (adaptive battle music, exploration BGM)
└── Dialogue          (narrator VO, story beats)
```

### Assign AudioSource to Mixer Group

```csharp
using UnityEngine.Audio;

public AudioMixerGroup creatureSFXGroup;

void Start() {
    GetComponent<AudioSource>().outputAudioMixerGroup = creatureSFXGroup;
}
```

### Control Mixer from Code

```csharp
using UnityEngine.Audio;

public AudioMixer audioMixer;

// ✅ Set volume (exposed parameter)
audioMixer.SetFloat("MusicVolume", -10f); // dB (-80 to 0)

// Convert linear (0-1) slider to dB
float volumeDB = Mathf.Log10(volumeLinear) * 20f;
audioMixer.SetFloat("MusicVolume", volumeDB);
```

---

## Audio Effects

### Duck Music During Creature Ability

```csharp
// Gene Forge: duck music during special ability VO/SFX
public AudioMixerSnapshot normalSnapshot;
public AudioMixerSnapshot abilitySnapshot;

public void PlayAbilityWithDuck(AudioClip clip) {
    abilitySnapshot.TransitionTo(0.3f);
    audioSource.PlayOneShot(clip);
    Invoke(nameof(RestoreMusic), clip.length);
}

void RestoreMusic() {
    normalSnapshot.TransitionTo(1.0f);
}
```

---

## Audio Performance

### Optimize Audio Loading

```csharp
// Audio Import Settings:
// - Load Type:
//   - Decompress On Load: Small SFX clips (creature hit sounds)
//   - Compressed In Memory: Medium clips (ability SFX) — RECOMMENDED
//   - Streaming: Battle music tracks

// Compression Format:
// - ADPCM: Fast, good for short SFX (RECOMMENDED for creature SFX)
// - Vorbis: High compression, good for music and long ambient
```

---

## Music Systems

### Crossfade Between Battle Phases

```csharp
// Gene Forge: crossfade from exploration BGM to battle music
public IEnumerator CrossfadeMusic(AudioSource from, AudioSource to, float duration) {
    float elapsed = 0f;
    to.Play();

    while (elapsed < duration) {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        from.volume = Mathf.Lerp(1f, 0f, t);
        to.volume = Mathf.Lerp(0f, 1f, t);
        yield return null;
    }

    from.Stop();
}
```

---

## Common Patterns

### Random Pitch Variation (Avoid Repetition)

```csharp
// Gene Forge: vary creature attack SFX pitch per hit
void PlayCreatureAttackSFX(AudioClip clip) {
    AudioSource source = GetComponent<AudioSource>();
    source.pitch = Random.Range(0.9f, 1.1f);
    source.PlayOneShot(clip);
}
```

### Type-Based SFX Selection

```csharp
// Gene Forge: play type-appropriate ability SFX
public AudioClip[] typeAbilityClips; // indexed by CreatureType enum

void PlayTypeAbilitySFX(CreatureType type) {
    AudioClip clip = typeAbilityClips[(int)type];
    AudioSource.PlayClipAtPoint(clip, transform.position);
}
```

### Check if Sound is Playing

```csharp
if (audioSource.isPlaying) { }
```

---

## Audio Listener

```csharp
// Gene Forge: AudioListener on the isometric camera rig root.
// Disable listener on creature GameObjects.
AudioListener listener = GetComponent<AudioListener>();
listener.enabled = false;
```

---

## Debugging

- `Window > Audio > Audio Mixer` — visualize levels, test snapshots
- `Edit > Project Settings > Audio` — global volume, DSP buffer size, speaker mode

---

## Sources
- https://docs.unity3d.com/6000.0/Documentation/Manual/Audio.html
- https://docs.unity3d.com/6000.0/Documentation/Manual/AudioMixer.html
