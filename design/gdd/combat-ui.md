# Combat UI

## 1. Overview

The combat UI overlays the isometric 3D grid with all information needed to make tactical decisions without leaving the battlefield view. It consists of four persistent panels (grid display, creature info, move selection, turn order), dynamic tile highlighting, and contextual overlays (type effectiveness callouts, catch predictor, threat indicators). All panels use Unity UI Toolkit for runtime rendering. The UI is designed for mouse and keyboard on PC; no touch input in MVP.

## 2. Player Fantasy

The player feels like a commander with perfect battlefield awareness. At a glance they know every creature's health, the turn order, which tiles are reachable, and where danger is coming from. The UI doesn't obscure the action — it frames it. Hovering a tile should feel informative, not cluttered. The moment a "Super Effective!" callout flashes and the enemy's health bar drops dramatically, the UI should amplify the satisfaction of a good type matchup without getting in the way.

## 3. Detailed Rules

### Layout Regions

```
+-------------------------------------------------------+
|  [TURN ORDER BAR — top strip, full width]             |
+--------------------+----------------------------------+
|                    |                                  |
|  CREATURE INFO     |        3D GRID VIEWPORT          |
|  PANEL (left)      |        (center/right)            |
|                    |                                  |
|                    |                                  |
+--------------------+----------------------------------+
|         MOVE SELECTION PANEL (bottom strip)           |
+-------------------------------------------------------+
```

### Turn Order Bar
- Horizontal strip across the top of the screen.
- Shows creature portrait icons in initiative order (left = next to act).
- Active creature's icon is highlighted with a gold border and slightly enlarged.
- Each icon shows: creature portrait, HP pip bar (3–5 pips), status effect icon (first active status only).
- Enemy icons use red background tint; player icons use blue background tint.
- Clicking an enemy icon in the turn order selects them as a target (if targeting is active).
- Maximum 8 icons visible; scrolls horizontally if more creatures are in combat.

### Creature Info Panel (Left)
Displays the currently selected creature (player's active creature by default; enemy creature when hovering/targeting).

Fields shown:
- Creature name (TextMeshPro, large)
- Level badge
- Type badge(s) — up to 2 type icons with color coding
- HP bar: current/max displayed as "47 / 80", color-coded (green > 50%, yellow 25–50%, red < 25%)
- Instability meter: 0–100 bar, color shifts green → yellow → red. At 50+, a "!" warning icon appears.
- Status effects: up to 4 status icons with turn-count badges
- Active body parts: 3 small slot icons showing equipped parts (Head, Back, Legs minimum)
- Affinity bar: bond level 0–5 displayed as filled star icons (player creatures only)

### Move Selection Panel (Bottom)
Shown when it is the player's turn and a creature has been selected.

Layout: 6 buttons in a 2×3 grid.
- Slots 1–4: creature's four moves
- Slot 5: Gene Trap (capture option) — grayed if no Gene Traps in inventory or if the target is a trainer creature
- Slot 6: Switch — opens party swap overlay

Each move button shows:
- Move name
- Genome type icon + type name (color-coded per genome type)
- Damage form icon (Physical=fist, Energy=bolt, Bio=spiral) + form label
- PP remaining: "8 / 10"
- Power indicator (dots 1–5 scaled to power range)
- If move is out of PP: grayed out, labeled "No PP"
- If creature lacks body part for the move's form: grayed out, labeled "No Access" (form icon shown in red)

Hovering a move button highlights valid target tiles on the grid in the corresponding color. **Form-specific range overlay**: Physical moves show melee range (1–2 tiles, red), Energy moves show LoS cone/range (3–5 tiles, orange, blocked tiles dimmed), Bio moves show mid-range circle (2–3 tiles, green, ignoring cover). This overlay replaces the generic red attack range.

### Grid Display — Tile Highlighting
Tile highlight colors (additive overlay on tile material):

| Color | Meaning |
|-------|---------|
| Blue | Reachable movement tile for active creature |
| Red | Attack range for selected move |
| Green | Capture range (Gene Trap selected) |
| Gold | Terrain synergy tile (creature's type matches tile type) |
| Purple | Combo move range (adjacent ally with affinity 3+) |
| Orange | Hazard tile (electrified, poisoned, etc.) |
| White pulse | Currently hovered tile |

Only one highlight layer is active at a time (move selection overrides movement highlight). Highlights animate with a subtle pulse (opacity 0.6–1.0, 1Hz).

### Height Indicator
- Creatures on elevated tiles display a small upward arrow icon above their model.
- The height difference is shown as "+1" or "+2" in a badge when hovering a creature on high ground.

### Type Effectiveness Callout
- Rendered as a screen-space overlay that appears centered above the target creature.
- "Super Effective!" — red text, scale-up animation (0.8→1.0 in 0.15s), lingers 1.5s.
- "Not Very Effective..." — blue text, scale-down animation (1.0→0.9), lingers 1.5s.
- Neutral hits show no callout.
- No "Immune" or "No Effect" callout exists — the 14-type chart has no immunities (minimum 0.25x for dual-type resisted).
- Callouts queue; if two hits land in rapid succession, the second callout offsets vertically.

### Catch Predictor
- Appears in the Creature Info Panel when Gene Trap is selected as the active move.
- Shows: "Catch Chance: 43%" in a prominent label.
- Color coded: green > 60%, yellow 30–60%, red < 30%.
- Updates live as the player moves the Gene Trap cursor to different targets.
- If target is immune to capture (trainer battle, already owned), shows "Cannot Capture."

### Threat Indicators
- Wild enemy creatures display a threat meter above their model: 0–3 threat dots.
- 0 dots = passive (not targeting anyone)
- 1 dot = aware (targeting a creature)
- 2 dots = focused (committed target)
- 3 dots = enraged (taunt effect active or creature is low HP)
- Threat dot color matches the targeted creature's team color (red = targeting player, blue = targeting ally).

### Gene Trap Button
- Active only during player turn when wild encounter is active.
- Disabled states: no traps in inventory (shows count "0"), target is not capturable.
- Selecting Gene Trap switches tile highlights to green capture range.

### Switch Button
- Opens a party overlay listing the current party lineup (4–6 slots).
- Switching costs the creature's turn (uses its action).
- Fainted creatures shown grayed with a skull icon.
- Switching to a creature with synergy terrain already occupied shows a gold highlight on their portrait.

### Pause / Menu Access
- ESC key opens pause menu overlay (combat pauses; no time-based elements in MVP).

## 4. Formulas

**HP Bar Fill Ratio:**
```
fillRatio = currentHP / maxHP
```

**Instability Bar Fill:**
```
instabilityFill = instability / 100
```

**PP Display:**
```
ppLabel = currentPP + " / " + maxPP
```

**Catch Predictor Display:**
See Capture System for full formula. UI reads `CaptureSystem.CalculateCatchChance(target, trapType)` and displays as percentage rounded to nearest integer.

**Threat Dot Count:**
```
threatDots = Clamp(Floor(threatScore / 25), 0, 3)
```
Where threatScore is provided by the Threat/Aggro System (0–100).

## 5. Edge Cases

- **No valid targets for selected move:** Red highlight tiles are empty. Move button shows a subtle shake animation and a "No Targets" tooltip.
- **Gene Trap selected against a trainer:** Trap button becomes disabled immediately. If player somehow triggers it, show "Cannot capture trainer's creature" toast.
- **Creature has fewer than 4 moves:** Empty move slots display "—" and are non-interactive.
- **All moves at 0 PP:** The creature automatically uses "Struggle" (a built-in fallback move). Move panel shows only Struggle, Switch, and Gene Trap slots.
- **Status effect overflow (>4 active):** Only the 4 most recently applied are shown. A "+" badge on the 4th icon indicates more are active. Hovering shows a tooltip list.
- **Turn order changes mid-turn (speed buff/debuff):** Turn order bar re-sorts with a slide animation. If the active creature's turn order changes, a brief "Order Changed" toast appears.
- **Creature faints mid-highlighting:** If the highlighted creature faints before the player confirms action, highlights clear and the targeting UI resets to move selection.
- **Instability breakthrough:** At instability >= 75, the instability bar flashes white for 0.5s every 3 seconds. The creature's portrait in the turn order bar shows a distortion shimmer.

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Turn Manager | Drives when move selection panel is active; provides turn order |
| Creature Instance | Source for HP, instability, moves, status effects, affinity |
| Grid/Tile System | Provides reachable/attackable tile sets for highlighting |
| Capture System | Provides catch probability for Catch Predictor display |
| Type Chart System | Provides effectiveness multiplier for callout display |
| Threat/Aggro System | Provides threat score for threat dot display |
| Party System | Provides party list for Switch overlay |
| Combat Feedback | Handles type callout animation; UI owns placement, feedback owns animation |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `hpBarGreenThreshold` | 0.5 | Fill ratio above which bar is green |
| `hpBarYellowThreshold` | 0.25 | Fill ratio above which bar is yellow |
| `instabilityWarningThreshold` | 50 | Show "!" warning icon |
| `instabilityFlashThreshold` | 75 | Bar flashes white |
| `tileHighlightPulseHz` | 1.0 | Highlight pulse frequency |
| `tileHighlightMinOpacity` | 0.6 | Min opacity in pulse cycle |
| `effectivenessCalloutDuration` | 1.5s | How long callout lingers |
| `maxVisibleTurnOrderIcons` | 8 | Icons before horizontal scroll |
| `catchPredictorGreenThreshold` | 0.6 | Catch chance above which color is green |
| `catchPredictorYellowThreshold` | 0.3 | Catch chance above which color is yellow |

## 8. Acceptance Criteria

- [ ] Turn order bar displays all combat participants in correct initiative order with HP pips visible.
- [ ] Active creature's turn order icon is highlighted with gold border.
- [ ] Creature Info Panel shows correct HP, instability, type badges, and status effects for the selected creature.
- [ ] Selecting a move highlights valid tiles in the correct color (blue=move, red=attack, green=capture).
- [ ] "Super Effective!" callout appears with red text animation when a super-effective hit lands.
- [ ] "Not Very Effective..." callout appears with blue text when a resisted hit lands.
- [ ] Catch Predictor updates correctly when Gene Trap is selected and displays color-coded percentage.
- [ ] Gene Trap button is disabled during trainer battles and when trap inventory is 0.
- [ ] Threat dots above wild creatures reflect the current threat score from the Aggro System.
- [ ] Instability bar shows "!" warning at instability >= 50 and flashes at >= 75.
- [ ] Switching creatures via the Switch button uses the creature's action and removes its turn from the current round.
- [ ] Gold terrain synergy highlight appears on tiles matching the active creature's type.
- [ ] Move buttons display genome type icon and damage form icon correctly.
- [ ] Moves with inaccessible form (no body part) show "No Access" in red.
- [ ] Form-specific range overlays display correctly (Physical=melee, Energy=LoS, Bio=mid-range ignoring cover).
- [ ] No "Immune" or "No Effect" callout ever appears (no immunities in the 14-type chart).
