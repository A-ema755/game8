# Full Project Audit — Phase 3: Domain Specialists

**Date**: 2026-04-08
**Scope**: all (56 files, 14 implemented systems)
**Branch**: feature/Player-Input-Wiring
**Agents**: gameplay-programmer, ai-programmer, ui-programmer, performance-analyst

---

## Phase 3 Severity Rollup

| Agent | CRITICAL | MAJOR | MINOR | Total |
|-------|----------|-------|-------|-------|
| gameplay-programmer | 2 | 7 | 4 | 13 |
| ai-programmer | 2 | 6 | 6 | 14 |
| ui-programmer | 3 | 7 | 6 | 16 |
| performance-analyst | 0 | 7 | 10 | 17 |
| **Total** | **7** | **27** | **26** | **60** |

---

## gameplay-programmer — Full Gameplay Correctness Report

### TurnManager

**CRITICAL — StatusEffectProcessor not injected with CombatSettings; Paralysis/Burn/Poison hardcoded**

`StatusEffectProcessor` uses hardcoded `private const double ParalysisThreshold = 0.25` instead of `_settings.ParalysisSuppressionChance`. `CombatSettings` exposes `ParalysisSuppressionChance` but `StatusEffectProcessor` never receives `CombatSettings` — its constructor takes no arguments. The 25% value matches the GDD default but it is untunable by designers at runtime.
File: `Assets/Scripts/Combat/StatusEffectProcessor.cs:18`, `Assets/Scripts/Combat/CombatSettings.cs:67`

**CRITICAL — Confusion self-hit bypasses IDamageCalculator (quantified deviation)**

GDD §4.5 specifies confusion damage must use `DamageCalculator.Calculate()` with a synthetic Power-40 Physical move. The implementation uses a simplified `floor(ATK * power / (DEF * 50))` that omits the level coefficient `((2*level/5)+2)`, the `+2` base floor, and all multipliers. At level 1 the deviation is small; at level 30+ the simplified formula underestimates damage by 40-60% vs the GDD-specified formula. This is not a "close approximation" — it is structurally wrong for high-level creatures.
*(Specific bypass was flagged in Phase 2; the magnitude quantification is new.)*

**MAJOR — StatusEffectProcessor hardcoded Burn/Poison divisors**

`BurnDivisor = 16` and `PoisonDivisor = 8` as private constants. `CombatSettings` has `BurnDotDivisor` and `PoisonDotDivisor` properties that are never consumed. Designers cannot tune Burn/Poison severity without modifying source code. ADR-001 violation.
File: `Assets/Scripts/Combat/StatusEffectProcessor.cs:16-17`

**MAJOR — LINQ `.Any()` allocation in confusion check (hot path)**

Line 503: `entries.Any(e => e.Effect == StatusEffect.Confusion && !e.IsExpired)` allocates a lambda on every `UseMove` call for every creature. With 8 creatures and 10+ rounds this is ~80+ allocations per combat. A simple `for` loop would be zero-alloc.
File: `Assets/Scripts/Combat/TurnManager.cs:503`

**MINOR — HandleFaint fires CreatureFainted before clearing tile occupancy**

Order in `HandleFaint`: (1) fire `CreatureFainted` event, (2) clear tile occupant. Any UI subscriber querying `_grid.GetTile(creature.GridPosition).Occupant` will still see the fainted creature. Low risk for MVP but latent ordering bug.
File: `Assets/Scripts/Combat/TurnManager.cs:681-685`

**MINOR — ExecuteFlee bypasses CheckEndCondition, sets CombatActive directly**

Functionally correct (flee is unambiguous) but inconsistent with rest of combat termination. Maintenance hazard if `CheckEndCondition` gains side effects.

### DamageCalculator

**CRITICAL — RollCritical NullReferenceException when move.Effects is null**

`RollCritical()` iterates `foreach (var effect in move.Effects)` with no null guard. If a `MoveConfig` is created without initializing the `effects` array, this throws at runtime on every move use.
File: `Assets/Scripts/Combat/DamageCalculator.cs:189`

**MAJOR — TerrainMatchesCreatureType mapping may diverge from GDD direct equality check**

GDD §3.3 specifies terrain synergy as `defenderTile.Terrain == defender.Config.TerrainSynergyType`. Implementation delegates to `TypeChart.TerrainMatchesCreatureType()`. If that method groups multiple terrain variants, behavior silently diverges. Flagged as risk needing verification.
File: `Assets/Scripts/Combat/DamageCalculator.cs:160,164`

### CaptureSystem

**MAJOR — CaptureSystem.Attempt() has no trainer-encounter guard**

GDD §3.2 states Capture is blocked in Trainer encounters. `TurnActionValidator.ValidateCapture()` checks `EncounterType.Trainer` at submission time, but `CaptureSystem.Attempt()` itself has no guard. `TurnManager.ExecuteCapture()` calls `_captureSystem.Attempt()` unconditionally. Only protection is the validator — defense-in-depth missing.
File: `Assets/Scripts/Combat/TurnManager.cs:601-634`, `Assets/Scripts/Combat/CaptureSystem.cs:21`

### StatusEffectProcessor

**MAJOR — Single shared RNG roll for all status effects**

`ApplyStartOfRound` receives one `double rngRoll` used for Paralysis check. If a creature has both Paralysis and another probabilistic effect (e.g., Freeze thaw post-MVP), they share the same roll. Interface bakes in single-roll contract that will break.
File: `Assets/Scripts/Combat/IStatusEffectProcessor.cs`, `StatusEffectProcessor.cs`

### GridSystem

**MAJOR — HasLineOfSight height-block is asymmetric**

`checkHeightBlock = targetTile.Height > sourceTile.Height + 2` — only triggers when target is 2+ levels above source. Shooting down from height applies no block. GDD doesn't explicitly specify bidirectional blocking, may be intentional but undocumented.
File: `Assets/Scripts/Gameplay/Grid/GridSystem.cs:365`

**MAJOR — TargetingHelper.GetValidTargetTiles returns impassable tiles for AoE/Line**

`AddTilesInRange()` checks `tile.IsPassable` and skips impassable tiles. Wrong for targeting scenarios where creature stands next to impassable tile. Low risk for MVP (creatures can't spawn on impassable tiles).

### Test Coverage Gaps

**MAJOR — No tests for StatusEffectProcessor with CombatSettings knobs**

Tests use hardcoded expected values matching the hardcoded constants. No tests verify designer can change values via CombatSettings. Tests would pass even if CombatSettings were changed.

**MAJOR — No test for ExecuteUseMove where target is already fainted**

GDD §5 edge case: "If target fainted, UseMove still executes — PP consumed, TakeDamage(0) on fainted target is no-op." Code matches GDD but completely uncovered.

**MINOR — No test for recoil-kills-attacker mutual faint path**

The `ResolveConfusion` / recoil Victory path (lines 547-555) has no test coverage. Non-trivial edge case from GDD §5.

### gameplay-programmer Summary

| Severity | Count |
|----------|-------|
| CRITICAL | 2 |
| MAJOR | 7 |
| MINOR | 4 |
| **Total** | **13** |

---

## ai-programmer — Full AI System Correctness Report

### CRITICAL

**C1 — Struggle not implemented (GDD §5 edge case, Acceptance Criteria #3)**

GDD mandates: when all moves are out of PP, AI selects Struggle (power 10, Physical, typeless, 25% self-damage). `EnumerateCandidates` skips slots with `PP <= 0` and falls through to Wait. No Struggle candidate is ever generated. AI simply passes when PP-exhausted — incorrect behavior that changes late-game combat difficulty and violates a named acceptance criterion.

**C2 — ScoreFinishTarget divides by MaxHP with no zero guard**

`ScoreFinishTarget` line 108: `1.0f - ((float)action.Target.CurrentHP / action.Target.MaxHP)`. If `MaxHP == 0` (misconfigured ScriptableObject, test fixture skipping stat init), this produces `NaN`. NaN propagates through weighted sum, corrupts `CompositeScore` for every candidate, and makes `SelectBestCandidate`'s comparison fail silently — all candidates lose and Wait is returned every turn. `ScoreDamage` has the same gap at line 80.

### MAJOR

**M1 — RetreatHpThreshold declared but never read**

`AIPersonalityConfig` exposes `RetreatHpThreshold` (line 81). GDD §5 states "Below this HP, creature always tries to retreat." `AIDecisionSystem` never reads this field. A creature at 5% HP with `retreatHpThreshold = 0.15` behaves identically to one at 100% HP. The "always retreat" guarantee is completely absent.

**M2 — FocusFireBias and AbilityPreference declared but never read**

Both fields exist on `AIPersonalityConfig` (lines 69–70), described in GDD §3.3. Neither consumed anywhere. `focus-fire` and `self-buff` personality presets silently do nothing. AI cannot implement Hunter or Enhancer archetypes.

**M3 — ScorePosition uses Chebyshev distance, not A* path distance**

GDD §3.2 specifies `distanceToTarget` via A* path distance. Implementation uses `GridSystem.ChebyshevDistance` (straight-line). On maps with obstacles, walls, or height blockers, AI scores tile proximity as if all paths are open — approaches through impassable terrain and assigns incorrect retreat scores. Makes positional scoring unreliable on any non-flat grid.

**M4 — AoE targeting picks only the closest target, ignoring multi-hit value**

`EnumerateCandidates` for `TargetType.AoE` generates exactly one candidate — the closest live opponent. AoE moves should be scored against the tile maximizing hit count. AI never considers using AoE to hit a cluster vs single exposed target. `ScoreKillPotential` and `ScoreDamage` misleading (score one creature when move hits many).

**M5 — ScoreFormTactics: Physical "adjacent" bonus fires at distance 2, not 1**

Line 238: `if (dist <= 2) formPositionScore += 0.3f`. GDD says Physical scores higher "when actor is adjacent." Chebyshev distance 2 is two tiles diagonally — not adjacent. Inflates Physical scores at distances where adjacency bonus isn't warranted.

**M6 — ScoreGenomeMatchup: STAB double-scored**

STAB computation (line 201) `(stab - 1.0f) / 3.0f` applies to a STAB value in range [1.0, 1.5], yielding max +0.167 — undocumented in GDD. STAB is already embedded in `EstimateDamage` via `DamageCalculator.Estimate`. STAB-boosted moves get double-scored: once in damage, once in genome matchup.

### MINOR

**m1 — No test for PP-exhausted creature (all slots at 0)**

The edge case that should trigger Struggle per GDD — currently untested and behavior is wrong (C1).

**m2 — No test for ScoreFinishTarget with MaxHP == 0 or fainted target**

Silent NaN propagation path with no regression coverage.

**m3 — No test for AoE targeting behavior**

AoE branch untested. No test verifies candidate generation for AoE moves.

**m4 — No test for retreat override at RetreatHpThreshold**

`test_ScoreSelfPreservation_activates_below_threshold` tests scoring flag, not retreat forcing behavior. Since retreat path doesn't exist (M1), no test catches it.

**m5 — Wait action score not actually 0 when ScoreSelfPreservation fires**

Test comment "Wait action should score ~0" is misleading. With opponents present, ScoreSelfPreservation returns 1.0 and ScorePosition returns non-zero. Wait baseline is context-dependent — correct mechanically but test assertion fragile.

**m6 — Energy form stat pairing: ATK vs SPD (not ATK vs SP.DEF)**

GDD and implementation both say Energy uses "ATK vs SPD." In creature game conventions this is unusual (typically special offense vs special defense). May be intentional but worth confirming with game designer before balancing.

### ai-programmer Summary

| Severity | Count |
|----------|-------|
| CRITICAL | 2 |
| MAJOR | 6 |
| MINOR | 6 |
| **Total** | **14** |

---

## ui-programmer — Full UI Implementation Correctness Report

### CRITICAL

**C-01 — PlayerInputController never wired — dead controller**

`CombatHUDController.CreateSubControllers()` never instantiates or calls `PlayerInputController.Initialize()`. The entire per-creature input state machine (`BeginCreatureSelection`, `OnTileClicked`, `OnCreatureClicked`, capture mode, flee, `RestartAll`) is dead code at runtime. `CombatHUDController` has its own parallel input path that partially overlaps, but lacks movement, flee, tile-click routing, and the `_creatureIndex` / multi-creature advancement loop. Players with multiple alive creatures will only ever get the first creature's action collected — `PlayerInputCollector._expectedCount` will be > 1 but only one action will ever submit.
Files: `PlayerInputController.cs`, `CombatHUDController.cs`

**C-02 — CombatEnd overlay has hardcoded English strings**

`CombatHUDController.cs` lines 457–463: `"VICTORY"`, `"DEFEAT"`, `"ESCAPED"`, `"DRAW"` are all hardcoded. No localization key lookup.

**C-03 — SwitchOverlayController hardcoded strings**

Lines 79, 158, 159, 161: `"No other creatures available."`, `"FAINTED"`, `"ACTIVE"`, and HP format string `$"{creature.CurrentHP} / {creature.MaxHP}"` are all raw English.

### MAJOR

**M-01 — MoveSelectionPanelController always shows creature[0]'s moves**

`RefreshForCreature()` scans `_combatController.PlayerParty` for the first non-fainted creature every time. No parameter or setter for which creature is active. If player has two creatures alive, panel always shows creature[0]'s moves even when creature[1] is selecting. State synchronization bug independent of C-01.
File: `MoveSelectionPanelController.cs:91-98`

**M-02 — OnSwitchConfirmed submits Wait for wrong creature**

`OnSwitchConfirmed(CreatureInstance switchTo)` ignores `switchTo` entirely. Submits `ActionType.Wait` for the first non-fainted creature found by scan. If player has two alive creatures and is selecting for creature[1], submits Wait for creature[0] instead.
File: `CombatHUDController.cs:364-381`

**M-03 — TypeEffectivenessCallout scheduled closures fire after ClearAll — use-after-return**

`schedule.Execute` closures retain reference to `calloutLabel`. If `ClearAll()` called while chain is mid-flight, `ReturnToPool` fires on already-returned label, or on actively displaying label if pool recycled it. Closures not cancelled on `ClearAll`.
File: `TypeEffectivenessCallout.cs:96-125`

**M-04 — PlayerInputController event subscriptions never cleaned up**

Because `PlayerInputController` is never initialized (C-01), `UnsubscribeFromEvents()` / `OnDestroy()` won't fire properly. `OnDestroy` guard relies on `_combatController != null` set by `Initialize()` — if MonoBehaviour exists in scene but `Initialize` never called, `OnDestroy` unsubscribes nothing.
File: `PlayerInputController.cs:277-315`

**M-05 — CreatureInfoPanelController._explicitClickTarget not cleared on round boundary**

`SetDefaultTarget` doesn't clear `_explicitClickTarget`. If player clicked an enemy earlier, explicit click target keeps priority on round transition. Panel keeps showing previously clicked creature instead of new active creature.
File: `CreatureInfoPanelController.cs:89-93`

**M-06 — TurnOrderBarController tooltip hardcoded English strings**

`"[Player]"` / `"[Enemy]"` in tooltip format string. User-visible.
File: `TurnOrderBarController.cs:111`

**M-07 — Tab key: StopPropagation without PreventDefault**

`KeyCode.Tab` handled with `StopPropagation()` but not `PreventDefault()`. UI Toolkit focus system still moves focus between focusable elements, potentially fighting custom `CycleTarget` logic.
File: `CombatHUDController.cs:437-440`

### MINOR

**m-01 — CreatureInfoPanelController hardcoded format strings**

`"LVL {creature.Level}"`, `"{currentHP} / {maxHP}"` — inline English prefix, assumes LTR direction.
File: `CreatureInfoPanelController.cs:213,215,236,254`

**m-02 — MoveSelectionPanelController.UpdateTrapCount hardcoded format**

`$"x{count}"` — raw inline format.
File: `MoveSelectionPanelController.cs:215`

**m-03 — MoveSelectionPanelController hover callbacks never deregistered**

`MouseEnterEvent`/`MouseLeaveEvent` registered but never unregistered. Suppressed when panel hidden via `DisplayStyle.None`, but fragile dependency on `_isLocked` guard in lambda.
File: `MoveSelectionPanelController.cs:424-433`

**m-04 — SwitchOverlayController.CycleFocus double-modulo pattern**

`((start + attempts * direction) % _party.Count + _party.Count) % _party.Count` — correct but non-obvious. Readability flag only.
File: `SwitchOverlayController.cs:178-191`

**m-05 — TypeEffectivenessCallout hardcoded English strings**

`"Super Effective!"` and `"Not Very Effective..."` — user-facing combat feedback.
File: `TypeEffectivenessCallout.cs:74,79`

**m-06 — CombatEnd doesn't call TypeEffectivenessCallout.ClearAll()**

`ShowCombatEndOverlay()` hides panels but doesn't clear in-flight callout animations. Connects to M-03.
File: `CombatHUDController.cs`

### ui-programmer Summary

| Severity | Count |
|----------|-------|
| CRITICAL | 3 |
| MAJOR | 7 |
| MINOR | 6 |
| **Total** | **16** |

---

## performance-analyst — Full Performance Report

### Frame Budget: 16.67ms target (60 FPS)

### GridSystem.cs

**MAJOR | MEDIUM — GetNeighbours/GetPassableNeighbours: new List allocation per call**

`GetNeighbours` (line 109) and `GetPassableNeighbours` (line 131) each allocate `new List<TileData>(8)` on every invocation. `GetPassableNeighbours` is called inside A* and Dijkstra inner loops — once per expanded node. On 16×12 grid, single reachable-tiles query can expand 100+ nodes, producing 100+ allocations.

Mitigation: pass caller-owned scratch `List<TileData>` that is `Clear()`-ed each call.

**MAJOR | MEDIUM — GetReachableTiles: Comparer delegate allocation per call**

Line 276: `SortedSet` constructed with `Comparer<...>.Create((a, b) => {...})` — new heap-allocated delegate every call. Called every time movement highlights update.

Mitigation: cache comparer as `static readonly` field.

**MAJOR | MEDIUM — A* FindPath: new Dictionary and HashSet per call**

`FindPath` (line 192) allocates `SortedSet<AStarNode>`, `HashSet<Vector2Int>`, `Dictionary<Vector2Int, float>` on every invocation. 2-6 allocations per creature per round.

Mitigation: pre-allocate as `GridSystem` fields, clear at start of each call.

**MINOR | LOW — AStarNode: Interlocked.Increment unnecessary for single-threaded**

`Interlocked.Increment(ref _nextId)` per node construction. Atomic CPU fence unnecessary — replace with plain `++_nextId`.

**MINOR | LOW — AStarNode Comparer: closure at class init**

`static readonly` but `Comparer<AStarNode>.Create(...)` lambda still allocates closure at initialization. Once per AppDomain — low cost.

**MINOR | LOW — TileHighlightController.GetAttackTiles: new HashSet per call for blockedTiles**

Line 68: `new HashSet<Vector2Int>()` allocated every call even for Physical/Bio moves where always empty.

Mitigation: return static empty set for non-Energy moves.

**MINOR | LOW — TileHighlightController.GetSynergyTiles: new HashSet per call, no cache**

Allocates fresh HashSet and iterates entire grid every call. Terrain synergy tiles never change mid-combat. Not cached.

### TurnManager.cs

**MAJOR | HIGH — GetInitiativeOrder: new Dictionary + List copy per call, twice per round**

`GetInitiativeOrder` (line 723) allocates `new Dictionary<CreatureInstance, int>` for tiebreaks and `new List<CreatureInstance>` for return copy — every call. Called in both `ExecutePlayerAction` and `ExecuteEnemyAction`.

Mitigation: promote `tiebreaks` to reusable field.

**MAJOR | MEDIUM — GetInitiativeOrder comparator: CalculateInitiative called redundantly**

Inside sort comparator, `CalculateInitiative(a, opponents)` and `CalculateInitiative(b, opponents)` called for every comparison pair. O(N log N) comparisons × 2 initiative calcs each × opponent iteration. For 3v3: ~36 ChebyshevDistance calculations per sort, per round.

Mitigation: pre-compute initiative scores into array before sorting.

**MINOR | LOW — ExecuteCapture: List.Remove O(n)**

Line 614: `_enemyParty.Remove(action.Target)` is O(n) linear scan. Negligible at MVP sizes (≤6).

### AIDecisionSystem.cs

**MAJOR | MEDIUM — SelectBestCandidate: unsized List allocations trigger resize**

Three `new List<CandidateAction>()` default to capacity 4. With 4 moves × 3 opponents = 12 candidates, all three trigger at least one resize+copy.

Mitigation: size with `candidates.Count` or use in-place approach.

**MINOR | LOW — EnumerateCandidates: AvailableForms property accessed per slot**

`creature.AvailableForms.Contains(moveConfig.Form)` — property checks `_formsCacheDirty` 4 times per AI decision. Capture once before loop.

### DamageCalculator.cs

**MINOR | LOW — RollCritical: foreach over MoveEffect per hit**

Iterates `move.Effects` every `Calculate` call. Could cache `HasHighCrit` on MoveConfig.

### TurnOrderBarController.cs

**MAJOR | MEDIUM — SetActiveCreature: O(N) icon iteration per turn change**

`foreach (var kvp in _creatureToIcon)` removes `icon--active` from every icon. 6 `RemoveFromClassList` calls per turn.

Mitigation: track `_activeIcon` reference, O(N) → O(1).

**MINOR | LOW — CreateIcon: string.Substring + ToUpper per icon**

12 string allocations per round for 6-creature combat. Cache initials on CreatureInstance.

**MINOR | LOW — UpdateHPPips: Children() enumeration allocates IEnumerable**

`pipContainer.Children()` returns iterator. Called every `CreatureActed`.

Mitigation: cache pip references per creature.

### AIActionScorer.cs

**MINOR | LOW — ScoreTerrainSynergy: duplicate switch per call**

`TerrainMatchesCreatureType` and `MapTerrainToCreatureType` both switch on same `tile.Terrain`. Merge into one.

### CreatureInstance.cs

**MINOR | LOW — ApplyStatusEffect: List.Contains linear scan**

`_activeStatusEffects.Contains(status)` — linear scan on list. Trivial at ≤6 effects.

### performance-analyst Summary

| Severity | Count | Impact |
|----------|-------|--------|
| CRITICAL | 0 | — |
| MAJOR | 7 | GridSystem (3 MEDIUM), TurnManager (1 HIGH + 1 MEDIUM), AIDecisionSystem (1 MEDIUM), TurnOrderBarController (1 MEDIUM) |
| MINOR | 10 | Various LOW |
| **Total** | **17** | |

**Top 3 by frame impact:**
1. `GridSystem.GetPassableNeighbours` — new List in A* inner loop (highest allocation volume)
2. `TurnManager.GetInitiativeOrder` — unsized Dictionary + O(N²) CalculateInitiative in sort
3. `TurnOrderBarController.SetActiveCreature` — O(N) RemoveFromClassList per turn

---

## Phase 3 Cross-Cutting Issues (raised by 2+ agents)

| Issue | Agents |
|-------|--------|
| StatusEffectProcessor hardcoded constants / no CombatSettings injection | gameplay-prog, ai-prog (via scoring impact) |
| Multi-creature input flow broken (PlayerInputController unwired) | ui-prog (CRITICAL), gameplay-prog (implicit — test gap) |
| Hardcoded English strings throughout UI | ui-prog (3 findings across multiple files) |
| GridSystem allocation pressure in A*/Dijkstra inner loops | performance-analyst (3 findings), gameplay-prog (awareness) |

---

## Prioritized Action Items (Phase 3)

| # | Action | Severity | Agent | Impact |
|---|--------|----------|-------|--------|
| 1 | Wire PlayerInputController into CombatHUDController for multi-creature input | CRITICAL | ui-programmer | Multi-creature parties non-functional |
| 2 | Implement Struggle when all PP exhausted | CRITICAL | gameplay-programmer | AI passes instead of attacking |
| 3 | Add null guard to DamageCalculator.RollCritical for move.Effects | CRITICAL | gameplay-programmer | NullRef crash risk |
| 4 | Add zero guard to ScoreFinishTarget/ScoreDamage for MaxHP | CRITICAL | ai-programmer | NaN corruption |
| 5 | Fix MoveSelectionPanelController to track active creature | MAJOR | ui-programmer | Always shows wrong moves |
| 6 | Fix OnSwitchConfirmed to use correct active creature | MAJOR | ui-programmer | Submits action for wrong creature |
| 7 | Wire RetreatHpThreshold, FocusFireBias, AbilityPreference into AI | MAJOR | ai-programmer | Personality presets non-functional |
| 8 | Fix ScorePosition to use A* distance not Chebyshev | MAJOR | ai-programmer | Wrong scoring on maps with obstacles |
| 9 | Cancel TypeEffectivenessCallout scheduled closures on ClearAll | MAJOR | ui-programmer | Use-after-return bug |
| 10 | Pre-allocate GridSystem scratch lists for A*/Dijkstra | MAJOR | performance-analyst | 100+ allocs per pathfind |
| 11 | Pre-compute initiative scores before sort | MAJOR | performance-analyst | O(N²) redundant calculations |
| 12 | Add trainer-encounter guard to CaptureSystem.Attempt | MAJOR | gameplay-programmer | Defense-in-depth |
| 13 | Localize all hardcoded English strings in UI | MAJOR | ui-programmer | i18n blocking |
