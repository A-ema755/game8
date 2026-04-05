# Type System Redesign — Changelog

**Date**: 2026-04-05
**Scope**: 8-phase redesign across 20+ GDD files

---

## Summary

Replaced the original 8-type system (Fire, Water, Grass, Electric, Ice, Rock, Dark, Psychic) with a 14 genome type system organized in 3 tiers, and introduced a 3-form damage system (Physical, Energy, Bio).

---

## New Type System

### 14 Genome Types (3 Tiers)

| Tier | Types |
|------|-------|
| Standard (8) | Thermal, Aqua, Organic, Bioelectric, Cryo, Mineral, Toxic, Neural |
| Extended (4) | Ferro, Kinetic, Aero, Sonic |
| Apex (2) | Ark, Blight |

### Key Rules

- 36 super-effective relationships (2.0x)
- No immunities (minimum 0.25x for dual-type resisted)
- Every type resists itself (0.5x)
- Dual-type stacking is multiplicative (max 4.0x, min 0.25x)
- STAB 1.5x for primary, secondary, AND infused genome types
- Instability 80+ grants Blight as secondary genome type

### 3 Damage Forms

| Form | Stat Pairing | Range | Cover | Height Bonus |
|------|-------------|-------|-------|-------------|
| Physical | ATK vs DEF | 1-2 (melee) | Blocked by walls/cover | Yes |
| Energy | ATK vs SPD | 3-5 (ranged) | Cover reduces 50%, requires LoS | Yes |
| Bio | ACC vs DEF | 2-3 (mid) | Ignores cover | No |

Body parts unlock damage form access (Jaws/Claws→Physical, Glands/Vents→Energy, Spores/Stingers→Bio).

---

## Phase Execution Log

### Phase 1: Type Chart System GDD
- **File**: `type-chart-system.md` — Full rewrite
- 15x15 effectiveness matrix with 36 SE relationships
- CreatureType enum (14 values), EffectivenessLabel enum (Resisted/Neutral/SuperEffective)
- TypeChart static class with O(1) lookup (ADR-007)

### Phase 2: Damage & Health System GDD
- **File**: `damage-health-system.md` — Full rewrite
- Added DamageForm enum (None, Physical, Energy, Bio)
- DamageCalculator with GetFormStatPairing() and GetFormHeightBonus()
- MoveConfig updated: genomeType + form fields
- Form-terrain interaction table (walls, cover, LoS, height)

### Phase 3: Body Part System GDD
- **File**: `body-part-system.md` — Updated
- Added `DamageForm formAccess` to BodyPartConfig
- Section 3.8 "Damage Form Access" with GetAvailableForms()
- 5 new parts: glands-thermal, core-bioelectric, stinger-venom, spore-pods, tendrils-neural
- 7 new acceptance criteria

### Phase 4: DNA Alteration System GDD
- **File**: `dna-alteration-system.md` — Updated
- Type infusion grants STAB (1.5x) + resistance (0.5x)
- Blight secondary type at instability 80+
- Instability only from research stations, never combat
- 5 new edge cases, 7 new acceptance criteria

### Phase 5: Move Database GDD
- **File**: `move-database.md` — Full rewrite
- All 25 moves remapped to new genome types
- New moves: toxic-spore, leech-sting, neural-claw, mind-beam, ferro-bite
- Body part access gating for damage forms
- Form defaults table (Section 3.2)

### Phase 6: Creature & Supporting System GDDs
- **Files updated**:
  - `creature-database.md` — 8 creatures updated with genome types, default parts, form access
  - `creature-instance.md` — ActiveSecondaryType (Blight), AvailableForms HashSet
  - `ai-decision-system.md` — ScoreGenomeMatchup() and ScoreFormTactics() scoring
  - `combat-ui.md` — Genome type + damage form icons, form-specific range overlays
  - `combat-feedback.md` — Form-specific hit VFX, removed Immune callout
  - `terrain-system.md` — Renamed synergy types, form-terrain interaction table
  - `grid-tile-system.md` — Form-specific LoS notes, split height formula
  - `capture-system.md` — Specialist Gene Trap references genome type
  - `encounter-system.md` — Apex Type Encounter Design section
  - `pokedex-system.md` — Apex Registry tab, 14-type filter

### Phase 7: Global Sweep
- **Files updated**:
  - `systems-index.md` — Updated Type Chart, Damage & Health, Move Database descriptions
  - `game-concept.md` — All type references throughout
  - `architecture-reference.md` — Asset name examples, enum examples
  - `data-configuration-pipeline.md` — CreatureType enum (14 types), DamageForm enum, TerrainType enum
  - `color-pattern-system.md` — 14 genome type color table
  - `battle-scar-system.md` — Scar triggers renamed (Thermal, Cryo, Toxic, Bioelectric)
  - `combo-move-system.md` — All 10 combo pairings remapped
  - `day-night-cycle.md` — Dark→Neural, Psychic→Ark, Ghost→Blight
  - `environmental-puzzle-system.md` — All field ability types remapped

### Phase 8: Cleanup
- Deleted `type-system-redesign-brief.md`
- Deleted `type-system-redesign-prompt.md`
- Created this changelog

---

## Type Name Mapping (Old → New)

| Old Type | New Genome Type |
|----------|----------------|
| Fire | Thermal |
| Water | Aqua |
| Grass | Organic |
| Electric | Bioelectric |
| Ice | Cryo |
| Rock | Mineral |
| Dark | Neural |
| Psychic | Neural (context-dependent) |
| Poison | Toxic |
| Dragon | Removed (no direct mapping) |
| Flying | Aero |
| Ground | Kinetic |
| Ghost | Blight (context-dependent) |
| Fairy | Ark (context-dependent) |
| Crystal | Sonic or Neural (context-dependent) |

### New Types (no old equivalent)
- **Ferro** — Metallic/alloy genome
- **Kinetic** — Motion/force genome
- **Aero** — Air/wind genome
- **Sonic** — Sound/vibration genome
- **Ark** — Apex purifying genome
- **Blight** — Apex corrupting genome
