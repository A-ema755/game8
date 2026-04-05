# Unity 6 — Cinemachine (Gene Forge)

**Last verified:** 2026-02-13
**Status:** Production-Ready
**Package:** `com.unity.cinemachine` v3.0+ (Package Manager)

---

## Overview

**Cinemachine** is Unity's virtual camera system that enables professional, dynamic camera
behavior without manual scripting.

In **Gene Forge**, Cinemachine drives the isometric 3D camera rig. The primary setup is a
fixed-angle isometric virtual camera that follows the selected creature or pans across the
grid. Additional virtual cameras handle cinematic ability previews, creature capture sequences,
and DNA engineering UI zoom-ins.

**Use Cinemachine for (Gene Forge):**
- Isometric follow camera tracking the active selected creature or cursor
- Smooth pan/zoom when the player shifts grid selection
- Cinematic cameras for creature capture and DNA mutation cutscenes
- Tactical overview zoom-out at turn start
- Camera shake on high-impact ability use (impulse)

**Note:** Cinemachine 3.0 (Unity 6) is a major rewrite from 2.x. Use `CinemachineCamera`
(not `CinemachineVirtualCamera`). All examples below use the 3.0+ API.

---

## Installation

### Install via Package Manager

1. `Window > Package Manager`
2. Unity Registry > Search "Cinemachine"
3. Install `Cinemachine` (version 3.0+)

---

## Core Concepts

### 1. **Virtual Cameras**
- Define camera behavior (position, rotation, lens)
- Multiple virtual cameras can exist; only one is "live" at a time

### 2. **Cinemachine Brain**
- Component on main Camera
- Blends between virtual cameras
- Applies virtual camera settings to Unity Camera

### 3. **Priorities**
- Virtual cameras have priority values
- Highest priority camera is active
- Blends smoothly when priority changes

---

## Gene Forge Camera Rig Overview

| Virtual Camera | Priority | Purpose |
|---|---|---|
| `CamIso_Grid` | 10 | Default isometric view, follows grid cursor |
| `CamIso_Follow` | 12 | Follows active creature during its turn |
| `CamIso_ZoomOut` | 8 | Tactical overview at turn-start |
| `CamCinematic_Capture` | 20 | Creature capture cutscene |
| `CamCinematic_DNA` | 20 | DNA engineering UI close-up |
| `CamCinematic_Ability` | 18 | High-impact ability preview |

---

## Basic Setup

### 1. Add Cinemachine Brain to Main Camera

Add via Inspector: `Add Component > Cinemachine Brain`

Or it is automatically added when creating the first virtual camera from the menu.

### 2. Create Isometric Virtual Camera

`GameObject > Cinemachine > Cinemachine Camera`

Set the transform rotation to the standard isometric angle:
- **Rotation:** X: 30, Y: 45, Z: 0 (classic isometric look for Gene Forge's 3D grid)

---

## Isometric Follow Camera Setup

### CamIso_Follow — Tracks Active Creature

```csharp
using Unity.Cinemachine;

public class IsoCameraController : MonoBehaviour {
    [Header("Virtual Cameras")]
    public CinemachineCamera camIsoGrid;     // Priority 10 — default grid view
    public CinemachineCamera camIsoFollow;   // Priority 12 — follow active creature
    public CinemachineCamera camZoomOut;     // Priority 8  — tactical overview

    void Start() {
        // Start in grid overview mode
        SetGridMode();
    }

    /// <summary>Switch to follow the creature taking its turn.</summary>
    public void SetFollowTarget(Transform creatureTransform) {
        camIsoFollow.Follow = creatureTransform;
        camIsoFollow.Priority = 15; // Activate follow camera
    }

    /// <summary>Return to free-pan grid camera.</summary>
    public void SetGridMode() {
        camIsoFollow.Priority = 5;
        camIsoGrid.Priority = 10;
    }

    /// <summary>Zoom out to tactical overview at turn start.</summary>
    public void SetTacticalOverview() {
        camZoomOut.Priority = 14;
    }

    public void ClearTacticalOverview() {
        camZoomOut.Priority = 8;
    }
}
```

---

## Isometric Camera Body Configuration

For the isometric camera rig, configure the body component in the Inspector as follows:

```
CinemachineCamera (CamIso_Grid)
  Body: Position Composer
    - Tracked Object Offset:  (0, 1.5, 0)   ← aim at creature torso, not feet
    - Dead Zone Width/Height: 0.15           ← small dead zone to reduce micro-jitter
    - Horizontal/Vertical Damping: 0.3       ← smooth grid panning feel
  Aim: Do Nothing                            ← isometric camera never rotates to track
  Lens:
    - Field of View: 35                      ← compressed isometric perspective
    - Near Clip Plane: 0.3
    - Far Clip Plane: 100
```

For the follow camera during a creature's turn:

```
CinemachineCamera (CamIso_Follow)
  Body: Position Composer
    - Screen Position: (0.5, 0.45)           ← slightly below center for context
    - Horizontal Damping: 0.4
    - Vertical Damping: 0.4
  Aim: Do Nothing
  Lens:
    - Field of View: 30                      ← tighter than grid view
```

---

## Grid Cursor Pan (No Follow Target)

For panning the camera with the Input System based on grid selection:

```csharp
using Unity.Cinemachine;
using UnityEngine;

public class GridCameraTarget : MonoBehaviour {
    [SerializeField] float _panSpeed = 8f;
    private Vector3 _targetPosition;

    /// <summary>Called by GridInputHandler when selection cursor moves.</summary>
    public void MoveTo(Vector3 worldPosition) {
        _targetPosition = worldPosition;
    }

    void Update() {
        transform.position = Vector3.Lerp(transform.position, _targetPosition,
            Time.deltaTime * _panSpeed);
    }
}
```

Assign the `GridCameraTarget` GameObject as the `Follow` target of `CamIso_Grid`.

---

## Camera Shake (Impulse)

### Setup Impulse Source on Ability Effects

```csharp
using Unity.Cinemachine;
using UnityEngine;

public class AbilityImpact : MonoBehaviour {
    [SerializeField] CinemachineImpulseSource _impulseSource;

    public void TriggerImpact(float strength) {
        _impulseSource.GenerateImpulseWithForce(strength);
    }
}
```

### Add Impulse Listener to Isometric Cameras

On each isometric virtual camera, add:
`Add Component > CinemachineImpulseListener`

Use **Reaction** settings:
- `Amplitude Gain`: 0.4 (isometric shake is subtle — camera is far from action)
- `Frequency Gain`: 1.0

---

## Cinematic Cameras (Cutscenes)

### Creature Capture Sequence

```csharp
public class CaptureSequenceCamera : MonoBehaviour {
    public CinemachineCamera camCinematicCapture;

    public void PlayCaptureCamera(Transform capturedCreature) {
        camCinematicCapture.Follow = capturedCreature;
        camCinematicCapture.LookAt = capturedCreature;
        camCinematicCapture.Priority = 20; // Override all other cameras
    }

    public void EndCaptureCamera() {
        camCinematicCapture.Priority = 0;
    }
}
```

In the Inspector, configure `CamCinematic_Capture`:
```
Body: 3rd Person Follow
  - Camera Distance: 2.5
  - Shoulder Offset: (0.3, 0.5, 0)
  - Vertical Damping: 0.2
Aim: Composer
  - Tracked Object Offset: (0, 0.8, 0)     ← aim at creature's center mass
Lens: FOV 45
```

### DNA Engineering UI Zoom-In

```csharp
public void ShowDNACamera(Transform creatureForEngineering) {
    _camCinematicDNA.Follow = creatureForEngineering;
    _camCinematicDNA.LookAt = creatureForEngineering;
    _camCinematicDNA.Priority = 20;
}

public void HideDNACamera() {
    _camCinematicDNA.Priority = 0;
}
```

---

## Blending Between Cameras

### Priority-Based Blending (Standard Gene Forge Flow)

```csharp
// Turn start: zoom out for tactical view
camZoomOut.Priority = 14;

// Unit selected: follow the active creature
camIsoFollow.Follow = activeCreature.transform;
camIsoFollow.Priority = 15;

// Unit action complete: return to grid
camIsoFollow.Priority = 5;
camZoomOut.Priority = 8;
camIsoGrid.Priority = 10;
```

### Custom Blend Times

Create a Cinemachine Blender Settings asset:
`Assets > Create > Cinemachine > Cinemachine Blender Settings`

Assign to `Cinemachine Brain > Custom Blends`.

Recommended Gene Forge blend times:
- Grid ↔ Follow: 0.35s (quick response to turn changes)
- Any ↔ Cinematic Capture: 0.6s (smooth dramatic entry)
- Any ↔ DNA UI: 0.4s

---

## Timeline Integration (Cutscenes)

For longer creature capture or DNA reveal cutscenes:

```
1. Create Timeline: Assets > Create > Timeline
2. Add Cinemachine Track
3. Add CamCinematic_Capture and CamCinematic_DNA as clips
4. Timeline blends between cameras automatically
```

---

## Migration from Cinemachine 2.x

```csharp
// OLD (Cinemachine 2.x — do not use):
CinemachineVirtualCamera vcam;
vcam.m_Follow = creatureTransform;

// NEW (Cinemachine 3.0+ — Unity 6):
CinemachineCamera vcam;
vcam.Follow = creatureTransform; // No "m_" prefix
```

**Major API Changes:**
- `CinemachineVirtualCamera` → `CinemachineCamera`
- `m_Follow`, `m_LookAt` → `Follow`, `LookAt`
- Namespace: `using Unity.Cinemachine;` (not `using Cinemachine;`)
- Body/Aim sub-components reorganized in Inspector

---

## Performance Tips

- Keep inactive virtual cameras at **Priority 0** rather than disabling GameObjects
- Only one virtual camera should be "active" (highest priority) at a time
- Disable `CamCinematic_*` cameras entirely when not in a cutscene sequence
- Isometric cameras with `Do Nothing` aim mode cost less than Composer aim

---

## Debugging

### Cinemachine Debugger

`Window > Analysis > Cinemachine Debugger`

Shows active camera, blend info, and shot quality. Useful for verifying the correct
isometric camera is live during each game phase.

---

## Sources
- https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/manual/index.html
- https://learn.unity.com/tutorial/cinemachine
