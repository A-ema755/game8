# Full Project Audit — Phase 2: Design & Vision

**Date**: 2026-04-08
**Scope**: all (14 implemented systems)
**Branch**: feature/Player-Input-Wiring
**Agents**: game-designer, systems-designer, creative-director

---

## game-designer — Full GDD-to-Code Alignment Report

### 1. Data Configuration Pipeline

**Status: ALIGNED**

ConfigLoader matches GDD exactly: all 6 registries present, `Initialize()` idempotent, `GameSettings` loaded as singleton, `Reinitialize()` editor-only. `InstabilityThresholds` cached from settings with fallback defaults as specified.

**One extension beyond GDD (not drift):** GameSettings in code contains additional fields not in GDD §3.4 schema (`growthMultiplierFast/Medium/Slow/Erratic`, `personalityBoostMultiplier`, `personalityPenaltyMultiplier`, `dnaStatBonusPerMod`, `blightInstabilityThreshold`, `xpBaseMultiplier`, `maxPartySize`, `fallDamageBase`, `fallDamageMinDelta`). These are consumed by CreatureInstance and PartyState and fill real gameplay needs. The GDD §3.4 notes "Post-MVP: split GameSettings into domain-specific SOs" — acceptable expansion, but GDD §3.4 should be updated to list the full field schema so it stays the source of truth.

### 2. Grid / Tile System

**Status: ALIGNED**

EncounterManager's `BuildGrid()` correctly uses flat `HeightMapFlat`/`TileLayoutFlat` arrays (a Unity serialization workaround), height clamped to `GridSystem.MaxHeight`, passability set correctly. GDD §3.2 shows `TileData` without `ProvidesCover` — implementation adds this field (used by DamageCalculator for Energy cover penalty). This is a required addition since DamageCalculator §3.3 in the GDD references cover reduction.

**Minor DRIFT:** GDD `TileData` constructor signature is `(position, height, terrain, isPassable, blocksLoS)` — implementation adds `providesCover` as a 6th parameter. GDD should add this field since damage-health-system.md §3.6 specifies it.

### 3. Game State Manager

**Status: ALIGNED** (previously confirmed at 100%).

### 4. Type Chart System

**Status: ALIGNED** (previously confirmed at 100%, 15-type chart, static 2D array).

### 5. Creature Database

**Status: ALIGNED** — GDD and ConfigLoader registry match.

### 6. Move Database

**Status: ALIGNED** — MoveConfig in code matches GDD §3.2 schema. `IsDamaging`, `GenomeType`, `Form`, `Effects`, `AlwaysHits`, `Priority`, `TargetType`, `Range` all present.

### 7. Turn Manager

**Status: ALIGNED** with two documented known gaps as expected stubs.

Phase sequence, initiative formula, split-turn execution, faint handling, end condition, BattleStats, and all enums match GDD exactly.

**Documented stub (not drift, acknowledged in code):**
- `AccuracyStageMultiplier` and `EvasionStageMultiplier` on `CreatureInstance` always return 1.0f. GDD §3.9 explicitly notes these are not yet implemented (post-MVP). Code has the comment. Aligned.

**Confusion self-hit formula — DRIFT:**
- GDD §4.5 specifies: `DamageCalculator.Calculate(synthetic move: Power=40, Physical, GenomeType=None, AlwaysHits=true, attacker=creature, defender=creature)`
- Code uses a simplified approximation: `max(1, floor(ATK * ConfusionSelfHitPower / (DEF * 50)))` and notes "Post-MVP: expose IDamageCalculator.CalculateRaw overload."
- **Gap:** The code formula omits the level coefficient `(2×level/5+2)` present in the full damage formula. At level 10, the GDD formula produces roughly 2–3× more damage than the simplified code formula. This is noted as a known limitation but the magnitude difference is not trivial — a level 50 creature using the code formula hits itself for far less than intended.

**Status tick timing — StatusEffectProcessor does NOT read CombatSettings knobs (Phase 1 known finding confirmed):**
- `BurnDivisor = 16` and `PoisonDivisor = 8` are hardcoded constants in StatusEffectProcessor.
- GDD §7 and CombatSettings expose `BurnDotDivisor` and `PoisonDotDivisor` as tunable knobs.
- Code comment says "GDD §3.7" but hardcodes the values rather than reading from `CombatSettings`.
- **Drift: StatusEffectProcessor ignores CombatSettings.BurnDotDivisor and CombatSettings.PoisonDotDivisor.**

**WildFleeSuccessRate — ALIGNED:** Code reads `_settings.WildFleeSuccessRate` correctly from CombatSettings (defaults 1.0 for MVP).

### 8. Damage & Health System

**Status: ALIGNED** with one known hardcoded-constants issue (Phase 1 confirmed).

Formula matches exactly: `((2×level/5+2) × power × (offStat/defStat) / 50 + 2)`. All three form stat pairings correct. Height bonus `1.0 + (heightDiff × 0.1)` capped at 2.0×. Terrain synergy 1.2× attacker, 0.8× defender. Energy cover 0.5×. STAB 1.5×. Crit 1.5× at 6.25%/12.5%. Variance 0.85–1.0. Minimum 1.

**Confirmed DRIFT — hardcoded tuning constants:**
DamageCalculator.cs defines all tuning values as `private const` fields (`CritMultiplier = 1.5f`, `BaseCritChance = 0.0625f`, `HeightBonusPerLevel = 0.1f`, `AttackerTerrainSynergy = 1.2f`, `DefenderTerrainSynergy = 0.8f`, `CoverReduction = 0.5f`, `StatDivisor = 50f`).

GDD §7 explicitly assigns these to a ScriptableObject (`GameSettings` or `CombatSettings`). The corresponding values DO exist in GameSettings (`stabMultiplier`, `critMultiplier`, `critBaseChance`, `heightBonusPerLevel`) but DamageCalculator never reads from GameSettings — it uses its own constants. This means changes in the Inspector have zero effect on damage calculation.

### 9. Creature Instance

**Status: ALIGNED** with one formula deviation.

Fields, DNA mechanics, Blight threshold, AvailableForms, status management, XP/leveling, affinity, body part equip/unequip all match GDD.

**Tuning knobs — ALIGNED (improved):** Unlike GDD's hardcoded spec, the implementation reads growth multipliers, personality modifiers, DNA bonus, and blight threshold from `GameSettings` with fallback defaults. This is better than the GDD described (GDD §7 does not specify these come from GameSettings, but the code is correct per the project's ADR-001 principle).

**XP threshold formula — DRIFT:**
- GDD §4 specifies: `level² × 10`
- GDD §7 tuning knob says: `level² × 10` (quadratic)
- Code uses: `level * level * mult` where `mult = S.XpBaseMultiplier` (default 10) — **this matches the GDD**.
- However, GDD §3 (creature-instance.md inline code) shows: `0.8f * level * level * level + 10 * level + 50` (cubic)
- The GDD has an internal contradiction between its §3 code sketch (cubic formula) and §4/§7 (quadratic). The implementation matches §4/§7 (quadratic). The cubic formula in §3 looks like an earlier version that was not updated.
- **Action needed:** The GDD should be corrected to remove the cubic formula from §3 or explicitly flag it as superseded.

**Personality modifiers — EXTENDED (not drift):**
- GDD §3 only defines Aggressive (ATK +1.1×) and Cautious (DEF +1.1×, SPD 0.95×) and Feral (ATK +1.2×).
- Code adds: `Aggressive DEF 0.95×`, `Feral DEF 0.95×`, `Curious SPD +1.1×`, `Curious ATK 0.95×`, `Territorial DEF +1.1×`, `Territorial SPD 0.95×`.
- GDD §7 tuning table only documents 3 personalities. These extra modifiers are undocumented in the GDD — not wrong, but need GDD update.

### 10. Capture System

**Status: ALIGNED**

Formula `(config.CatchRate × trapModifier × hpFactor × statusBonus) / 255` matches exactly. HP factor `(3×maxHP − 2×currentHP) / (3×maxHP)` matches. Status bonus table (1.0/1.2/1.5/2.5) matches. "Highest only" rule matches. `baseCatchRate == 0` short-circuits to 0.0 (matches GDD "Uncatchable" edge case).

**MVP trap hardcoding — noted in code, not drift:**
CaptureSystem.Attempt uses `const float trapModifier = 1.0f`. Code comment explicitly documents this as MVP and post-MVP will read from inventory. Aligned with GDD §3.2 step 4 (MVP: trap returned to inventory).

**Catch Predictor color thresholds — DRIFT (minor):**
- GDD §3.6 color bands: red (<20%), orange (20-50%), yellow (50-80%), green (>80%)
- GDD §7 tuning knobs: `catchPredictorGreenThreshold = 0.6`, `catchPredictorYellowThreshold = 0.3`
- CreatureInfoPanelController.ShowCatchPredictor uses: `> 0.6` = good/green, `> 0.3` = marginal/yellow, else low/red — **3 bands, not 4**. The "orange" band from §3.6 is absent. The code only has 3 CSS classes (`catch-predictor--good`, `catch-predictor--marginal`, `catch-predictor--low`) vs GDD's 4 visual bands.
- Also: GDD §3.6 specifies "minimum display 1% if catchRate > 0" — code uses `Mathf.RoundToInt(catchRate * 100f)` which would round values below 0.5% to 0. This edge case is not handled.

### 11. Party System

**Status: ALIGNED**

`DepositToStorage` blocks correctly when only 1 conscious creature remains. `PromoteNextConscious` guards against infinite loop. `ReviveAll` at 1 HP. Storage unlimited. `PartyWiped` event fires when `HasConscious` transitions to false. `MaxPartySize` reads from `GameSettings` with fallback default 6.

**GDD §3.5 method signature inconsistency (minor):**
GDD shows `HasConscious` accessing `c.isFainted` (lowercase). Code accesses `c.IsFainted` (property). Code is correct per naming conventions. GDD sketch was informal — aligned.

**Party wipe RP penalty — NOT IMPLEMENTED:**
GDD §3.6 specifies `rpPenalty = Floor(rpEarnedSinceLastStation * 0.20)`. PartyState fires `PartyWiped` event but there is no RP tracking or penalty application in the current codebase (no Leveling/XP/RP system implemented yet). This is a missing system dependency, not drift in what is implemented. Not in scope for this audit.

### 12. AI Decision System

**Status: ALIGNED with two gaps**

All 8 scoring dimensions implemented and weights match GDD defaults. Tiebreak logic (power → accuracy → random) matches GDD §3.7. Jitter `randomnessFactor` reads from `AIPersonalityConfig`. Candidate enumeration checks PP, form access, target range.

**ScoreThreatTarget — DRIFT:**
- GDD §3.2 `ScoreThreatTarget`: "Uses Threat/Aggro System's current threat values — `target.currentThreat / maxThreatInBattle`"
- Code `ScoreFinishTarget`: uses `1.0 - (target.CurrentHP / target.MaxHP)` (lowest HP fraction = highest priority).
- Code comment: "Simplified MVP targeting — no Threat/Aggro System dependency. Post-MVP: replace with threat values."
- This is a deliberate MVP simplification, not an error. The behavioral effect is different: GDD prioritizes whatever has accumulated the most threat; code prioritizes the most-wounded target. This changes tactical behavior.
- **Classification: Known MVP scope reduction. Should be documented in the GDD or systems-index as a known delta, not silently different.**

**Lookahead — NOT IMPLEMENTED:**
- GDD §3.5 "Trainer AI uses 2-turn lookahead on top 3 candidates."
- Code has no lookahead. `DecideAction` is single-turn only.
- Code comment for this is absent — the omission is not acknowledged.
- **Drift: GDD lookahead spec has no corresponding implementation and no "post-MVP" annotation in the code.**

**Wild AI threat/aggro — NOT IMPLEMENTED (same as above):** wild creatures use same scorer as trainer AI, not the threat/aggro model the GDD specifies.

### 13. Encounter System

**Status: ALIGNED** with scope-appropriate MVP simplifications.

`EncounterManager.InitializeEncounter` builds grid from config, spawns enemies, places player creatures in party-slot order, falls back to grid center on out-of-bounds tiles, logs errors for missing species IDs. `ValidateConfig` checks all constraints from GDD.

**ProvidesCover always false — known MVP limitation:**
`GetTileProperties` returns `providesCover = false` for all terrain at MVP. Code comment says "MVP: no terrain provides cover." GDD §3.3 uses heights 0–3; code uses `GridSystem.MaxHeight` (4) as impassable cliff threshold. GDD height map says 0–3 with 3 as cliff/unreachable — implementation uses 4 as the threshold (heights 0–3 are passable), which is one step higher than GDD specifies.

**Height map value interpretation — DRIFT:**
- GDD §3.3: "0=Ground, 1=Elevated low, 2=Elevated high, 3=Cliff/unreachable (impassable)"
- Code: `if (height >= GridSystem.MaxHeight)` where `MaxHeight = 4` → tiles with height 3 are **passable** in code, only height 4 is impassable
- GDD says height 3 should be impassable cliff. Code makes it passable.

### 14. Combat UI (CombatController / HUD / CreatureInfoPanel)

**Status: ALIGNED** with one confirmed hardcoded threshold.

CombatController correctly wires the coroutine loop, relays all TurnManager events, manages `PlayerInputCollector`, and signals `CombatUIPhase` transitions.

**Instability thresholds — DRIFT (Phase 1 known finding confirmed):**
- GDD §5 (combat-ui.md): "At 50+, a '!' warning icon appears" and "At instability >= 75, the instability bar flashes white."
- GDD §7 tuning knobs: `instabilityWarningThreshold = 50`, `instabilityFlashThreshold = 75`.
- CreatureInfoPanelController hardcodes `if (instability >= 75)` → critical CSS class, `if (instability >= 50)` → volatile CSS class, `if (instability >= 50)` → warning visible.
- These values match the GDD defaults, but they are hardcoded rather than reading from `InstabilityThresholds.CriticalMin` (75) and `InstabilityThresholds.UnstableMin` (50) which ARE already data-driven and accessible.
- **Drift: thresholds should read from InstabilityThresholds static class (already populated from GameSettings) instead of hardcoded literals.**

**Catch predictor color bands — DRIFT (same as §10 above):** 3 bands in code vs 4 in GDD.

### game-designer Summary Table

| # | System | Status | Drift Description |
|---|--------|--------|-------------------|
| 1 | Data Configuration Pipeline | ALIGNED | GDD §3.4 GameSettings schema needs update to list all fields |
| 2 | Grid / Tile System | ALIGNED | `ProvidesCover` not in GDD TileData sketch; height-3-as-cliff not enforced in code |
| 3 | Game State Manager | ALIGNED | — |
| 5 | Type Chart System | ALIGNED | — |
| 6 | Creature Database | ALIGNED | — |
| 7 | Move Database | ALIGNED | — |
| 8 | Turn Manager | DRIFT | (a) StatusEffectProcessor hardcodes BurnDivisor/PoisonDivisor ignoring CombatSettings knobs; (b) confusion self-hit formula omits level coefficient |
| 9 | Damage & Health System | DRIFT | DamageCalculator uses private `const` values — GameSettings equivalents have no effect |
| 10 | Creature Instance | DRIFT | GDD §3 code sketch uses cubic XP formula contradicting §4/§7 quadratic; extra personality modifiers undocumented in GDD |
| 12 | Capture System | DRIFT | Catch Predictor uses 3 color bands vs GDD 4; sub-0.5% catch rate displays as 0% |
| 14 | Party System | ALIGNED | RP wipe penalty not implemented (dependent system not in scope) |
| 16 | AI Decision System | DRIFT | (a) ScoreThreatTarget replaced by ScoreFinishTarget (MVP delta, undocumented in code); (b) Trainer 2-turn lookahead not implemented, not acknowledged as post-MVP |
| 17 | Encounter System | DRIFT | Height-3 tiles should be impassable per GDD §3.3 but `MaxHeight=4` makes height-3 passable |
| 26 | Combat UI | DRIFT | CreatureInfoPanelController hardcodes instability thresholds (50/75) instead of reading from `InstabilityThresholds` |

### Prioritized Drift Fixes

**P0 — Correctness bugs (wrong behavior):**
1. `StatusEffectProcessor` hardcodes `BurnDivisor=16`/`PoisonDivisor=8` — ignores CombatSettings tuning knobs. Fix: inject `CombatSettings` or read from it.
2. Encounter height-3 tiles are passable — GDD specifies impassable cliff. Fix: change `EncounterManager.GetTileProperties` to `if (height >= 3)` (or make `MaxHeight=3` the cliff threshold).
3. `DamageCalculator` private constants never read from `GameSettings` — makes Inspector tuning nonfunctional for damage. Fix: inject settings and read values from it.

**P1 — Design fidelity gaps:**
4. Confusion self-hit formula omits level coefficient — deals significantly less damage than GDD specifies, especially at high levels.
5. AI lookahead (2-turn) for Trainer AI not implemented and not annotated as post-MVP in code.
6. `CreatureInfoPanelController` hardcodes instability thresholds — should use `InstabilityThresholds.CriticalMin` / `InstabilityThresholds.UnstableMin`.

**P2 — GDD documentation updates needed:**
7. GDD `creature-instance.md` §3 cubic XP formula contradicts §4/§7 quadratic — remove or flag cubic as superseded.
8. GDD `data-configuration-pipeline.md` §3.4 GameSettings schema missing ~10 fields that exist in code.
9. GDD `combat-ui.md` catch predictor has 4 color bands — code implements 3. Reconcile.
10. GDD `creature-instance.md` §7 only documents 3 personality trait modifiers — code implements 6. Update GDD tuning table.
11. GDD `ai-decision-system.md` `ScoreThreatTarget` is functionally replaced by `ScoreFinishTarget` in MVP — document this delta.

---

## systems-designer — Full System Coherence Report

### System Interface Map

```
ConfigLoader (GeneForge.Core)
  └─ feeds ─► CreatureConfig, MoveConfig, BodyPartConfig, AIPersonalityConfig, GameSettings

GameSettings (GeneForge.Core)
  └─ feeds ─► CreatureInstance (growth/XP/DNA), TypeChart (StabMultiplier), InstabilityThresholds

TypeChart (GeneForge.Combat)
  ← depends on: CreatureType enum (Core), GameSettings.StabMultiplier via ConfigLoader
  └─ consumed by: DamageCalculator, AIActionScorer, CaptureCalculator (indirectly)

CreatureInstance (GeneForge.Creatures)
  ← depends on: CreatureConfig, ConfigLoader, GameSettings
  └─ consumed by: TurnManager, DamageCalculator, AIDecisionSystem, AIActionScorer,
                  CaptureSystem, CaptureCalculator, CombatController, PlayerInputController

TurnManager (GeneForge.Combat)
  ← constructor: GridSystem, List<CreatureInstance>×2, EncounterType, CombatSettings,
                 IDamageCalculator, ICaptureSystem, IAIDecisionSystem,
                 IMoveEffectApplier, IStatusEffectProcessor, IPlayerInputProvider
  └─ events: RoundStarted, RoundEnded, CreatureActed, CreatureFainted, CreatureCaptured

DamageCalculator → IDamageCalculator
  ← depends on: TypeChart (static), GridSystem, CreatureInstance, MoveConfig
  └─ static method DamageCalculator.Estimate() consumed directly by AIActionScorer

StatusEffectProcessor → IStatusEffectProcessor
  ← depends on: CreatureInstance, StatusEffectEntry
  ! KNOWN GAP: does NOT receive CombatSettings; uses private constants

MoveEffectApplier → IMoveEffectApplier
  ← depends on: CreatureInstance, MoveEffect, GridSystem

AIDecisionSystem → IAIDecisionSystem
  ← depends on: AIPersonalityConfig, CreatureInstance, AIActionScorer (static), GridSystem
  └─ moveLookup: Func<string,MoveConfig> (defaults to ConfigLoader.GetMove)

AIActionScorer (static, GeneForge.Combat)
  ← depends on: DamageCalculator.Estimate (static), TypeChart (static), GridSystem, CreatureInstance
  ! KNOWN GAP: duplicates GetFormStatPairing from DamageCalculator

CaptureSystem → ICaptureSystem
  ← depends on: CaptureCalculator (static), CreatureInstance

EncounterManager → IEncounterManager
  ← depends on: EncounterConfig, PartyState (GeneForge.Gameplay), CreatureConfig, GridSystem
  └─ produces: BattleContext

BattleContext
  ← immutable snapshot; consumed by CombatController to wire TurnManager

CombatController (GeneForge.UI, MonoBehaviour)
  ← depends on: TurnManager, IPlayerInputProvider (inner PlayerInputCollector),
                EncounterManager, all injected combat subsystems
  └─ bridges coroutine loop ↔ synchronous TurnManager

PlayerInputController (GeneForge.UI, MonoBehaviour)
  ← depends on: CombatController, MoveSelectionPanelController, TileHighlightController,
                SwitchOverlayController, CreatureInfoPanelController, CombatSettings
  └─ uses: TargetingHelper (static), TurnActionValidator (static)
```

### Findings

#### CRITICAL

**C-01 — StatusEffectProcessor ignores CombatSettings; hardcodes DoT divisors**

`StatusEffectProcessor` has private constants `BurnDivisor = 16` and `PoisonDotDivisor = 8` that duplicate the values in `CombatSettings.BurnDotDivisor` and `CombatSettings.PoisonDotDivisor`. The processor is injected via `IStatusEffectProcessor` which takes no `CombatSettings` parameter. Tuning `BurnDotDivisor` or `PoisonDotDivisor` in the asset has zero effect on runtime damage. This is a live data contract break: designer changes to the ScriptableObject silently do nothing.

Fix path: add `CombatSettings settings` parameter to `StatusEffectProcessor` constructor, remove private constants, read from settings.

**C-02 — ConfusionSelfHitDamage bypasses IDamageCalculator with a non-injectable formula**

`TurnManager.CalculateConfusionSelfHitDamage()` computes `floor(ATK * power / (DEF * 50))` inline, referencing `_settings.ConfusionSelfHitPower` but NOT going through `_damageCalculator`. This means:
- Any override or test double for `IDamageCalculator` does not cover confusion self-hit
- Crit, variance, terrain, height, STAB — all bypassed
- Formula uses a hardcoded `50f` divisor not tied to `DamageCalculator.StatDivisor`

The code comment says "Post-MVP: expose `IDamageCalculator.CalculateRaw(power, form, attacker, target)` overload" — this is correct but until done the formula is a hidden split in the damage pipeline. Any balance tuning to `StatDivisor` in `DamageCalculator` won't propagate here.

Not a crash risk but is a formula chain break that will produce silently wrong numbers after any `DamageCalculator` refactor.

#### MAJOR

**M-01 — GetFormStatPairing duplicated across DamageCalculator and AIActionScorer**

`DamageCalculator.GetFormStatPairing` (private static) and `AIActionScorer.GetFormStatPairing` (private static) are identical. AIActionScorer's XML comment says "Mirrors DamageCalculator stat pairing logic." If the stat pairing for a damage form ever changes (e.g. Energy switches to `BIO_ATK vs SPD`), both must be updated. One missed update causes AI to score moves using different stat assumptions than actual damage — producing systematically wrong AI decisions without any error.

Fix path: extract to a shared static utility in `GeneForge.Combat` (e.g. `DamageFormHelper.GetStatPairing`). Both callers reference it.

**M-02 — BattleContext.PlayerCreatures / EnemyCreatures are IReadOnlyList; TurnManager constructor requires List**

`BattleContext` exposes `IReadOnlyList<CreatureInstance>` for both parties. `CombatController.StartCombat(EncounterConfig, PartyState)` wraps them with `new List<CreatureInstance>(context.PlayerCreatures)` — correct. But the full-parameter `StartCombat` overload takes `List<CreatureInstance>` directly, which is mutable. TurnManager takes `List<CreatureInstance>` and calls `_enemyParty.Remove(...)` during capture. This means external callers using the full-parameter overload can accidentally share the same list as another system, with TurnManager silently mutating it mid-combat.

The convenience overload's copy-on-entry pattern is correct but the contract isn't enforced by the type system. The full-parameter path has no defensive copy.

**M-03 — EncounterManager.GetTileProperties hardcodes cover=false for all terrain**

`EncounterManager.GetTileProperties` returns `providesCover = false` for all terrain types unconditionally ("MVP: no terrain provides cover"). `DamageCalculator.GetCoverMultiplier` applies a 0.5× multiplier for Energy moves when `tile.ProvidesCover` is true. These two are coherent today, but if EncounterConfig tile layout data ever sets `providesCover = true` on a `TileData` directly, DamageCalculator will apply the cover reduction while EncounterManager was supposed to control it. The cover pipeline has no single owner — it's split between EncounterManager (sets it) and DamageCalculator (reads it) with no GDD reference tracing ownership.

**M-04 — AIDecisionSystem ignores BattleContext.CaptureAllowed and RetreatAllowed**

`TurnManager` enforces "no Flee in trainer battles" at execution time. `AIDecisionSystem.DecideAction` is passed `EncounterType` implicitly through the interface contract doc ("Must never return Flee for Trainer encounters") but the implementation does not check encounter type at all — it can return `ActionType.Wait` or a move, never `ActionType.Flee`, so currently this is fine. However `BattleContext.CaptureAllowed` is never threaded into `AIDecisionSystem` or `TurnManager`. If enemy AI ever needs to attempt capture (post-MVP trainer steal scenario), the contract to check `CaptureAllowed` is invisible to the AI system.

#### MINOR

**m-01 — CombatController.StartCombat (convenience) silently uses first AIPersonalityConfig from dictionary**

```csharp
foreach (var kvp in ConfigLoader.AIPersonalities)
{
    aiPersonality = kvp.Value;
    break;
}
```

All enemies in an encounter share one personality regardless of species or encounter config. `EncounterConfig` has no `AIPersonalityId` field to specify per-encounter personality. Per the GDD, enemy AI personality should be encounter- or species-driven. This is an MVP shortcut but the contract between `EncounterConfig` and `AIDecisionSystem` is incomplete — the encounter config owns no reference to personality.

**m-02 — PlayerInputController computes movement range independently of TurnManager**

`PlayerInputController.ShowMovementTiles` computes `SPD / MovementDivisor` directly. `TurnManager.ExecuteMovementStep` computes the same formula. `TargetingHelper.GetMovementTiles` also computes it. Three places, same formula. They all read from `CombatSettings.MovementDivisor` so values stay in sync, but if the movement formula ever changes (e.g. adds terrain cost), it must be updated in three locations.

**m-03 — EffectivenessLabel enum defined in TypeChart.cs, not Core/Enums.cs**

`EffectivenessLabel` (Resisted/Neutral/SuperEffective) is declared inside `TypeChart.cs` in namespace `GeneForge.Combat`. Combat UI (`TypeEffectivenessCallout`) and any future Pokedex system that displays effectiveness labels must reference `GeneForge.Combat` even for pure display logic. Per ADR-008, enums with cross-system meaning should live in `GeneForge.Core`. This one is consumed by Combat UI and potentially future Pokedex — belongs in Core.

**m-04 — TurnAction.TargetTile field is TileData, not Vector2Int**

`TurnAction.TargetTile` is typed as `TileData` (a reference type with mutable `Occupant`). `TurnManager.ExecuteUseMove` passes `action.Target` (a creature) to `_moveEffectApplier.Apply` but uses `action.TargetTile` nowhere in the current MVP implementation — `MoveEffectApplier` only reads `effect.AffectsSelf`. When AoE and Line effects are implemented post-MVP, `TurnAction.TargetTile` will hand a mutable `TileData` reference into the effect pipeline, violating the struct's "immutable submitted action" contract. The `TurnAction` docstring says it is immutable, but `TileData.Occupant` is mutable through the reference.

**m-05 — PartyState.Fainted event vs CreatureInstance.Fainted event — dual faint notification paths**

`CreatureInstance.TakeDamage` fires `Fainted` event on HP=0. `TurnManager.HandleFaint` also fires `TurnManager.CreatureFainted` event. `PartyState.PartyWiped` fires when `HasConscious` transitions to false inside `NotifyChanged()`. There are three separate faint events across three systems. Post-MVP systems (Pokedex, Rival, Living Ecosystem) could subscribe to any of them and get inconsistent notification timing. No single canonical "creature fainted in combat" event exists.

### GDD Dependency Map Compliance

| System | systems-index.md Layer | Implemented Layer | Match |
|--------|------------------------|-------------------|-------|
| Data Config Pipeline | Foundation | Core namespace | Yes |
| TypeChart | Foundation | Combat namespace | Acceptable (ADR-007) |
| GameStateManager | Foundation | Core namespace | Yes |
| CreatureDatabase/Config | Core | Creatures namespace | Yes |
| MoveDatabase/Config | Core | Creatures namespace | Yes |
| TurnManager | Core | Combat namespace | Yes |
| Damage & Health | Core | Combat namespace | Yes |
| CreatureInstance | Core | Creatures namespace | Yes |
| CaptureSystem | Feature | Combat namespace | Yes |
| PartyState | Feature | Gameplay namespace | Yes |
| AIDecisionSystem | Feature | Combat namespace | Yes |
| EncounterManager | Feature | Combat namespace | Yes |
| Combat UI (CombatController, etc.) | Presentation | UI namespace | Yes |

All 14 implemented systems sit in their correct dependency layers. No system reaches upward to a layer that depends on it.

### systems-designer Summary

| Severity | Count | Issues |
|----------|-------|--------|
| CRITICAL | 2 | C-01 (StatusEffectProcessor ignores CombatSettings), C-02 (confusion damage bypasses IDamageCalculator) |
| MAJOR | 4 | M-01 (GetFormStatPairing duplicate), M-02 (mutable party list contract), M-03 (cover ownership split), M-04 (AI blind to CaptureAllowed/RetreatAllowed) |
| MINOR | 5 | m-01 (AI personality selection), m-02 (movement formula triplicated), m-03 (EffectivenessLabel in wrong namespace), m-04 (TileData ref in immutable TurnAction), m-05 (triple faint event paths) |

**Priority order for fixes:** C-01 first (breaks designer tuning today), C-02 second (breaks formula chain on any DamageCalculator refactor), then M-01 (next most likely to diverge silently).

---

## creative-director — Vision & Pillar Adherence Audit

### Per-System Pillar Alignment

#### Foundation Layer

| # | System | Status | Pillar(s) Served | Assessment |
|---|--------|--------|-------------------|------------|
| 1 | Data Configuration Pipeline | ON-PILLAR | All (enabling layer) | ScriptableObject-driven data model is pillar-agnostic infrastructure. CreatureConfig, MoveConfig, EncounterConfig, AIPersonalityConfig all exist as SO. No design values hardcoded. Properly enables every pillar without constraining any. |
| 2 | Grid / Tile System | ON-PILLAR | P2 (Tactical Grid Mastery) | Height 0-4, A* pathfinding, Chebyshev distance, tile terrain types, occupancy tracking, LoS — all present. Height-variable grid is the spatial foundation P2 demands. TerrainType enum includes synergy-relevant types (Lava, Water, Grass, etc.). Strong. |
| 3 | Game State Manager | ON-PILLAR | All (enabling layer) | Scene state machine. Infrastructure — no pillar tension. |
| 5 | Type Chart | ON-PILLAR | P2 (primary), P1 (secondary) | 14 genome types across 3 tiers, 36 SE relationships, STAB calculation, terrain-type synergy mapping, dual-type defense. Also supports P1 through ActiveSecondaryType Blight mechanic (instability overrides secondary type). Well integrated. |

#### Core Layer

| # | System | Status | Pillar(s) Served | Assessment |
|---|--------|--------|-------------------|------------|
| 6 | Creature Database | ON-PILLAR | P1 (primary), P2 (secondary) | CreatureConfig includes: BaseStats, PrimaryType, SecondaryType, TerrainSynergyType, GrowthCurve, MovePool, AvailableSlots (body part slots), CatchRate. Body part slot support directly enables P1 modular engineering. |
| 7 | Move Database | ON-PILLAR | P2 (primary), P1 (secondary) | MoveConfig covers: GenomeType, DamageForm (Physical/Energy/Bio), Power, Accuracy, Range, TargetType, Priority, Effects, PP. Three damage forms interact with body part form access — ties moves to P1 engineering choices. Range-based targeting supports P2 positioning. |
| 8 | Turn Manager | ON-PILLAR | P2 (primary) | Proximity-based initiative (closer creatures act first) rewards aggressive positioning. Split turns (move + act) give positioning its own turn economy. Movement uses A* pathfinding with SPD-based range. Height, terrain synergy, cover all flow through to damage. This is P2's beating heart. |
| 9 | Damage & Health System | ON-PILLAR | P2 (primary), P1 (secondary) | DamageCalculator stacks: level coefficient, stat ratio, STAB, type effectiveness, terrain synergy (attacker/defender), height bonus per damage form, cover reduction. Damage forms (Physical/Energy/Bio) each interact with grid differently — Bio ignores height, Energy takes cover penalty, Physical blocked at targeting. Strongest P2 implementation: the grid directly multiplies damage output. |
| 10 | Creature Instance | ON-PILLAR | P1 (primary), P2 (secondary) | Tracks: equipped body parts, applied DNA mods, instability, available damage forms (derived from parts), personality, affinity bonds, battle scars, grid position, facing, status effects. DNA mod system with instability→Blight secondary type override is P1's core data model. Body part equipping with form access gating bridges P1 engineering and P2 combat capability. |

#### Feature Layer

| # | System | Status | Pillar(s) Served | Assessment |
|---|--------|--------|-------------------|------------|
| 12 | Capture System | ON-PILLAR | P3 (primary), P1 (secondary) | CaptureCalculator uses: species catch rate, HP factor, status bonus, trap modifier. Catch Predictor shows probability without revealing formula internals — aligns with P3 "earned through observation" principle. Captured creatures become P1 engineering material. |
| 14 | Party System | ON-PILLAR | P1 (supporting), P2 (supporting) | Active party + storage, swap, deposit/withdraw, promote next conscious. Party composition is a P1 expression choice and P2 tactical choice. PartyWiped event cleanly feeds loss condition. |
| 16 | AI Decision System | ON-PILLAR | P2 (primary), P4 (secondary) | Scoring-based with personality weights (AIPersonalityConfig), random jitter, range-based candidate enumeration, form access checking, tiebreaker logic. AI respects grid positioning (range checks, position-aware scoring). Personality profiles serve P4 (trainers with distinct behavior). No adaptive counter-strategy yet — correctly deferred to P4 full vision. |
| 17 | Encounter System | ON-PILLAR | P2 (primary), P4 (supporting) | EncounterManager builds grid from config, places creatures at authored positions, validates config integrity. EncounterType enum (Wild, Trainer, Nest, Trophy, Horde) supports P4 world variety. Grid construction with authored height maps and terrain layouts directly enables P2 map design. |

#### Presentation Layer

| # | System | Status | Pillar(s) Served | Assessment |
|---|--------|--------|-------------------|------------|
| 26 | Combat UI | ON-PILLAR | P2 (primary), P3 (supporting) | CombatHUD, CreatureInfoPanel, MoveSelectionPanel, SwitchOverlay, TileHighlight, TurnOrderBar, TypeEffectivenessCallout. Tile highlighting and turn order bar serve P2 tactical readability. Type effectiveness callout supports P3 progressive learning (seeing effectiveness in context). |

### Pillar Health

| Pillar | Status | Assessment |
|--------|--------|------------|
| P1: Genetic Architect | HEALTHY (foundation only) | Data model has all hooks (DNA mods, body parts, instability, damage form access, personality, scars, affinity). Blight secondary type override from instability is a clever P1-P2 bridge. **But no expression-layer system is implemented yet.** DNA Alteration (#13), Body Part System (#34), Move Customization (#51) all Draft. Identity is invisible to players. |
| P2: Tactical Grid Mastery | STRONG | 8/14 systems serve P2. Height, terrain synergy, cover, damage forms with different spatial rules, proximity initiative, A* pathfinding, range targeting, form access gating. Grid meaningfully affects every combat calculation. Best-represented pillar. |
| P3: Discovery Through Play | ADEQUATE | Catch predictor, type callout present. Pokedex (primary P3 system) not yet built. **Technical design debt:** current implementation reveals all creature data immediately (CreatureConfig fully accessible). No progressive disclosure gating exists in code. Pokedex must retroactively gate data that's currently exposed. |
| P4: Living World | MINIMAL | Correctly deferred per pillar hierarchy. Encounter types hint at world variety. Living Ecosystem, Rival Trainer, Campaign Map, Weather all Draft. |

### Tone & Identity Consistency

Consistent across all implemented systems. Every system references its GDD section. Naming is coherent:
- GenomeType (not "Element")
- DamageForm (not "DamageType")
- Gene Traps (not "Pokeballs")
- "Field researcher + genetic engineer" vocabulary embedded in code comments and class names

**Flag**: "Capture" language in code is neutral (CaptureSystem, CaptureCalculator). GDD calls them "Gene Traps" — UI presentation layer should use "Gene Trap" vocabulary, not generic "capture."

### Contradiction Check

**No implemented system contradicts any pillar.** Specific checks:

- `CreatureInstance.AwardXP()` auto-levels creatures — could drift toward "grind treadmill" anti-pillar. But DNA engineering is the authored growth path; leveling provides stat scaling, DNA provides identity. **No contradiction as long as DNA mods remain more impactful than raw levels.**
- Full heal on level-up could reduce combat stakes (P2). Minor — revisit if playtesting shows trivial encounters become free heal opportunities.
- No auto-resolve or auto-targeting exists — P2 protected.
- No bestiary or full-data reveal — P3 protected (not yet enforced via Pokedex gating).

### Priority Order Assessment

Current implementation queue:
```
1-6:   Foundation + Data (Config, TypeChart, State, Grid, Creatures, Moves)
7-9:   Core Combat (Instance, TurnManager, Damage)
10:    Leveling/XP  ← next unimplemented
11-14: AI, Capture, Party, Encounters
15:    DNA Alteration  ← P1 identity system
16:    Pokedex         ← P3 identity system
17:    Campaign Map    ← P4 entry point
18-22: UI, Save/Load, Settings, Shell
```

**Tension with pillar hierarchy:**
- P2 (rank 2) got 8 systems before P1 (rank 1) got any expression-layer systems — defensible because combat loop is the stage P1 performs on
- **But Leveling/XP (queue position 10) is ahead of DNA Alteration (position 15).** Leveling is automatic progression — closer to "NOT a grind treadmill" anti-pillar than any positive pillar. DNA Alteration IS Pillar 1.
- First playable prototype should demonstrate all pillars at minimum depth
- Body Part System (#34) should follow immediately after DNA Alteration — it's the tangible expression that prevents the "numbers-only RPG" trap

### Strategic Recommendation

**Promote DNA Alteration System ahead of Leveling/XP in implementation queue.**
- Leveling/XP serves no pillar directly; basic XP already works via CreatureInstance.AwardXP()
- DNA Alteration IS Pillar 1 — without it, the game's identity claim is untestable
- Body Part System should follow immediately after DNA Alteration

### Checklist Results

- [x] Each implemented system serves at least one game pillar
- [x] No implemented system contradicts a pillar
- [~] Feature priorities mostly align with pillar hierarchy — **DNA Alteration (P1 identity) should be promoted ahead of Leveling/XP**
- [x] Player fantasy described in GDDs is achievable with current implementation �� data model supports all four pillars
- [x] Tone and identity are consistent across all implemented systems

### Verdict

> "A tactics game with type charts is competent. A tactics game where your creatures are personally engineered with visible trade-offs is Gene Forge."

---

## Phase 2 Cross-Cutting Issues

| Issue | Raised By | Phase 1 Also? |
|-------|-----------|---------------|
| StatusEffectProcessor ignores CombatSettings | game-designer, systems-designer | Yes (all 3 Phase 1 agents) |
| Confusion self-hit bypasses IDamageCalculator | game-designer, systems-designer | No (new) |
| DamageCalculator hardcoded constants | game-designer | Yes (all 3 Phase 1 agents) |
| GetFormStatPairing duplication | systems-designer | Yes (lead-programmer) |
| Height-3 cliff tiles passable | game-designer | No (new) |
| Movement formula triplicated | systems-designer | No (new) |
| Instability thresholds hardcoded in UI | game-designer | Yes (lead-programmer, tech-director) |
