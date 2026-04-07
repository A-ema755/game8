# Implementation Prompt: Combat UI (#26)

## Agent

Invoke **team-ui** skill: `/team-ui Combat UI`

Team composition for this feature:
- **ux-designer** — Validate layout regions, information hierarchy, and interaction flows
- **ui-programmer** — Implement UI Toolkit panels, data binding, tile highlighting
- **art-director** — Review visual direction: color coding, type badges, HP bar styling

> **Note**: This is a cross-discipline UI system. team-ui is the correct orchestration. The ux-designer validates flows, ui-programmer builds, art-director reviews visuals. No audio or AI agents needed.

## Objective

Implement the **Combat UI** — the player-facing interface for tactical grid combat. This is system #26 in the systems index, a Presentation Layer system that overlays the isometric 3D grid with turn order, creature info, move selection, tile highlighting, and contextual displays (catch predictor, type effectiveness callouts). Built with Unity UI Toolkit.

## Authoritative Design Source

`design/gdd/combat-ui.md` — all layout regions, panel contents, highlighting rules, edge cases, and acceptance criteria live there. Do NOT deviate from the GDD without explicit approval.

## What Already Exists

### Completed systems this builds on:
- `Assets/Scripts/Combat/TurnManager.cs` — phase-based sequencer with events: `RoundStarted`, `RoundEnded`, `CreatureActed`, `CreatureFainted`, `CreatureCaptured`, `PhaseChanged`
- `Assets/Scripts/Combat/Enums/CombatPhase.cs` — RoundStart, PlayerCreatureSelect, PlayerAction, EnemyAction, RoundEnd
- `Assets/Scripts/Combat/Enums/ActionType.cs` — UseMove, Capture, Item, Flee, Wait
- `Assets/Scripts/Combat/TurnAction.cs` — action data (ActionType, move slot, target, destination)
- `Assets/Scripts/Combat/CaptureCalculator.cs` — `CalculateCatchRate()`, `GetStatusBonus()` for catch predictor
- `Assets/Scripts/Combat/DamageCalculator.cs` — damage calculation for feedback
- `Assets/Scripts/Combat/TypeChart.cs` — `GetMultiplier()` for type effectiveness callouts
- `Assets/Scripts/Creatures/CreatureInstance.cs` — HP, MaxHP, Level, Instability, ActiveStatusEffects, LearnedMoveIds, LearnedMovePP, ComputedStats, AvailableForms, ActiveSecondaryType
- `Assets/Scripts/Creatures/CreatureConfig.cs` — DisplayName, PrimaryType, SecondaryType, CatchRate
- `Assets/Scripts/Creatures/MoveConfig.cs` — DisplayName, GenomeType, Form, Power, Accuracy, PP, TargetType
- `Assets/Scripts/Gameplay/Grid/GridSystem.cs` — `GetReachableTiles()`, `GetTile()`, grid queries
- `Assets/Scripts/Gameplay/Grid/TileData.cs` — Height, Terrain, ProvidesCover
- `Assets/Scripts/Gameplay/PartyState.cs` — party roster for switch overlay
- `Assets/Scripts/Core/Enums.cs` — CreatureType, DamageForm, StatusEffect, BodySlot, TerrainType

### Empty placeholder directories:
- `Assets/Scripts/UI/` — target for UI scripts
- `Assets/UI/` — target for UXML/USS files

### Assembly layout:
- `Assets/Scripts/GeneForge.Core.asmdef` (GUID: `a2365be9`) — Core namespace
- `Assets/Scripts/Combat/GeneForge.Combat.asmdef` (GUID: `ac121ad6`) — Combat namespace
- `Assets/Scripts/Gameplay/Grid/GeneForge.Grid.asmdef` — Grid namespace
- A new `GeneForge.UI.asmdef` will be needed in `Assets/Scripts/UI/`, referencing Core, Combat, and Grid

### Key architectural decisions:
- **ADR-004**: C# events for system decoupling — UI subscribes to TurnManager events
- **ADR-008**: Domain namespaces — UI gets `GeneForge.UI` namespace
- Unity UI Toolkit recommended over UGUI (Unity 6 best practice)

## Scope — What to Implement

### MVP scope (implement these):

#### 1. `Assets/Scripts/UI/GeneForge.UI.asmdef`

New assembly definition referencing Core, Combat, Grid assemblies.

#### 2. `Assets/UI/Combat/CombatHUD.uxml`

**Root UXML document** defining the 4-region layout from GDD §3:

```
+-------------------------------------------------------+
|  [TURN ORDER BAR — top strip, full width]             |
+--------------------+----------------------------------+
|  CREATURE INFO     |        3D GRID VIEWPORT          |
|  PANEL (left)      |        (center/right)            |
+--------------------+----------------------------------+
|         MOVE SELECTION PANEL (bottom strip)           |
+-------------------------------------------------------+
```

#### 3. `Assets/UI/Combat/CombatHUD.uss`

**Stylesheet** for combat UI — colors, spacing, HP bar styling, type badge colors.

Define USS variables for:
- HP bar color thresholds (green > 50%, yellow 25–50%, red < 25%)
- Type badge colors per CreatureType (14 colors)
- Instability bar gradient (green → yellow → red)
- Catch predictor color coding (green > 60%, yellow 30–60%, red < 30%)

#### 4. `Assets/Scripts/UI/CombatHUDController.cs`

**MonoBehaviour** in `GeneForge.UI` namespace. Root controller that owns the UI Document and coordinates sub-panels.

> **Exception to ADR-002**: UI controllers are MonoBehaviours because they must attach to GameObjects with UIDocument components. Keep logic minimal — delegate to pure C# helpers where possible.

**Responsibilities:**
- Initialize sub-panel controllers on Awake
- Subscribe to TurnManager events (PhaseChanged, CreatureActed, CreatureFainted, CreatureCaptured)
- Route player input from UI buttons to TurnManager action submission
- Show/hide panels based on combat phase

#### 5. `Assets/Scripts/UI/TurnOrderBarController.cs`

**Reads from:** TurnManager initiative order
**Displays:** Creature portrait icons in order, HP pips, status icon, active highlight

#### 6. `Assets/Scripts/UI/CreatureInfoPanelController.cs`

**Reads from:** Selected CreatureInstance
**Displays:** Name, level, type badges, HP bar (current/max), instability meter, status effects, body part slots

**Catch Predictor integration:** When Gene Trap is selected as active move, show catch probability via `CaptureCalculator.CalculateCatchRate()`. Color-coded: green > 60%, yellow 30–60%, red < 30%.

#### 7. `Assets/Scripts/UI/MoveSelectionPanelController.cs`

**Layout:** 2×3 button grid (4 moves + Gene Trap + Switch)
**Each move button shows:** Move name, genome type icon, damage form icon, PP remaining
**States:** Normal, No PP (grayed), No Access (red, missing body part form)

**On move hover:** Emit event for grid tile highlighting (form-specific range)
**On move click:** Submit action to TurnManager

**Gene Trap button:**
- Disabled in Trainer encounters (captureAllowed = false)
- Disabled when trap count = 0
- On select: switch to capture targeting mode

**Switch button:**
- Opens party overlay from PartyState
- Fainted creatures grayed with skull icon
- Switching costs creature's action

#### 8. `Assets/Scripts/UI/TileHighlightController.cs`

**Manages grid tile highlight overlays.** Pure C# class (not MonoBehaviour).

**Highlight colors from GDD:**
| Color | Meaning |
|-------|---------|
| Blue | Reachable movement tiles |
| Red | Attack range for selected move |
| Green | Capture range (Gene Trap) |
| Gold | Terrain synergy tiles |

**Form-specific range overlay:**
- Physical: melee range (1–2 tiles)
- Energy: LoS cone/range (3–5 tiles, blocked tiles dimmed)
- Bio: mid-range circle (2–3 tiles, ignoring cover)

#### 9. `Assets/Scripts/UI/TypeEffectivenessCallout.cs`

**Displays floating text above target creature on hit.**
- "Super Effective!" — red text, scale-up animation
- "Not Very Effective..." — blue text, scale-down
- Neutral = no callout
- No "Immune" callout (14-type chart has no immunities)
- Queued display if multiple hits land in sequence

#### 10. Tests

**No EditMode unit tests for UI MonoBehaviours** — UI Toolkit requires runtime context. Instead:

- `Assets/Tests/EditMode/TileHighlightTests.cs` — test pure C# highlight logic (tile selection, form-specific range calculation)
- Manual PlayMode verification checklist in PR description

### Out of scope (skip these):
- Threat dot indicators above enemy creatures (depends on Threat/Aggro System)
- Combo move range highlighting (depends on Combo Move System)
- Affinity bar display (depends on Creature Affinity System)
- Full boss health bar UI (Trophy encounter visual — cosmetic polish)
- Turn order change animations
- Instability breakthrough flash effect (polish)
- Pause menu overlay
- Controller/gamepad input (PC mouse+keyboard only for MVP)
- Audio feedback on button press (depends on Audio System)

## Constraints

- **UI framework**: Unity UI Toolkit (UXML + USS + C#), NOT UGUI Canvas
- **Namespace**: `GeneForge.UI`
- **MonoBehaviours allowed** for UI controllers only (must attach to UIDocument GameObjects)
- **Event-driven**: Subscribe to TurnManager events, don't poll
- **No game logic in UI**: UI reads state and submits actions — never calculates damage or modifies creature state
- **XML doc comments** on all public API
- **Minimal USS** — define core layout and type colors, don't over-design visuals for MVP
- Create `GeneForge.UI.asmdef` referencing Core, Combat, Grid assemblies

## Collaboration Protocol

Follow Question -> Options -> Decision -> Draft -> Approval:
1. Ask before creating any file
2. Show draft or summary before writing
3. Get explicit approval for the full changeset
4. No commits without user instruction

## Branch

Create branch `feature/Combat-UI` from `main`.
