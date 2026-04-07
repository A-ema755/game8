# Implementation Prompt: Combat Controller Orchestration

## Agent

Invoke **team-combat** skill: `/team-combat Combat Controller`

Team composition for this feature:
- **game-designer** — Verify phase flow matches GDD turn sequence, validate input timing rules
- **gameplay-programmer** — Implement CombatController MonoBehaviour, wire TurnManager ↔ UI ↔ player input
- **ui-programmer** — Ensure UI panels respond correctly to phase transitions and events
- **qa-tester** — Write integration test checklist, verify event flow end-to-end

> **Note**: This is the glue system connecting all existing combat pieces. team-combat is correct because it crosses combat logic, UI, and AI. The gameplay-programmer leads implementation; ui-programmer validates UI integration.

## Objective

Implement the **CombatController** — the MonoBehaviour orchestrator that wires TurnManager events to UI updates and routes player input back to TurnManager. This is the missing bridge between pure C# combat logic and the Unity scene. Without it, TurnManager can run in tests but not in a live scene.

## Authoritative Design Source

- `design/gdd/combat-ui.md` §3 — Layout regions, phase-based panel visibility, input routing
- `design/gdd/turn-manager.md` §3.1–3.4 — Phase sequence, split turn structure, TurnAction submission

## What Already Exists

### Completed systems this builds on:
- `Assets/Scripts/Combat/TurnManager.cs` — Full phase sequencer with events: `RoundStarted`, `RoundEnded`, `CreatureActed`, `CreatureFainted`, `CreatureCaptured`, `PhaseChanged`
- `Assets/Scripts/Combat/EncounterManager.cs` — `InitializeEncounter()` returns `BattleContext`
- `Assets/Scripts/Combat/BattleContext.cs` — Grid, creatures, encounter config
- `Assets/Scripts/Combat/DamageCalculator.cs`, `CaptureSystem.cs`, `StatusEffectProcessor.cs`, `MoveEffectApplier.cs` — All combat subsystems implemented
- `Assets/Scripts/Combat/AIDecisionSystem.cs` — Enemy AI for EnemyAction phase
- `Assets/Scripts/UI/CombatHUDController.cs` — Root UI controller (exists, needs wiring)
- `Assets/Scripts/UI/MoveSelectionPanelController.cs` — Move buttons (exists, needs input routing)
- `Assets/Scripts/UI/TurnOrderBarController.cs` — Initiative display (exists, needs event subscription)
- `Assets/Scripts/UI/CreatureInfoPanelController.cs` — Creature details (exists, needs data binding)
- `Assets/Scripts/UI/TileHighlightController.cs` — Tile highlights (exists, needs phase-aware toggling)
- `Assets/Scripts/UI/SwitchOverlayController.cs` — Party swap UI (exists, needs action submission)
- `Assets/Scripts/UI/TypeEffectivenessCallout.cs` — Hit feedback (exists, needs event hookup)
- `Assets/Scripts/Creatures/CreatureInstance.cs` — Full runtime state with `DeductPP()`, `TakeDamage()`, `Heal()`
- `Assets/Scripts/Gameplay/Grid/GridSystem.cs` — Pathfinding, tile queries
- `Assets/Scripts/Gameplay/PartyState.cs` — Player party roster

### Assembly layout:
- `GeneForge.Core.asmdef` — Core, Creatures, Gameplay
- `GeneForge.Combat.asmdef` — Combat logic, references Core + Grid
- `GeneForge.UI.asmdef` — UI controllers, references Core + Combat + Grid

### Key architectural decisions:
- **ADR-002**: Pure C# for logic, MonoBehaviours only for Unity scene integration
- **ADR-003**: Constructor injection for testability — CombatController constructs TurnManager with all dependencies
- **ADR-004**: C# events for decoupling — UI subscribes to TurnManager events, never polls

## Scope — What to Implement

### 1. `Assets/Scripts/UI/CombatController.cs` (replace existing stub)

**MonoBehaviour** in `GeneForge.UI` namespace. Scene-level orchestrator.

**Lifecycle:**
1. `Awake()` — Cache UI panel references (serialized fields)
2. `StartCombat(EncounterConfig config)` — Public entry point:
   - Call `EncounterManager.InitializeEncounter(config, partyState)`
   - Construct `TurnManager` with all injected dependencies (DamageCalculator, CaptureSystem, StatusEffectProcessor, MoveEffectApplier, AIDecisionSystem)
   - Subscribe to all TurnManager events
   - Initialize UI panels with BattleContext data
   - Begin round loop
3. `OnDestroy()` — Unsubscribe all events

**Phase routing (from TurnManager.PhaseChanged):**

| Phase | UI State |
|-------|----------|
| RoundStart | Disable input, show status effect animations |
| PlayerCreatureSelect | Enable MoveSelectionPanel, enable TileHighlights, show creature info |
| PlayerAction | Disable input, show action execution animations |
| EnemyAction | Disable input, show enemy actions |
| RoundEnd | Disable input, show duration tick feedback |

**Input collection during PlayerCreatureSelect:**
- For each non-fainted player creature, collect a `TurnAction`
- MoveSelectionPanel click → build TurnAction with ActionType.UseMove
- Tile click during movement phase → set MovementTarget
- Gene Trap button → ActionType.Capture + target selection
- Switch button → open SwitchOverlay → swap creature
- Wait button → ActionType.Wait
- Once all actions collected → call `TurnManager.SubmitPlayerActions(actions)` → advance phase

**Event handlers:**
- `OnCreatureActed` → Update HP bars, play move animations, show effectiveness callout
- `OnCreatureFainted` → Gray out fainted creature, update turn order
- `OnCreatureCaptured` → Play capture animation, remove from grid
- `OnRoundStarted` → Update round counter, refresh turn order bar
- `OnRoundEnded` → Check combat result, show victory/defeat if ended

### 2. `Assets/Scripts/Combat/IPlayerInputProvider.cs`

**Interface** for abstracting player input — enables testing without UI.

```csharp
public interface IPlayerInputProvider
{
    /// Collect TurnActions for all player creatures this round.
    /// Called during PlayerCreatureSelect phase.
    void BeginActionCollection(IReadOnlyList<CreatureInstance> creatures);
    
    /// True when all creature actions have been submitted.
    bool AllActionsReady { get; }
    
    /// Get the collected actions. Only valid when AllActionsReady is true.
    IReadOnlyDictionary<CreatureInstance, TurnAction> GetActions();
}
```

### 3. Tests

- `Assets/Tests/EditMode/CombatControllerTests.cs` — Test event wiring with mock TurnManager calls
- Verify: phase transitions trigger correct UI panel states
- Verify: TurnAction construction from mock input
- Verify: combat end triggers result screen

### Out of scope:
- Animation playback (log to console for MVP)
- Sound effects on actions
- Victory/defeat screen UI (log result for MVP)
- AI turn visualization timing (instant for MVP)
- Camera movement during actions
- Undo/redo action selection

## Constraints

- **CombatController**: MonoBehaviour (exception to ADR-002 — scene integration required)
- **IPlayerInputProvider**: Pure C# interface in Combat namespace
- **Namespace**: `GeneForge.UI` for controller, `GeneForge.Combat` for interface
- **No game logic in CombatController** — delegates everything to TurnManager
- **XML doc comments** on all public API
- Follow collaboration protocol: Question → Options → Decision → Draft → Approval

## Verification

- Run `/code-review` on all new files
- Unity compilation: 0 errors
- Manual test: start combat scene → see phase transitions in console → player can select moves → combat resolves

## Branch

Create branch `feature/Combat-Controller` from `main`.
