# Full Project Audit — Phase 1: Code & Architecture

**Date**: 2026-04-08
**Scope**: all (56 files, 14 implemented systems)
**Branch**: feature/Player-Input-Wiring
**Agents**: lead-programmer, unity-specialist, technical-director

---

## Severity Rollup

| Agent | CRITICAL | MAJOR | MINOR | Total |
|-------|----------|-------|-------|-------|
| lead-programmer | 0 | 11 | 33 | 44 |
| unity-specialist | 1 | 6 | 9 | 16 |
| technical-director | 0 | 4 | 8 | 12 |
| **Deduplicated Total** | **1** | **~14** | **~35** | **~50** |

---

## Cross-Cutting Issues (flagged by 2+ agents)

| # | Issue | Agents | Severity |
|---|-------|--------|----------|
| X1 | Missing `.asmdef` for `Creatures/` and `Gameplay/` — ADR-008 partial compliance, no compile-time boundary enforcement | All 3 | CRITICAL |
| X2 | `DamageCalculator.cs:17-31` hardcoded tuning constants duplicate `GameSettings`/`CombatSettings` values | All 3 | MAJOR |
| X3 | `StatusEffectProcessor.cs:16-18` hardcodes Burn/Poison/Paralysis values, ignores `CombatSettings` knobs entirely | All 3 | MAJOR |
| X4 | `Resources.Load<CombatSettings>` in `CombatController.cs:149` and `PlayerInputController.cs:121` — bypasses ConfigLoader, duplicated | All 3 | MAJOR |
| X5 | `CreatureInstance.cs:460` — `Enumerable.Contains` on `IReadOnlyList` allocates enumerator every `EquipPart` call | lead-prog, unity | MAJOR |
| X6 | `CreatureInfoPanelController.cs:251-270` — instability thresholds hardcoded (100, 75, 50) bypassing `InstabilityThresholds` cache | lead-prog, tech-dir | MAJOR |

---

## lead-programmer — Full Code Quality Report

### Core (10 files)

**IStateHandler.cs** — Clean. No findings.

**ConfigBase.cs** — Clean. No findings.

**GameStateManager.cs** — Clean. No findings.

**Enums.cs** — 1 finding.
- `Enums.cs:104` — MINOR — `InstabilityThresholds` static class mixes enum helper logic with data caching; violates SRP (threshold math + settings coupling in one class inside an enums file).

**GameSettings.cs** — Clean. No findings.

**ConfigStubs.cs** — 1 finding.
- `ConfigStubs.cs:7` — MINOR — `BodyPartConfig` and `StatusEffectConfig` stubs have no doc comments on the classes themselves (only inline comments); public API standard requires `/// <summary>`.

**ConfigLoader.cs** — 2 findings.
- `ConfigLoader.cs:92` — MINOR — `Debug.Log` fires once per collection type on every load; at 6 collections this creates log noise in production builds. Should be `#if UNITY_EDITOR` or removed.
- `ConfigLoader.cs:33-38` — MINOR — Static dictionaries on a MonoBehaviour survive domain reload in Editor without `Reinitialize()` being called; `_initialized` flag gate prevents re-load after `Reinitialize()` only resets in `#if UNITY_EDITOR`. Non-editor domain reload path is unprotected (edge case, but noted).

**AIPersonalityConfig.cs** — Clean. No findings.

**EncounterConfig.cs** — 1 finding.
- `EncounterConfig.cs:81-86` — MAJOR — `GetHeight(x, z)` and `GetTerrain(x, z)` perform zero bounds checking before indexing the flat arrays. An out-of-bounds `x` or `z` throws `IndexOutOfRangeException` at runtime. `ValidateConfig` in `EncounterManager` catches bad sizes before use, but that's a separate call path — the helper methods themselves are unsafe.

**EncounterCreatureEntry.cs** — Clean. No findings.

### Creatures (4 files)

**CreatureConfig.cs** — Clean. No findings.

**CreatureDatabase.cs** — Clean. No findings.

**MoveConfig.cs** — 1 finding.
- `MoveConfig.cs:28-58` — MINOR — `MoveEffect` is declared `class` with a comment justifying it; however, the comment says "optional fields benefit from reference semantics in Inspector" — `chance` and `magnitude` are never null. ADR-009 calls for structs for serializable value containers. Low risk but inconsistent with stated ADR.

**CreatureInstance.cs** — 4 findings.
- `CreatureInstance.cs:460` — MAJOR — `Enumerable.Contains(_config.AvailableSlots, slot)` allocates an enumerator every call to `EquipPart`. `_config.AvailableSlots` is `IReadOnlyList<BodySlot>`; use a plain `for` loop or `List<T>.Contains`.
- `CreatureInstance.cs:28` — MINOR — `private static GameSettings S => ConfigLoader.Settings` creates a static dependency on `ConfigLoader` inside a data class. Fine per ADR-003 but not documented at the property — add inline note or suppress with `// ADR-003`.
- `CreatureInstance.cs:187-195` — MINOR — `AvailableForms` property returns mutable `HashSet<DamageForm>` directly; callers can mutate the cache. Return type should be `IReadOnlySet<DamageForm>` (C# 9+) or copy-on-return.
- `CreatureInstance.cs:621-636` — MINOR — `AwardXP` while-loop calls `RecalculateStats()` and `CheckMovePoolForNewMoves()` per level-up; multiple successive level-ups call these O(n) operations n times. Acceptable for MVP but worth noting for high-level creatures.

### Combat (30 files)

**BattleStats.cs** — Clean. No findings.

**CaptureResultArgs.cs** — Clean. No findings.

**CreatureActedArgs.cs** — 1 finding.
- `CreatureActedArgs.cs:1-63` — MINOR — `CreatureActedArgs` lacks a `Target` field. `CombatHUDController.OnCreatureActed` acknowledges this gap in a comment and uses a brittle workaround (scanning all enemies). Adding `Target` here is the correct fix; noted as a missing field, not a doc issue.

**Enums/ActionType.cs** — Clean. No findings.

**Enums/CombatPhase.cs** — Clean. No findings.

**Enums/CombatResult.cs** — Clean. No findings.

**IAIDecisionSystem.cs** — Clean. No findings.

**IDamageCalculator.cs** — Clean. No findings.

**IMoveEffectApplier.cs** — 1 finding.
- `IMoveEffectApplier.cs:9-10` — MINOR — Doc comment says "Does NOT handle Recoil or Drain" but that is an implementation detail of `MoveEffectApplier`, not a contract of the interface. The interface itself should not bake in implementation choices; move the note to the concrete class.

**IStatusEffectProcessor.cs** — Clean. No findings.

**StatusEffectEntry.cs** — 1 finding.
- `StatusEffectEntry.cs:26,32` — MAJOR — `Effect` and `RemainingRounds` are public mutable fields on a struct (no `readonly`, no property). Any caller can mutate a copy without TurnManager noticing (value-type semantics). The deliberate write-back pattern (`statusEntries[i] = entry`) works correctly, but unguarded public fields invite unintentional mutation bugs. Should be read-only properties with a dedicated `WithDecrementedRounds()` method or similar.

**TurnAction.cs** — Clean. No findings.

**DamageCalculator.cs** — 2 findings.
- `DamageCalculator.cs:17` — MINOR — `private const float StabMultiplier = 1.5f` is declared but marked "unused here — lives in TypeChart, listed for reference." Dead constant; remove it. It also shadows `TypeChart.StabMultiplier` and could mislead future editors.
- `DamageCalculator.cs:104-127` — MAJOR — `GetFormStatPairing` is duplicated verbatim in `AIActionScorer.cs:262-285`. Any change to the stat-pairing logic must be made in two places. This is a DRY violation; extract to a shared utility (e.g., `CombatFormulas` static class).

**TypeChart.cs** — 1 finding.
- `TypeChart.cs:169` — MINOR — `Debug.LogError("[TypeChart] Not initialized...")` inside `GetMultiplier` will fire per damage calculation if initialization is missed; no guard after logging (returns 1.0f silently). Should throw or assert in dev builds rather than silently return neutral.

**CaptureCalculator.cs** — Clean. No findings.

**IEncounterManager.cs** — Clean. No findings.

**CandidateAction.cs** — 1 finding.
- `CandidateAction.cs:23` — MINOR — `CompositeScore { get; set; }` is public mutable on a class that is described as scored by `AIActionScorer`. The setter is `set` not `internal set`, so any external caller can mutate scores. Should be `internal set` to enforce that only the AI pipeline writes scores.

**BattleContext.cs** — Clean. No findings.

**EncounterManager.cs** — 1 finding.
- `EncounterManager.cs:25` — MINOR — Default constructor `public EncounterManager() : this(ConfigLoader.GetCreature)` introduces a direct static dependency in the no-arg constructor. This is acceptable per ADR-003 (ConfigLoader is an approved singleton), but it means unit tests that new up `EncounterManager()` without initializing ConfigLoader will throw or return null silently. Add a null-check or doc note.

**AIActionScorer.cs** — 2 findings.
- `AIActionScorer.cs:262-285` — MAJOR — `GetFormStatPairing` duplicated from `DamageCalculator` (see above).
- `AIActionScorer.cs:214-255` — MINOR — `ScoreFormTactics` is 42 lines (excluding the method signature); exceeds the 40-line limit by 2 lines. Marginal but flagged.

**AIDecisionSystem.cs** — 2 findings.
- `AIDecisionSystem.cs:117,134,150` — MINOR — `SelectBestCandidate` allocates three intermediate `List<CandidateAction>` on every call (`tied`, `powerTied`, `accTied`). Called once per enemy per round — low frequency, but avoidable with in-place max-finding.
- `AIDecisionSystem.cs:103-161` — MINOR — `SelectBestCandidate` is 59 lines; exceeds the 40-line limit. Extract tiebreaker stages into private helpers.

**CaptureSystem.cs** — Clean. No findings.

**ICaptureSystem.cs** — Clean. No findings.

**MoveEffectApplier.cs** — Clean. No findings.

**StatusEffectProcessor.cs** — 1 finding.
- `StatusEffectProcessor.cs:16-18` — MAJOR — `BurnDivisor`, `PoisonDivisor`, and `ParalysisThreshold` are private constants hardcoded in the class. Per ADR-001 all tuning values must come from `CombatSettings`. `CombatSettings` already has `BurnDotDivisor` and `PoisonDotDivisor` — the processor ignores them and uses hardcoded fallbacks instead. This violates ADR-001 and means the Inspector knobs for Burn/Poison have zero effect.

**IPlayerInputProvider.cs** — Clean. No findings.

**TurnManager.cs** — 5 findings.
- `TurnManager.cs:503` — MAJOR — `entries.Any(e => e.Effect == StatusEffect.Confusion && !e.IsExpired)` allocates a LINQ delegate and enumerator on every `ExecuteUseMove` call (called once per creature per round). Replace with a manual loop.
- `TurnManager.cs:704-705` — MINOR — `_enemyParty.All(c => c.IsFainted)` and `_playerParty.All(c => c.IsFainted)` each allocate a LINQ delegate. `CheckEndCondition` is called after every faint and capture. Replace with a cached helper or manual loop.
- `TurnManager.cs:859` — MINOR — `_playerParty.Concat(_enemyParty)` in `InitializeStatusDurations` allocates an iterator. Acceptable in the constructor (called once), but inconsistent with the allocation-avoidance pattern elsewhere. Replace with two separate `foreach` loops.
- `TurnManager.cs:139-173` — MINOR — Constructor is 12 parameters; valid but at the upper edge of readability. Consider a `TurnManagerConfig` value object grouping `settings`, `damageCalculator`, `captureSystem`, etc. Not a standards violation, noted as a suggestion.
- `TurnManager.cs:346-421` — MINOR — `ExecuteMovementStep` is 76 lines; significantly exceeds the 40-line limit. Split movement validation, pathfinding, position update, and facing update into focused private methods.

**TargetingHelper.cs** — Clean. No findings.

**TurnActionValidator.cs** — Clean. No findings.

**CombatSettings.cs** — Clean. No findings.

### Gameplay (3 files)

**PartyState.cs** — Clean. No findings.

**GridSystem.cs** — 2 findings.
- `GridSystem.cs:192-245` — MINOR — `FindPath` is 54 lines; exceeds the 40-line limit. The inner loop body (neighbour evaluation) can be extracted.
- `GridSystem.cs:266-316` — MINOR — `GetReachableTiles` is 51 lines; exceeds the 40-line limit. Same issue — inner loop body extractable.

**TileData.cs** — Clean. No findings.

### UI (9 files)

**CombatController.cs** — 3 findings.
- `CombatController.cs:149` — MAJOR — `Resources.Load<CombatSettings>("Data/CombatSettings")` called inside `StartCombat` at runtime. Per ADR-001 config loading belongs to `ConfigLoader`, not scattered `Resources.Load` calls in MonoBehaviours. Also duplicated in `PlayerInputController.cs:121`.
- `CombatController.cs:119-168` — MINOR — Convenience `StartCombat` overload is 50 lines; exceeds the 40-line limit (subsystem construction + context extraction). Extract subsystem factory to a private method.
- `CombatController.cs:354-386` — MINOR — `PlayerInputCollector` inner class is not documented (`/// <summary>` exists but `SubmitAction` and `CancelAction` are missing doc comments on public methods).

**PlayerInputController.cs** — 3 findings.
- `PlayerInputController.cs:121` — MAJOR — Duplicate `Resources.Load<CombatSettings>` call (same issue as `CombatController`). Both classes independently load the same asset; should be injected or fetched once.
- `PlayerInputController.cs:319-334` — MINOR — `OnPhaseChanged` allocates a `new List<CreatureInstance>()` on every `PlayerSelect` phase transition (every round). Use the existing `_creaturesNeedingActions` list cleared and refilled instead.
- `PlayerInputController.cs:399-416` — MINOR — `OnMoveHighlightRequested` performs a `ConfigLoader.GetMove` lookup per move button hover (every mouse hover event). Should cache the move-to-slot mapping on `AdvanceToNextCreature` rather than re-resolving on every hover.

**CombatHUDController.cs** — 4 findings.
- `CombatHUDController.cs:248-284` — MINOR — `OnRoundStarted` allocates `new List<CreatureInstance>()`, calls `AddRange` twice, `RemoveAll`, and `Sort` every round. All avoidable: reuse a cached buffer, avoid the allocation-heavy `RemoveAll` lambda.
- `CombatHUDController.cs:292-310` — MINOR — `OnCreatureActed` performs a heuristic "find probable target" loop over all enemies on every `UseMove` action. This is O(n) with allocating delegates and is explicitly described in comments as a workaround for `CreatureActedArgs` lacking a `Target` field. Fixing the root cause (`CreatureActedArgs`) eliminates this code entirely.
- `CombatHUDController.cs` — MINOR — No `OnDestroy` method; `UnsubscribeFromController` is only called in `OnDisable`. If the GameObject is destroyed directly, `OnDisable` fires first so this is technically safe, but explicit `OnDestroy` unsubscription is the idiomatic Unity pattern and matches `CombatController` and `PlayerInputController`.
- `CombatHUDController.cs:24` — MINOR — `[SerializeField] CombatController combatController` is a public-facing serialized field with no doc comment on a MonoBehaviour. Coding standards require `/// <summary>` on all public/serialized API.

**CreatureInfoPanelController.cs** — 3 findings.
- `CreatureInfoPanelController.cs:251,262,264,270` — MAJOR — `const float MaxInstability = 100f` is a hardcoded magic constant; `75` and `50` thresholds are also hardcoded inline. These should use `InstabilityThresholds.CriticalMin` / `UnstableMin` from `Enums.cs` (which already cache these values from `GameSettings`). Current code bypasses the data-driven system entirely and will diverge if thresholds change.
- `CreatureInfoPanelController.cs:308` — MINOR — `new Color(0.18f, 0.80f, 0.35f)` (equipped indicator) and `new Color(0.91f, 0.63f, 0.06f)` (overflow badge) are hardcoded inline color literals. Per ADR-001 visual constants should come from USS stylesheets, not inline C# style assignments.
- `CreatureInfoPanelController.cs:338` — MINOR — `effects.Skip(3).Select(e => e.ToString())` allocates inside `RefreshStatusEffects`, which is called on every `CreatureActed` event. Replace with a manual `StringBuilder` loop.

**MoveSelectionPanelController.cs** — 2 findings.
- `MoveSelectionPanelController.cs:366-372` — MINOR — `BuildTypeLabelClasses()` calls `System.Enum.GetValues(typeof(CreatureType))` once at static init (acceptable), but `ApplyTypeColorClass` iterates the full dictionary to remove all classes on every refresh. With 14 types this is 14 `RemoveFromClassList` calls per move button per `BindCreature` call. Should track the currently applied class and remove only that one.
- `MoveSelectionPanelController.cs:153-170` — MINOR — `CycleTarget` allocates `new List<CreatureInstance>()` on every Tab keypress. Cache a reusable buffer or iterate `EnemyParty` directly with index arithmetic.

**SwitchOverlayController.cs** — 1 finding.
- `SwitchOverlayController.cs:64-88` — MINOR — `Show` creates new `VisualElement` slot objects for the entire party list every time the overlay opens. If the overlay opens/closes multiple times per combat, this creates GC pressure. Slots should be pooled or reused.

**TileHighlightController.cs** — 1 finding.
- `TileHighlightController.cs:19` — MINOR — `_lastMoveHoverTime` field is set by `ShouldComputeOnHover()` but `ShouldComputeOnHover()` has a side effect (sets `_lastMoveHoverTime`) even when returning false on re-entry. A method named `Should...` with a mutating side effect violates the command-query separation principle; rename to `ThrottleHover()` or restructure.

**TurnOrderBarController.cs** — 1 finding.
- `TurnOrderBarController.cs:23` — MINOR — `internal static readonly Dictionary<CreatureType, Color> TypeColors` is `internal` solely so `SwitchOverlayController` can access it directly. This is a cross-class coupling within the UI layer. Extract to a shared `CombatTypeColors` static class or make it package-private via a dedicated accessor.

**TypeEffectivenessCallout.cs** — Clean. No findings.

### lead-programmer Summary
| Severity | Count |
|----------|-------|
| CRITICAL | 0 |
| MAJOR | 11 |
| MINOR | 33 |
| **Total** | **44** |

### Top Priorities (MAJOR findings)
1. `EncounterConfig.cs:81-86` — Unsafe flat-array indexers, no bounds check, runtime crash risk.
2. `StatusEffectProcessor.cs:16-18` — Hardcoded Burn/Poison divisors ignore `CombatSettings` knobs; ADR-001 violation.
3. `DamageCalculator.cs / AIActionScorer.cs` — `GetFormStatPairing` duplicated verbatim; DRY violation, logic drift risk.
4. `StatusEffectEntry.cs:26,32` — Public mutable fields on a struct invite silent value-type mutation bugs.
5. `TurnManager.cs:503` — LINQ `Any` with delegate allocation on every `ExecuteUseMove` call.
6. `CreatureInfoPanelController.cs:251-270` — Instability thresholds hardcoded, bypasses `InstabilityThresholds` cache entirely.
7. `CombatController.cs:149` / `PlayerInputController.cs:121` — Duplicated `Resources.Load<CombatSettings>` violates ADR-001.
8. `CreatureInstance.cs:460` — `Enumerable.Contains` on `IReadOnlyList` allocates enumerator every `EquipPart` call.

---

## unity-specialist — Full Unity Patterns Report

### Core/

**ConfigLoader.cs**

MAJOR — `Resources.Load` / `Resources.LoadAll` used throughout (lines 69, 81). The deprecated-apis.md lists `Resources.Load()` → Addressables as the migration target. This is explicitly allowed for MVP per ADR-001 and the doc itself says "post-MVP for us", so flagged MAJOR (not CRITICAL) as known tech-debt. No action needed now, but there is no in-code TODO marking this as deferred — add one so it doesn't get missed.

MINOR — `CombatController.cs` (line 149) also calls `Resources.Load<CombatSettings>` and `PlayerInputController.cs` (line 121) does the same. Same note applies.

**GameStateManager.cs**

MINOR — `DontDestroyOnLoad` on line 63. ADR-003 explicitly approves GameStateManager as a required singleton, so expected. The static event `StateChanged` (line 43) survives scene loads — the file already has a warning comment about this, which is correct. Audited as clean.

MINOR — Coroutine-equivalent using `Awaitable` (line 103) is correct Unity 6 pattern. Clean.

### Creatures/

**CreatureInstance.cs**

MINOR — `Enumerable.Contains` called on line 460 (`EquipPart`): `if (!Enumerable.Contains(_config.AvailableSlots, slot))`. This is a LINQ call that allocates an enumerator on every call to `EquipPart`. `AvailableSlots` is `IReadOnlyList<BodySlot>` — use a plain `for` loop or `List<T>.Contains` instead. Not a hot path, but LINQ allocation on a list is a code quality issue.

`CreatureInstance` is a plain C# class (correct, ADR-002). `[System.Serializable]` is present for JsonUtility (correct, ADR-005). No MonoBehaviour violation.

### Combat/

**DamageCalculator.cs**

MAJOR — Hardcoded tuning constants at lines 17-31 (e.g., `HeightBonusPerLevel = 0.1f`, `AttackerTerrainSynergy = 1.2f`, `DefenderTerrainSynergy = 0.8f`, `CoverReduction = 0.5f`, `StatDivisor = 50f`, `BaseDamageFloor = 2f`, `MinDamage = 1`).

The project forbids hardcoded gameplay values — all values must come from ScriptableObjects (ADR-001, technical-preferences.md). `GameSettings` and `CombatSettings` exist and are injected elsewhere in this exact system. `HeightBonusPerLevel` duplicates `GameSettings.HeightBonusPerLevel` (line 28 in GameSettings.cs). `MinDamage` duplicates `GameSettings.MinDamage`. The terrain synergy multipliers (1.2/0.8), cover reduction (0.5), stat divisor (50), and base damage floor (2) are not in any ScriptableObject.

`StabMultiplier` at line 17 has a comment "unused here — lives in TypeChart, listed for reference" — but it's still declared as a private const. Harmless but confusing since it shadows the name.

**CombatSettings.cs**

MINOR — `BurnDotDivisor` (line 44) and `PoisonDotDivisor` (line 49) are defined here but `StatusEffectProcessor.cs` ignores them: it uses its own private constants `BurnDivisor = 16` and `PoisonDivisor = 8` on lines 17-18. This is a split-config bug — the ScriptableObject knobs exist but the implementation doesn't read them.

MAJOR — `StatusEffectProcessor` does not accept `CombatSettings` as a dependency. It has hardcoded `BurnDivisor = 16` and `PoisonDivisor = 8` (lines 17-18) and `ParalysisThreshold = 0.25` (line 19). These values are all defined as tuning knobs in `CombatSettings` (`burnDotDivisor`, `poisonDotDivisor`, `paralysisSuppressionChance`) but the processor reads its own internal constants instead. The ScriptableObject knobs are dead configuration.

**TypeChart.cs**

MINOR — `_superEffectiveMultiplier` and `_resistedMultiplier` are declared on lines 235-236 as cached fields but they are never updated from `GameSettings` (only `_stabMultiplier` is read from settings in `CacheSettings()`). The comment on line 231 says "these cached values are reference constants for UI and balance tools" — acceptable but the values (2.0/0.5) are effectively hardcoded since they're baked into the matrix. If the GDD ever allows tuning SE/resist multipliers these would need wiring. MINOR tracking item only.

**CombatController.cs**

MAJOR — `Resources.Load<CombatSettings>("Data/CombatSettings")` at line 149. MVP-permitted but needs a deferred TODO comment.

MAJOR — `ScriptableObject.CreateInstance<AIPersonalityConfig>()` at line 139 and `ScriptableObject.CreateInstance<CombatSettings>()` at line 153. Creating ScriptableObjects at runtime with `CreateInstance` for default fallbacks bypasses the inspector-configured values. These instances will use C# field initializer defaults, not the .asset file values. If the asset isn't found in production this silently runs with untuned values. The fallback should at minimum log an error (not a warning), and ideally hard-fail in non-editor builds. Using `Debug.LogWarning` (lines 138, 152) is too soft for a missing required asset.

**EncounterManager.cs**

Clean — pure C#, no MonoBehaviour, DI-injected lookup function, correct ADR-002 compliance.

### UI/

**CombatHUDController.cs**

MINOR — `Camera.main` accessed at line 53 (`_cachedCamera = Camera.main`) in `Initialize()`. `Camera.main` is cached (good), but `Camera.main` itself calls `FindObjectOfType` under the hood. Caching in `Initialize()` is the correct mitigation — fine as long as Initialize is called once. If the camera can change (e.g., Cinemachine brain swap), the cache becomes stale. Flag for awareness but not a bug now.

MINOR — `KeyDownEvent` is used for keyboard input (line 107 `_root.RegisterCallback<KeyDownEvent>(OnKeyDown)`). The deprecated-apis reference says to use the new Input System. `KeyDownEvent` is a UI Toolkit event (not legacy `Input.GetKey`) so it is not the deprecated `Input` class — acceptable for UI navigation within UI Toolkit. However it uses `KeyCode` enum (line 402+), which is a legacy Unity enum. UI Toolkit's `KeyDownEvent` exposes `evt.keyCode` as `KeyCode` — standard UI Toolkit API, not the legacy Input Manager. Not a violation, but if the project adds full gamepad support, this keyboard-only path will need Input System `InputAction` mapping.

MAJOR (shared with `PlayerInputController`) — `Resources.Load<CombatSettings>` in `PlayerInputController.Initialize()` at line 121. See ConfigLoader note.

**PlayerInputController.cs**

MINOR — `OnDestroy` exists and calls `UnsubscribeFromEvents()` (line 642). Clean. No event leak.

No `FindObjectOfType`. No `Update()` polling — all input flows through events and callbacks. Clean.

**CreatureInfoPanelController.cs**

MINOR — Hardcoded color values at lines 308 and 346:
- Line 308: `new Color(0.18f, 0.80f, 0.35f)` for the equipped indicator border color
- Line 346: `new Color(0.91f, 0.63f, 0.06f)` for the overflow badge background

These should be USS classes or at minimum named constants, not inline magic floats. The visual spec recommends CSS-like styling (USS) over inline styles — this contradicts the project's UI Toolkit pattern of using USS classes for all visual state. Also `style.borderBottomColor` (line 308) is an inline style, not a class — inconsistent with the rest of the file which uses `AddToClassList`.

MINOR — `const float MaxInstability = 100f` at line 251 is a local constant inside `Refresh()`. This value is defined in `GameSettings.InstabilityMax` (and cached in `InstabilityThresholds.Max`). Should read from `InstabilityThresholds.Max` instead of a hardcoded 100.

**MoveSelectionPanelController.cs**

MINOR — `BuildTypeLabelClasses()` at line 370 calls `System.Enum.GetValues(typeof(CreatureType))`. Called once statically to build a dictionary, which is then cached in `TypeLabelClasses`. The static constructor approach is correct — no allocation at runtime. Clean.

### Assembly Definitions

**GeneForge.Core.asmdef**

MAJOR — `"autoReferenced": true`. All four assembly definitions use `autoReferenced: true`. For a project at this stage, `autoReferenced: true` means every assembly in the project implicitly sees every other assembly, defeating the purpose of having assembly definitions at all. The dependency graph should be explicit: Core should be `autoReferenced: true` (OK as the base), but Combat, Grid, UI should be `autoReferenced: false` with explicit GUID references. Combat and UI already list explicit GUID references in their `references` array — but `autoReferenced: true` makes the explicit references irrelevant since everything can see everything. This is a compilation boundary leak.

Correct pattern:
- `GeneForge.Core`: `autoReferenced: true` (acceptable — everyone needs it)
- `GeneForge.Grid`: `autoReferenced: false`, explicit refs to Core
- `GeneForge.Combat`: `autoReferenced: false`, explicit refs to Core + Grid
- `GeneForge.UI`: `autoReferenced: false`, explicit refs to Core + Grid + Combat

**GeneForge.Tests.EditMode.asmdef**

MINOR — References a GUID `b8c4e1a2d3f5094ab9e7c6d8a1f2b3c4` that doesn't match any of the four production assemblies' GUIDs (Core is `a2365be9fbad0954a9bb29716da7ec7e`, Grid is `ac121ad629328e54a9154761bb5ff138`, Combat is `a420e07fa2692194cb2e6c26e72f6720`). The unknown GUID may be a Gameplay or Creatures assembly that doesn't have an asmdef yet. If `Assets/Scripts/Creatures/` and `Assets/Scripts/Gameplay/` lack asmdef files, their code compiles into the default `Assembly-CSharp` assembly and that GUID reference will fail to resolve.

CRITICAL — `Assets/Scripts/Creatures/` and `Assets/Scripts/Gameplay/` have no `.asmdef` files. Two of the most important game systems (CreatureInstance, CreatureConfig, PartyState, GridSystem) compile into the monolithic `Assembly-CSharp` assembly instead of isolated assemblies. This:
1. Means any change in these folders triggers a full project recompile
2. Means the assembly boundary controls in GeneForge.Combat.asmdef are porous — Combat references Grid via explicit GUID but Creatures is only reachable via `autoReferenced: true` on the default assembly
3. The unresolved GUID in EditMode tests (`b8c4e1a2d3f5094ab9e7c6d8a1f2b3c4`) likely points to a missing Creatures asmdef that was planned but never created

**GeneForge.Tests.PlayMode.asmdef**

MINOR — Uses assembly name strings (`"GeneForge.Core"`, `"GeneForge.Combat"`) instead of GUIDs. Unity recommends GUID references for cross-assembly asmdef references for stability. Name-based references break if the assembly is renamed.

### unity-specialist Summary

| Severity | Count | Systems Affected |
|----------|-------|------------------|
| CRITICAL | 1 | Assembly definitions — Creatures/ and Gameplay/ missing asmdef |
| MAJOR | 6 | DamageCalculator hardcoded values; StatusEffectProcessor ignoring CombatSettings knobs; autoReferenced:true on all asmdefs; Resources.Load in CombatController+PlayerInputController (2); ScriptableObject.CreateInstance fallbacks too soft |
| MINOR | 9 | CreatureInstance LINQ in EquipPart; TypeChart dead cached fields; Camera.main cache; CombatHUDController KeyCode in UI Toolkit; CreatureInfoPanelController inline styles + hardcoded MaxInstability; ConfigLoader/CombatController/PlayerInputController missing deferred TODO on Resources.Load; PlayMode asmdef name refs; EditMode asmdef unresolved GUID |

**Total: 16 findings (1 CRITICAL, 6 MAJOR, 9 MINOR)**

### Priority Actions
1. **CRITICAL** — Add `GeneForge.Creatures.asmdef` under `Assets/Scripts/Creatures/` and `GeneForge.Gameplay.asmdef` under `Assets/Scripts/Gameplay/`. Wire their GUIDs into Combat, UI, and Test asmdefs. Set all non-Core asmdefs to `autoReferenced: false`.
2. **MAJOR** — `DamageCalculator.cs` constants need to migrate to `CombatSettings` or `GameSettings` and be injected via constructor.
3. **MAJOR** — `StatusEffectProcessor` needs a `CombatSettings` constructor parameter to read `BurnDotDivisor`, `PoisonDotDivisor`, and `ParalysisSuppressionChance`.
4. **MAJOR** — `CombatController.StartCombat` fallback `ScriptableObject.CreateInstance` should be `Debug.LogError` + early return (or exception in non-editor builds).

---

## technical-director — Full Architecture Report

### 1. Assembly Definition & Dependency Graph

#### Resolved GUID Map
| Assembly | GUID | References |
|----------|------|------------|
| GeneForge.Core | `a2365be...` | (none) |
| GeneForge.Grid | `a420e07...` | Core |
| GeneForge.Combat | `ac12164...` | Core, Grid |
| GeneForge.UI | `b8c4e1a...` | Core, Combat, Grid |
| GeneForge.Tests.EditMode | — | Core, Combat, Grid, UI, NUnit |
| GeneForge.Tests.PlayMode | — | Core, Combat, NUnit |

#### Dependency Direction Analysis

Layer hierarchy per spec:
```
Foundation: Core
Core:       Creatures, Grid
Feature:    Combat, Gameplay
Presentation: UI
```

**FINDING [MAJOR] #1 — Missing GeneForge.Creatures assembly definition**

ADR-008 specifies `GeneForge.Creatures` as a separate assembly. No `.asmdef` exists under `Assets/Scripts/Creatures/`. All Creatures code compiles into `GeneForge.Core` because it lives under `Assets/Scripts/` where `GeneForge.Core.asmdef` is at the root level. This means:
- `CreatureConfig`, `CreatureInstance`, `MoveConfig`, `CreatureDatabase` all compile into GeneForge.Core
- The namespace is `GeneForge.Creatures` (correct) but the assembly is wrong
- No compile-time enforcement of Creatures→Core dependency direction
- Combat assembly can reference Creatures types only because they accidentally land in Core

**FINDING [MAJOR] #2 — Missing GeneForge.Gameplay assembly definition**

ADR-008 specifies `GeneForge.Moves` and `GeneForge.Combat` assemblies. `PartyState` lives at `Assets/Scripts/Gameplay/PartyState.cs` with namespace `GeneForge.Gameplay` but there is no `GeneForge.Gameplay.asmdef`. PartyState compiles into whichever assembly covers `Assets/Scripts/Gameplay/` — currently there is only `GeneForge.Grid.asmdef` under `Assets/Scripts/Gameplay/Grid/`, so PartyState falls to `GeneForge.Core` again.

**FINDING [MINOR] #3 — ADR-008 specifies GeneForge.Moves assembly, none exists**

MoveConfig lives in `GeneForge.Creatures` namespace. ADR-008 calls for a `GeneForge.Moves` namespace and assembly. This is a naming deviation — moves are bundled with Creatures.

#### Actual Compile-Time Assembly Boundaries
```
GeneForge.Core:    Core/* + Creatures/* + Gameplay/PartyState.cs (everything not covered by sub-asmdefs)
GeneForge.Grid:    Gameplay/Grid/*
GeneForge.Combat:  Combat/*
GeneForge.UI:      UI/*
```

### 2. ADR Compliance

**ADR-001: ScriptableObjects for all config data — COMPLIANT**
All config types extend `ConfigBase : ScriptableObject`. `GameSettings`, `CombatSettings`, `AIPersonalityConfig`, `EncounterConfig`, `CreatureConfig`, `MoveConfig`, `BodyPartConfig`, `StatusEffectConfig` — all SOs. `CombatSettings` is a separate SO from `GameSettings` (loaded via `Resources.Load`). No JSON config files anywhere.

**ADR-002: Plain C# classes for pure logic — COMPLIANT**
`GridSystem`, `TurnManager`, `DamageCalculator`, `TypeChart`, `CaptureCalculator`, `CaptureSystem`, `AIDecisionSystem`, `AIActionScorer`, `MoveEffectApplier`, `StatusEffectProcessor`, `EncounterManager`, `TargetingHelper`, `TurnActionValidator`, `PartyState`, `CreatureInstance` — all plain C# (no MonoBehaviour). Correctly documented.

**ADR-003: Minimal singletons — COMPLIANT**
Only `GameStateManager` and `ConfigLoader` use singleton pattern. Both documented as ADR-003 approved. All Combat systems use constructor injection. `CombatController` is MonoBehaviour but not singleton — explained as "ADR-003 exception: MonoBehaviour required for coroutine lifecycle."

**ADR-004: C# events for system decoupling — COMPLIANT**
No `UnityEvent` usage anywhere. All events are `System.Action<T>` or `event Action<T>`. TurnManager uses instance events (not static — correct). GameStateManager uses one static event with documented deregistration warning.

**ADR-005: JSON saves via JsonUtility — NOT YET EXERCISED**
`PartyState` is marked `[System.Serializable]`. `CreatureInstance` is `[System.Serializable]`. Parallel-list serialization pattern for body parts and affinity (correct for JsonUtility). No save/load code exists yet — structural compliance only.

**ADR-006: Modular creature body parts as SOs — COMPLIANT (stub)**
`BodyPartConfig : ConfigBase` exists as a stub. `CreatureInstance` has slot-based equip/unequip with parallel lists. Form access cache derived from equipped parts. Architecture is in place.

**ADR-007: Type chart as static 2D array — COMPLIANT**
`TypeChart._matrix` is `float[,]` (static 2D array, 15x15). Populated once via `Initialize()`. Thread-safe after init. Self-validation on startup.

**ADR-008: Domain namespaces — PARTIAL COMPLIANCE**
Namespaces are correct: `GeneForge.Core`, `GeneForge.Creatures`, `GeneForge.Combat`, `GeneForge.Grid`, `GeneForge.Gameplay`, `GeneForge.UI`. Assembly definitions only exist for Core (de facto root), Grid, Combat, UI. **Creatures and Gameplay lack their own assemblies** (see Finding #1, #2).

**ADR-009: BaseStats as struct — COMPLIANT**
`BaseStats`, `LevelMoveEntry`, `StatsBlock`, `StatusEffectEntry`, `TurnAction`, `CreatureActedArgs`, `CaptureResultArgs`, `TurnActionValidator.ValidationResult` — all structs. `MoveEffect` is explicitly documented as class with rationale ("optional fields benefit from reference semantics in Inspector").

**FINDING [MINOR] #4 — StatusEffectEntry has public fields, not properties**

`StatusEffectEntry.Effect` and `StatusEffectEntry.RemainingRounds` are public fields (lines 26, 32). This is functional for a mutable struct used internally by TurnManager, but violates the "no public fields" forbidden pattern. These fields are mutated directly by `StatusEffectProcessor`. Converting to properties would be more consistent.

### 3. Forbidden Pattern Scan

#### FindObjectOfType in hot paths — CLEAN
Zero hits.

#### Hardcoded gameplay values — PARTIAL COMPLIANCE

**FINDING [MAJOR] #5 — DamageCalculator hardcodes tuning constants**

`DamageCalculator` (lines 17-31) declares 13 `private const` values including `CritMultiplier`, `BaseCritChance`, `HighCritChance`, `HeightBonusPerLevel`, `AttackerTerrainSynergy`, `DefenderTerrainSynergy`, `CoverReduction`, etc. Some of these overlap with `GameSettings` fields (e.g., `CritMultiplier`, `HeightBonusPerLevel`) but DamageCalculator does NOT read from GameSettings. Instead it uses its own constants.

This creates two sources of truth: GameSettings has `critMultiplier = 1.5f` and `heightBonusPerLevel = 0.1f`, while DamageCalculator has identical but independent `CritMultiplier = 1.5f` and `HeightBonusPerLevel = 0.1f`. If a designer changes GameSettings in the Inspector, DamageCalculator ignores the change.

**FINDING [MAJOR] #6 — StatusEffectProcessor hardcodes DoT divisors**

`StatusEffectProcessor` (lines 17-18) has `BurnDivisor = 16` and `PoisonDivisor = 8` as `private const`. `CombatSettings` also declares `burnDotDivisor` and `poisonDotDivisor` with the same defaults. The processor does not receive CombatSettings — it ignores data-driven values entirely.

**FINDING [MINOR] #7 — CreatureInfoPanelController hardcodes instability thresholds**

Lines 262-263: `instability >= 75` and `instability >= 50` are hardcoded. Should reference `InstabilityThresholds.CriticalMin` / `UnstableMin` from `Enums.cs`.

#### Update() polling — CLEAN
Zero `void Update()` methods. CombatController uses coroutines. UI controllers are event-driven.

#### Public fields — MOSTLY CLEAN
Only `StatusEffectEntry.Effect` and `StatusEffectEntry.RemainingRounds` are bare public fields. Everything else uses properties or `[SerializeField]` private fields. See Finding #4.

### 4. Namespace & Dependency Direction Violations

**FINDING [MAJOR] #8 — Combat→Gameplay upward reference via EncounterManager and IEncounterManager**

`EncounterManager.cs` (line 4): `using GeneForge.Gameplay;` — references `PartyState`.
`IEncounterManager.cs` (line 3): `using GeneForge.Gameplay;` — references `PartyState` in interface signature.

Per the layer hierarchy, Combat is at the Feature layer alongside Gameplay. Cross-feature references are acceptable between peers, BUT the concern is that an *interface* (`IEncounterManager`) in the Combat assembly has a compile-time dependency on a type (`PartyState`) from Gameplay. This couples the Combat assembly to Gameplay.

If `PartyState` ever gets its own assembly (per ADR-008), Combat would need to reference it. Current state: no compile-time issue because `PartyState` compiles into Core. But architecturally this is a direction violation — `PartyState` is Feature-layer, referenced by Feature-layer Combat interface.

**Recommendation**: Accept for MVP. Post-MVP: introduce an `IPartyProvider` interface in Core that both Combat and Gameplay depend on.

**FINDING [MINOR] #9 — SwitchOverlayController references TurnOrderBarController.TypeColors**

`SwitchOverlayController.cs` line 145: `TurnOrderBarController.TypeColors.TryGetValue(...)`. This creates a horizontal dependency between two presentation-layer classes, which is fine for co-located UI, but `TypeColors` is an internal implementation detail exposed as `internal static`. If TurnOrderBar is ever moved to a different assembly, this breaks. Minor coupling smell.

### 5. Cross-System Interface Contracts

All TurnManager dependencies are interface-based:
- `IDamageCalculator` ✓
- `ICaptureSystem` ✓
- `IAIDecisionSystem` ✓
- `IMoveEffectApplier` ✓
- `IStatusEffectProcessor` ✓
- `IPlayerInputProvider` ✓
- `IEncounterManager` ✓

Constructor injection pattern is consistently applied. All interfaces live in `GeneForge.Combat` assembly alongside their consumers.

**FINDING [MINOR] #10 — CombatSettings not injected into StatusEffectProcessor**

`StatusEffectProcessor` uses hardcoded constants instead of receiving `CombatSettings` via constructor. The interface `IStatusEffectProcessor` doesn't parameterize with settings. This is the root cause of Finding #6.

**FINDING [MINOR] #11 — PlayerInputController loads CombatSettings via Resources.Load**

Line 121: `_settings = Resources.Load<CombatSettings>("Data/CombatSettings");`. This bypasses the injection pattern used elsewhere. Should receive CombatSettings through `Initialize()`.

**FINDING [MINOR] #12 — CombatController loads CombatSettings via Resources.Load**

Line 149: Same pattern. The convenience `StartCombat(config, party)` overload creates default subsystems and loads settings directly. Acceptable for a convenience API, but means CombatSettings has two loading paths (injected via full-param overload, Resources.Load via convenience overload).

### 6. Tech Debt Inventory

#### Known TODOs (3 total)
| File | Line | Content |
|------|------|---------|
| `TurnManager.cs` | 393 | `TODO: Gap fix #7 — fall damage skipped entirely (post-MVP)` |
| `PlayerInputController.cs` | 378 | `TODO: add Switch ActionType post-MVP` |
| `TurnOrderBarController.cs` | 131 | `TODO: Replace with CreatureConfig.PortraitSprite when art pipeline delivers portraits` |

All three are documented post-MVP items. No urgent tech debt.

#### Static Coupling via ConfigLoader

`CreatureInstance` accesses `ConfigLoader.Settings` and `ConfigLoader.GetMove()` / `ConfigLoader.GetBodyPart()` throughout its methods. Acceptable per ADR-003 (ConfigLoader is an approved singleton) but makes `CreatureInstance` untestable without calling `ConfigLoader.Initialize()` first. Test fallback defaults are provided throughout (good).

### 7. Architecture Health

Overall architecture is **strong**. The codebase demonstrates disciplined application of:
- Interface-based dependency injection for all TurnManager subsystems
- Event-driven communication (C# events, no UnityEvents)
- Data-driven configuration via ScriptableObjects
- Struct value types for immutable data containers
- No MonoBehaviour on pure logic classes
- Clean separation of presentation (UI/) from logic (Combat/)
- Zero forbidden pattern violations for FindObjectOfType and Update() polling

The four MAJOR findings are all fixable without architectural changes — they're gaps in the existing good patterns, not structural problems.

### technical-director Summary

| Severity | Count | IDs |
|----------|-------|-----|
| CRITICAL | 0 | — |
| MAJOR | 4 | #1, #2, #5, #6 |
| MINOR | 8 | #3, #4, #7, #8, #9, #10, #11, #12 |

### MAJOR Findings Requiring Action
1. **#1 + #2**: Missing `GeneForge.Creatures` and `GeneForge.Gameplay` assembly definitions. ADR-008 compliance is partial. No compile-time dependency enforcement for 2 of 4 domain assemblies.
2. **#5 + #6**: DamageCalculator and StatusEffectProcessor hardcode gameplay tuning values that also exist in GameSettings/CombatSettings ScriptableObjects. Creates dual source of truth.

---

## Prioritized Action Items (Phase 1)

| # | Action | Severity | Suggested Owner | Systems Affected |
|---|--------|----------|-----------------|------------------|
| 1 | Add `.asmdef` for Creatures/ and Gameplay/; set non-Core to `autoReferenced: false` | CRITICAL | technical-director | All assemblies |
| 2 | Migrate DamageCalculator constants to CombatSettings/GameSettings injection | MAJOR | gameplay-programmer | Damage, AI |
| 3 | Wire StatusEffectProcessor to read CombatSettings (Burn/Poison/Paralysis) | MAJOR | gameplay-programmer | Status effects |
| 4 | Extract shared `GetFormStatPairing` to utility class | MAJOR | lead-programmer | DamageCalculator, AIActionScorer |
| 5 | Replace `Resources.Load<CombatSettings>` with ConfigLoader or injection | MAJOR | unity-specialist | CombatController, PlayerInputController |
| 6 | Add bounds checking to `EncounterConfig.GetHeight/GetTerrain` | MAJOR | gameplay-programmer | Encounter system |
| 7 | Fix `StatusEffectEntry` — make fields readonly with mutation methods | MAJOR | lead-programmer | TurnManager, StatusEffectProcessor |
| 8 | Replace `Enumerable.Contains` in CreatureInstance.EquipPart | MAJOR | lead-programmer | Creature system |
| 9 | Wire CreatureInfoPanelController to use `InstabilityThresholds` | MAJOR | ui-programmer | UI |
| 10 | Replace TurnManager LINQ allocations in hot paths | MAJOR | gameplay-programmer | Combat loop |
| 11 | Harden CombatController SO fallbacks (LogError + early return) | MAJOR | unity-specialist | Combat init |
| 12 | Add `Target` field to `CreatureActedArgs` | MINOR | gameplay-programmer | Combat events, HUD |
