# Procedural Animation System

## 1. Overview

The Procedural Animation System blends creature animations based on equipped body parts rather than using fixed per-species animations. Parts like Wings, Heavy Shells, Extra Limbs, and Tails modify idle, walk, and attack animations procedurally. Animation playback speed scales with creature size. The result: no two heavily modified creatures animate identically. The system is post-MVP and allows creature customization to manifest visually in movement and combat.

## 2. Player Fantasy

A creature with Wings walks differently than one without. A tiny creature moves fast, a huge one lumbers. A creature with Extra Limbs swings more arms in attack animations. Every visual customization creates a uniquely animated creature, making heavily modified teams feel genuinely distinct.

## 3. Detailed Rules

### 3.1 Animation Blend Tree

Each creature has a blend tree that layers animations based on equipped parts:

```csharp
[System.Serializable]
public class AnimationBlendProfile
{
    public string creatureId;
    public Animator animator;
    
    // Base animations (all creatures)
    public AnimationClip idleBase;
    public AnimationClip walkBase;
    public AnimationClip attackBase;
    
    // Part-specific overlays
    public Dictionary<string, AnimationClip> partIdleOverrides;  // wings-feathered -> wing-flap-idle
    public Dictionary<string, AnimationClip> partWalkOverrides;
    public Dictionary<string, AnimationClip> partAttackOverrides;
}
```

### 3.2 Part Animation Contributions

| Part | Idle Effect | Walk Effect | Attack Effect |
|------|------------|------------|---------------|
| Wings | Flapping wings | Graceful glide-step | Aerial spin attack |
| Heavy Shell | Stationary, grounded | Slow, heavy steps | Armored bash |
| Extra Limbs | Multiple appendage twitch | Multi-limb walk | Multi-swing attack |
| Tail Blade | Tail swaying | Tail balancing | Tail whip combo |
| Small Size | Bouncy, quick | Rapid shuffling | Quick jabs |
| Large Size | Majestic pose | Lumbering strides | Heavy slow swings |

### 3.3 Animation Blending

```csharp
public void UpdateCreatureAnimation(CreatureInstance creature)
{
    var profile = animationBlendDatabase.Get(creature.speciesId);
    
    // Collect active part modifications
    List<AnimationClip> activeClips = new() { profile.idleBase };
    foreach (var part in creature.equippedParts)
    {
        if (profile.partIdleOverrides.TryGetValue(part.id, out var partClip))
            activeClips.Add(partClip);
    }
    
    // Blend clips together (average positions/rotations)
    AnimationClip blendedClip = BlendAnimationClips(activeClips);
    
    // Set animator
    animator.SetClip(blendedClip);
}
```

### 3.4 Playback Speed Scaling

```csharp
public void SetAnimationSpeed(CreatureInstance creature)
{
    // Size mods affect speed
    float sizeMultiplier = creature.GetSizeMultiplier();
    
    // Small creatures: 1.3x speed
    // Large creatures: 0.7x speed
    
    animator.speed = 1.0f * sizeMultiplier;
}
```

### 3.5 Combat Animation Layering

During combat, attacks layer additional animations:

- **Base attack:** Punch/slash default
- **Part overlay:** Wings add spin, claws add slash lines, etc.
- **Status effect:** Burn adds flame particles, poison adds cloud, etc.

## 4. Formulas

### Blend Weight Per Part

```
totalParts = equippedPartsCount
weightPerPart = 1.0 / (totalParts + 1)  [+1 for base animation]
baseAnimationWeight = 1.0
partAnimationWeight = weightPerPart (each part contributes equally)
```

### Playback Speed Scaling

```
baseSPD = 1.0
sizeMultiplier = (creature.sizeModifier >= 1.0) ? 1.0 + (size - 1) * 0.3 : 1.0 - (1 - size) * 0.3
finalSpeed = baseSPD * sizeMultiplier
// Small (0.5): 0.85x speed
// Normal (1.0): 1.0x speed
// Large (1.5): 1.15x speed
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Creature has no parts equipped | Base animation plays normally |
| Creature equips conflicting part (e.g., heavy shell + wings) | Conflict is prevented by part system; blending doesn't occur |
| Animation blend has > 4 parts | Blend still occurs; weights are normalized; may visually overlap |
| Creature size is modified mid-battle | Playback speed doesn't change mid-battle; applies next encounter |
| Part is unequipped mid-animation | Animation seamlessly transitions back to base |
| Creature animation not found in database | Fallback to base animation (no crash) |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Body Part System | Equipped parts drive animation selection |
| Creature Instance | Reads size, equipped parts for blending |
| Animator Controller | Unity Animator integration for playback |
| Size & Proportion System | Size multiplier affects animation speed |
| Combat System | Attack animations triggered per move |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `baseAnimationSpeed` | 1.0 | Normal playback speed |
| `smallSizeSpeedBonus` | 1.3 | Small creatures (0.5 size) |
| `largeSizeSpeedPenalty` | 0.7 | Large creatures (1.5 size) |
| `partAnimationBlendWeight` | Equal | Each part contributes equally |
| `maxAnimationBlends` | 4 | Hard cap on simultaneous part animations |
| `animationTransitionTime` | 0.2s | Blend duration for animation changes |

## 8. Acceptance Criteria

- [ ] Creatures with no parts play base animation normally.
- [ ] Equipped parts contribute visual animation overlays (idle, walk, attack).
- [ ] Animation blending combines multiple part clips into single output.
- [ ] Playback speed scales with creature size (small=faster, large=slower).
- [ ] Wings add flap/glide animations to idle and walk.
- [ ] Heavy Shell adds slow, grounded movement.
- [ ] Extra Limbs add multiple appendage movements.
- [ ] Tail Blade adds tail-swing attack animation.
- [ ] Animation transitions smoothly when parts are equipped/unequipped.
- [ ] No two heavily modified creatures animate identically (blending creates variation).
- [ ] Combat attacks layer part effects (claw slashes, wing spins, etc.).
- [ ] Animation data persists through save/load (creature state determines animation).
