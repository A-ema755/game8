# Implementation Prompt: Full Audit Remediation (2026-04-08)

## Source

All findings from the full project audit: `production/reports/full-audit-phase1-2026-04-08.md` through `full-audit-phase4-2026-04-08.md`. This prompt addresses every CRITICAL, MAJOR, and MINOR finding across 11 agents and 4 phases.

## Execution Strategy

Work in **6 sequential work phases** (A→F). Each phase is a self-contained PR. Complete and verify each phase before starting the next — later phases depend on earlier ones.

**For each phase:**
1. Read the relevant source files before making changes
2. Run `/code-review` on all modified files before committing
3. Verify Unity compiles with 0 errors after each phase
4. Run existing tests — no regressions allowed
5. Follow collaboration protocol: show changes, get approval, then commit

---

## Phase A: Critical Infrastructure (Assembly Definitions + Config Wiring)

### Agent

Use **unity-specialist** agent for assembly definition work, then **gameplay-programmer** agent for config wiring.

### Objective

Fix the project's assembly boundaries and wire all combat subsystems to read from ScriptableObject config instead of hardcoded constants. This unblocks designer tuning and enforces compile-time dependency boundaries.

### A1 — Add missing assembly definitions

**Source findings**: Phase 1 — unity-specialist CRITICAL, technical-director MAJOR #1 + #2

Create two new `.asmdef` files:

1. `Assets/Scripts/Creatures/GeneForge.Creatures.asmdef`
   - References: `GeneForge.Core` (by GUID)
   - `autoReferenced: false`
   - `rootNamespace: GeneForge.Creatures`

2. `Assets/Scripts/Gameplay/GeneForge.Gameplay.asmdef`
   - References: `GeneForge.Core` (by GUID), `GeneForge.Grid` (by GUID)
   - `autoReferenced: false`
   - `rootNamespace: GeneForge.Gameplay`

Then update existing asmdefs:
- `GeneForge.Grid.asmdef` → set `autoReferenced: false`
- `GeneForge.Combat.asmdef` → set `autoReferenced: false`, add references to `GeneForge.Creatures` and `GeneForge.Gameplay` (by GUID)
- `GeneForge.UI.asmdef` → set `autoReferenced: false`, add references to `GeneForge.Creatures` and `GeneForge.Gameplay` (by GUID)
- `GeneForge.Core.asmdef` → keep `autoReferenced: true` (base assembly)
- `GeneForge.Tests.EditMode.asmdef` → update references: add `GeneForge.Creatures` and `GeneForge.Gameplay` GUIDs, remove any stale/unresolved GUIDs
- `GeneForge.Tests.PlayMode.asmdef` → convert name-based references to GUID-based

**Verify**: Unity compiles with 0 errors. All tests pass.

### A2 — Wire StatusEffectProcessor to CombatSettings

**Source findings**: Phase 1 (all 3 agents), Phase 2 (game-designer, systems-designer CRITICAL C-01), Phase 3 (gameplay-programmer)

Files to modify:
- `Assets/Scripts/Combat/IStatusEffectProcessor.cs` — add `CombatSettings` parameter to constructor contract (or add `Initialize(CombatSettings)` method)
- `Assets/Scripts/Combat/StatusEffectProcessor.cs`:
  - Add `CombatSettings _settings` field
  - Add constructor parameter `CombatSettings settings`
  - Replace `private const int BurnDivisor = 16` → `_settings.BurnDotDivisor`
  - Replace `private const int PoisonDivisor = 8` → `_settings.PoisonDotDivisor`
  - Replace `private const double ParalysisThreshold = 0.25` → `_settings.ParalysisSuppressionChance`
  - Remove the three `private const` declarations
- `Assets/Scripts/UI/CombatController.cs` — update `StatusEffectProcessor` construction to pass `CombatSettings`
- `Assets/Tests/EditMode/StatusEffectProcessorTests.cs` — update tests to construct with `CombatSettings`, add tests verifying knobs are actually read

### A3 — Wire DamageCalculator to GameSettings/CombatSettings

**Source findings**: Phase 1 (all 3 agents), Phase 2 (game-designer)

Files to modify:
- `Assets/Scripts/Combat/DamageCalculator.cs`:
  - Add constructor with `GameSettings` and/or `CombatSettings` parameter
  - Replace all 13 `private const` fields (lines 17-31) with values read from settings:
    - `CritMultiplier` → `settings.CritMultiplier`
    - `BaseCritChance` → `settings.CritBaseChance`
    - `HighCritChance` → derive or add to settings
    - `HeightBonusPerLevel` → `settings.HeightBonusPerLevel`
    - `AttackerTerrainSynergy`, `DefenderTerrainSynergy`, `CoverReduction`, `StatDivisor`, `BaseDamageFloor`, `MinDamage` → add to `CombatSettings` if not already present
    - `VarianceMin`, `VarianceMax` → add to `CombatSettings`
  - Remove dead `StabMultiplier` const (line 17) — already in TypeChart
  - Make `StatDivisor` accessible (needed by confusion self-hit fix in Phase B)
- `Assets/Scripts/Combat/IDamageCalculator.cs` — update interface if constructor changes affect it
- `Assets/Scripts/UI/CombatController.cs` — update DamageCalculator construction
- `Assets/Tests/EditMode/DamageCalculatorTests.cs` — update construction, add test verifying settings are read

### A4 — Replace Resources.Load with ConfigLoader injection

**Source findings**: Phase 1 (all 3 agents), Phase 2 (technical-director #11, #12)

Files to modify:
- `Assets/Scripts/Combat/CombatSettings.cs` — add to `ConfigLoader` registry if not already
- `Assets/Scripts/Core/ConfigLoader.cs` — add `CombatSettings` accessor if missing
- `Assets/Scripts/UI/CombatController.cs`:
  - Line 149: Replace `Resources.Load<CombatSettings>("Data/CombatSettings")` with `ConfigLoader.GetCombatSettings()` or inject via parameter
  - Lines 139, 153: Change `ScriptableObject.CreateInstance` fallbacks from `Debug.LogWarning` to `Debug.LogError` + early return
- `Assets/Scripts/UI/PlayerInputController.cs`:
  - Line 121: Replace `Resources.Load<CombatSettings>("Data/CombatSettings")` with injection via `Initialize()` parameter

---

## Phase B: Critical Combat Correctness

### Agent

Use **gameplay-programmer** agent for combat logic, **ai-programmer** agent for AI fixes, **ui-programmer** agent for input wiring. Can run AI and UI fixes in parallel since they touch different files.

### Objective

Fix all correctness bugs that produce wrong behavior, crashes, or non-functional systems.

### B1 — Wire PlayerInputController into CombatHUDController

**Source findings**: Phase 3 — ui-programmer CRITICAL C-01

This is the highest-priority fix. Multi-creature parties cannot select actions.

Files to modify:
- `Assets/Scripts/UI/CombatHUDController.cs`:
  - Add `[SerializeField] PlayerInputController _playerInputController` or wire via `CreateSubControllers()`
  - Call `_playerInputController.Initialize(...)` during combat setup
  - Route `PlayerCreatureSelect` phase to `PlayerInputController` instead of the partial parallel path
  - Fix `OnSwitchConfirmed` (MAJOR M-02) — use `PlayerInputController._activeCreature` instead of scanning party
- `Assets/Scripts/UI/MoveSelectionPanelController.cs`:
  - Fix `RefreshForCreature()` (MAJOR M-01) — accept creature parameter instead of scanning for first non-fainted
- `Assets/Scripts/UI/PlayerInputController.cs`:
  - Verify `Initialize()` / `OnDestroy()` lifecycle is correct
  - Fix event subscription cleanup (MAJOR M-04)

**Verify**: Manual test with 2+ creature party — each creature gets action selection in sequence.

### B2 — Implement Struggle

**Source findings**: Phase 3 — ai-programmer CRITICAL C1

Files to modify:
- `Assets/Scripts/Combat/AIDecisionSystem.cs` — In `EnumerateCandidates`: when all move slots have PP ≤ 0, generate a Struggle candidate (power 10, Physical, typeless, 25% recoil self-damage per GDD)
- `Assets/Scripts/Combat/TurnManager.cs` — In `ExecuteUseMove`: handle Struggle (no PP deduction, apply 25% self-damage after hit)
- `Assets/Scripts/UI/PlayerInputController.cs` — When all moves have 0 PP, auto-select Struggle for player creature or show Struggle as only option
- `Assets/Tests/EditMode/AIActionScorerTests.cs` — Add test: PP-exhausted creature generates Struggle candidate
- `Assets/Tests/EditMode/TurnManagerTests.cs` — Add test: Struggle deals damage + self-recoil

### B3 — Add null/zero guards

**Source findings**: Phase 3 — gameplay-programmer CRITICAL (RollCritical), ai-programmer CRITICAL C2

Files to modify:
- `Assets/Scripts/Combat/DamageCalculator.cs`:
  - Line 189: Add `if (move.Effects == null) return false;` before `foreach`
- `Assets/Scripts/Combat/AIActionScorer.cs`:
  - `ScoreFinishTarget` line 108: Guard `if (action.Target.MaxHP <= 0) return 0f;`
  - `ScoreDamage` line 80: Same guard for division by MaxHP
- `Assets/Tests/EditMode/DamageCalculatorTests.cs` — Add test: move with null Effects doesn't throw
- `Assets/Tests/EditMode/AIActionScorerTests.cs` — Add test: MaxHP=0 creature doesn't produce NaN

### B4 — Route confusion self-hit through IDamageCalculator

**Source findings**: Phase 2 — systems-designer CRITICAL C-02, game-designer DRIFT

Files to modify:
- `Assets/Scripts/Combat/IDamageCalculator.cs` — Add overload: `int CalculateRaw(int power, DamageForm form, CreatureInstance attacker, CreatureInstance defender)` (no crit, no variance, AlwaysHits, GenomeType.None)
- `Assets/Scripts/Combat/DamageCalculator.cs` — Implement `CalculateRaw` using the full formula (including level coefficient `(2*level/5+2)`)
- `Assets/Scripts/Combat/TurnManager.cs` — Replace `CalculateConfusionSelfHitDamage()` inline formula with `_damageCalculator.CalculateRaw(_settings.ConfusionSelfHitPower, DamageForm.Physical, creature, creature)`
- `Assets/Tests/EditMode/DamageCalculatorTests.cs` — Add test: confusion self-hit at level 50 matches GDD formula
- `Assets/Tests/EditMode/TurnManagerTests.cs` — Add test: confusion routes through IDamageCalculator

### B5 — Fix height-3 impassable tiles

**Source findings**: Phase 2 — game-designer DRIFT (Encounter System)

Files to modify:
- `Assets/Scripts/Combat/EncounterManager.cs` — In `GetTileProperties`: change `if (height >= GridSystem.MaxHeight)` to `if (height >= 3)` OR change `GridSystem.MaxHeight` to 3. Confirm which approach aligns with GDD §3.3 ("0=Ground, 1=Elevated low, 2=Elevated high, 3=Cliff/unreachable")
- `Assets/Tests/EditMode/GridSystemTests.cs` — Add test: height-3 tile is impassable
- `Assets/Tests/EditMode/EncounterManagerTests.cs` — Add test: height-3 in encounter config produces impassable tile

### B6 — Add trainer-encounter guard to CaptureSystem

**Source findings**: Phase 3 — gameplay-programmer MAJOR

Files to modify:
- `Assets/Scripts/Combat/TurnManager.cs` — In `ExecuteCapture()`: add early guard `if (_encounterType == EncounterType.Trainer) { /* log error, skip */ return; }`
- `Assets/Tests/EditMode/CaptureSystemTests.cs` — Add test: capture attempt in trainer encounter is blocked

---

## Phase C: AI Correctness

### Agent

Use **ai-programmer** agent for all AI fixes.

### Objective

Fix AI scoring bugs, wire unused personality fields, and correct distance/targeting calculations.

### C1 — Wire RetreatHpThreshold, FocusFireBias, AbilityPreference

**Source findings**: Phase 3 — ai-programmer MAJOR M1, M2

Files to modify:
- `Assets/Scripts/Combat/AIDecisionSystem.cs`:
  - Read `_personality.RetreatHpThreshold` — when creature HP% < threshold, override to Flee (or heavily weight self-preservation)
  - Read `_personality.FocusFireBias` — multiply `ScoreFinishTarget` by this factor to make Hunter archetype focus wounded targets
  - Read `_personality.AbilityPreference` — weight status moves vs damage moves per personality
- `Assets/Tests/EditMode/AIActionScorerTests.cs` — Add tests for each personality field influencing behavior

### C2 — Fix ScorePosition to use A* path distance

**Source findings**: Phase 3 — ai-programmer MAJOR M3

Files to modify:
- `Assets/Scripts/Combat/AIActionScorer.cs` — Replace `GridSystem.ChebyshevDistance` calls in `ScorePosition` with `GridSystem.FindPath().Count` (or a bounded path-length query). Cache results per (actor, target) pair to avoid repeated A* calls.
- `Assets/Tests/EditMode/AIActionScorerTests.cs` — Add test: ScorePosition with obstacle between actor and target uses path distance, not straight line

### C3 — Fix AoE targeting to consider multi-hit value

**Source findings**: Phase 3 — ai-programmer MAJOR M4

Files to modify:
- `Assets/Scripts/Combat/AIDecisionSystem.cs` — In `EnumerateCandidates` for `TargetType.AoE`: generate candidates for each in-range tile, score by number of enemies hit. At minimum, score against each enemy individually (not just closest).
- `Assets/Tests/EditMode/AIActionScorerTests.cs` — Add test: AoE move prefers tile hitting 2 enemies over tile hitting 1

### C4 — Fix Physical adjacency bonus distance

**Source findings**: Phase 3 — ai-programmer MAJOR M5

Files to modify:
- `Assets/Scripts/Combat/AIActionScorer.cs` — Line 238: Change `if (dist <= 2)` to `if (dist <= 1)` for Physical adjacency bonus
- `Assets/Tests/EditMode/AIActionScorerTests.cs` — Add test: Physical bonus only fires at distance 1

### C5 — Fix STAB double-scoring

**Source findings**: Phase 3 — ai-programmer MAJOR M6

Files to modify:
- `Assets/Scripts/Combat/AIActionScorer.cs` — In `ScoreGenomeMatchup`: remove the STAB bonus calculation (line ~201) since STAB is already included in `DamageCalculator.Estimate()` which feeds `ScoreDamage`. The genome matchup score should only reflect type effectiveness, not STAB.
- `Assets/Tests/EditMode/AIActionScorerTests.cs` — Add test: STAB move score equals non-STAB move score when type matchup is identical (proving STAB isn't double-counted)

---

## Phase D: UI Fixes

### Agent

Use **ui-programmer** agent for all UI fixes.

### Objective

Fix UI state synchronization bugs, use-after-return issues, hardcoded strings, and input handling gaps.

### D1 — Fix TypeEffectivenessCallout use-after-return

**Source findings**: Phase 3 — ui-programmer MAJOR M-03, MINOR m-06

Files to modify:
- `Assets/Scripts/UI/TypeEffectivenessCallout.cs`:
  - In `ClearAll()`: cancel all pending `schedule.Execute` chains before returning labels to pool. Store scheduled action references and call `.Pause()` or track a generation counter that closures check before executing.
- `Assets/Scripts/UI/CombatHUDController.cs`:
  - In `ShowCombatEndOverlay()`: call `_typeCallout.ClearAll()` before hiding panels

### D2 — Fix CreatureInfoPanelController stale click target

**Source findings**: Phase 3 — ui-programmer MAJOR M-05

Files to modify:
- `Assets/Scripts/UI/CreatureInfoPanelController.cs`:
  - In `SetDefaultTarget()`: clear `_explicitClickTarget = null` before calling `SetTarget`
  - Or: subscribe to `TurnManager.RoundStarted` and clear explicit target on round boundary

### D3 — Fix Tab key PreventDefault

**Source findings**: Phase 3 — ui-programmer MAJOR M-07

Files to modify:
- `Assets/Scripts/UI/CombatHUDController.cs`:
  - Lines 437-440: Add `evt.PreventDefault()` alongside `evt.StopPropagation()` for `KeyCode.Tab`

### D4 — Wire instability thresholds to InstabilityThresholds

**Source findings**: Phase 1 (lead-programmer, tech-director), Phase 2 (game-designer)

Files to modify:
- `Assets/Scripts/UI/CreatureInfoPanelController.cs`:
  - Line 251: Replace `const float MaxInstability = 100f` with `InstabilityThresholds.Max`
  - Lines 262-264: Replace `>= 75` with `>= InstabilityThresholds.CriticalMin`, `>= 50` with `>= InstabilityThresholds.UnstableMin`
  - Add `using GeneForge.Core;` if not present

### D5 — Localize hardcoded English strings

**Source findings**: Phase 3 — ui-programmer CRITICAL C-02, C-03, MAJOR M-06, MINOR m-01, m-02, m-05

This is a larger task. For MVP, extract all hardcoded strings to `static class CombatStrings` constants (or a localization lookup). Full i18n can come later, but the strings must not be inline.

Files with hardcoded strings:
- `Assets/Scripts/UI/CombatHUDController.cs` — "VICTORY", "DEFEAT", "ESCAPED", "DRAW"
- `Assets/Scripts/UI/SwitchOverlayController.cs` — "No other creatures available.", "FAINTED", "ACTIVE", HP format
- `Assets/Scripts/UI/TypeEffectivenessCallout.cs` — "Super Effective!", "Not Very Effective..."
- `Assets/Scripts/UI/TurnOrderBarController.cs` — "[Player]", "[Enemy]" tooltip
- `Assets/Scripts/UI/CreatureInfoPanelController.cs` — "LVL {level}", HP format
- `Assets/Scripts/UI/MoveSelectionPanelController.cs` — "x{count}" trap format

Create `Assets/Scripts/UI/CombatStrings.cs` with all string constants. Replace inline literals with references.

### D6 — Fix inline Color literals

**Source findings**: Phase 1 — unity-specialist MINOR, lead-programmer MINOR

Files to modify:
- `Assets/Scripts/UI/CreatureInfoPanelController.cs`:
  - Lines 308, 346: Replace `new Color(0.18f, 0.80f, 0.35f)` and `new Color(0.91f, 0.63f, 0.06f)` with USS classes or named constants

---

## Phase E: Code Quality & Performance

### Agent

Use **lead-programmer** agent for code quality, **performance-analyst** agent for allocation fixes. Can run in parallel on different files.

### Objective

Fix DRY violations, unsafe APIs, allocation hotspots, and minor code quality issues.

### E1 — Extract shared GetFormStatPairing

**Source findings**: Phase 1 (lead-programmer MAJOR), Phase 2 (systems-designer MAJOR M-01)

Files to modify:
- Create `Assets/Scripts/Combat/DamageFormHelper.cs` — static class with `GetStatPairing(DamageForm, CreatureInstance attacker, CreatureInstance target, out int offStat, out int defStat)`
- `Assets/Scripts/Combat/DamageCalculator.cs` — replace private `GetFormStatPairing` with call to `DamageFormHelper`
- `Assets/Scripts/Combat/AIActionScorer.cs` — replace private `GetFormStatPairing` with call to `DamageFormHelper`

### E2 — Fix StatusEffectEntry public mutable fields

**Source findings**: Phase 1 — lead-programmer MAJOR

Files to modify:
- `Assets/Scripts/Combat/StatusEffectEntry.cs`:
  - Make `Effect` and `RemainingRounds` readonly properties
  - Add `WithDecrementedRounds()` method returning new struct
- `Assets/Scripts/Combat/StatusEffectProcessor.cs` — update to use `WithDecrementedRounds()`
- `Assets/Scripts/Combat/TurnManager.cs` — update write-back pattern

### E3 — Add bounds checking to EncounterConfig

**Source findings**: Phase 1 — lead-programmer MAJOR

Files to modify:
- `Assets/Scripts/Core/EncounterConfig.cs`:
  - `GetHeight(x, z)`: add bounds check, return 0 or throw on out-of-bounds
  - `GetTerrain(x, z)`: same

### E4 — Fix GridSystem allocations (A* inner loop)

**Source findings**: Phase 3 — performance-analyst MAJOR (3 findings)

Files to modify:
- `Assets/Scripts/Gameplay/Grid/GridSystem.cs`:
  - `GetNeighbours` / `GetPassableNeighbours`: accept caller-owned `List<TileData>` scratch buffer, `Clear()` each call instead of `new List<TileData>(8)`
  - `GetReachableTiles`: cache `Comparer` as `static readonly` field
  - `FindPath`: pre-allocate `HashSet<Vector2Int>` and `Dictionary<Vector2Int, float>` as instance fields, `Clear()` per call
  - Replace `Interlocked.Increment` on `AStarNode._nextId` with plain `++`

### E5 — Fix TurnManager allocations

**Source findings**: Phase 3 — performance-analyst MAJOR (2 findings), Phase 1 — lead-programmer MAJOR

Files to modify:
- `Assets/Scripts/Combat/TurnManager.cs`:
  - `GetInitiativeOrder`: promote `tiebreaks` dictionary to reusable field; pre-compute initiative scores before sort
  - Line 503: Replace `entries.Any(...)` with manual `for` loop
  - Lines 704-705: Replace `_enemyParty.All(...)` / `_playerParty.All(...)` with manual loops

### E6 — Fix AIDecisionSystem allocations

**Source findings**: Phase 3 — performance-analyst MAJOR

Files to modify:
- `Assets/Scripts/Combat/AIDecisionSystem.cs`:
  - `SelectBestCandidate`: size `tied`, `powerTied`, `accTied` lists with `candidates.Count` capacity. Or refactor to in-place max-finding without intermediate lists.

### E7 — Fix TurnOrderBarController O(N) SetActiveCreature

**Source findings**: Phase 3 — performance-analyst MAJOR

Files to modify:
- `Assets/Scripts/UI/TurnOrderBarController.cs`:
  - Track `_activeIcon` field. In `SetActiveCreature`: remove class from `_activeIcon` only, then set new `_activeIcon`. O(N) → O(1).

### E8 — Remaining MINOR fixes (batch)

These are low-priority but should be cleaned up:

| File | Line | Fix |
|------|------|-----|
| `CandidateAction.cs:23` | — | Change `CompositeScore` setter to `internal set` |
| `IMoveEffectApplier.cs:9-10` | — | Move "Does NOT handle Recoil or Drain" note to concrete class |
| `TypeChart.cs:169` | — | Change `Debug.LogError` to `Debug.Assert` or throw in dev builds |
| `ConfigLoader.cs:92` | — | Wrap `Debug.Log` in `#if UNITY_EDITOR` |
| `ConfigStubs.cs:7` | — | Add `/// <summary>` to BodyPartConfig and StatusEffectConfig |
| `CreatureInstance.cs:28` | — | Add `// ADR-003: approved singleton access` comment |
| `CreatureInstance.cs:187-195` | — | Return `IReadOnlySet<DamageForm>` instead of mutable HashSet |
| `EncounterManager.cs:25` | — | Add null-check on `ConfigLoader.GetCreature` in default constructor |
| `TileHighlightController.cs:19` | — | Rename `ShouldComputeOnHover` to `ThrottleHover` |
| `SwitchOverlayController.cs:64-88` | — | Pool VisualElement slots instead of creating new ones per open |
| `MoveSelectionPanelController.cs:366` | — | Track currently applied type class, remove only that one |
| `MoveSelectionPanelController.cs:153` | — | Cache reusable list for CycleTarget instead of allocating |
| `PlayerInputController.cs:319` | — | Reuse `_creaturesNeedingActions` list instead of allocating new one |
| `PlayerInputController.cs:399` | — | Cache move-to-slot mapping on AdvanceToNextCreature |
| `CombatHUDController.cs:248` | — | Reuse cached buffer for OnRoundStarted creature list |
| `CombatHUDController.cs:24` | — | Add `/// <summary>` to serialized `combatController` field |
| `CreatureInfoPanelController.cs:338` | — | Replace LINQ `Skip(3).Select()` with manual StringBuilder loop |
| `TurnOrderBarController.cs:136` | — | Cache initials string on CreatureInstance |
| `AIActionScorer.cs:214` | — | Extract `ScoreFormTactics` inner logic to stay under 40-line limit |
| `AIDecisionSystem.cs:103` | — | Extract tiebreaker stages from `SelectBestCandidate` into helpers |
| `GridSystem.cs:192` | — | Extract `FindPath` inner loop body into helper method |
| `GridSystem.cs:266` | — | Extract `GetReachableTiles` inner loop body |
| `TurnManager.cs:346` | — | Split `ExecuteMovementStep` into validation/pathfinding/position/facing helpers |
| `TurnManager.cs:139` | — | Consider grouping 12 constructor params into `TurnManagerConfig` value object |
| `CombatController.cs:119` | — | Extract subsystem factory from convenience `StartCombat` overload |
| `MoveConfig.cs:28-58` | — | Consider making `MoveEffect` a struct per ADR-009 (low priority) |
| `Enums.cs:104` | — | Consider moving `InstabilityThresholds` to its own file (SRP) |

---

## Phase F: Test Coverage (Fill Critical Gaps)

### Agent

Use **qa-tester** agent to design tests, then **gameplay-programmer** / **ai-programmer** / **ui-programmer** agents to implement them in their respective domains.

### Objective

Close all MUST-TEST gaps identified by the Phase 4 qa-tester. Phases A-E fix the bugs; this phase proves they stay fixed.

### F1 — Critical formula tests

- `DamageCalculatorTests`: null-Effects safety, confusion self-hit at level 50 matches GDD, MaxHP=0 doesn't crash
- `StatusEffectProcessorTests`: CombatSettings knobs are actually read (change divisor, verify different DoT damage)
- `CaptureSystemTests`: trainer encounter blocks capture

### F2 — Combat flow tests

- `TurnManagerTests`: Struggle fallback when all PP exhausted, target-already-fainted still executes (PP consumed), recoil-kills-attacker mutual faint path
- `CombatControllerTests`: multi-creature input sequence (creature 1 action → creature 2 action → both submitted)

### F3 — AI behavior tests

- `AIActionScorerTests`: MaxHP=0 NaN guard, STAB not double-scored, RetreatHpThreshold influences behavior, FocusFireBias influences scoring, AbilityPreference influences scoring, Physical adjacency bonus only at distance 1, AoE picks multi-target tile
- `AIDecisionSystem`: PP-exhausted creature generates Struggle

### F4 — Grid and targeting tests

- `GridSystemTests`: height-3 tile is impassable
- `TargetingHelperTests`: AoE returns multiple valid target tiles
- `EncounterManagerTests`: height-3 in config produces impassable tile

### F5 — UI state tests (if testable in EditMode)

- MoveSelectionPanel shows correct creature's moves (not always creature[0])
- OnSwitchConfirmed submits for active creature (not first non-fainted)

---

## Verification Checklist (after all phases)

- [ ] Unity compiles with 0 errors
- [ ] All existing tests pass (no regressions)
- [ ] All new tests from Phase F pass
- [ ] Run `/code-review` on all modified files
- [ ] Run `/balance-check` to verify formulas match GDD after settings wiring
- [ ] Manual playtest: 2-creature party, full combat round, all action types

## Branch Strategy

- Create branch `fix/audit-remediation-phase-A` from current branch
- After each phase merges, create next branch from updated main
- Or: single branch `fix/audit-remediation` with phase commits clearly labeled

## Estimated Scope

| Phase | Files Modified | New Files | Tests Added |
|-------|---------------|-----------|-------------|
| A | ~12 | 2 (.asmdef) | ~6 |
| B | ~10 | 0 | ~10 |
| C | ~3 | 0 | ~7 |
| D | ~8 | 1 (CombatStrings.cs) | 0 |
| E | ~20 | 1 (DamageFormHelper.cs) | 0 |
| F | ~10 (test files) | 0 | ~20 |
