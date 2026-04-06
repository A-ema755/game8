# Design Review: Grid / Tile System

**File**: `design/gdd/grid-tile-system.md`
**Reviewed**: 2026-04-05
**Reviewer**: design-review pipeline (automated)

---

## Section Completeness Check

| # | Required Section | Present? | Notes |
|---|-----------------|----------|-------|
| 1 | Overview | Yes | Complete one-paragraph summary |
| 2 | Player Fantasy | Yes | Clear, evocative, pillar-aligned |
| 3 | Detailed Rules | Yes | Extensive with code examples |
| 4 | Formulas | Yes | Table format with expressions |
| 5 | Edge Cases | Yes | Table format, 8 cases covered |
| 6 | Dependencies | Yes | Bidirectional table |
| 7 | Tuning Knobs | Yes | 10 parameters with defaults |
| 8 | Acceptance Criteria | Yes | 14 testable criteria |

All 8 required sections present.

---

## Findings

### CRITICAL

None found.

### MAJOR

[MAJOR] Overview > No explicit pillar alignment statement > Add a sentence or note identifying which game pillars this system primarily serves (Tactical Grid Mastery is the primary; Genetic Architect secondary via body-part grid interactions). Required by design doc standards for cross-referencing.

[MAJOR] Detailed Rules §3.4 > Height rules contradiction: "Descend any levels = 1.0 cost, Yes (free)" but "Descend 3+ levels = triggers fall check" — the table says descending any levels is allowed and free, but the fall damage rule below it introduces a penalty for 3+ level drops. The word "free" in the table row is misleading since fall damage applies. > Clarify the table: descend 1-2 levels is free (cost 1.0, no damage), descend 3+ is allowed but triggers fall damage. Remove "free" from the 3+ row or split into two rows.

[MAJOR] Detailed Rules §3.4 > Fall damage formula references `fallDamageBase` but the code constant is not defined in the GridSystem code block (§3.3). It only appears in Tuning Knobs as a GameSettings SO value. > Add a note in §3.4 clarifying that `fallDamageBase` is sourced from `GameSettings` ScriptableObject, not from GridSystem constants.

[MAJOR] Formulas > Height advantage formula `+0.1x per height level above target (cap 2.0x)` — the "x" notation is ambiguous. Does +0.1x mean the multiplier is `1.0 + 0.1 * heightDelta` or `heightDelta * 0.1`? The cap of 2.0x suggests the former (since 10 height levels would reach 2.0x, but max height delta is only 4). > Rewrite as: `heightAdvantage = 1.0 + (0.1 × heightDelta)`, capped at `2.0`. Note: with MaxHeight=4, effective max is `1.4x`. Clarify that this formula lives in DamageCalculator, not GridSystem.

[MAJOR] Tuning Knobs > Missing tuning knobs for several hardcoded values in Detailed Rules: climb cost (`1.5`), flat/descend cost (`1.0`), max grid dimensions (`16×12`), and height advantage multiplier per level (`0.1`). These are gameplay-critical values that appear in code blocks and formulas but have no Tuning Knobs entry. > Add tuning knob entries for: `ClimbStepCost` (1.5), `FlatStepCost` (1.0), `MaxGridWidth` (16), `MaxGridDepth` (12), `HeightAdvantagePerLevel` (0.1), `HeightAdvantageCap` (2.0).

[MAJOR] Formulas > Height advantage formula and cap (`2.0x`) have no Tuning Knobs entries. > Add `HeightAdvantagePerLevel` and `HeightAdvantageCap` to Tuning Knobs with note that they are applied in DamageCalculator, not GridSystem.

### MINOR

[MINOR] Detailed Rules §3.5 > A* code references `ChebyshevDistance` in the `Heuristic` method but the function definition appears later in §3.6. Forward reference is fine for a design doc but a brief note would aid readability. > Add a one-line note: "See §3.6 for ChebyshevDistance definition."

[MINOR] Dependencies > `Enums.cs (TerrainType)` is listed as a dependency but the Terrain System GDD (`terrain-system.md`) is not listed as a dependency despite TerrainType being defined there. > Add `Terrain System` as an inbound dependency alongside `Enums.cs`.

[MINOR] Dependencies > `Encounter System` is not listed as a dependency, but grid dimensions are "tunable per encounter config" (§3.1), implying the Encounter System provides grid size. > Add `Encounter System` as an inbound dependency with note: "Provides grid dimensions per encounter."

[MINOR] Edge Cases > No edge case for diagonal corner-cutting validation in pathfinding. The edge case table mentions "Corner-cutting not allowed" but the A* code in §3.5 does not implement corner-cutting prevention. > Add a note that the implementation must add corner-cutting checks to the A* neighbour loop (check that both cardinal tiles adjacent to the diagonal are passable).

[MINOR] Acceptance Criteria > No acceptance criterion for corner-cutting prevention despite it being listed as an edge case. > Add: "A* does not allow diagonal movement through two diagonally adjacent impassable tiles."

[MINOR] Acceptance Criteria > No acceptance criterion for grid dimension validation (min 6×6, max 16×12). > Add: "GridSystem rejects dimensions outside MinGridWidth–MaxGridWidth / MinGridDepth–MaxGridDepth range."

[MINOR] Detailed Rules §3.9 > Cover damage reduction ("Cover tiles reduce Energy damage by 50%") is a hardcoded value with no Tuning Knobs entry. > Add `CoverDamageReduction` to Tuning Knobs (default 0.5). Note: may belong in Damage & Health System GDD; cross-reference if so.

[MINOR] Overview > References "8-directional movement" but does not mention terrain types or terrain synergy, which are integral to tile behavior. > Add brief mention of terrain types in overview for completeness.

---

## Summary

- **CRITICAL**: 0
- **MAJOR**: 5 (unique issues; some overlap — e.g., height advantage appears in both Formulas and Tuning Knobs findings)
- **MINOR**: 7

The document is well-structured and substantially complete. The primary gaps are: (1) missing pillar alignment statement, (2) ambiguous height descent rules, (3) unclear height advantage formula notation, and (4) several gameplay values missing from Tuning Knobs.

---

## Fixes Applied

1. **[MAJOR] Pillar alignment** — Added pillar alignment statement to Overview (Tactical Grid Mastery primary, Genetic Architect secondary). Also added terrain type mention to overview.
2. **[MAJOR] Height descent contradiction** — Split "Descend any levels" row into "Descend 1–2 levels" (no damage) and "Descend 3+ levels" (triggers fall damage). Removed misleading "free" label.
3. **[MAJOR] Fall damage source** — Clarified that `FallDamageBase` and `FallDamageMinDelta` come from `GameSettings` ScriptableObject. Updated formula to use `FallDamageMinDelta` parameter.
4. **[MAJOR] Height advantage formula** — Rewrote as `heightAdvantage = 1.0 + (HeightAdvantagePerLevel × heightDelta)`, capped at `HeightAdvantageCap`. Noted effective max is 1.4× with MaxHeight=4. Clarified this lives in DamageCalculator.
5. **[MAJOR] Missing tuning knobs** — Added 7 entries: `MaxGridWidth`, `MaxGridDepth`, `ClimbStepCost`, `FlatStepCost`, `HeightAdvantagePerLevel`, `HeightAdvantageCap`, `CoverDamageReduction`. All marked `<!-- TODO: Tune in balance pass -->` where values are estimated.
6. **[MINOR] A* heuristic note** — Added comment in code referencing §3.6 for ChebyshevDistance definition.
7. **[MINOR] Terrain System dependency** — Added Terrain System (`terrain-system.md`) as inbound dependency.
8. **[MINOR] Encounter System dependency** — Added Encounter System (`encounter-system.md`) as inbound dependency for grid dimensions.
9. **[MINOR] Corner-cutting implementation note** — Expanded edge case with implementation guidance for A* neighbour validation.
10. **[MINOR] Corner-cutting acceptance criterion** — Added test criterion for diagonal corner-cutting prevention.
11. **[MINOR] Grid dimension acceptance criterion** — Added test criterion for dimension range validation.
12. **[MINOR] Cover damage reduction tuning knob** — Added `CoverDamageReduction` (0.5) with cross-reference to Damage & Health System GDD.
