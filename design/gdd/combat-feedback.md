# Combat Feedback

## 1. Overview

Combat feedback is the layer of visual and audio responses that make every action in combat feel impactful and readable. It covers damage number popups, type effectiveness text callouts, screen shake, creature hit flash, death animations, the capture sequence, and instability visual glitches. All feedback is triggered by events from the Turn Manager and Damage system, keeping feedback logic decoupled from game logic. Effects use Unity's particle system and coroutines; no feedback element blocks combat resolution.

## 2. Player Fantasy

Every hit should feel satisfying. A super-effective strike should feel dramatically different from a glancing blow — the damage number is bigger, redder, and the screen shakes harder. When a capture succeeds, the sequence builds tension through trap shakes and resolves with a sparkling burst of light. Death animations should feel conclusive but not grim — a creature fading and sinking is respectful, not brutal. The overall feel is "animated comic book" — punchy, readable, expressive.

## 3. Detailed Rules

### Damage Popups
- Spawned as a world-space canvas element above the damaged creature's position.
- Float upward 1.2 units over 0.8s, then fade out over 0.3s.
- Font size scales with damage magnitude:
  - 1–10 damage: small (24pt)
  - 11–30 damage: medium (32pt)
  - 31–60 damage: large (40pt)
  - 61+ damage: extra large (52pt)

**Color coding by type effectiveness:**
| Effectiveness | Color | Example |
|--------------|-------|---------|
| Super effective (2x+) | Red (#FF3333) | "64!" |
| Neutral (1x) | White (#FFFFFF) | "32" |
| Not very effective (0.5x–) | Blue (#6699FF) | "16" |

Note: No "Immune" or "0 damage" effectiveness exists — the 14-type chart has no immunities (minimum 0.25x for dual-type resisted).

- Critical hits add a "★" prefix to the number in gold: "★48"
- Healing numbers display in green (#33FF77) with a "+" prefix: "+25"
- Status effect damage (burn, poison) uses the status type's color, smaller font (20pt), italic.

### Type Effectiveness Callouts
- "Super Effective!" — rendered in screen space above the target, bold red text.
  - Entry animation: scale 0.6 → 1.1 → 1.0 over 0.2s (overshoot bounce).
  - Linger 1.4s, then fade out over 0.3s.
- "Not Very Effective..." — blue text, scale 1.0 → 0.9 over 0.15s (deflate).
  - Linger 1.4s, fade out 0.3s.
- No "No Effect!" callout — the 14-type chart has no immunities.
- Callouts queue vertically if multiple hits land within 0.5s of each other. Second callout offsets Y by +0.6 units.

### Form-Specific Hit VFX
Each damage form has a distinct visual effect on hit, layered with the genome type's color:

| Form | VFX Description | Particle Style |
|------|----------------|---------------|
| **Physical** | Impact burst — dirt/debris particles radiate outward from hit point | Short, sharp, opaque; brown/gray base tinted by genome type color |
| **Energy** | Beam/bolt flash — bright flash at impact with trailing energy wisps | Bright, emissive; color matches genome type (Thermal=orange, Cryo=blue, etc.) |
| **Bio** | Spore/infection spread — small organic particles drift outward slowly | Soft, translucent; green/purple base tinted by genome type color |

The form VFX plays simultaneously with the hit flash and damage popup. It is purely cosmetic and does not affect timing.

### Screen Shake
Triggered by `CombatFeedbackManager.TriggerShake(intensity, duration)`.

| Trigger | Intensity | Duration |
|---------|-----------|----------|
| Light hit (1–15 damage) | 0.05 | 0.15s |
| Medium hit (16–40 damage) | 0.12 | 0.25s |
| Heavy hit (41+ damage) | 0.22 | 0.40s |
| Critical hit | +0.08 bonus | +0.1s bonus |
| Creature death | 0.18 | 0.30s |
| Capture fail (trap break) | 0.06 | 0.20s |

Shake implementation: Cinemachine `CinemachineBasicMultiChannelPerlin` noise amplitude override, ramped in over first 10% of duration, ramped out over last 30%.

Screen shake is a player-toggleable setting (see Settings System). When disabled, intensity = 0 for all triggers.

### Creature Hit Flash
- On any hit, the creature's renderer materials switch to a white emissive material for 0.1s, then revert.
- Implementation: swap `Material` references on all `MeshRenderer` components of the creature model.
- Flash does not interrupt move or idle animations — it is applied as a material override.

```csharp
public IEnumerator FlashHit(CreatureView creatureView)
{
    creatureView.SetMaterialOverride(_whiteMaterial);
    yield return new WaitForSeconds(0.1f);
    creatureView.ClearMaterialOverride();
}
```

### Death Animation
- Triggered when a creature's HP reaches 0.
- Sequence:
  1. Play death SFX (creature-specific cry, lower pitch).
  2. Fade creature model alpha from 1.0 → 0.0 over 0.6s using material `_BaseColor.a`.
  3. Simultaneously translate creature model Y down by 0.5 units over 0.6s.
  4. At alpha = 0, disable the creature's GameObject.
  5. Combat continues only after death animation completes (brief blocking pause, max 0.8s).
- Death screen shake fires at animation start (not end).
- If combat speed is 2x or 4x, death animation is scaled proportionally (0.3s at 2x, 0.15s at 4x).

### Capture Sequence

**Step 1 — Trap Throw**
- Gene Trap projectile spawns at the player's active creature position and arcs toward the target using a Bezier curve over 0.5s.
- Trap throw SFX plays on launch.

**Step 2 — Trap Land**
- Trap lands and the target creature is sucked into the trap with a light flash and vacuum particle effect (inward particle burst, 0.15s).
- Capture attempt SFX plays.

**Step 3 — Shake Phase**
- Trap shakes 1–3 times based on catch roll progress (the closer to success, the more shakes).
- Each shake: translate X ±0.15 units over 0.1s, repeat.
- Shake SFX plays per shake.

```
shakeCount:
  catchRoll / catchThreshold < 0.33  → 1 shake
  catchRoll / catchThreshold < 0.66  → 2 shakes
  catchRoll / catchThreshold >= 0.66 → 3 shakes
  (capture success regardless of count if roll >= threshold)
```

**Step 4a — Success**
- After final shake, trap snaps shut with a click SFX.
- Gold sparkle particle burst (radial, 0.3s duration, 24 particles).
- Screen flash: white alpha 0.4 → 0.0 over 0.5s.
- Success SFX (chime).
- Creature name appears in a "Captured!" banner overlay (1.5s).
- Trap shrinks to 0 scale over 0.5s and disappears.

**Step 4b — Failure (Trap Break)**
- Trap bursts open with a crack SFX.
- Red particle burst (small, 0.2s).
- Screen shake (light).
- Creature re-materializes with a flash (reverse of capture flash).
- Target creature returns to its grid position.

### Instability Visual Glitch
Triggered when a creature's instability >= 50.

- Low instability glitch (50–74): creature model UV offset flickers for 0.05s every 4s. Single-frame color noise on model texture.
- High instability glitch (75–99): UV flicker every 2s, color noise more intense, brief screen space chromatic aberration (post-process, 0.1s, strength 0.3).
- Max instability (100): constant mild UV flicker, particle "static" effect floating around creature model, red eye glow on creature head mesh.

Implementation: `InstabilityFeedbackController` component on each creature view; reads instability value each frame and drives shader parameter `_GlitchIntensity` (0–1).

## 4. Formulas

**Damage Popup Scale:**
```
if damage <= 10: fontSize = 24
else if damage <= 30: fontSize = 32
else if damage <= 60: fontSize = 40
else: fontSize = 52
```

**Screen Shake Intensity:**
```
baseIntensity = LookupTable(damage)   // from table above
if isCritical: intensity += 0.08
if screenShakeEnabled == false: intensity = 0
duration = baseDuration + (isCritical ? 0.1 : 0)
```

**Shake Count for Capture:**
```
progress = catchRoll / catchThreshold
shakeCount = progress < 0.33 ? 1 : progress < 0.66 ? 2 : 3
```

**Death Animation Duration (speed-adjusted):**
```
adjustedDuration = 0.6 / combatSpeedMultiplier
```

**Glitch Intensity:**
```
glitchIntensity = Clamp((instability - 50) / 50f, 0, 1)
// 0.0 at instability=50, 1.0 at instability=100
```

## 5. Edge Cases

- **Multiple creatures hit simultaneously (AoE):** Each creature gets its own damage popup and hit flash. Screen shake fires once for the heaviest single hit in the AoE, not additive.
- **Heavily resisted hit (0.25x dual-type):** Light screen shake. Blue small-font popup appears. "Not Very Effective..." callout. No "Immune" or "No Effect" state exists in the 14-type chart.
- **Creature faints mid-capture sequence:** If the creature's HP somehow reaches 0 during the capture animation window, abort capture and play death animation instead. This shouldn't happen under normal rules (capture requires >0 HP) but guard against it.
- **Screen shake with disabled setting:** All `TriggerShake` calls become no-ops. No camera movement occurs.
- **Capture at 4x speed:** All capture sequence timings scale by 1/4. Shakes are visually compressed but still occur. Success sparkle duration: 0.075s.
- **Death animation interrupted by another death (AoE kill):** Queue death animations; play them sequentially with 0.1s offset between each, capped at 2 simultaneous.
- **Instability glitch during death animation:** Stop glitch effects immediately when death animation begins. Do not run both simultaneously.
- **Healing while at full HP:** No popup, no flash. Silently ignored visually.

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Turn Manager | Fires events that trigger feedback (OnHit, OnDeath, OnCapture, etc.) |
| Damage & Health System | Provides damage value, genome type, damage form, crit flag for popup coloring/sizing and form VFX selection |
| Capture System | Provides catch roll result and shake count for capture sequence |
| Type Chart System | Provides effectiveness multiplier for callout selection |
| Creature Instance | Source for instability value driving glitch effects |
| Settings System | Screen shake toggle, combat speed multiplier |
| Audio System | All SFX calls routed through AudioManager |
| VFX System | Particle effects (sparkle, vacuum, static) managed by VFX pool |
| Combat UI | Type callout placement coordinated with UI overlay layer |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `damagePopupFloatHeight` | 1.2 units | How high popups float |
| `damagePopupFloatDuration` | 0.8s | Float time before fade |
| `damagePopupFadeDuration` | 0.3s | Fade out time |
| `hitFlashDuration` | 0.1s | White material override time |
| `deathFadeDuration` | 0.6s | Fade + sink time |
| `deathSinkDistance` | 0.5 units | Y translation during death |
| `captureThrowDuration` | 0.5s | Trap arc travel time |
| `captureSuccessSparkleCount` | 24 | Particles in success burst |
| `captureSuccessFlashAlpha` | 0.4 | White screen flash max alpha |
| `instabilityGlitchLowThreshold` | 50 | Start of UV flicker |
| `instabilityGlitchHighThreshold` | 75 | Start of chromatic aberration |
| `glitchLowInterval` | 4s | Seconds between low glitch ticks |
| `glitchHighInterval` | 2s | Seconds between high glitch ticks |
| `screenShakeHeavyThreshold` | 41 | Damage above which heavy shake fires |
| `screenShakeMediumThreshold` | 16 | Damage above which medium shake fires |

## 8. Acceptance Criteria

- [ ] Damage numbers appear above the target, float upward, and fade out correctly.
- [ ] Red damage numbers appear for super-effective hits; blue for resisted hits; white for neutral.
- [ ] Critical hit numbers display a gold "★" prefix.
- [ ] "Super Effective!" and "Not Very Effective..." callouts appear with the correct animations.
- [ ] Screen shake fires on hits with intensity proportional to damage; does not fire when screen shake is disabled in settings.
- [ ] Creature model flashes white for exactly 0.1s on any hit.
- [ ] Death animation fades and sinks the model, then disables the GameObject.
- [ ] Capture sequence shows 1–3 shakes based on catch roll progress.
- [ ] Capture success shows gold sparkle burst and white screen flash.
- [ ] Capture failure shows red particle burst and creature re-materializes.
- [ ] Instability glitch (UV flicker) activates at instability >= 50.
- [ ] Chromatic aberration post-process activates at instability >= 75.
- [ ] All feedback timing scales correctly at 2x and 4x combat speed.
- [ ] AoE hits produce individual popups per creature but only one screen shake.
- [ ] Physical form hits display impact/debris VFX.
- [ ] Energy form hits display beam/bolt flash VFX tinted by genome type color.
- [ ] Bio form hits display spore/infection VFX.
- [ ] No "Immune" or "No Effect" callout ever appears (14-type chart has no immunities).
