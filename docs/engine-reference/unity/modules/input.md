# Unity 6.3 — Input Module Reference

**Last verified:** 2026-02-13
**Knowledge Gap:** Unity 6 uses new Input System (legacy Input deprecated)

**Gene Forge context:** Isometric 3D tactical grid combat. Input actions include tile cursor
movement (WASD/arrow), tile select/confirm (click/button), camera pan/rotate (RMB drag / Q/E),
ability selection (1-4 / face buttons), and menu navigation. Input System package is the
required approach — never use legacy `Input` class.

---

## Overview

Unity 6 input systems:
- **Input System Package** (REQUIRED for Gene Forge): Cross-platform, rebindable, modern
- **Legacy Input Manager**: Deprecated, do not use

---

## Key Changes from 2022 LTS

### Legacy Input Deprecated in Unity 6

```csharp
// ❌ DEPRECATED: Do not use in Gene Forge
if (Input.GetKeyDown(KeyCode.Space)) { }

// ✅ CORRECT: Input System package
using UnityEngine.InputSystem;
if (Keyboard.current.spaceKey.wasPressedThisFrame) { }
```

**Migration Required:** Install `com.unity.inputsystem` package.

---

## Input System Package Setup

### Installation
1. `Window > Package Manager`
2. Search "Input System"
3. Install package
4. Restart Unity when prompted

### Enable New Input System
`Edit > Project Settings > Player > Active Input Handling`:
- **Input System Package (New)** — required for Gene Forge

---

## Gene Forge Input Actions

### Recommended Input Actions Layout

```
Action Maps:
  BattleGrid
    Actions:
      - MoveCursor       (Value, Vector2)   — WASD / arrow keys / left stick
      - ConfirmSelect    (Button)           — Enter / left click / A button
      - CancelBack       (Button)           — Escape / right click / B button
      - AbilitySlot1-4   (Button)           — 1-4 / Y/X/LB/RB
      - CameraRotate     (Value, Vector2)   — RMB drag / right stick
      - CameraPan        (Value, Vector2)   — MMB drag / optional
      - EndTurn          (Button)           — Space / Start button

  Menus
    Actions:
      - Navigate         (Value, Vector2)
      - Submit           (Button)
      - Cancel           (Button)
```

### Use Generated Input Class

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleInputHandler : MonoBehaviour {
    private GeneForgeControls controls;

    void Awake() {
        controls = new GeneForgeControls();

        controls.BattleGrid.ConfirmSelect.performed += ctx => OnConfirmSelect();
        controls.BattleGrid.CancelBack.performed += ctx => OnCancelBack();
        controls.BattleGrid.EndTurn.performed += ctx => OnEndTurn();

        // Ability slots
        controls.BattleGrid.AbilitySlot1.performed += ctx => OnAbilitySlot(0);
        controls.BattleGrid.AbilitySlot2.performed += ctx => OnAbilitySlot(1);
        controls.BattleGrid.AbilitySlot3.performed += ctx => OnAbilitySlot(2);
        controls.BattleGrid.AbilitySlot4.performed += ctx => OnAbilitySlot(3);
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Update() {
        // Cursor movement (continuous)
        Vector2 cursorInput = controls.BattleGrid.MoveCursor.ReadValue<Vector2>();
        gridCursor.Move(cursorInput);

        // Camera rotation
        Vector2 camInput = controls.BattleGrid.CameraRotate.ReadValue<Vector2>();
        isoCamera.ApplyRotationInput(camInput);
    }

    void OnConfirmSelect() { gridCursor.Confirm(); }
    void OnCancelBack()    { gridCursor.Cancel(); }
    void OnEndTurn()       { turnManager.EndPlayerTurn(); }
    void OnAbilitySlot(int slot) { abilityUI.SelectSlot(slot); }
}
```

---

## Direct Device Access (Quick & Dirty)

### Keyboard

```csharp
using UnityEngine.InputSystem;

void Update() {
    if (Keyboard.current.spaceKey.wasPressedThisFrame) { }
    if (Keyboard.current.escapeKey.wasPressedThisFrame) { }
}
```

### Mouse

```csharp
using UnityEngine.InputSystem;

void Update() {
    Vector2 mousePos = Mouse.current.position.ReadValue();

    // Gene Forge: tile picking via ray from iso camera
    if (Mouse.current.leftButton.wasPressedThisFrame) {
        gridCursor.TryPickTileAtScreenPos(mousePos);
    }

    Vector2 scroll = Mouse.current.scroll.ReadValue();
}
```

### Gamepad

```csharp
using UnityEngine.InputSystem;

void Update() {
    Gamepad gamepad = Gamepad.current;
    if (gamepad == null) return;

    if (gamepad.buttonSouth.wasPressedThisFrame) { OnConfirmSelect(); } // A/Cross
    if (gamepad.buttonEast.wasPressedThisFrame)  { OnCancelBack(); }    // B/Circle

    Vector2 leftStick = gamepad.leftStick.ReadValue();
    // Gene Forge: map stick to 8-directional iso grid cursor movement
}
```

---

## Input Action Callbacks

```csharp
// started: Input began
controls.BattleGrid.ConfirmSelect.started   += ctx => Debug.Log("Select started");

// performed: Action triggered
controls.BattleGrid.ConfirmSelect.performed += ctx => Debug.Log("Select performed");

// canceled: Released or interrupted
controls.BattleGrid.ConfirmSelect.canceled  += ctx => Debug.Log("Select canceled");
```

---

## Control Schemes & Device Switching

```
Control Schemes:
  - Keyboard&Mouse (Keyboard, Mouse)
  - Gamepad (Gamepad)
```

```csharp
controls.BattleGrid.MoveCursor.performed += ctx => {
    if (ctx.control.device is Keyboard) {
        // WASD grid cursor
    } else if (ctx.control.device is Gamepad) {
        // Stick grid cursor
    }
};
```

---

## Rebinding (Runtime Key Mapping)

```csharp
public void RebindConfirmKey() {
    var rebindOp = controls.BattleGrid.ConfirmSelect.PerformInteractiveRebinding()
        .WithControlsExcluding("Mouse")
        .OnComplete(op => { op.Dispose(); })
        .Start();
}

// Save/Load
string rebinds = controls.SaveBindingOverridesAsJson();
PlayerPrefs.SetString("InputBindings", rebinds);
controls.LoadBindingOverridesFromJson(PlayerPrefs.GetString("InputBindings"));
```

---

## PlayerInput Component (Simplified Setup)

```csharp
// Gene Forge: use PlayerInput for simple single-player setup
public class Player : MonoBehaviour {
    public void OnMoveCursor(InputValue value) {
        Vector2 move = value.Get<Vector2>();
        gridCursor.Move(move);
    }

    public void OnConfirmSelect(InputValue value) {
        if (value.isPressed) gridCursor.Confirm();
    }
}
```

---

## Debugging

- `Window > Analysis > Input Debugger` — see active devices, input values, action states

---

## Sources
- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/index.html
- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/QuickStartGuide.html
