# Full Project Audit — Phase 4: Quality Gate

**Date**: 2026-04-08
**Scope**: 22 test files vs 56 implementation files
**Branch**: feature/Player-Input-Wiring
**Agent**: qa-tester

---

## qa-tester — Full Test Coverage & Quality Report

### Per-Test-File Assessment

| Test File | Test Count | Systems Covered | Grade | Status |
|-----------|-----------|-----------------|-------|--------|
| GameStateManagerTests | 14 | State machine, handlers, transitions, events | A | Comprehensive |
| ConfigLoaderTests | 8 | Config loading, registry, ID validation, error handling | A | Comprehensive |
| TypeChartTests | 58 | Type effectiveness (36 SE + 50 resist), STAB, edge cases, balance | A | Comprehensive |
| CreatureDatabaseTests | 14 | Asset loading, move references, archetype slots, type coverage | B+ | Good; missing primary type coverage gap validation |
| MoveDatabaseTests | 18 | Move schema, damaging/status split, priority, effects | B | Good; light on effect interaction chains |
| CreatureInstanceTests | 57 | Stats, leveling, moves, DNA mods, status, events, HP/fainting | A | Comprehensive |
| PartyStateTests | 16 | Party management, storage, lead, fainting, deposit/withdraw | A | Comprehensive |
| DamageCalculatorTests | ~40 | Damage formula, STAB, type effectiveness, crit, fallback | B+ | Gaps in null-Effects, MaxHP=0, confusion self-hit |
| CaptureCalculatorTests | 19 | Catch rate formula, status bonuses, HP factor, RNG determinism | A | Comprehensive |
| CaptureSystemTests | ~5 | Integration with capture calculator | B | Minimal; lacks trainer guard tests |
| GridSystemTests | ~20 | Grid creation, tile access, bounds, height, terrain | B | Height-3 passability not explicitly tested |
| AIActionScorerTests | ~50 | Damage scoring, position, threat, terrain synergy | B | Missing: STAB double-score, MaxHP=0, unused config fields |
| TurnManagerTests | ~30 | Turn sequencing, phases, events, faint logic, status ticks | B+ | Struggle path untested |
| StatusEffectProcessorTests | ~10 | Burn/Poison DoT, Paralysis/Sleep/Freeze suppression | B | Missing: CombatSettings knob validation |
| MoveEffectApplierTests | ~10 | Status application, chance gating, self-targeting | B | Minimal effect chain coverage |
| TargetingHelperTests | ~10 | Target tiles, LoS blocking, range validation | B | AoE closest-target bug untested |
| TurnActionValidatorTests | ~15 | Action invariants, move validation, target validity | B | Struggle fallback missing |
| TileHighlightTests | ~10 | Movement, attack tiles, form-specific targeting | B | Form-specific LoS issues unclear |
| CombatControllerTests | ~10 | Action submission, event relay, PlayerInputProvider | C | **Multi-creature input wiring never tested** |
| EncounterManagerTests | ~10 | Encounter setup, grid spawning | B | Minimal coverage |
| GameStateManagerPlayModeTests | 2 | Scene loading, DontDestroyOnLoad | B | Light; only 2 tests |
| ConfigLoaderPlayModeTests | 4 | Integration initialization, GameSettings load | B | Light; only 4 tests |

---

### CRITICAL Issue Test Coverage

| Critical Issue (Phases 1-3) | Test Coverage | Status |
|------------------------------|---------------|--------|
| StatusEffectProcessor hardcoded Burn/Poison/Paralysis | NO TEST | Tests assume hardcoded values match; no test verifies CombatSettings knobs have effect |
| DamageCalculator.RollCritical NullRef on move.Effects | NO TEST | Tests create moves with empty list, never null |
| Confusion self-hit bypasses IDamageCalculator | NO TEST | Formula deviation at high levels not validated |
| ScoreFinishTarget divides by MaxHP with no zero guard | NO TEST | NaN risk when MaxHP=0; no dead creature test |
| Struggle not implemented (PP-exhausted AI passes) | NO TEST | No test exercises all-PP-exhausted state |
| PlayerInputController never wired | NO TEST | CombatControllerTests don't test multi-creature sequence |
| Missing .asmdef for Creatures/Gameplay | N/A | Build/compile issue, not testable via NUnit |

### MAJOR Issue Test Coverage

| Major Issue (Phases 1-3) | Test Coverage | Priority |
|---------------------------|---------------|----------|
| DamageCalculator hardcoded constants ignore GameSettings | PARTIAL | MUST-FIX |
| GetFormStatPairing duplicated | IMPLICIT | REFACTOR |
| Height-3 tiles passable (GDD: impassable cliff) | NO TEST | MUST-FIX |
| RetreatHpThreshold, FocusFireBias, AbilityPreference never read | NO TEST | MUST-FIX |
| ScorePosition uses Chebyshev not A* distance | NO TEST | SHOULD-FIX |
| AoE targeting picks only closest target | NO TEST | MUST-FIX |
| STAB double-scored in AI genome matchup | NO TEST | MUST-FIX |
| MoveSelectionPanel always shows creature[0] | NO TEST | SHOULD-FIX |
| OnSwitchConfirmed submits for wrong creature | NO TEST | SHOULD-FIX |
| TypeEffectivenessCallout use-after-return | NO TEST | MUST-FIX |
| Single shared RNG roll in StatusEffectProcessor | ASSUMED | MUST-TEST |
| CaptureSystem no trainer guard | NO TEST | MUST-FIX |
| GridSystem allocations in A* inner loop | NO TEST | NICE-TO-FIX |
| Hardcoded English strings in UI | NO TEST | NICE-TO-FIX |

---

### Missing Test Coverage — Priority Map

#### MUST-TEST (Acceptance Criteria Gaps)

**1. Damage Calculation Edge Cases**
- Null move.Effects field → NullRef safety
- MaxHP = 0 → division by zero
- Confusion self-hit formula validation (vs standard damage at level 50+)
- Gap: DamageCalculatorTests needs null-effects test

**2. Combat Validation & Fallback Logic**
- PP-exhausted creature action (Struggle or error)
- Move validation when all moves have 0 PP
- Gap: TurnActionValidator + TurnManager need Struggle tests

**3. AI Decision System**
- ScoreFinishTarget with MaxHP = 0 (NaN guard)
- STAB application (shouldn't double-score genome + primary)
- RetreatHpThreshold read + behavior
- FocusFireBias + AbilityPreference usage
- Gap: AIActionScorerTests missing all 4 subtests

**4. Targeting & Movement**
- AoE move selecting all adjacent targets (not just closest)
- Height-3 tiles impassable (GridSystem or TargetingHelper)
- Gap: TargetingHelperTests + GridSystemTests need height edge case

**5. Status Effect Processing**
- CombatSettings knobs actually used (burnDotDivisor, etc.)
- Paralysis suppression chance threshold
- Gap: StatusEffectProcessorTests assumes hardcoding, doesn't verify CombatSettings

**6. Capture System Trainer Guard**
- Trainer battles should disable capture attempts
- Gap: CaptureSystemTests lacks trainer-type validation

**7. Player Input Wiring**
- Multi-creature team action submission
- CombatController → IPlayerInputProvider → TurnManager flow
- Creature 1 action followed by Creature 2
- Gap: CombatControllerTests has no multi-creature sequence test

#### SHOULD-TEST (Design Violations)

1. ScorePosition using Chebyshev vs A* distance
2. MoveSelectionPanel showing correct creature
3. OnSwitchConfirmed submitting for correct creature
4. Move effect chains (multi-effect ordering)

#### NICE-TO-TEST (Performance & Code Quality)

1. GridSystem A* allocation efficiency
2. Hardcoded string localization audit
3. Type Chart initialization performance

---

### Coverage Summary Table

| System | Test File | Tests | Grade | Key Gaps | Risk |
|--------|-----------|-------|-------|----------|------|
| Core State Machine | GameStateManagerTests | 14 | A | None | LOW |
| Config System | ConfigLoaderTests | 8 | A | None | LOW |
| Type Chart | TypeChartTests | 58 | A | None | LOW |
| Creature Stats | CreatureInstanceTests | 57 | A | Personality edge cases | LOW |
| Party Management | PartyStateTests | 16 | A | None | LOW |
| Capture Formula | CaptureCalculatorTests | 19 | A | None | LOW |
| Damage Calculation | DamageCalculatorTests | ~40 | B+ | Null Effects, MaxHP=0, confusion formula | **MEDIUM** |
| Creature Database | CreatureDatabaseTests | 14 | B+ | Primary type gap | LOW |
| Move Database | MoveDatabaseTests | 18 | B | Effect chain interactions | LOW |
| Grid System | GridSystemTests | ~20 | B | Height-3 passability, A* allocations | **MEDIUM** |
| AI Scoring | AIActionScorerTests | ~50 | B | STAB double-score, MaxHP=0, dead config fields | **HIGH** |
| Turn Manager | TurnManagerTests | ~30 | B+ | Struggle, multi-creature sequence | **MEDIUM** |
| Status Effects | StatusEffectProcessorTests | ~10 | B | CombatSettings knob validation | **MEDIUM** |
| Move Effects | MoveEffectApplierTests | ~10 | B | Effect chains, multi-effect ordering | LOW |
| Targeting | TargetingHelperTests | ~10 | B | AoE closest-target, melee cover | **MEDIUM** |
| Tile Highlights | TileHighlightTests | ~10 | B | Form-specific LoS | LOW |
| Action Validation | TurnActionValidatorTests | ~15 | B | Struggle fallback, illegal targets | **MEDIUM** |
| Capture System | CaptureSystemTests | ~5 | B | Trainer guard, no-capture validation | **MEDIUM** |
| **Combat Controller** | **CombatControllerTests** | **~10** | **C** | **Multi-creature input wiring** | **CRITICAL** |
| Encounter Manager | EncounterManagerTests | ~10 | B | Minimal coverage | LOW |
| GameState PlayMode | GameStateManagerPlayMode | 2 | B | Light | LOW |
| ConfigLoader PlayMode | ConfigLoaderPlayMode | 4 | B | Light | LOW |

---

### Recommended Test Additions (Priority Order)

**This Sprint — CRITICAL Blockers (7 tests)**
1. DamageCalculatorTests: null-effects safety test
2. DamageCalculatorTests: confusion self-hit formula at level 50
3. AIActionScorerTests: MaxHP=0 NaN safety test
4. TurnManagerTests/TurnActionValidatorTests: Struggle fallback test
5. CombatControllerTests: multi-creature input sequence test
6. StatusEffectProcessorTests: CombatSettings knob integration test
7. CaptureSystemTests: trainer encounter guard test

**Next Sprint — MAJOR Gaps (4 tests)**
8. GridSystemTests: height-3 impassable tile test
9. TargetingHelperTests: AoE multi-target selection test
10. AIActionScorerTests: STAB double-score bug test
11. AIActionScorerTests: config knob usage tests (Retreat, FocusFire, AbilityPref)

**Polish — Nice-to-Have (3 tests)**
12. A* allocation profiling
13. Localization string audit
14. MoveEffectApplier effect chain tests

---

### Verdict

**Test suite is GOOD for foundation systems (6 A-grade suites) but has CRITICAL gaps in combat integration, AI edge cases, and UI input wiring.** The 7 CRITICAL issues from Phases 1-3 have ZERO test coverage — any regression in these areas would be silently introduced.

**Overall Test Health: B- (solid foundation, critical integration gaps)**
