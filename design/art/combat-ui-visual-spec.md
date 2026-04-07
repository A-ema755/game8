# Gene Forge — Combat UI Visual Design Spec v0.1

> Authoritative design source: `design/gdd/combat-ui.md`
> UX spec companion: `design/ux/combat-ui-ux-spec.md`
> This document defines the visual treatment. The GDD defines "what"; the UX spec defines "how it behaves"; this spec defines "how it looks."

---

## Design Philosophy

The aesthetic is **biopunk lab interface** — not sterile sci-fi white, not grunge fantasy. Think genome sequencer readout crossed with a field researcher's ruggedized tablet. Panels feel like durable matte hardware running phosphor-glow software. The grid viewport is the specimen; the panels are the instruments observing it.

Three rules govern every decision:

1. **Panels recede, grid commands.** Dark, low-chroma panel backgrounds. The 3D viewport is never competed with.
2. **Color carries meaning, not decoration.** Every color in the UI is a signal. Type colors, bar thresholds, tile highlights — all functional, not ambient.
3. **Feedback is punchy and fast, UI is still.** The UI structure is static and calm. The feedback layer (callouts, pulses, transitions) does the expressive work.

---

## 1. Color Palette

### 1.1 Panel Foundations

| Token | Hex | Usage |
|---|---|---|
| `--panel-bg-primary` | `#0D1117` | Main panel backgrounds (creature info, move panel) |
| `--panel-bg-secondary` | `#151C25` | Turn order bar background |
| `--panel-bg-raised` | `#1C2631` | Elevated cards, move buttons, type badges |
| `--panel-border-default` | `#2A3A4A` | Standard panel dividers and button outlines |
| `--panel-border-active` | `#C8980A` | Active/selected states — gold |
| `--panel-border-subtle` | `#1E2D3D` | Hairline separators between elements |

Near-black with cool blue-green undertones. Matches the biopunk "lab in the field" tone. Avoids pure black (#000000).

### 1.2 Text Colors

| Token | Hex | Usage |
|---|---|---|
| `--text-primary` | `#E8EEF4` | Names, move names, stat values — main reading text |
| `--text-secondary` | `#7A9BB5` | Labels, sub-labels, "HP", "PP", "LVL" prefixes |
| `--text-disabled` | `#3D5468` | Grayed-out elements (empty move slots, no-PP) |
| `--text-warning` | `#E8A020` | Warning states ("!", instability warnings) |
| `--text-danger` | `#D94040` | Error/danger states ("No Access", low HP text) |
| `--text-callout-se` | `#FF4444` | "Super Effective!" callout |
| `--text-callout-nve` | `#5588FF` | "Not Very Effective..." callout |

### 1.3 HP Bar Thresholds

HP bar uses a single bar element. Fill color changes based on fill ratio, not a gradient.

| State | Threshold | Fill Color | Hex |
|---|---|---|---|
| Healthy | > 50% | Phosphor green | `#2ECC5A` |
| Caution | 25%–50% | Amber | `#E8A020` |
| Critical | < 25% | Red | `#D94040` |

Track (background) color: `#0A1520`
Track border: `#1E2D3D`

### 1.4 Instability Bar Gradient

Pre-baked texture strip with these gradient stops:

| Stop Position | Color | Hex | State |
|---|---|---|---|
| 0% | Stable green | `#2ECC5A` | Clean DNA |
| 45% | Transitional yellow | `#C8C020` | Tension building |
| 65% | Amber-orange | `#E8720A` | Warning zone |
| 85%–100% | Danger red | `#CC2222` | Breakthrough risk |

"!" warning icon at fill >= 50%: `--text-warning` `#E8A020`.
At fill >= 75%: right 25% of bar flashes white (`#FFFFFF`) at 0.5Hz.

### 1.5 Catch Predictor Colors

| State | Threshold | Color | Hex |
|---|---|---|---|
| Good odds | > 60% | Green | `#2ECC5A` |
| Marginal | 30%–60% | Amber | `#E8A020` |
| Low odds | < 30% | Red | `#D94040` |
| Cannot capture | — | Muted gray text | `--text-disabled` |

### 1.6 Type Badge Colors (All 14 Types)

Each type badge is a small pill with solid background and white label text.

| Type | Background | Hex | Text |
|---|---|---|---|
| Thermal | Ember orange | `#C84A1A` | White |
| Aqua | Ocean blue | `#1A6ECC` | White |
| Organic | Forest green | `#2A7A2E` | White |
| Bioelectric | Electric yellow | `#B8A010` | White |
| Cryo | Ice blue | `#3A8FB5` | White |
| Mineral | Stone gray | `#6A7A5A` | White |
| Toxic | Acid purple | `#8030C0` | White |
| Neural | Pale violet | `#6050A8` | White |
| Ferro | Steel blue-gray | `#4A6078` | White |
| Kinetic | Burnt orange | `#A05020` | White |
| Aero | Sky cyan | `#2898B0` | White |
| Sonic | Deep magenta | `#922870` | White |
| Ark | Bright gold | `#C89A10` | Dark `#0D1117` |
| Blight | Sickly olive | `#587010` | White |

Differentiation notes: Thermal/Kinetic — orange-red (fire) vs burnt brown-orange (impact). Neural/Toxic — cool violet (mind) vs saturated purple (corruption). Ark gets gold with dark text to signal apex-tier. Blight's sickly olive reads as disease/decay.

### 1.7 Damage Form Colors

| Form | Color | Hex | Icon |
|---|---|---|---|
| Physical | Bone white | `#D4C8A8` | Fist |
| Energy | Plasma blue | `#40A8E8` | Bolt |
| Bio | Spore green | `#58C870` | Spiral |

### 1.8 Tile Highlight Colors

Additive overlays on 3D tile material. Base colors at alpha 0.35; pulse drives to alpha 0.6 at peak.

| Color | Hex | Meaning |
|---|---|---|
| Movement blue | `#2060C8` | Reachable movement tiles |
| Attack red | `#C82020` | Attack range for selected move |
| Capture green | `#20C840` | Gene Trap capture range |
| Synergy gold | `#C8900A` | Terrain synergy (creature type matches tile) |
| Combo purple | `#8040C8` | Combo move range (post-MVP) |
| Hazard orange | `#C86020` | Hazard tile (electrified, poisoned, etc.) |
| Hover white | `#FFFFFF` | Currently hovered tile |

Hover white uses tighter pulse: 0.8–1.0 opacity at 2Hz.

### 1.9 Team Tints (Turn Order Icons)

Semi-transparent color overlay on portrait icon background.

| Team | Hex | Opacity |
|---|---|---|
| Player | `#1A4A8A` | 0.7 |
| Enemy | `#7A1A1A` | 0.7 |

Active icon overlay: `#C8900A` (gold), replaces team tint while active.

### 1.10 Button States

| State | Background | Border | Text | Notes |
|---|---|---|---|---|
| Normal | `#1C2631` | `#2A3A4A` | `#E8EEF4` | Resting state |
| Hover | `#243040` | `#3A5A6A` | `#FFFFFF` | Instant response |
| Pressed | `#111820` | `#2A3A4A` | `#C0CCD8` | Darkens slightly |
| Selected (gold) | `#1E1A0A` | `#C8900A` | `#E8D880` | Gold border + warm text |
| Disabled — No PP | `#141A22` | `#1E2D3D` | `#3D5468` | Full desaturation |
| Disabled — No Access | `#1A1010` | `#4A2020` | `#884040` | Red tint = blocked |
| Struggle | `#0D1A1A` | `#304848` | `#708888` | Teal-gray, distinct |

---

## 2. Typography

### Font Families

**Rajdhani** (Google Fonts, SIL Open Font License) — structural voice. Geometric, condensed, industrial edge. Bold and SemiBold weights only in UI.

**Share Tech Mono** (Google Fonts, OFL) — data readout voice. Monospace prevents number jitter during live updates.

Both imported as SDF fonts via TextMeshPro for crisp rendering at all scales.

**Rule**: Never use Rajdhani for bare numbers that update live. Never use Share Tech Mono for button labels or display names.

### Size Chart

| Element | Font | Weight | Size | Color Token |
|---|---|---|---|---|
| Creature name | Rajdhani | Bold | 22px | `--text-primary` |
| Level badge ("LVL 14") | Rajdhani | SemiBold | 14px | `--text-secondary` |
| Type badge label | Rajdhani | Bold | 11px | White |
| HP label ("HP") | Rajdhani | SemiBold | 11px | `--text-secondary` |
| HP value ("47 / 80") | Share Tech Mono | Regular | 13px | `--text-primary` |
| Instability label ("INSTABILITY") | Rajdhani | SemiBold | 10px | `--text-secondary` |
| Instability value ("63") | Share Tech Mono | Regular | 12px | Dynamic per threshold |
| Move name | Rajdhani | Bold | 15px | `--text-primary` |
| Move type label | Rajdhani | SemiBold | 11px | Type color (§1.6) |
| Move form label | Rajdhani | SemiBold | 10px | Form color (§1.7) |
| PP value ("8 / 10") | Share Tech Mono | Regular | 12px | `--text-primary` |
| Turn order creature name | Rajdhani | SemiBold | 10px | `--text-primary` |
| Catch predictor label | Rajdhani | Bold | 16px | Dynamic per threshold |
| Catch predictor prefix | Rajdhani | SemiBold | 11px | `--text-secondary` |
| "Super Effective!" callout | Rajdhani | Bold | 28px | `--text-callout-se` |
| "Not Very Effective..." callout | Rajdhani | Bold | 22px | `--text-callout-nve` |
| Status effect tooltip | Rajdhani | Regular | 12px | `--text-primary` |
| "No PP" / "No Access" labels | Rajdhani | SemiBold | 11px | Per disabled state |
| Height badge ("+2") | Share Tech Mono | Regular | 12px | `#E8EEF4` |
| Toast messages | Rajdhani | SemiBold | 13px | `--text-primary` |

Letter spacing: all Rajdhani labels use `letter-spacing: 0.04em`. Monospace elements use `letter-spacing: 0`.

---

## 3. Spacing and Layout

All measurements target 1080p (1920x1080) as reference resolution. Scale via `PanelSettings.referenceResolution` and `PanelSettings.screenMatchMode`.

### 3.1 Panel Geometry

| Panel | Width | Height | Position |
|---|---|---|---|
| Turn order bar | 100% (1920px) | 72px | Top, y=0 |
| Creature info panel | 320px | calc(100% - 72px - 140px) ~= 868px | Left, below turn bar |
| Move selection panel | 100% (1920px) | 140px | Bottom |
| Grid viewport | Fills remainder (~1600px) | 868px | Center-right |

### 3.2 Panel Internal Spacing

| Context | Value |
|---|---|
| Panel outer padding | 12px all sides |
| Section internal padding | 8px |
| Gap between labeled elements (HP bar + label) | 4px |
| Gap between stacked sections (name → HP → instability) | 10px |
| Gap between move buttons (row gap) | 8px |
| Gap between move buttons (column gap) | 8px |
| Panel border radius (outer corners) | 3px |
| Move button inner border radius | 4px |
| Type badge border radius | 3px |
| Status effect icon border radius | 2px |

Slight 3px outer radius on panels for a softer industrial feel while maintaining the lab-instrument aesthetic. Move buttons and badges get 4px and 3px respectively for legibility on interactive elements.

### 3.3 Icon and Bar Sizes

| Element | Size / Dimensions |
|---|---|
| Turn order portrait icon | 52x52px |
| Turn order active icon (enlarged) | 62x62px |
| HP pip (turn order bar) | 6x6px, 2px gap between pips |
| HP bar height | 10px |
| HP bar width | Full panel content width minus label (~270px) |
| Instability bar height | 8px |
| Instability bar width | Same as HP bar |
| Type badge height | 20px, min-width 52px, auto-width with 6px h-padding |
| Status effect icon | 24x24px |
| Status turn-count badge | 12x12px, positioned bottom-right of icon |
| Body part slot icon | 32x32px |
| Damage form icon (move button) | 16x16px |
| Type icon (move button) | 16x16px |
| Height badge | 28x18px |
| Catch predictor label block | Full creature info panel width |

### 3.4 Move Button Layout

Move selection panel contains a centered `max-width: 1200px` flex container. Button layout: 3 columns x 2 rows. Each button = `(1200 - 12*2) / 3` = 392px wide x 64px tall. 360px dead space flanking each side (panel background fills full width).

### 3.5 Turn Order Bar Layout

Icons in a horizontal flex row, left-aligned, 8px gap. Scroll starts when total icon width exceeds panel width minus 48px. Active icon: 62x62px, pushes adjacent icons via margin. Use `margin: -5px` on adjacent icons to soften the gap change during enlargement animation.

---

## 4. Component Styling

### 4.1 Turn Order Icon

Stacked layers:
1. Square base: 52x52px, `background: team tint at 0.7 opacity` over `--panel-bg-raised`, `border-radius: 3px`
2. Portrait image: cropped square, fills base
3. Gold border overlay when active: `2px solid #C8900A`, `border-radius: 3px`
4. HP pip strip: pinned to bottom edge, 2px inset. Each pip: 6x6px, `border-radius: 1px`. Fill: `#2ECC5A` (full) / `#1E2D3D` (empty)
5. Status icon: 14x14px, top-right corner, 2px inset. First active status only.

HP pip count formula:
```
visiblePips = creature.MaxHP <= 30 ? 3 : creature.MaxHP <= 60 ? 4 : 5
filledPips  = ceil(visiblePips * (creature.CurrentHP / creature.MaxHP))
```

Enemy tint: `background-color: #7A1A1A` at 0.7 — applied as `::before` pseudo-element overlay so portrait remains visible underneath.

### 4.2 HP Bar

```
[Label "HP"]  [Value "47 / 80"]
[============================  ]  <- track with fill
```

- Track: `height: 10px`, `background: #0A1520`, `border: 1px solid #1E2D3D`, `border-radius: 2px`
- Fill: solid color per threshold (no gradient)
- Fill transition: `width 0.3s ease-out`
- Color threshold crossing: instant swap, no crossfade

### 4.3 Instability Bar

Same track as HP bar but 8px tall (2px shorter, visually subordinate).

Fill is a pre-baked texture strip (gradient per §1.4). Masking via overflow-hidden controls visible amount.

At instability >= 50: "!" icon (`#E8A020`, Rajdhani Bold 14px) appears right of bar.
At instability >= 75: right 25% flashes via semi-transparent white overlay pulsing opacity 0→0.6 at 0.5Hz.

### 4.4 Move Buttons

Each button is a flex column:

```
[Row 1] [TypeIcon] [TypeLabel]    [FormIcon] [FormLabel]   (11px)
[Row 2] Move Name                                          (15px)
[Row 3] PP: [8 / 10]   Power: [*****]                     (12px)
```

- Outer: `height: 64px`, `padding: 8px 12px`, `border-radius: 4px`
- Background: `--panel-bg-raised` `#1C2631`
- Border: `1px solid #2A3A4A`

Row 1: Type icon + label left-aligned; form icon + label right-aligned (justify-content: space-between).

Row 2: Move name, Rajdhani Bold 15px, left-aligned. Truncate with ellipsis if needed.

Row 3: PP readout left, power dots right. Power dots: 5 circles at 6x6px, 3px gap, `border-radius: 50%`. Filled: type color at 80% opacity. Empty: `#1E2D3D`.

**No PP state**: `opacity: 0.55` on whole button. "NO PP" overlay label centered: Rajdhani SemiBold 11px, `--text-disabled`, `letter-spacing: 0.08em`.

**No Access state**: `background: #1A1010`, `border-color: #4A2020`, text `#884040`. Form icon gets red tint. "NO ACCESS" label same treatment as No PP in `#884040`.

**Struggle state**: `background: #0D1A1A`, `border-color: #304848`. "STRUGGLE" label in `#708888`. Occupies slot 1 only; slots 2-4 are `display: none`.

### 4.5 Type Badge

Pill shape: `height: 20px`, `padding: 0 6px`, `border-radius: 3px`, `background: [type color]`. Label: Rajdhani Bold 11px, uppercase, `letter-spacing: 0.06em`, white (dark `#0D1117` for Ark).

Two badges max: horizontal row, `gap: 4px`.

### 4.6 Status Effect Icons

Each: 24x24px, `border-radius: 2px`. Background: translucent dark backing `#0D111780`.

Turn-count badge: 12x12px circle pinned bottom-right, `background: #1C2631`, `border: 1px solid #2A3A4A`. Text: Share Tech Mono Regular 9px.

Overflow (>4 active): 4th icon shows "+" badge — `background: #E8A020`, white text.

### 4.7 Catch Predictor

Displayed in creature info panel when Gene Trap is active move.

```
CATCH CHANCE
43%
```

"CATCH CHANCE" label: Rajdhani SemiBold 11px, `--text-secondary`, `letter-spacing: 0.08em`.
Percentage: Rajdhani Bold 28px, color per §1.5 thresholds.

"Cannot Capture": replaces percentage. Rajdhani SemiBold 14px, `--text-disabled`.

Block styling: `background: #0A1520`, `border: 1px solid --panel-border-subtle`, `padding: 8px`, `border-radius: 2px`.

---

## 5. Animation Specs

USS transitions where possible. Complex multi-stage animations (type callouts, switch overlay) require C# coroutines or UI Toolkit Experimental Animation API.

### 5.1 Tile Highlight Pulse

- Property: `opacity` of tile highlight overlay
- Range: 0.35 → 0.6 → 0.35
- Frequency: 1Hz (1 second full cycle)
- Easing: `ease-in-out` (sinusoidal)
- Implementation: Sine wave from `Time.time` in C#
- Hover white: range 0.8 → 1.0, frequency 2Hz

### 5.2 Type Effectiveness Callout

**"Super Effective!"**
- Entry: scale `0.6 → 1.1` over 0.1s, `cubic-bezier(0.2, 0.8, 0.4, 1.2)` (overshoot)
- Settle: scale `1.1 → 1.0` over 0.1s, `ease-out`
- Linger: 1.2s hold
- Exit: opacity `1.0 → 0.0` over 0.3s, `ease-in`
- Total: 1.7s

**"Not Very Effective..."**
- Entry: scale `1.0 → 0.9` over 0.15s, `ease-out` (deflate, no bounce)
- Linger: 1.25s hold
- Exit: opacity `1.0 → 0.0` over 0.3s, `ease-in`
- Total: 1.7s

Queued second callout: Y offset `+40px` from first.

### 5.3 Move Button — Invalid Target Shake

- Sequence: translate X `0 → +6px → -6px → +4px → -4px → 0`
- Each step: 0.05s, total 0.25s
- Easing: `linear` (mechanical rattle)
- After shake: border flashes `#884040` for 0.1s

### 5.4 HP Bar Fill Transition

- Property: `width` (fill element)
- Duration: 0.3s
- Easing: `ease-out`
- Color threshold crossing: instant swap before transition begins

### 5.5 Turn Order Active Icon Enlargement

- Size: `52px → 62px` over 0.2s, `ease-out`
- Team tint → gold: 0.2s transition
- Gold border: `border-width 0 → 2px` over 0.1s (appears just before full size)
- Reverse on turn end: 0.15s

### 5.6 Switch Overlay — Open and Close

**Open:**
- Background dim: `rgba(0,0,0,0)` → `rgba(0,0,0,0.72)` over 0.2s, `ease-out`
- Panel slide: `translateY(+120px)` → `translateY(0)` over 0.25s, `cubic-bezier(0.25, 0.46, 0.45, 0.94)`
- Panel starts 0.05s after dim

**Close:**
- Panel slide: `translateY(0)` → `translateY(+120px)` over 0.18s, `ease-in`
- Dim fade: 0.15s, starting 0.05s after panel exit
- Total: 0.23s (snappier than open — closing feels decisive)

Fainted slots: `opacity: 0.35`, `filter: grayscale(80%)`. Skull icon: 16x16px centered on portrait, `#D94040`.

---

## 6. Consistency Notes — Visual Identity Foundation

This spec is the first layer of the Gene Forge art bible. Every decision here sets precedent for party management UI, research station UI, and world map UI.

### Panel Darkness is Non-Negotiable
The grid viewport operates at full brightness. Panel backgrounds must stay at `#0D1117`–`#1C2631`. Future lighter surfaces (tutorial cards, tooltips) must use the dark panel family plus a `backdrop-filter: blur` treatment — no light-mode surfaces anywhere.

### Type Color System is Canonical
The 14 type colors in §1.6 are the single source of truth for the entire game: move buttons, type badges, tile overlays, damage tints, portrait borders in party screen. No alternate type colors in other screens.

### Gold = Active / Selected / Apex
`#C8900A` and variants (`#C8980A`, `#E8D880`) signal "this is the thing happening now." Reserved for: turn order active icon, selected move button, terrain synergy tiles, Ark type badge, Pokedex completion tiers. Do not use for decorative borders or secondary states.

### Phosphor Green = Healthy / Progressing / Confirmed
`#2ECC5A` signals a good state: HP above 50%, successful capture, affinity pips, capture range tiles. This is the "yes" color.

### Red Family = Danger / Blocked / Enemy
`#D94040` (system danger), `#CC2222` (instability peak), `#C82020` (attack tiles), `#7A1A1A` (enemy tint), `#FF4444` (super effective callout). Same red family intentionally.

### Font Register Rule
Rajdhani is the structural voice (labels, names, callouts). Share Tech Mono is the data readout voice (numbers). Maintain this split in all future UI.

### Instability Visual Language
Instability effects (glitch, UV flicker, chromatic aberration) should feel biological — color noise not static, drift not jitter. A creature's appearance fights to hold itself together. Biopunk keeps horror and sci-fi readable simultaneously.
