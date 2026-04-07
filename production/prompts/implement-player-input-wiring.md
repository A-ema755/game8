# Implementation Prompt: Player Input → TurnAction Wiring

## Agent

Invoke **team-ui** skill: `/team-ui Player Input Wiring`

Team composition for this feature:
- **ux-designer** — Validate input flow: click sequences, targeting modes, visual feedback
- **ui-programmer** — Wire MoveSelectionPanel buttons → TurnAction construction → TurnManager submission
- **gameplay-programmer** — Implement targeting mode state machine and input validation

> **Note**: This is primarily a UI interaction system. team-ui handles the input → action pipeline. The gameplay-programmer ensures TurnAction invariants are met (valid targets, range checks, PP checks).

## Objective

Implement the **player input pipeline** that translates UI interactions (move button clicks, tile clicks, creature clicks) into valid `TurnAction` structs submitted to TurnManager. This is the interactive loop during `PlayerCreatureSelect` phase — the player selects actions for each creature, then confirms to advance.

## Authoritative Design Source

- `design/gdd/combat-ui.md` §3 — Move selection panel, targeting modes, input states
- `design/gdd/turn-manager.md` §3.3 — TurnAction struct invariants
- `design/gdd/turn-manager.md` §3.4 — Split turn execution (movement + action)

## What Already Exists

### TurnAction struct (already implemented):
```csharp
public readonly struct TurnAction
{
    public readonly ActionType Action;
    public readonly Vector2Int? MovementTarget;
    public readonly MoveConfig Move;
    public readonly CreatureInstance Target;
    public readonly TileData TargetTile;
    public readonly int MovePPSlot;
    public readonly bool Suppressed;
}
```

### UI panels (already implemented, need wiring):
- `Assets/Scripts/UI/MoveSelectionPanelController.cs` — 4 move buttons + Gene Trap + Switch
- `Assets/Scripts/UI/TileHighlightController.cs` — Blue (movement), Red (attack), Green (capture)
- `Assets/Scripts/UI/SwitchOverlayController.cs` — Party swap overlay
- `Assets/Scripts/UI/CreatureInfoPanelController.cs` — Shows selected creature stats

### Combat systems (already implemented):
- `Assets/Scripts/Gameplay/Grid/GridSystem.cs` — `GetReachableTiles()`, `GetTilesInRange()`, LoS queries
- `Assets/Scripts/Creatures/CreatureInstance.cs` — `LearnedMoveIds`, `LearnedMovePP`, `ComputedStats`
- `Assets/Scripts/Creatures/MoveConfig.cs` — `TargetType`, `Range`, `Form`

## Scope — What to Implement

### 1. `Assets/Scripts/UI/PlayerInputController.cs`

**MonoBehaviour** in `GeneForge.UI` namespace. Implements `IPlayerInputProvider`.

**State machine:**
```
Idle → SelectingCreature → SelectingAction → SelectingMoveTarget → SelectingMovement → Confirming
```

**Per-creature input flow:**
1. Highlight active creature (gold border)
2. Show reachable movement tiles (blue highlights)
3. Player optionally clicks movement tile → set `MovementTarget`
4. MoveSelectionPanel activates:
   - Click move button → enter `SelectingMoveTarget` state
   - Click Gene Trap → enter capture targeting state
   - Click Switch → open SwitchOverlay
   - Click Wait → submit `ActionType.Wait`
5. In `SelectingMoveTarget`:
   - Show attack range tiles (red highlights) based on `MoveConfig.TargetType` and `Range`
   - For `TargetType.Single`: click enemy creature → set `Target`
   - For `TargetType.AoE`/`Line`: click tile → set `TargetTile`
6. Validate TurnAction invariants → add to action dictionary
7. Advance to next creature or enter `Confirming` if all creatures have actions

**Validation rules (from GDD §3.3):**
- UseMove: `Move` non-null, `MovePPSlot` 0–3, PP > 0, target in range
- Capture: `Target` non-null, target is enemy, encounter allows capture
- Flee: `MovementTarget` must be null (consumes entire turn)
- Wait: all optional fields null

### 2. `Assets/Scripts/UI/TargetingHelper.cs`

**Pure C# class** in `GeneForge.UI` namespace. Calculates valid targets for a move.

```csharp
public static class TargetingHelper
{
    /// Returns valid target tiles for a move from actor's position.
    public static List<Vector2Int> GetValidTargetTiles(
        MoveConfig move, CreatureInstance actor, GridSystem grid);
    
    /// Returns valid creature targets for a single-target move.
    public static List<CreatureInstance> GetValidCreatureTargets(
        MoveConfig move, CreatureInstance actor, GridSystem grid,
        IReadOnlyList<CreatureInstance> enemies);
    
    /// Returns reachable movement tiles for a creature.
    public static List<Vector2Int> GetMovementTiles(
        CreatureInstance creature, GridSystem grid, int movementDivisor);
}
```

### 3. Tests

- `Assets/Tests/EditMode/TargetingHelperTests.cs` — Range calculations, LoS filtering, movement range
- `Assets/Tests/EditMode/PlayerInputControllerTests.cs` — TurnAction construction, validation rules

### Out of scope:
- Undo/redo individual creature actions (MVP: restart all)
- Drag-to-move (click-only for MVP)
- Move preview damage estimation tooltip
- AoE/Line targeting preview visualization (just highlight tiles)

## Constraints

- **PlayerInputController**: MonoBehaviour (needs UI event hookup)
- **TargetingHelper**: Pure C# static class (testable)
- **Namespace**: `GeneForge.UI`
- **No game logic** — only builds TurnActions, never modifies state
- **XML doc comments** on all public API
- Follow collaboration protocol: Question → Options → Decision → Draft → Approval

## Verification

- Run `/code-review` on all new files
- Unity compilation: 0 errors
- Manual test: click creature → see movement tiles → click move → see range → click target → action submitted

## Branch

Create branch `feature/Player-Input-Wiring` from `main`.
