# Full Project Audit Report — Gene Forge

**Date**: 2026-04-08
**Scope**: all (56 files, 14 implemented systems, 22 test files)
**Branch**: feature/Player-Input-Wiring
**Agents**: 11 across 4 phases
**Systems Audited**: 14 of 53
**Files Reviewed**: 78 (56 implementation + 22 test)

---

## Executive Summary

Gene Forge's codebase has **strong architectural foundations** — interface-based DI, event-driven communication, ScriptableObject config, struct value types, zero forbidden pattern violations. The 14 implemented systems are well-structured and all serve at least one game pillar.

However, the audit uncovered **critical gaps in three areas**: (1) the multi-creature player input pipeline is completely unwired, (2) several combat subsystems hardcode values that should come from ScriptableObjects — making designer tuning non-functional, and (3) AI has multiple correctness bugs including missing Struggle, NaN risks, and non-functional personality presets. Test coverage is good for foundation systems but has zero coverage on all 7 CRITICAL issues.

**Biggest risk**: the PlayerInputController is dead code — multi-creature parties cannot select actions. This blocks any playtest with >1 creature.

---

## Cross-Cutting Issues (raised by 2+ agents)

| # | Issue | Phases | Agents | Severity |
|---|-------|--------|--------|----------|
| 1 | StatusEffectProcessor ignores CombatSettings knobs (Burn/Poison/Paralysis hardcoded) | 1,2,3 | 7 agents | CRITICAL |
| 2 | DamageCalculator hardcoded tuning constants — GameSettings has no effect | 1,2 | 5 agents | MAJOR |
| 3 | Missing .asmdef for Creatures/ and Gameplay/ — ADR-008 violation | 1 | 3 agents | CRITICAL |
| 4 | Resources.Load in CombatController/PlayerInputController — bypasses ConfigLoader | 1,2 | 3 agents | MAJOR |
| 5 | GetFormStatPairing duplicated in DamageCalculator + AIActionScorer | 1,2 | 3 agents | MAJOR |
| 6 | Confusion self-hit bypasses IDamageCalculator — 40-60% damage deviation at high levels | 2,3 | 3 agents | CRITICAL |
| 7 | Instability thresholds hardcoded in CreatureInfoPanelController | 1,2 | 3 agents | MAJOR |
| 8 | Height-3 tiles passable when GDD says impassable cliff | 2 | 2 agents | MAJOR |
| 9 | Movement formula triplicated across 3 files | 2 | 2 agents | MINOR |
| 10 | GridSystem allocations in A* inner loop (100+ per pathfind) | 3 | 2 agents | MAJOR |

---

## Severity Rollup (All Phases)

| Severity | Phase 1 | Phase 2 | Phase 3 | Phase 4 | Total |
|----------|---------|---------|---------|---------|-------|
| CRITICAL | 1 | 2 | 7 | 7 blockers | **~10 unique** |
| MAJOR | ~14 | ~8 | 27 | 14 gaps | **~30 unique** |
| MINOR | ~35 | ~13 | 26 | — | **~50 unique** |

---

## System Health Matrix

| System | Code Quality | Unity Patterns | Architecture | GDD Alignment | Perf | Tests | Overall |
|--------|-------------|---------------|-------------|---------------|------|-------|---------|
| Data Config Pipeline | A | A | A | ALIGNED | A | A | **A** |
| Game State Manager | A | A | A | ALIGNED | A | A | **A** |
| Type Chart | A | B | A | ALIGNED | A | A | **A** |
| Creature Database | A | A | A | ALIGNED | A | B+ | **A** |
| Move Database | A | B | A | ALIGNED | A | B | **A-** |
| Party System | A | A | A | ALIGNED | A | A | **A** |
| Capture Calculator | A | A | A | ALIGNED | A | A | **A** |
| Grid / Tile System | B | B | A | ALIGNED | C | B | **B** |
| Creature Instance | B | B | A | DRIFT (minor) | B | A | **B+** |
| Damage & Health | B | C | B | DRIFT | B | B+ | **B-** |
| Turn Manager | B | B | B | DRIFT | C | B+ | **B-** |
| Capture System | B | B | B | ALIGNED | A | B | **B** |
| AI Decision System | C | B | B | DRIFT | C | B | **C+** |
| Encounter System | B | B | B | DRIFT | A | B | **B** |
| Combat UI | C | C | B | DRIFT | C | C | **C** |

---

## Top 15 Prioritized Action Items

| # | Action | Severity | Source Phase | Systems |
|---|--------|----------|-------------|---------|
| 1 | Wire PlayerInputController into CombatHUDController for multi-creature input | CRITICAL | P3 (ui-prog) | Combat UI |
| 2 | Implement Struggle when all PP exhausted (AI and player) | CRITICAL | P3 (ai-prog) | AI, Turn Manager |
| 3 | Wire StatusEffectProcessor to read CombatSettings (Burn/Poison/Paralysis) | CRITICAL | P1-3 (all) | Status Effects |
| 4 | Add null guard to DamageCalculator.RollCritical for move.Effects | CRITICAL | P3 (gameplay) | Damage |
| 5 | Add zero guard to ScoreFinishTarget/ScoreDamage for MaxHP | CRITICAL | P3 (ai-prog) | AI |
| 6 | Add .asmdef for Creatures/ and Gameplay/; set non-Core to autoReferenced:false | CRITICAL | P1 (all) | All assemblies |
| 7 | Migrate DamageCalculator constants to CombatSettings/GameSettings injection | MAJOR | P1-2 (all) | Damage, AI |
| 8 | Fix MoveSelectionPanel to track active creature (not always creature[0]) | MAJOR | P3 (ui-prog) | Combat UI |
| 9 | Fix OnSwitchConfirmed to submit action for correct creature | MAJOR | P3 (ui-prog) | Combat UI |
| 10 | Wire RetreatHpThreshold, FocusFireBias, AbilityPreference into AI | MAJOR | P3 (ai-prog) | AI |
| 11 | Fix ScorePosition to use A* distance not Chebyshev | MAJOR | P3 (ai-prog) | AI |
| 12 | Fix height-3 tiles to be impassable per GDD | MAJOR | P2 (game-design) | Grid, Encounters |
| 13 | Extract shared GetFormStatPairing to utility class | MAJOR | P1-2 | Damage, AI |
| 14 | Pre-allocate GridSystem scratch lists for A*/Dijkstra | MAJOR | P3 (perf) | Grid |
| 15 | Cancel TypeEffectivenessCallout scheduled closures on ClearAll | MAJOR | P3 (ui-prog) | Combat UI |

---

## Strategic Recommendations

### From creative-director (Phase 2):
**Promote DNA Alteration System ahead of Leveling/XP in implementation queue.** P1 (Genetic Architect) is the identity pillar — without it Gene Forge is indistinguishable from "any tactics RPG with type charts." The data model has all hooks; the expression layer is missing.

### From qa-tester (Phase 4):
**7 CRITICAL issues have zero test coverage.** Write these tests before any new feature work:
1. Null-effects safety (DamageCalculator)
2. Confusion self-hit formula at level 50
3. MaxHP=0 NaN safety (AIActionScorer)
4. Struggle fallback (TurnManager)
5. Multi-creature input sequence (CombatController)
6. CombatSettings knob integration (StatusEffectProcessor)
7. Trainer encounter capture guard (CaptureSystem)

### From systems-designer (Phase 2):
**Confusion self-hit damage must route through IDamageCalculator.** The inline formula creates a hidden split in the damage pipeline that will produce silently wrong numbers after any DamageCalculator refactor.

---

## Recommended Next Steps

1. `/code-review` — Fix the 6 CRITICAL code issues (items #1-5 above + .asmdef)
2. `/balance-check` — After wiring StatusEffectProcessor and DamageCalculator to settings, verify all formulas match GDD
3. `/team-combat` — Coordinate multi-creature input wiring (UI + gameplay + QA)
4. `/design-system` — Begin DNA Alteration System GDD→implementation (creative-director recommendation)
5. `/tech-debt` — Track the ~50 MINOR findings for polish sprint

---

## Verdict: CONCERNS

Strong architecture, disciplined patterns, good foundation test coverage. But critical combat integration gaps (unwired input, hardcoded values, AI correctness) must be resolved before any playtest or vertical slice milestone.

---

## Phase Reports (Full Detail)

- [Phase 1: Code & Architecture](full-audit-phase1-2026-04-08.md)
- [Phase 2: Design & Vision](full-audit-phase2-2026-04-08.md)
- [Phase 3: Domain Specialists](full-audit-phase3-2026-04-08.md)
- [Phase 4: Quality Gate](full-audit-phase4-2026-04-08.md)
