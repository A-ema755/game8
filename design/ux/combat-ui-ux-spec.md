# Combat UI — UX Spec
# System #26 | Gene Forge
# Status: Draft | Author: ux-designer | For: ui-programmer

> Authoritative design source: design/gdd/combat-ui.md
> This document is the implementation-layer companion — state machine, data
> bindings, interaction rules, keyboard shortcuts, edge cases, accessibility.
> The GDD answers "what"; this spec answers "how the UI behaves."

---

## 1. User Flow Map

### 1.1 Top-Level Combat UI States

```
INACTIVE
  │  TurnManager constructed, CombatActive = true
  ▼
ROUND_START
  │  RoundStarted event fires
  │  UI: show all panels; update HP bars from DoT; animate turn order re-sort
  ▼
PLAYER_SELECT          ← CombatPhase.PlayerCreatureSelect
  │  Move panel visible and interactive
  │  Player selects move → enters MOVE_TARGETING sub-state
  │  Player selects Switch → enters SWITCH_OVERLAY sub-state
  │  Player selects Gene Trap (if valid) → enters CAPTURE_TARGETING sub-state
  │  Player submits all party actions → phase ends
  ▼
PLAYER_EXECUTING       ← CombatPhase.PlayerAction
  │  Move panel locked (non-interactive)
  │  Each CreatureActed event: play feedback, update HP bars, update turn order
  │  CreatureFainted event: remove from turn order, play faint animation
  ▼
ENEMY_EXECUTING        ← CombatPhase.EnemyAction
  │  Move panel locked
  │  CreatureActed events: same as above
  ▼
ROUND_END
  │  RoundEnded event fires
  │  UI: update status effect durations, decrement badges, flash expiring effects
  │  Loop → ROUND_START
  ▼
COMBAT_END             ← CombatActive = false
  │  Show Victory / Defeat / Fled / Captured overlay
  └─► GameStateManager handles scene transition
```

### 1.2 MOVE_TARGETING Sub-State

```
PLAYER_SELECT
  │  Player clicks move button (or presses 1–4)
  ▼
MOVE_TARGETING
  │  Highlight valid tiles for selected move's form + range
  │  Creature Info Panel: switch to show hovered enemy on mouse-over tile
  │  Player clicks valid tile or creature → confirm action → back to PLAYER_SELECT
  │  Player presses ESC or clicks same move button again → cancel → back to PLAYER_SELECT
  │  Player hovers different move button → update highlights without confirming
```

### 1.3 CAPTURE_TARGETING Sub-State

```
PLAYER_SELECT
  │  Player clicks Gene Trap button (or presses 5)
  ▼
CAPTURE_TARGETING
  │  Highlight capturable tiles in green
  │  Creature Info Panel: show Catch Predictor when valid target hovered
  │  Player clicks valid target → confirm capture action → back to PLAYER_SELECT
  │  Player presses ESC → cancel → back to PLAYER_SELECT
```

### 1.4 SWITCH_OVERLAY Sub-State

```
PLAYER_SELECT
  │  Player clicks Switch button (or presses 6)
  ▼
SWITCH_OVERLAY
  │  Party overlay opens (full-screen dimmed overlay)
  │  Fainted creatures: grayed, skull icon, non-interactive
  │  Player clicks party member → confirm switch action → overlay closes → back to PLAYER_SELECT
  │  Player presses ESC → cancel (no action consumed) → overlay closes → back to PLAYER_SELECT
```

### 1.5 Transition Triggers Summary

| Transition | Trigger |
|---|---|
| INACTIVE → ROUND_START | `TurnManager.RoundStarted` event |
| ROUND_START → PLAYER_SELECT | `TurnManager.PhaseChanged` → `CombatPhase.PlayerCreatureSelect` |
| PLAYER_SELECT → PLAYER_EXECUTING | `TurnManager.PhaseChanged` → `CombatPhase.PlayerAction` |
| PLAYER_EXECUTING → ENEMY_EXECUTING | `TurnManager.PhaseChanged` → `CombatPhase.EnemyAction` |
| ENEMY_EXECUTING → ROUND_END | `TurnManager.PhaseChanged` → `CombatPhase.RoundEnd` |
| ROUND_END → ROUND_START | `TurnManager.RoundStarted` (next round) |
| Any → COMBAT_END | `TurnManager.CombatActive == false` (polled after each event) |

---

## 2. Panel Visibility Rules

### 2.1 Per-Phase Visibility Table

| Panel | INACTIVE | ROUND_START | PLAYER_SELECT | PLAYER_EXECUTING | ENEMY_EXECUTING | ROUND_END | COMBAT_END |
|---|---|---|---|---|---|---|---|
| Turn Order Bar | Hidden | Visible | Visible | Visible | Visible | Visible | Hidden |
| Creature Info Panel | Hidden | Visible | Visible | Visible | Visible | Visible | Hidden |
| Move Selection Panel | Hidden | Hidden | **Interactive** | Locked | Locked | Hidden | Hidden |
| Tile Highlight Overlay | Hidden | Hidden | Visible (on hover/select) | Fades out | Hidden | Hidden | Hidden |
| Catch Predictor (in Info Panel) | Hidden | Hidden | Visible when Gene Trap selected + valid target hovered | Hidden | Hidden | Hidden | Hidden |
| Switch Overlay | Hidden | Hidden | Visible when Switch activated | Hidden | Hidden | Hidden | Hidden |
| Combat End Overlay | Hidden | Hidden | Hidden | Hidden | Hidden | Hidden | Visible |

### 2.2 Locked vs Hidden

- **Locked**: Panel visible but all buttons `pickingMode = PickingMode.Ignore`. Visual opacity unchanged. Prevents mis-clicks during execution.
- **Hidden**: `display: none` in USS (`DisplayStyle.None`). Not in layout, not interactive, not tabbable.

### 2.3 Move Panel Lock Indicator

When `PLAYER_EXECUTING` or `ENEMY_EXECUTING`, apply a CSS class `.panel--locked` to the move panel root. This class adds 50% opacity and a cursor: `not-allowed` override. No spinner or loading indicator — the 3D execution animations are the feedback that the system is processing.

---

## 3. Interaction Patterns

### 3.1 Move Button Interaction (PLAYER_SELECT state only)

| Input | Action |
|---|---|
| Hover move button | Immediately show form-specific tile highlights on grid. Update Creature Info Panel if enemy in range. No commit. |
| Click move button (not already selected) | Enter MOVE_TARGETING. Button shows selected state (gold border). |
| Click same move button again | Cancel MOVE_TARGETING. Clear highlights. Return to idle PLAYER_SELECT. |
| Click different move button while in MOVE_TARGETING | Switch to new move's highlights. Previous move deselects. |
| Press 1–4 | Same as clicking move button 1–4 |
| Press 5 | Same as clicking Gene Trap button |
| Press 6 | Same as clicking Switch button |

**Disabled move button interactions:**

| State | Visual | Clickable |
|---|---|---|
| No PP | Gray overlay, "No PP" label | No (PickingMode.Ignore) |
| No Access (missing body part) | Form icon red tint, "No Access" label | No |
| Normal (0 PP edge case — all depleted) | Shows "Struggle" in slot 1 only | Yes |

### 3.2 Tile / Creature Click Interaction (MOVE_TARGETING state)

| Input | Action |
|---|---|
| Hover highlighted tile | White pulse on tile. If creature on tile: Creature Info Panel updates to show that creature. |
| Hover non-highlighted tile | No effect. Cursor standard. |
| Click highlighted tile (valid target) | Confirm action. Clear highlights. Return to PLAYER_SELECT (awaiting remaining party creature actions if 2+ in party). |
| Click non-highlighted tile | No action. Subtle shake on move button (visual feedback). |
| Click enemy icon in Turn Order Bar | Select that enemy as target if in valid range. Equivalent to clicking their tile. |
| Right-click anywhere | Cancel targeting. Same as ESC. |

### 3.3 Turn Order Bar Interaction

| Input | Action |
|---|---|
| Hover any icon | Tooltip: creature name + HP text. |
| Click enemy icon during MOVE_TARGETING | Select as target (if their tile is in valid range). |
| Click enemy icon during CAPTURE_TARGETING | Select as target if capturable. |
| Click player icon | Switch Creature Info Panel to show that player creature. No action committed. |
| Click enemy icon outside targeting | Switch Creature Info Panel to show that enemy. No action committed. |
| Scroll wheel on Turn Order Bar (if >8 creatures) | Scroll horizontally. |

### 3.4 Creature Info Panel Interaction

Panel is display-only. No interactive elements except:
- Hovering status effect icon: tooltip with effect name, description, remaining turns.
- Hovering instability bar when `!` visible: tooltip "Instability high — DNA breakdown risk."
- Panel switches content on target hover/click. Does not require click to inspect enemy.

### 3.5 Switch Overlay Interaction

| Input | Action |
|---|---|
| Click active (non-fainted) party slot | Confirm switch action. Overlay closes. Creature's turn is consumed. |
| Click fainted party slot | No action. Slot shakes (visual feedback). |
| Click backdrop (dimmed area) | Cancel. Overlay closes. No action. |
| ESC | Cancel. Overlay closes. No action. |
| Tab | Cycle through non-fainted party slots. |
| Enter | Confirm focused slot. |

---

## 4. Data Binding Map

### 4.1 Turn Order Bar

Each turn order icon is a reusable `TurnOrderIcon` visual element, bound to one `CreatureInstance`.

| UI Element | Data Source | Update Trigger |
|---|---|---|
| Portrait image | `CreatureConfig.portraitSprite` (via Config ref on CreatureInstance) | Static — set on spawn |
| Gold border (active) | `TurnManager.CurrentPhase` + initiative position index == 0 | `PhaseChanged` event |
| Enlarged scale | Same as gold border | `PhaseChanged` event |
| HP pip fill count | `creature.CurrentHP / creature.MaxHP` mapped to 3–5 pips | `CreatureActed`, `RoundStarted` |
| Status icon (first status) | `creature.ActiveStatusEffects[0]` | `CreatureActed`, `RoundStarted`, `RoundEnded` |
| Red/blue tint | Team membership (player vs enemy) | Static — set on spawn |
| Faint removal | `creature.IsFainted` | `CreatureFainted` event |
| Turn order position (re-sort) | Re-query initiative order after `RoundStarted` | `RoundStarted` |

HP pip count formula:
```
visiblePips = creature.MaxHP <= 30 ? 3 : creature.MaxHP <= 60 ? 4 : 5
filledPips  = ceil(visiblePips × (creature.CurrentHP / creature.MaxHP))
```

### 4.2 Creature Info Panel

| UI Element | Data Source | Update Trigger |
|---|---|---|
| Creature name | `creature.Nickname` (falls back to `creature.DisplayName`) | Panel target changes |
| Level badge | `creature.Level` | Panel target changes, `CreatureActed` (on level-up — post-MVP) |
| Primary type badge | `creature.Config.PrimaryType` | Panel target changes |
| Secondary type badge | `creature.ActiveSecondaryType` (Blight override at instability >= 80) | Panel target changes, `CreatureActed` |
| HP bar fill ratio | `creature.CurrentHP / creature.MaxHP` | `CreatureActed`, `RoundStarted` |
| HP bar color | fill ratio: >0.5 = green, 0.25–0.5 = yellow, <0.25 = red | Same as fill |
| HP text label | `$"{creature.CurrentHP} / {creature.MaxHP}"` | Same as fill |
| Instability bar fill | `creature.Instability / 100f` | `CreatureActed` |
| Instability bar color | 0–49 = green, 50–74 = yellow, 75–100 = red | Same as fill |
| Instability `!` icon | `creature.Instability >= 50` | Same as fill |
| Status effect icons (up to 4) | `creature.ActiveStatusEffects` (first 4 entries) | `CreatureActed`, `RoundStarted`, `RoundEnded` |
| Status overflow badge | `creature.ActiveStatusEffects.Count > 4` → show "+" badge | Same as above |
| Body part slot icons (3) | `creature.EquippedPartIds[BodySlot.Head/Back/Legs]` | Panel target changes |
| Catch Predictor label | `CaptureCalculator.CalculateCatchRate(creature, activeTrapConfig)` × 100, rounded to int | Active trap selection + target hover |
| Catch Predictor color | >60% = green, 30–60% = yellow, <30% = red | Same as above |
| Catch Predictor visibility | Gene Trap selected AND target is wild AND `creature.IsFainted == false` | CAPTURE_TARGETING state entry/exit |
| "Cannot Capture" text | Gene Trap selected AND (trainer battle OR already owned) | Same as above |

Panel target selection priority:
1. Creature hovered on grid tile (during targeting)
2. Creature whose tile is hovered (idle hover)
3. Enemy/player icon clicked in Turn Order Bar
4. Default: player's active creature (first non-fainted in player party)

### 4.3 Move Selection Panel

Each move button is bound to one move slot (index 0–3) of the active player creature.

| UI Element | Data Source | Update Trigger |
|---|---|---|
| Move name | `ConfigLoader.GetMove(creature.LearnedMoveIds[i]).name` | Creature changes (Switch) |
| Genome type icon | `move.GenomeType` → sprite lookup | Same |
| Genome type color (text tint) | `move.GenomeType` → color palette | Same |
| Damage form icon | `move.Form` → Physical/Energy/Bio sprite | Same |
| PP label | `$"{creature.LearnedMovePP[i]} / {move.PP}"` | `CreatureActed` (PP consumed) |
| "No PP" overlay | `creature.LearnedMovePP[i] == 0` | Same |
| "No Access" overlay | `!creature.AvailableForms.Contains(move.Form)` | Creature changes |
| Empty slot "—" | `i >= creature.LearnedMoveIds.Count` | Creature changes |
| Struggle slot | `creature.LearnedMovePP.All(pp => pp == 0)` → show Struggle in slot 0 only | `CreatureActed` |
| Gene Trap count label | `PlayerInventory.GetCount("gene-trap-*")` (sum all trap types) | Inventory changes |
| Gene Trap disabled | `encounterType == Trainer` OR trap count == 0 | State entry, `CreatureCaptured` |

Gene Trap button shows the count of the currently selected trap type in the player's active slot. Trap type cycling is post-MVP; MVP uses first available trap type.

### 4.4 Tile Highlight Overlay

Tile highlights are applied as material additive tint on `TileData` mesh renderers, not as UI Toolkit elements. The UI layer communicates desired highlights to a `TileHighlightController` MonoBehaviour.

| Highlight Set | Data Query | Trigger |
|---|---|---|
| Blue (movement) | `GridSystem.GetReachableTiles(creature.GridPosition, movementRange)` | PLAYER_SELECT idle state (default) |
| Red (attack) | `GridSystem` query based on `move.Form`, `move.Range`, `move.TargetType`, creature position, LoS for Energy moves | Move button hover / MOVE_TARGETING entry |
| Green (capture) | `GridSystem` tiles within Gene Trap range containing wild non-owned creatures | CAPTURE_TARGETING entry |
| Gold (terrain synergy) | `GridSystem.GetTile(pos).terrainType == creature.Config.PrimaryType` for all visible tiles | PLAYER_SELECT entry, persists through targeting |
| White pulse | Currently hovered tile | MouseEnterEvent on tile collider |
| Clear all | — | Phase changes to PLAYER_EXECUTING or later |

Form-specific range queries:
- **Physical** (`move.Form == DamageForm.Physical`): tiles within `move.Range` (1–2) from creature position — no LoS check.
- **Energy** (`DamageForm.Energy`): tiles within `move.Range` (3–5) filtered by `GridSystem.HasLineOfSight(pos, tile.Position)`. Blocked tiles shown at 30% opacity red (dimmed, not fully highlighted).
- **Bio** (`DamageForm.Bio`): tiles within `move.Range` (2–3) — no LoS check (penetrating).

### 4.5 Type Effectiveness Callout

| Parameter | Source |
|---|---|
| Label text | `TypeChart.GetLabel(multiplier)` → "Super Effective!" or "Not Very Effective..." |
| World anchor position | Target `CreatureInstance.GridPosition` → world space via `GridSystem.GetTile().worldPosition + Vector3.up * 2f` |
| Effectiveness level | `TypeChart.GetMultiplier(move.GenomeType, target.Config.PrimaryType, target.ActiveSecondaryType)` |

Queue rule: if previous callout is still visible (within 0.5s of spawn), offset Y += 0.6 world units.

---

## 5. Keyboard Shortcuts

All shortcuts active only when the Combat scene is focused and no text input field has focus.

### 5.1 PLAYER_SELECT State

| Key | Action |
|---|---|
| `1` | Select / confirm move slot 1 (or toggle targeting if already selected) |
| `2` | Select / confirm move slot 2 |
| `3` | Select / confirm move slot 3 |
| `4` | Select / confirm move slot 4 |
| `5` | Select Gene Trap (if available) |
| `6` | Open Switch overlay |
| `ESC` | No action in idle PLAYER_SELECT. If in targeting sub-state: cancel targeting. |

### 5.2 MOVE_TARGETING State

| Key | Action |
|---|---|
| `Tab` | Cycle to next valid target (enemy creature on highlighted tile). Cycles in initiative order. |
| `Shift+Tab` | Cycle to previous valid target. |
| `Enter` or `Space` | Confirm currently focused target. |
| `ESC` or `RMB` | Cancel targeting. Return to PLAYER_SELECT. |
| `1–4` | Switch to different move without cancelling (re-enter targeting with new move's range). |

### 5.3 SWITCH_OVERLAY State

| Key | Action |
|---|---|
| `Tab` | Cycle focus to next non-fainted party slot. |
| `Shift+Tab` | Cycle focus to previous non-fainted party slot. |
| `Enter` or `Space` | Confirm focused party slot. |
| `ESC` | Cancel. Close overlay. |

### 5.4 Global (Any State)

| Key | Action |
|---|---|
| `ESC` (when no sub-state active) | Reserved for future pause menu (post-MVP). Currently no-op. |

### 5.5 Tab Navigation Order

Tab order in PLAYER_SELECT (when not targeting):
1. Move slot 1
2. Move slot 2
3. Move slot 3
4. Move slot 4
5. Gene Trap button
6. Switch button
7. (wrap to 1)

Tab does not navigate to Turn Order Bar or Creature Info Panel (display-only elements).

---

## 6. Edge Cases

### 6.1 Creature Faints Mid-Targeting

**Scenario**: Player enters MOVE_TARGETING for a single-target move, hovers an enemy tile. Before player clicks, that enemy's HP reaches 0 from a DoT tick (RoundStart). TurnManager fires `CreatureFainted`.

**Resolution**:
1. `CreatureFainted` handler checks: is the fainted creature the current hover target?
2. If yes: clear tile highlights, dismiss targeting, return to PLAYER_SELECT.
3. Creature Info Panel reverts to default (player's active creature).
4. Toast message: "Target fainted." (2s, top-center).
5. If the fainted creature was the only valid target for the selected move: move button enters no-valid-targets state, showing a subtle shake on re-hover.

### 6.2 All Moves at 0 PP

**Scenario**: All 4 of a creature's learned moves have 0 PP remaining.

**Resolution**:
1. Move slots 1–4 show `No PP` grayed state.
2. A "Struggle" auto-move is injected into slot 1 display only:
   - Name: "Struggle"
   - Genome type: None (blank icon)
   - Damage form: Physical
   - PP label: "—"
3. Slots 2–4 remain grayed with `No PP`.
4. Gene Trap and Switch remain available.
5. Selecting Struggle: targets any enemy tile (standard Physical range). No PP cost.

Note: Coordinate with gameplay-programmer on TurnManager Struggle implementation status before shipping this behavior.

### 6.3 Fewer Than 4 Learned Moves

**Scenario**: Creature has 1–3 learned moves (valid for low-level creatures).

**Resolution**:
- Slots for missing moves show a `—` placeholder label.
- `PickingMode.Ignore` on empty slots.
- No "No PP" overlay — these are structurally empty, not depleted.
- Tab navigation skips empty slots.

### 6.4 Status Effect Overflow (More Than 4 Active)

**Scenario**: Creature has 5 or more simultaneous active status effects.

**Resolution**:
- Display effects at indices 0–3 (most recently applied first).
- Index 3 slot shows the effect icon with a "+" badge.
- Hovering the "+" badge shows a tooltip listing all overflow effects by name.
- Tooltip format: single-column list, each entry: "[icon] Effect Name (N turns)".
- Tooltip dismisses on mouse-leave.

### 6.5 Gene Trap Used Against Trainer Creature

**Scenario**: Player selects Gene Trap button in a trainer encounter (EncounterType.Trainer).

**Resolution**:
- Gene Trap button is disabled at state entry for trainer encounters. `PickingMode.Ignore`, gray overlay.
- Tooltip on hover: "Cannot capture trainer's creatures."

### 6.6 Turn Order Re-Sort Mid-Round

**Scenario**: A move applies a speed buff/debuff, which would change initiative order.

**Resolution** (MVP):
- TurnManager calculates initiative at phase start and does not recalculate mid-phase. No visual re-sort occurs mid-round.
- Turn Order Bar updates on `RoundStarted` only.
- Speed stat changes take effect in the next round's initiative calculation.

### 6.7 Switch with Only One Non-Fainted Creature

**Scenario**: Player opens Switch overlay but only one non-fainted party member exists (the active one).

**Resolution**:
- Switch overlay opens normally.
- Active creature slot shows a "Currently Active" badge and is non-interactive.
- All fainted slots are grayed.
- Informational text: "No other creatures available."
- ESC closes overlay. No turn consumed.

### 6.8 All Player Creatures Faint During Execution

**Scenario**: During PLAYER_EXECUTING or ENEMY_EXECUTING, final player creature faints. `TurnManager.CombatActive` becomes false with `CombatResult.Defeat`.

**Resolution**:
1. `CreatureFainted` event removes creature from Turn Order Bar.
2. Poll `TurnManager.CombatActive` after each `CreatureFainted` event.
3. If `false`: immediately transition to COMBAT_END state.
4. Show Defeat overlay regardless of what execution animations are still playing.
5. Lock all panels. Do not allow further input.

### 6.9 Height Indicator on Elevated Creatures

| Condition | UI Element |
|---|---|
| Creature on tile height 1 | Small upward arrow icon above 3D model (world-space canvas) |
| Creature on tile height 2+ | Upward arrow + height value badge: "+2" |
| Hovering an elevated creature | Height badge in Creature Info Panel next to level badge: `Lv.12 ↑+2` |

Height badges are rendered as world-space UI elements, not as part of the UI Toolkit layout.

### 6.10 No Valid Tiles for Selected Move

**Scenario**: Player selects a move but no tiles fall within the form-specific range.

**Resolution**:
- No tiles highlighted.
- Move button border pulses red (1 pulse, 0.3s).
- Tooltip on button: "No valid targets."
- Player can still select the move and enter targeting state, but clicking anywhere does nothing.
- ESC or re-clicking the button cancels.

---

## 7. Accessibility Notes

### 7.1 Text Size Requirements

| Element | Minimum Font Size | Recommended |
|---|---|---|
| Creature name (Info Panel) | 16px | 20px |
| HP text label | 14px | 16px |
| Move name | 14px | 16px |
| PP label | 12px | 14px |
| Status effect tooltips | 12px | 14px |
| Catch Predictor percentage | 16px | 18px (high importance) |
| Turn order tooltips | 12px | 14px |

All sizes are logical pixels at 1080p. UI must scale proportionally at 1440p and 4K.

### 7.2 Color Usage Rules

Color must never be the sole differentiator for critical information. Every color-coded element must have a secondary signal.

| Element | Color | Secondary Signal |
|---|---|---|
| HP bar state | Green / Yellow / Red | Text label shows exact number "47 / 80" |
| Instability state | Green / Yellow / Red | `!` icon at warning threshold; text value via tooltip |
| Catch Predictor | Green / Yellow / Red | Percentage text always shown |
| Type effectiveness callout | Red / Blue | Text label ("Super Effective!" / "Not Very Effective...") |
| Team tint (Turn Order) | Blue / Red | Ensure contrast ratio >= 4.5:1 against background |
| Tile highlights | Blue / Red / Green / Gold | Pulse animation distinguishes active highlight layer |

### 7.3 Colorblind-Safe Palette Notes

Default palette has red/green conflict on HP bar and tile highlights. Architecture the color assignment so it can be swapped via a `UIColorPalette` ScriptableObject (one field per semantic color) without code changes.

- **Deuteranopia/Protanopia (red-green)**: HP bar alternative: orange-yellow-white gradient. Tile highlight: Blue (movement) and Yellow-Orange (attack) instead of Blue/Red.
- **Tritanopia (blue-yellow)**: Tile highlight: Cyan (movement), Red-Orange (attack).

Colorblind alternate palettes deferred to pre-launch polish phase. The toggle will live in Settings System (System #29).

### 7.4 Keyboard-Only Usability

Full combat is playable without a mouse using Tab + number keys + Enter/ESC per Section 5.

- [ ] All interactive move buttons reachable via Tab
- [ ] All targeting confirmable via Tab + Enter (cycle to target, confirm)
- [ ] Switch overlay fully navigable via Tab + Enter + ESC
- [ ] No tooltip requires hover to access critical information

### 7.5 Contrast Ratios

Target WCAG AA compliance (4.5:1 for normal text, 3:1 for large text):
- HP text on dark panel background: white (#FFFFFF) text on panel color meets 4.5:1.
- "No PP" label on gray overlay: verify label color meets 4.5:1 against overlay.
- Catch Predictor text: large text (18px+) requires 3:1 minimum.
- Type effectiveness callouts: text has drop shadow or semi-transparent backing for contrast against any grid background.

### 7.6 No Flashing Content

- Instability bar flash (at instability >= 75, white flash every 3s): 0.33Hz — well below the 3Hz photosensitive threshold. Safe.
- All pulse animations (tile highlights at 1Hz): safe.
- No effect should exceed 3Hz in luminance oscillation.

---

## 8. Implementation Notes for ui-programmer

### 8.1 Event Subscription Pattern

Subscribe to TurnManager events in a `CombatHUDController` MonoBehaviour:

```csharp
_turnManager.RoundStarted     += OnRoundStarted;
_turnManager.RoundEnded       += OnRoundEnded;
_turnManager.CreatureActed    += OnCreatureActed;
_turnManager.CreatureFainted  += OnCreatureFainted;
_turnManager.CreatureCaptured += OnCreatureCaptured;
```

Unsubscribe in `OnDestroy`. Do not use `FindObjectOfType` to locate TurnManager — inject via serialized reference per ADR-003.

### 8.2 Panel Architecture

| Component | Responsibility |
|---|---|
| `CombatHUDController` | State machine, event routing, phase visibility |
| `TurnOrderBarController` | Icon pool management, initiative sort |
| `CreatureInfoPanelController` | Data binding, target tracking, Catch Predictor |
| `MoveSelectionPanelController` | Move button state, PP display, form highlights |
| `TileHighlightController` (MonoBehaviour) | Grid overlay, communicates with GridSystem |
| `TypeCalloutController` | Screen-space callout spawning, queue management |
| `SwitchOverlayController` | Party overlay, faint state, navigation |

### 8.3 Target Tracking

`CreatureInfoPanelController` maintains a priority stack for the current display target:
1. Explicit click selection (highest priority)
2. Hover over grid tile (creature present)
3. Default: first non-fainted player creature

Pop priority on hover-exit. On explicit click, hold until another explicit click or ESC.

### 8.4 Tile Highlight Performance

`TileHighlightController` must cache reachable tile sets per creature per phase. Do not call `GridSystem.GetReachableTiles()` on every frame or mouse-move. Call once per:
- PLAYER_SELECT state entry
- Move button hover (debounce 50ms)

Clear cache on `PhaseChanged`.

### 8.5 MVP Exclusions (Do Not Implement)

- Threat dot indicators (Threat/Aggro System not built — System #40)
- Combo move range highlighting (Combo Move System not built — System #20)
- Affinity bar (Creature Affinity System not built — System #19)
- Gamepad/controller input
- Audio feedback hooks (Audio System post-MVP — System #31)
- Pause menu (post-MVP)
- Instability breakthrough flash visual (VFX System not built — System #32)
- Mid-round initiative re-sort animation
- Trap type selection cycling (single trap type per turn in MVP)
