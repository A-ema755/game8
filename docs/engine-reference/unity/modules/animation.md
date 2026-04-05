# Unity 6.3 — Animation Module Reference

**Last verified:** 2026-02-13
**Knowledge Gap:** Unity 6 animation improvements, Timeline enhancements

**Gene Forge context:** Isometric 3D creature combat. Creatures use Animator Controllers for
idle/attack/hit/death states. Timeline used for cutscenes and ability VFX sequences.
Animation Rigging used for procedural aim/look-at on biped/quadruped creature rigs.

---

## Overview

Unity 6.3 animation systems:
- **Animator Controller (Mecanim)**: State machine-based (RECOMMENDED)
- **Timeline**: Cinematic sequences, cutscenes, ability VFX
- **Animation Rigging**: Procedural runtime animation
- **Legacy Animation**: Deprecated, avoid

---

## Key Changes from 2022 LTS

### Animation Rigging Package (Production-Ready in Unity 6)

```csharp
// Install: Package Manager > Animation Rigging
// Runtime IK, aim constraints, procedural animation
```

### Timeline Improvements
- Better performance
- More track types
- Improved signal system

---

## Animator Controller (Mecanim)

### Basic Setup

```csharp
// Create: Assets > Create > Animator Controller
// Add to GameObject: Add Component > Animator
// Assign Controller: Animator > Controller = YourAnimatorController
```

### State Transitions

```csharp
Animator animator = GetComponent<Animator>();

// ✅ Trigger transition
animator.SetTrigger("Attack");   // Gene Forge: "Attack", "UseAbility", "TakeDamage", "Die"

// ✅ Bool parameter
animator.SetBool("IsIdle", true);

// ✅ Float parameter (blend trees)
animator.SetFloat("Speed", currentSpeed);

// ✅ Integer parameter
animator.SetInteger("AbilityIndex", 2);
```

### Animation Layers
- **Base Layer**: Default animations (idle, move)
- **Override Layers**: Replace base layer (attack, ability cast)
- **Additive Layers**: Add on top of base (breathing, hit flash)

```csharp
// Set layer weight (0-1)
animator.SetLayerWeight(1, 0.5f); // 50% blend
```

---

## Blend Trees

### 1D Blend Tree (Speed blending)

```csharp
// Idle (Speed = 0) → Walk (Speed = 0.5) → Run (Speed = 1.0)
animator.SetFloat("Speed", moveSpeed);
```

### 2D Blend Tree (Directional movement)

```csharp
// X-axis: Strafe (-1 to 1)
// Y-axis: Forward/Back (-1 to 1)
// Gene Forge: isometric move direction mapped to iso-space X/Y
animator.SetFloat("MoveX", isoInput.x);
animator.SetFloat("MoveY", isoInput.y);
```

---

## Animation Events

### Trigger Events from Animation Clips

```csharp
// Add in Animation window: Right-click timeline > Add Animation Event
// Must have matching method on GameObject:

public void OnAttackHit() {
    // Gene Forge: Resolve damage at hit frame — check ACC vs target DEF
    combatManager.ResolveAttackHit(this);
}

public void OnAbilityRelease() {
    // Gene Forge: Spawn ability projectile or AOE at release frame
    abilitySystem.FireQueuedAbility();
}

public void OnFootstep() {
    AudioSource.PlayClipAtPoint(footstepClip, transform.position);
}
```

---

## Root Motion

### Character Movement via Animation

```csharp
Animator animator = GetComponent<Animator>();
// Gene Forge: Creatures snap to grid tiles; root motion OFF by default.
// Enable only for special charge/lunge abilities with manual grid reconciliation.
animator.applyRootMotion = false;

void OnAnimatorMove() {
    // Custom root motion handling if needed
    transform.position += animator.deltaPosition;
    transform.rotation *= animator.deltaRotation;
}
```

---

## Animation Rigging (Unity 6+)

### IK (Inverse Kinematics)

```csharp
// Install: Package Manager > Animation Rigging
// Add: Rig Builder component + Rig GameObject

// Gene Forge use case: creature limb placement on uneven ProBuilder tile heights

TwoBoneIKConstraint ikConstraint = rig.GetComponentInChildren<TwoBoneIKConstraint>();
ikConstraint.data.target = targetTransform;
ikConstraint.weight = 1f; // 0-1 blend
```

### Aim Constraint (Look At)

```csharp
// Gene Forge: Creature head/torso aims toward attack target
MultiAimConstraint aimConstraint = rig.GetComponentInChildren<MultiAimConstraint>();
aimConstraint.data.sourceObjects[0] = new WeightedTransform(targetTransform, 1f);
```

---

## Timeline (Cutscenes / Ability VFX)

### Basic Timeline Setup

```csharp
// Create: Assets > Create > Timeline
// Add to GameObject: Add Component > Playable Director
// Assign Timeline: Playable Director > Playable = YourTimeline

PlayableDirector director = GetComponent<PlayableDirector>();
director.Play();
```

### Timeline Tracks
- **Activation Track**: Enable/disable GameObjects
- **Animation Track**: Play animations on Animator
- **Audio Track**: Synchronized audio playback
- **Cinemachine Track**: Camera movement (Gene Forge: iso camera sweeps)
- **Signal Track**: Trigger events at specific times

### Signal System (Events)

```csharp
// Gene Forge: use signals to trigger UI popups mid-cutscene
public class CutsceneEvents : MonoBehaviour {
    public void OnDialogueStart() { }
    public void OnDNARevealStart() { }   // Gene Forge: DNA unlock cinematics
}
```

---

## Animation Playback Control

### Play Animation Directly (No State Machine)

```csharp
// ✅ CrossFade (smooth transition)
animator.CrossFade("Attack", 0.2f); // 0.2s transition

// ✅ Play (instant)
animator.Play("Idle");

// ❌ Avoid: Legacy Animation component
Animation anim = GetComponent<Animation>(); // DEPRECATED
```

---

## Performance Optimization

### Culling
- `Animator > Culling Mode`:
  - **Always Animate**: Always update (expensive)
  - **Cull Update Transforms**: Stop updating bones when off-screen (RECOMMENDED)
  - **Cull Completely**: Stop all animation when off-screen

### Gene Forge Notes
- Creatures off the isometric battle grid: use **Cull Update Transforms**
- Reserve **Always Animate** only for the active creature taking its turn

---

## Common Patterns

### Check if Animation Finished

```csharp
// Gene Forge: wait for attack animation to complete before advancing turn
AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
if (stateInfo.IsName("Attack") && stateInfo.normalizedTime >= 1.0f) {
    turnManager.OnAnimationComplete();
}
```

### Override Animation Speed

```csharp
// Gene Forge: scale speed by creature SPD stat
animator.speed = Mathf.Lerp(0.7f, 1.5f, creature.Stats.SPD / 100f);
```

---

## Debugging

### Animator Window
- `Window > Animation > Animator` — visualize state machine, see active state

### Animation Window
- `Window > Animation > Animation` — edit animation clips, add events

---

## Sources
- https://docs.unity3d.com/6000.0/Documentation/Manual/AnimationOverview.html
- https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.3/manual/index.html
- https://docs.unity3d.com/Packages/com.unity.timeline@1.8/manual/index.html
