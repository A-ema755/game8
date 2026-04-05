# Color & Pattern System

## 1. Overview

The Color & Pattern System makes DNA modifications visually apparent on creatures. Base color is determined by primary type (fire=orange, water=blue, grass=green, etc.). Patterns (stripes, spots, veins, crystalline structures) are extracted from donor species during DNA splicing and layered onto the creature's appearance. Glow effects come from auras or high instability. Battle scars are permanent marks visible on the creature model. Camouflage mechanics provide +Evasion when creature color matches terrain tile. The system ties visual design to mechanical choices.

## 2. Player Fantasy

Your modified creatures look unmistakably yours. A fire creature with water DNA layers shows visible blending of colors. A crystal-infused creature glows with crystalline patterns. Heavy scarring from battles makes a creature look battle-hardened and veteran. Creatures with matching terrain color become harder to see — the visual readout matches the mechanical bonus.

## 3. Detailed Rules

### 3.1 Base Color

Each creature type has a base color:

| Genome Type | Base Color | Hex |
|-------------|-----------|-----|
| Thermal | Orange | #FF8C00 |
| Aqua | Blue | #4169E1 |
| Organic | Green | #32CD32 |
| Bioelectric | Yellow | #FFD700 |
| Cryo | Cyan | #00FFFF |
| Mineral | Brown | #8B4513 |
| Toxic | Sickly Green | #7FFF00 |
| Neural | Magenta | #FF1493 |
| Ferro | Gunmetal | #6E7B8B |
| Kinetic | Burnt Orange | #CC5500 |
| Aero | Sky Blue | #87CEEB |
| Sonic | Violet | #9400D3 |
| Ark | Gold | #FFD700 |
| Blight | Corrupted Red | #8B0000 |

Color is applied as a tint to the creature model's base material.

### 3.2 Pattern Extraction & Application

When splicing DNA from a donor creature, patterns are extracted:

```csharp
[System.Serializable]
public class PatternData
{
    public string patternId;
    public string patternName;       // "Tiger Stripes", "Crystalline Veins"
    public Color patternColor;
    public Texture2D patternMask;    // Grayscale mask for blending
    public float intensity = 0.5f;
}

public void ApplyPattern(CreatureInstance target, PatternData pattern)
{
    target.activePatterns.Add(pattern);
    
    // Blend pattern onto creature material
    var material = target.renderer.material;
    material.SetTexture("_PatternMask", pattern.patternMask);
    material.SetColor("_PatternColor", pattern.patternColor);
    material.SetFloat("_PatternIntensity", pattern.intensity);
}
```

### 3.3 Glow Effects

Creatures with Aura parts or high instability emit glow:

```csharp
public void UpdateCreatureGlow(CreatureInstance creature)
{
    float glowIntensity = 0.0f;
    
    // Aura part contribution
    if (creature.HasAuraPart)
        glowIntensity += 0.4f;
    
    // Instability contribution
    if (creature.instability >= 50)
        glowIntensity += (creature.instability - 50) / 50f * 0.6f;  // Up to 0.6
    
    var material = creature.renderer.material;
    material.SetFloat("_EmissionIntensity", glowIntensity);
    material.SetColor("_EmissionColor", creature.GetDominantColor());
}
```

### 3.4 Battle Scars

Scars are permanent marks applied when a creature survives < 10% HP:

```csharp
[System.Serializable]
public class ScarData
{
    public string scarId;
    public ScarType scarType;      // Burn, Frost, Claw, Impact
    public Vector3 position;        // On creature model (0–1 normalized)
    public float scale;
    public Color scarColor;         // Type-specific color
    public long timestampApplied;
}

public enum ScarType
{
    Burn,       // Orange-red scar
    Frost,      // Pale blue scar
    Claw,       // Red scratch lines
    Impact,     // Grey dent/crater
    Poison      // Green chemical burn
}
```

Scars are baked into the creature's texture/material and cannot be removed.

### 3.5 Camouflage & Evasion

```csharp
public float GetCamouflageBonus(CreatureInstance creature, TerrainType tileType)
{
    Color creatureColor = creature.GetDisplayColor();
    Color terrainColor = GetTerrainColor(tileType);
    
    float colorSimilarity = Vector3.Distance(
        new Vector3(creatureColor.r, creatureColor.g, creatureColor.b),
        new Vector3(terrainColor.r, terrainColor.g, terrainColor.b)
    );
    
    // Similar color = low distance = high evasion bonus
    float evasionBonus = Mathf.Max(0.0f, 1.0f - colorSimilarity) * 0.15f;  // Up to +15%
    
    return evasionBonus;
}
```

## 4. Formulas

### Base Color Tint

```
displayColor = baseTypeColor
```

### Pattern Blending

```
patternIntensity = Min(1.0, patternCount * 0.25)  [each pattern contributes 25%]
finalColor = Lerp(displayColor, patternColor, patternIntensity)
```

### Glow Intensity

```
glowFromAura = 0.4 (if Aura part equipped)
glowFromInstability = (instability - 50) / 50 * 0.6  [when instability >= 50]
totalGlow = Min(1.0, glowFromAura + glowFromInstability)
```

### Camouflage Evasion Bonus

```
colorDistance = Euclidean distance between creature and terrain color (0–1 normalized)
evasionBonus = Max(0.0, 1.0 - colorDistance) * 0.15
finalEvasion = baseEvasion + evasionBonus
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Creature with multiple patterns applied; colors clash visually | Blend all patterns with reduced intensity each; visual result may be muddy (intentional) |
| Camouflage bonus applied but terrain color is unknown | Default to 0% bonus for that tile type |
| Scar applied to a creature with very glossy/reflective material | Scar is still visible but may be subtle; reflection highlights the scar |
| Creature color changes due to type infusion; scars remain | Scars are baked; they don't change color with the creature's new color |
| Instability drops from 100 to 50; glow dims | Glow smoothly fades over 0.5 seconds |
| Creature is captured/reset; color/pattern/scars remain | All cosmetics persist; they define the creature's identity |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Creature Instance | Stores color, patterns, scars, glow state |
| DNA Alteration System | Patterns extracted during DNA splicing |
| Body Part System | Aura parts trigger glow |
| Terrain System | Terrain colors for camouflage calculation |
| Battle Scar System | Scars applied on survivor < 10% HP |
| Renderer/Graphics | Material properties for color/pattern/glow |
| Save/Load System | Persists creature appearance data |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `baseColorIntensity` | 1.0 | Saturation of base type color |
| `patternIntensityPerPattern` | 0.25 | Each pattern contributes 25% |
| `maxPatternCount` | 4 | Max simultaneous patterns |
| `auraGlowIntensity` | 0.4 | Glow from Aura part |
| `instabilityGlowThreshold` | 50 | Instability level when glow starts |
| `instabilityGlowMaxIntensity` | 0.6 | Max glow from instability alone |
| `camouflageEvasionBonus` | 0.15 | +15% evasion when colors match |
| `glowTransitionSpeed` | 0.5s | Fade duration for glow changes |

## 8. Acceptance Criteria

- [ ] Creatures display correct base color for their primary type.
- [ ] Patterns extracted from donor species apply visually to target creature.
- [ ] Multiple patterns can be applied and blend together visually.
- [ ] Glow intensity increases with instability (>= 50).
- [ ] Aura parts contribute to glow effect.
- [ ] Battle scars appear visually on creature model and remain permanent.
- [ ] Scar colors match scar types (burn=orange, frost=blue, etc.).
- [ ] Camouflage bonus applies when creature color matches terrain.
- [ ] Camouflage evasion bonus is calculated correctly (+15% max).
- [ ] Color and pattern data persist through save/load.
- [ ] Glow transitions smoothly when instability changes.
- [ ] Scars remain visible when creature is swapped between parties.
