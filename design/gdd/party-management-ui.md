# Party Management UI

## 1. Overview

The Party Management UI is the full-screen interface for reviewing, modifying, and reorganizing the player's creature roster. It contains two primary screens: the Creature Details screen (stats, parts, DNA mods, instability, affinity, scars) and the Creature Forge sub-screen (DNA alteration with real-time 3D model preview). It also provides access to storage (unlimited creature box), a part browser, and party swap. All screens are built with Unity UI Toolkit and are accessible from Research Stations only (no mid-combat access except the Switch overlay).

## 2. Player Fantasy

Opening the Party Management UI should feel like entering a personal laboratory. The player sees their creatures as living projects — the Forge screen makes DNA modification feel hands-on and consequential. Watching the 3D model update in real time as a new wing part slots in is the core moment of "I built this." The storage browser rewards organization and collection. The instability meter and scar list give each creature a history and personality — they're not just stats, they're characters.

## 3. Detailed Rules

### Navigation Structure
```
Party Management UI
├── Party Overview (default landing screen)
│   ├── Active Party Slots (4–6 cards)
│   └── Quick actions: Heal All, Sort Party
├── Creature Details Screen
│   ├── Stats Tab
│   ├── Moves Tab
│   ├── DNA Tab
│   └── Lore Tab (Pokedex link)
├── Creature Forge Sub-Screen
│   ├── 3D Model Preview (left panel)
│   ├── Slot Diagram (center)
│   └── Part/DNA Browser (right panel)
├── Storage Browser
│   ├── Search bar
│   ├── Filter chips (type, rarity, level range, has-part)
│   └── Sort options (level, name, instability, affinity)
└── Party Swap Interface
```

### Party Overview
- Shows all active party slots as cards (portrait, name, level, HP bar, type badges).
- Fainted creatures display a red border and skull icon.
- Clicking a card opens the Creature Details screen for that creature.
- Drag-and-drop reordering of party slots.
- "Heal All" button only active at Research Station (costs 0 RP at default station; cost configurable post-MVP).

### Creature Details Screen

**Stats Tab:**
- Base stats displayed as horizontal bars: HP, Attack, Defense, Speed, Accuracy.
- Each bar shows: stat name, current value, and a fill bar normalized to 150 (max possible base stat).
- DNA mod bonuses shown as a lighter color extension on the same bar (stacked visual).
- Total stat sum displayed at bottom.
- Level-up projection: "Next level: +3 HP, +2 ATK" (from LevelingSystem growth curve).

**Moves Tab:**
- 4 move slots, each showing: move name, type icon, category icon, power, accuracy, PP (current/max), and a description tooltip on hover.
- "Forget Move" button per slot (requires confirmation dialog).
- "Learn Move" button if creature has unlearned moves available at current level (via move reminder).
- DNA-modified moves show a DNA helix icon and a tooltip listing active modifications.

**DNA Tab:**
- Instability meter (large, prominent): current value / 100 with color coding.
- DNA modification list: each active mod shown as a card with: mod name, type, instability contribution, and a "Remove" button (only if mod is removable; innate mods from eggs are not removable).
- Body part slot diagram (see Forge sub-screen for details).
- Personality trait badge (if equipped): name, description, visual indicator color.
- "Open Forge" button — navigates to Creature Forge sub-screen.

**Lore Tab:**
- Links to the creature's Pokedex entry.
- Shows Pokedex research tier (Silhouette / Basic / Full / Research Complete).
- Scar list: each scar shown with position label, type, and source battle ID.
- Affinity bar: current bond level (0–5), XP progress to next level, bond description.
- Creature biography: auto-generated from battles fought, captures, DNA mods applied (e.g., "Captured in Verdant Basin. Has survived 3 near-death battles. 7 DNA mods applied.").

### Creature Forge Sub-Screen

**3D Model Preview (left panel):**
- Renders the creature's current 3D model in a dedicated preview viewport (RenderTexture).
- Rotates slowly (15°/s) on Y axis; player can click-drag to manually rotate.
- Model updates in real time when parts are added or removed (apply/preview is immediate in the forge; confirmation commits changes).
- Parts being previewed (not yet confirmed) render with a blue highlight glow.
- Unstable mods (instability contribution > 20) render with a red particle aura.
- Instability meter updates live as parts are previewed.

**Slot Diagram (center panel):**
Displays the creature's archetype body diagram with clickable slot hotspots.

Slot types by archetype:
| Archetype | Slots |
|-----------|-------|
| Bipedal | Head, Back, L-Arm, R-Arm, Legs, Tail |
| Quadruped | Head, Back, Front-L, Front-R, Rear-L, Rear-R |
| Serpentine | Head, Upper-Body, Mid-Body, Lower-Body, Tail |
| Avian | Head, Wings, Talons, Tail, Back |
| Amorphous | Core, Extension-1, Extension-2, Extension-3 |

- Occupied slots show the equipped part icon.
- Empty slots show a faint outline with a "+" icon.
- Clicking a slot opens the Part Browser filtered to compatible parts for that slot.
- Conflicting part combinations show a warning icon on both conflicting slots (e.g., Carapace + Wings conflict).

**Part/DNA Browser (right panel):**
- Tab strip: "Body Parts" | "DNA Mods" | "Personality"
- Part cards show: part name, type icon, slot compatibility, rarity badge, description, stat changes, move unlocks, instability cost.
- Filter: by slot type, part type, rarity (Common/Uncommon/Rare/Forbidden).
- Sort: by instability cost, rarity, recently acquired.
- "Equip" button: places part in the previewed state. "Confirm" commits all previewed changes.
- "Remove" button on equipped parts.
- Parts not owned are shown as grayed locked cards. Hovering shows acquisition hint.

**Confirmation Flow:**
1. Player makes changes in Forge (preview mode).
2. A "Confirm Changes" button glows at the bottom, listing: parts added/removed, mods applied/removed, instability delta.
3. If any change has mutation risk, a risk warning dialog appears: "This mod has a 15% chance of side effects. Proceed?"
4. On confirmation, changes apply to the CreatureInstance and cannot be undone (except removing the mod later if it's removable).

### Storage Browser
- Grid of creature cards (4 columns).
- Search: fuzzy name search across creature names and species.
- Filter chips: type badges, rarity dots, level range slider, "Has Part" toggle, "Fainted" toggle.
- Sort dropdown: Level (desc), Name (A-Z), Instability (desc), Affinity (desc), Date Captured.
- Clicking a creature card opens Creature Details (view-only if not in party; "Add to Party" button available if party has an open slot or a slot can be swapped).
- Party slots shown at top of storage screen; drag creature card to party slot to swap.
- Max storage: unlimited (soft cap of 999 for UI pagination).

### Party Swap Interface
- Accessible from Storage Browser or the "Switch" option during combat.
- Side-by-side view: current party (left) vs. selected box creature (right).
- Stat comparison bars highlight which creature is higher in each stat.
- "Swap" button replaces the selected party slot with the box creature.
- Swapped creatures go to storage automatically.

## 4. Formulas

**Stat Bar Fill:**
```
fillRatio = baseStat / 150
dnaBonusFill = dnaBonusValue / 150
totalBarFill = (baseStat + dnaBonusValue) / 150
```

**Instability Contribution Display:**
```
modInstabilityContribution = mod.instabilityCost  // from DNAModConfig
totalInstability = sum(mod.instabilityCost for mod in equippedMods)
```

**Part Conflict Check:**
```
hasConflict = partA.conflictTags.Any(tag => partB.conflictTags.Contains(tag))
```

**Biography Auto-Generation:**
```
biography = $"Captured in {captureLocation}. "
          + $"Has survived {nearDeathCount} near-death battles. "
          + $"{dnaModCount} DNA mods applied."
          + (scarCount > 0 ? $" Bears {scarCount} battle scars." : "")
```

## 5. Edge Cases

- **Creature has 0 moves (all forgotten):** Moves Tab shows a warning "This creature has no moves. It will use Struggle in battle." The Learn Move button is highlighted.
- **Part equip that would exceed instability 100:** Show a red warning "Instability will reach [value]. Creature may become uncontrollable." Allow equip but require extra confirmation click.
- **Confirmation interrupted by closing UI:** If player closes the Forge before confirming, previewed (uncommitted) changes are discarded. A "Discard Changes?" dialog appears if there are uncommitted previews.
- **Storage has 0 creatures:** Storage browser shows an empty state illustration: "No creatures in storage."
- **All party slots fainted:** Party overview shows all red borders. "Heal All" button is the primary CTA. Cannot open Forge while all creatures are fainted.
- **Innate mod (from egg) has "Remove" clicked:** Button is disabled. Tooltip: "Innate traits cannot be removed."
- **Part requiring higher station tier:** Part card renders with a padlock and tier requirement label. Equip button disabled.
- **3D preview model not loaded yet:** Show a spinner in the preview viewport. Timeout after 3s shows a fallback silhouette.
- **Creature with Feral personality at high instability:** A flashing red warning banner at top of Forge: "WARNING: Feral + High Instability — may attack allies." Equipping further instability mods requires double confirmation.

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Creature Instance | Source for all creature stats, moves, DNA mods, parts, affinity, scars |
| DNA Alteration System | Executes mod apply/remove operations from Forge confirmation |
| Body Part System | Provides part configs, slot compatibility, conflict rules |
| Pokedex System | Lore Tab reads Pokedex research tier and entry data |
| Creature Affinity System | Affinity bar data and bond descriptions |
| Battle Scar System | Scar list data for Lore Tab |
| Station Upgrade System | Gates part/mod browser to available station features |
| Save/Load System | Party and storage state persists in save JSON |
| VFX System | 3D preview glow effects for previewed/unstable parts |
| Party System | Executes party swap operations |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `modelPreviewRotationSpeed` | 15°/s | Auto-rotate in preview |
| `statBarMaxValue` | 150 | Normalization cap for stat bars |
| `storagePageSize` | 30 | Creatures per storage grid page |
| `forgePreviewGlowColor` | Blue (#4488FF) | Color for previewed parts |
| `unstableModAuraThreshold` | 20 | Instability cost above which red aura shows |
| `instabilityOverflowWarning` | 100 | Threshold for double-confirm dialog |
| `feralInstabilityWarningThreshold` | 60 | Warning threshold for Feral personality |
| `modelPreviewLoadTimeout` | 3s | Before fallback silhouette shows |
| `storageMaxDisplay` | 999 | Soft cap for pagination |

## 8. Acceptance Criteria

- [ ] Party overview displays all active party creatures with correct HP, type badges, and faint state.
- [ ] Creature Details Stats Tab shows all 6 base stats as bars, with DNA bonus visually overlaid.
- [ ] Creature Forge 3D preview updates in real time when a part is equipped in preview mode.
- [ ] Uncommitted forge changes are discarded (with confirm dialog) when the UI is closed without confirming.
- [ ] Part conflict (e.g., Carapace + Wings) shows warning icons on both conflicting slots.
- [ ] Innate mods (from eggs) cannot be removed — button is disabled with tooltip.
- [ ] Storage browser search and filter reduce the displayed creature list correctly.
- [ ] Party swap replaces the correct party slot and moves the displaced creature to storage.
- [ ] Scar list in the Lore Tab displays all scars with correct type and source labels.
- [ ] Parts requiring a higher station tier display a padlock and cannot be equipped.
- [ ] Instability meter updates live as mods are previewed in the Forge.
- [ ] "Confirm Changes" dialog lists all pending changes and mutation risk before committing.
