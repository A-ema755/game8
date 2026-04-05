# VFX System

## 1. Overview

The VFX System manages visual effects for creature abilities, DNA mutations, environmental interactions, and status effects. All particles are pooled for performance. Effects are queued by combat turn events and render at correct isometric depths using a configurable render queue. Key effects include move impact particles, capture trap glows, DNA modification glows, instability distortion glitches, terrain synergy auras, and height-difference indicators.

## 2. Player Fantasy

When a creature casts an ability, particles burst outward with type-appropriate colors. Capturing a creature triggers a triumphant shimmer effect. A heavily modified creature glows with layered auras reflecting its DNA mutations — a fire+crystal hybrid visibly combines flame and crystalline particle effects. Instability manifests as screen-space distortion and glitch artifacts that grow more intense as the meter fills. The isometric perspective is maintained throughout, with depth sorting preventing particle clipping.

## 3. Detailed Rules

### 3.1 VFX Pool Manager

```csharp
public class VFXPoolManager : MonoBehaviour
{
    public static VFXPoolManager Instance { get; private set; }
    
    [SerializeField] private int _poolSizePerEffect = 8;
    
    private Dictionary<string, Queue<ParticleSystem>> _pools;
    
    public void PlayEffect(string effectId, Vector3 position, Quaternion rotation = default);
    public void PlayEffectAtCreature(string effectId, CreatureInstance target);
    public void PlayEffectAtTile(string effectId, Vector3Int gridPos);
}
```

### 3.2 Effect Categories

**Ability Impact:**
- Type-specific hit effects: flame-burst, frost-shatter, electric-zap, grass-bloom, water-splash, rock-crack, poison-cloud
- Each effect is a short (0.5–1.0s) particle burst centered on target

**Capture:**
- trap-throw: arc particles trailing the trap
- trap-glow: pulsing aura around the creature during capture attempt
- trap-shake: impact particles at each shake
- capture-success: triumphant shimmer/confetti effect

**DNA Mutation Glow:**
- DNA-modified creatures emit a subtle persistent aura matching their primary DNA color
- Colors: fire-orange, ice-blue, electric-yellow, grass-green, poison-purple, crystal-white, normal-grey
- Glow intensity scales with total DNA mods applied (0 mods = no glow; 5+ mods = bright aura)

**Instability Distortion:**
- At instability >= 50: subtle screen-space distortion (shader-based)
- At instability >= 75: glitch artifacts (green/magenta scanline effects, brief position shifts)
- Intensity: (instability - 50) / 50 for baseline, capped at 1.0

**Status Effects:**
- Burn: small flame ticks on creature sprite
- Poison: purple cloud puffs around creature
- Paralyze: electric arcs connecting creature to ground
- Bleed: red particle trails on movement

**Terrain Synergy Glow:**
- When a creature stands on a matching terrain type, a 1-second pulse aura appears
- Color matches terrain type (grass-green for grass, water-blue for water, etc.)

**Height Indicators:**
- Elevated creatures show an upward-pointing arrow particle trail
- Creatures below ground level show a downward-pointing indicator
- Indicators refresh every 0.5 seconds

### 3.3 Particle System Configuration

```csharp
[CreateAssetMenu(menuName = "GeneForge/VFXConfig")]
public class VFXConfig : ScriptableObject
{
    [System.Serializable]
    public class EffectEntry
    {
        public string effectId;           // e.g., "flame-burst"
        public ParticleSystem prefab;
        public float duration;            // max lifetime before pool reclaim
        public int renderQueue = 3000;    // isometric depth sort
    }
    
    public List<EffectEntry> effects;
    public Material instabilityDistortionMaterial;
}
```

### 3.4 Render Queue & Depth Sorting

All particles render using a custom material with sorted render queue. The isometric camera (45° angle) requires:

- Particles at world height Y below creature center: queue 2900
- Particles at creature center height: queue 3000
- Particles above creature center: queue 3100

This prevents particles from clipping through the grid or appearing behind sprites incorrectly.

### 3.5 DNA Glow Tinting

```csharp
public void UpdateCreatureDNAGlow(CreatureInstance creature)
{
    List<string> dnaSources = creature.appliedMods.Select(m => m.sourceSpeciesId).Distinct().ToList();
    
    if (dnaSources.Count == 0)
    {
        DisableGlow(creature);
        return;
    }
    
    Color[] typeColors = GetTypeColorsForDNA(dnaSources);
    float intensity = Mathf.Min(1.0f, creature.appliedMods.Count / 5f);
    
    ApplyGlowAura(creature, typeColors, intensity);
}
```

## 4. Formulas

### DNA Glow Intensity

```
glowIntensity = Min(appliedModCount / 5.0, 1.0)
```

### Instability Distortion Strength

```
distortionStrength = (instability - 50) / 50     [when instability >= 50]
distortionStrength = 0                           [when instability < 50]
distortionStrength = Clamp(distortionStrength, 0.0, 1.0)
```

### Particle Lifetime Pooling

```
maxEffectDuration = config.duration + 0.5s buffer
particlesRecycledAfter = maxEffectDuration + 1.0s safety margin
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Pool exhausted for a specific effect type | Oldest particle system in pool is recycled and reset (may cut off effect early) |
| Creature is deleted while DNA glow is active | Glow persists for 1 second fade-out, then cleaned up |
| Multiple status effects active simultaneously | All particle effects play together; no layering limit |
| Instability reaches 100 then drops to 50 mid-battle | Distortion smoothly fades over 0.3 seconds |
| Effect plays at invalid grid position | Positioned at nearest valid tile; no error |
| VFXConfig missing a referenced effect ID | Silent fallback; no effect plays; no crash |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Turn Manager | Triggers ability impact VFX on move execution |
| Capture System | Fires trap and capture success effects |
| Creature Instance | Reads instability for distortion shader, DNA mods for glow |
| DNA Alteration System | Updates glow on mod application |
| Combat Feedback | Coordinates particle timing with damage popups |
| Grid Tile System | Provides tile positions for terrain synergy glows |
| Camera System | Render queue sorts relative to isometric view |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `poolSizePerEffect` | 8 | Concurrent particles per effect type |
| `dnaGlowIntensityPerMod` | 0.20 | Intensity per mod (5 mods = full glow) |
| `instabilityDistortionThreshold` | 50 | Instability level when distortion starts |
| `distortionMaxStrength` | 1.0 | At instability 100 |
| `particlePoolRecycleDelay` | 1.0s | Safety buffer before reusing pooled system |
| `heightIndicatorRefreshRate` | 0.5s | How often indicators update position |

## 8. Acceptance Criteria

- [ ] VFXPoolManager initializes all pooled particle systems without error.
- [ ] PlayEffect() retrieves the correct ParticleSystem from the pool and plays it.
- [ ] DNA glow color matches the primary DNA source of the creature and scales with mod count.
- [ ] Instability distortion shader activates at >= 50 instability and intensifies smoothly to 100.
- [ ] Capture trap effects (throw, glow, shake, success) play in correct sequence.
- [ ] Terrain synergy glow appears and disappears correctly when creatures move on/off matching tiles.
- [ ] Height indicator arrows appear correctly for elevated and lowered creatures.
- [ ] All particle effects render at correct depth (no clipping through creatures or grid).
- [ ] Pool exhaustion recycles oldest effects without crashing.
- [ ] VFX data persists visually through save/load (active glows redraw on load).
