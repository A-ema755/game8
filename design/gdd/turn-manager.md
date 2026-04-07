# Turn Manager

**System**: Turn Manager (#8)
**Version**: 2.0
**Status**: Ready for Approval
**Namespace**: `GeneForge.Combat`

---

## 1. Overview

The Turn Manager is the phase-based combat sequencer for Gene Forge's isometric grid battles. It is a pure C# class (no MonoBehaviour, per ADR-002) that drives a round loop through five ordered phases: RoundStart, PlayerCreatureSelect, PlayerAction, EnemyAction, and RoundEnd. Each creature's turn is a **split turn**: an optional Movement step (reposition up to movement range) followed by one Action step (UseMove, Capture, Flee, or Wait). Within each action phase, creature execution order is determined by a proximity-based initiative formula — creatures closer to their nearest enemy act first, with SPD as a tiebreaker — giving the tactical grid direct influence over turn sequencing. Moves with a non-zero `Priority` field on `MoveConfig` override proximity ordering within the same phase, creating a two-tier sort. The system owns no game logic itself; it delegates damage resolution to `DamageCalculator`, status ticking to `StatusEffectProcessor`, capture resolution to `CaptureSystem`, and AI decisions to `IAIDecisionSystem`. It publishes C# events (`RoundStarted`, `RoundEnded`, `CreatureActed`, `CreatureFainted`, `CreatureCaptured`) at every meaningful state transition, allowing combat UI, audio, VFX, and analytics to react without coupling. A `BattleStats` object accumulates aggregate combat metrics for the session. Seeded RNG ensures deterministic behavior in tests and replays.

---

## 2. Player Fantasy

Combat feels like a chess clock the player can read and influence. Every round, the player surveys the board, commits actions for the full party, then watches execution unfold in a sequence they predicted — or cleverly manipulated. When a creature with high SPD closes on an enemy, the player knows it will act first in that positioning cluster. When they queue a Priority +1 move, they feel the spike of certainty: *this fires before anything else*. The split-turn structure — move then act — creates fluid, dynamic combat where creatures reposition and strike in one smooth sequence, rewarding aggressive positioning without forcing a choice between mobility and offense. The phase structure creates a satisfying rhythm: deliberate planning (PlayerCreatureSelect), exciting resolution (PlayerAction and EnemyAction), brief exhale (RoundEnd). There is no ambiguity about whose turn it is or why a particular creature acted when it did.

**Primary pillars served**:
- **Tactical Grid Mastery** — positioning rewards are legible; move+act means every creature can engage the grid meaningfully each turn
- **Genetic Architect** — build choices that prioritize SPD or Priority moves manifest in turn-order advantages
- **Discovery Through Play** — players discover initiative rules through combat feedback, not menus

---

## 3. Detailed Rules

### 3.1 Phase Sequence

```
RoundStart
    │  Apply start-of-round status effects (Burn/Poison DoT, Paralysis check,
    │  Sleep check, Freeze check).
    │  Reset per-round BattleStats counters.
    │  Fire RoundStarted event.
    │  Check end condition after each DoT faint.
    ▼
PlayerCreatureSelect
    │  Player submits one TurnAction per non-fainted party creature.
    │  Each TurnAction = optional MovementTarget + one ActionType.
    │  No time limit (MVP). Actions are immutable once submitted.
    ▼
PlayerAction
    │  Sort active player creatures by initiative (see §3.6).
    │  For each creature in sorted order:
    │    1. Execute Movement step (if MovementTarget set)
    │    2. Execute Action step (UseMove/Capture/Wait)
    │  Fire CreatureActed per execution.
    │  Check end condition after each faint. Abort phase if combat ends.
    ▼
EnemyAction
    │  Sort active enemy creatures by initiative (opponents = player party).
    │  AI resolves TurnAction per creature. Execute in sorted order.
    │  Fire CreatureActed per execution.
    │  Check end condition after each faint. Abort phase if combat ends.
    ▼
RoundEnd
    │  Apply end-of-round status effects (decrement durations).
    │  Clear queued actions.
    │  Fire RoundEnded event.
    │  Increment round counter.
    └─► loop → RoundStart (or EndCombat if win/loss condition met)
```

The loop runs until `CheckEndCondition` returns true. End conditions are checked:
- After every faint triggered during PlayerAction or EnemyAction phases.
- After every faint triggered during RoundStart (DoT kills).
- After RoundEnd (handles Draw condition when max rounds reached, post-MVP).

### 3.2 Enums

```csharp
public enum CombatPhase
{
    RoundStart           = 0,
    PlayerCreatureSelect = 1,
    PlayerAction         = 2,
    EnemyAction          = 3,
    RoundEnd             = 4
}

public enum CombatResult
{
    Ongoing = 0,
    Victory = 1,   // All enemy creatures fainted
    Defeat  = 2,   // All player creatures fainted
    Fled    = 3,   // Player executed successful Flee action
    Draw    = 4    // Max rounds exceeded (post-MVP)
}

public enum ActionType
{
    UseMove  = 0,   // Use a learned move
    Capture  = 1,   // Throw a Gene Trap at a target creature
    Item     = 2,   // Use a held item (post-MVP; silently ignored in MVP)
    Flee     = 3,   // Attempt to exit combat (consumes entire turn — no movement)
    Wait     = 4    // Pass — no action taken
}
```

Note: `ActionType.Move` removed. Movement is always available as part of the split turn via `TurnAction.MovementTarget`.

### 3.3 TurnAction Struct (Split Turn)

```csharp
namespace GeneForge.Combat
{
    /// <summary>
    /// Describes one creature's full turn for the current round.
    /// Split turn: optional movement + one action.
    /// Immutable — set during PlayerCreatureSelect, consumed during action phases.
    /// </summary>
    public readonly struct TurnAction
    {
        // ── Movement Step (optional) ──
        /// <summary>Target tile for reposition. Null = don't move.</summary>
        public readonly Vector2Int? MovementTarget;

        // ── Action Step ──
        /// <summary>Action type for this turn.</summary>
        public readonly ActionType Action;

        /// <summary>Move config for UseMove actions. Null otherwise.</summary>
        public readonly MoveConfig Move;

        /// <summary>Target creature for UseMove (single target) or Capture. Null otherwise.</summary>
        public readonly CreatureInstance Target;

        /// <summary>Target tile for AoE/Line moves. Null otherwise.</summary>
        public readonly TileData TargetTile;

        /// <summary>Index 0–3 into Actor.LearnedMoveIds for PP tracking. -1 if N/A.</summary>
        public readonly int MovePPSlot;

        /// <summary>True if this action was suppressed by a status effect.</summary>
        public readonly bool Suppressed;

        public TurnAction(
            ActionType action,
            Vector2Int? movementTarget = null,
            MoveConfig move            = null,
            CreatureInstance target     = null,
            TileData targetTile        = null,
            int movePPSlot             = -1,
            bool suppressed            = false)
        {
            Action         = action;
            MovementTarget = movementTarget;
            Move           = move;
            Target         = target;
            TargetTile     = targetTile;
            MovePPSlot     = movePPSlot;
            Suppressed     = suppressed;
        }
    }
}
```

**Field invariants:**
- For `ActionType.UseMove`: `Move` is non-null, `MovePPSlot` is 0–3. `Target` is non-null for `TargetType.Single`; `TargetTile` is non-null for `TargetType.AoE`, `TargetType.Line`.
- For `ActionType.Capture`: `Target` is non-null.
- For `ActionType.Flee`: `MovementTarget` must be null (flee consumes entire turn).
- For `ActionType.Wait`: all optional fields null or -1.
- `MovementTarget` can be set alongside any ActionType except Flee.

### 3.4 Split Turn Execution

Each creature's turn resolves in two steps during their initiative slot:

**Step 1 — Movement (optional)**
1. If `MovementTarget` is null, skip.
2. If creature is suppressed (Sleep, Freeze, Paralysis proc), skip both movement and action.
3. Call `Pathfinder.FindPath(_grid, startTile, targetTile, actor)`.
4. If path is null (invalid target, occupied, out of movement range), skip movement. Action still executes.
5. If path valid, move creature along path. Update `GridSystem` tile occupancy. Update `Actor.SetGridPosition()` and `Actor.SetFacing()`.

**Step 2 — Action**
1. If creature is suppressed, skip. Fire `CreatureActed` with `Suppressed = true`.
2. Execute based on `ActionType` (UseMove, Capture, Wait, Flee).

Movement resolves before action so creatures can reposition into range before attacking. Initiative is calculated based on positions at the **start** of the phase (before any movement), not recalculated mid-phase.

### 3.5 Movement Range

Movement range per creature = `CreatureInstance.ComputedStats.SPD / MovementDivisor` (integer division), minimum 1 tile.

| Variable | Definition | Default |
|---|---|---|
| `ComputedStats.SPD` | Creature's computed speed stat | 10–150 |
| `MovementDivisor` | GameSettings tuning knob | 20 |
| Result | Max tiles per turn | Typically 1–7 |

Example: SPD 60 creature, divisor 20 → `60 / 20 = 3` tiles per turn.

Path cost uses Chebyshev distance (diagonal = 1 tile cost, matching 8-directional grid movement).

### 3.6 Initiative and Action Ordering

Initiative ordering governs which creature acts first within each action phase. Recalculated fresh each round based on positions at phase start.

**Step 1 — Priority bracket sort (descending)**
Actions grouped by the Priority field of the queued move. Higher priority acts first. Non-UseMove actions have implicit priority 0.

**Step 2 — Proximity sort within each priority bracket (ascending)**
```
initiativeScore = (minChebyshevDistToNearestLiveEnemy × 1000) − ComputedStats.SPD
```
Lower score = acts earlier (closer, faster creatures act first).

The `× 1000` weight ensures a creature 1 tile closer always acts before a farther creature regardless of SPD. Max SPD ~150 at level 50; 1-tile difference = 1000 score gap.

**Step 3 — Random tiebreak (seeded, stable)**
If two creatures share identical priority bracket and initiative score, a pre-computed tiebreak value (single `_rng.Next()` per creature, computed before the sort) determines order. Stable within a round.

```csharp
private List<CreatureInstance> GetInitiativeOrder(
    List<CreatureInstance> actors,
    List<CreatureInstance> opponents)
{
    var active = actors.Where(c => !c.IsFainted).ToList();

    // Pre-compute tiebreak values once per creature per round.
    var tiebreaks = new Dictionary<CreatureInstance, int>();
    foreach (var c in active)
        tiebreaks[c] = _rng.Next();

    active.Sort((a, b) =>
    {
        // Step 1: Priority bracket (descending)
        int pa = _queuedActions.TryGetValue(a, out var qa) && qa.Move != null ? qa.Move.Priority : 0;
        int pb = _queuedActions.TryGetValue(b, out var qb) && qb.Move != null ? qb.Move.Priority : 0;
        if (pa != pb) return pb.CompareTo(pa);

        // Step 2: Initiative score (ascending)
        int ia = CalculateInitiative(a, opponents);
        int ib = CalculateInitiative(b, opponents);
        if (ia != ib) return ia.CompareTo(ib);

        // Step 3: Pre-computed random tiebreak
        return tiebreaks[a].CompareTo(tiebreaks[b]);
    });

    return active;
}
```

### 3.7 Status Effect Tick Timing

**RoundStart window** (`ApplyStartOfRoundEffects`):
- **Burn**: deal `max(1, floor(maxHP / 16))` damage.
- **Poison**: deal `max(1, floor(maxHP / 8))` damage.
- **Paralysis**: 25% chance creature is suppressed this round (both movement and action). Roll stored as flag.
- **Sleep**: creature is suppressed unconditionally. Decrement duration. If duration reaches 0, remove Sleep.
- **Freeze**: creature is suppressed. Roll 20% chance to thaw (post-MVP). MVP: decrement fixed duration. If 0, remove Freeze.
- **Confusion**: roll happens at action execution time, not RoundStart (see §3.8).

**RoundEnd window** (`ApplyEndOfRoundEffects`):
- Decrement duration counters for all time-limited effects not already removed during RoundStart.
- Effects with duration 0 after decrement are removed.
- Burn and Poison have no duration — persist until cured or faint.

**Processing order**: Player creatures processed before enemy creatures. Within each group, initiative order. This order matters for DoT faint edge cases.

**Status duration tracking**: Current `CreatureInstance` stores `List<StatusEffect>` (presence only). This system requires per-status duration. Implementation must extend to `Dictionary<StatusEffect, int>` or a `StatusEffectEntry` class with effect + remaining rounds. MVP simplified durations: Sleep = 3 rounds fixed, Freeze = 2 rounds fixed, Confusion = 3 rounds fixed.

### 3.8 Action Execution Details

**UseMove** execution sequence:
1. Check if Actor is suppressed (Paralysis/Sleep/Freeze). If suppressed, skip. No PP consumed.
2. If Confused: roll 33% self-hit. If self-hit, apply confusion damage (see §4.5) to Actor. If Actor faints, call `HandleFaint`. Skip normal move execution. No PP consumed.
3. Deduct 1 PP from `Actor.LearnedMovePP[MovePPSlot]`. PP cannot go below 0.
4. Hit check: `MoveHitCheck(Move, Actor, Target)`. If miss, fire `CreatureActed` with miss flag. Return.
5. If `Move.IsDamaging`, call `DamageCalculator.Calculate(Move, Actor, Target, _grid)`. Call `Target.TakeDamage(result)`.
6. If Target fainted, call `HandleFaint(Target)`. Check end condition. If combat ends, return.
7. Apply `Recoil` effects from `Move.Effects`. If Actor faints from recoil, call `HandleFaint(Actor)`. Check end condition.
8. Apply `Drain` effects. Heal Actor by `floor(damage × drainFraction)`.
9. Apply remaining `Move.Effects` via `MoveEffectApplier.Apply`.
10. Fire `CreatureActed` event.
11. `await Task.Yield()` — single-frame yield for animation/UI.

**Capture** execution sequence:
1. Call `CaptureSystem.Attempt(Target, Actor)`.
2. Fire `CreatureCaptured(Target, success)`.
3. If success: remove Target from `_enemyParty`. Clear tile occupancy. Check end condition.
4. Fire `CreatureActed`.

**Flee** execution sequence:
1. If `EncounterType.Trainer`: no-op. Log "Can't flee trainer battle." Fire `CreatureActed`. Return.
2. If `EncounterType.Wild`: set `CombatActive = false`, `_stats.Result = CombatResult.Fled`. Fire `CreatureActed`. Combat loop exits after current action.

**Wait** execution sequence:
1. No state changes. Fire `CreatureActed`.

### 3.9 Hit Check Formula

```csharp
private bool MoveHitCheck(MoveConfig move, CreatureInstance attacker, CreatureInstance target)
{
    if (move.AlwaysHits) return true;

    float hitChance = (move.Accuracy / 100f)
                    * attacker.AccuracyStageMultiplier
                    * (1f / target.EvasionStageMultiplier);

    hitChance = Mathf.Clamp01(hitChance);
    return (float)_rng.NextDouble() < hitChance;
}
```

`AccuracyStageMultiplier` and `EvasionStageMultiplier` are not yet on `CreatureInstance`. Default both to 1.0 until implemented. Stage multiplier table:

| Stage | Multiplier |
|-------|-----------|
| -3 | 0.50 |
| -2 | 0.67 |
| -1 | 0.75 |
| 0 | 1.00 |
| +1 | 1.33 |
| +2 | 1.50 |
| +3 | 2.00 |

### 3.10 Faint Handling

```csharp
private void HandleFaint(CreatureInstance creature)
{
    CreatureFainted?.Invoke(creature);

    var tile = _grid.GetTile(creature.GridPosition);
    if (tile != null) tile.Occupant = null;

    _stats.FaintsThisRound++;

    if (CheckEndCondition(out var result))
    {
        CombatActive = false;
        _stats.Result = result;
    }
}
```

A faint during PlayerAction prevents EnemyAction only if *all* enemy creatures are now fainted. Partial faint does not interrupt the phase.

### 3.11 CheckEndCondition

```csharp
private bool CheckEndCondition(out CombatResult result)
{
    bool allEnemyFainted  = _enemyParty.All(c => c.IsFainted);
    bool allPlayerFainted = _playerParty.All(c => c.IsFainted);

    // Victory takes priority over mutual faint (recoil scenario).
    if (allEnemyFainted)  { result = CombatResult.Victory; return true; }
    if (allPlayerFainted) { result = CombatResult.Defeat;  return true; }

    result = CombatResult.Ongoing;
    return false;
}
```

### 3.12 BattleStats

```csharp
public class BattleStats
{
    public int TotalDamageDealt;
    public int TotalDamageTaken;
    public int RoundsElapsed;
    public int FaintsThisRound;
    public int ActionsThisRound;
    public int CapturesThisRound;
    public int TotalCaptures;
    public CombatResult Result = CombatResult.Ongoing;
}
```

`FaintsThisRound` and `ActionsThisRound` reset at `ApplyStartOfRoundEffects`. `RoundsElapsed` incremented at RoundEnd. `TotalCaptures` accumulates across rounds.

### 3.13 Constructor

```csharp
public TurnManager(
    GridSystem grid,
    List<CreatureInstance> playerParty,
    List<CreatureInstance> enemyParty,
    EncounterType encounterType,
    int seed = 0)
```

`EncounterType` is required for Flee behavior and Capture validation. Seed of 0 = use system random; non-zero = deterministic.

---

## 4. Formulas

### 4.1 Initiative Score

```
initiativeScore(c) = (minChebyshevDist(c, opponents) × 1000) − c.ComputedStats.SPD
```

| Variable | Definition | Range |
|---|---|---|
| `minChebyshevDist` | Min Chebyshev distance to nearest live opponent | 1 to grid diagonal |
| `× 1000` | Distance weight constant | Fixed |
| `ComputedStats.SPD` | Computed speed stat | ~10–150 |
| Result | Lower = acts earlier | Negative to ~grid diagonal × 1000 |

**Example A** — Distance 2, SPD 40 vs Distance 3, SPD 80:
- A: `(2 × 1000) − 40 = 1960`
- B: `(3 × 1000) − 80 = 2920`
- A acts first.

**Example B** — Same distance 2, same SPD 60:
- Both: `(2 × 1000) − 60 = 1940`
- Random tiebreak.

**Example C** — Priority override: creature A queues Priority +1 at distance 5 (score 4940); creature B queues Priority 0 at distance 1 (score 940). A acts first (higher priority bracket).

### 4.2 Sort Key

```
sortKey(c) = (−move.Priority, initiativeScore(c), tiebreak[c])
```

Sort ascending. Negative priority = higher priority first.

### 4.3 Hit Chance

```
hitChance = clamp01( (move.Accuracy / 100) × accuracyMult × (1 / evasionMult) )
```

**Example**: Accuracy 80, attacker +1 stage (1.33×), defender 0:
`0.8 × 1.33 × 1.0 = 1.064` → clamped to 1.0 (guaranteed hit)

### 4.4 Status DoT

```
burnDamage   = max(1, floor(creature.MaxHP / 16))
poisonDamage = max(1, floor(creature.MaxHP / 8))
```

**Example**: 120 MaxHP → Burn: `floor(120/16) = 7`, Poison: `floor(120/8) = 15`
**Example**: 10 MaxHP → Burn: `floor(10/16) = 0` → enforced min `1`, Poison: `floor(10/8) = 1`

### 4.5 Confusion Self-Hit

```
confusionSelfHitChance = 0.33
confusionDamage = DamageCalculator.Calculate(
    move: synthetic(Power=40, Form=Physical, GenomeType=None, AlwaysHits=true),
    attacker: creature,
    defender: creature,
    grid: _grid
)
```

No STAB (GenomeType.None). No type effectiveness. Minimum 1 damage.

### 4.6 Movement Range

```
movementRange = max(1, floor(ComputedStats.SPD / MovementDivisor))
```

| Variable | Default | Safe Range |
|---|---|---|
| `MovementDivisor` | 20 | 10–40 |

**Example**: SPD 60, divisor 20 → 3 tiles. SPD 15, divisor 20 → `floor(0.75) = 0` → enforced min 1.

### 4.7 Status Durations (MVP — Fixed)

| Status | Duration | Notes |
|---|---|---|
| Sleep | 3 rounds | Suppresses movement + action |
| Freeze | 2 rounds | Suppresses movement + action |
| Confusion | 3 rounds | 33% self-hit roll per action |
| Paralysis | No limit | 25% suppression roll per round |
| Burn | No limit | Persists until cured |
| Poison | No limit | Persists until cured |
| Taunt | 3 rounds | Fixed |
| Stealth | Until attack or hit | Event-driven removal |

---

## 5. Edge Cases

| Situation | Explicit Behavior |
|---|---|
| All player creatures faint during RoundStart from DoT | `HandleFaint` per faint. After last player creature faints, `CheckEndCondition` returns `Defeat`. `CombatActive = false`. Remaining RoundStart processing for enemies skipped. Round loop exits. |
| All enemies faint during RoundStart from DoT | `Victory` returned. PlayerCreatureSelect never starts. |
| All enemies faint during PlayerAction | `CombatActive = false` with `Victory`. Remaining player creatures in initiative order skipped. EnemyAction does not run. |
| All player creatures faint during EnemyAction | `Defeat`. Remaining enemies skipped. RoundEnd does not run. |
| Simultaneous mutual faint (recoil kills attacker + target) | Target faint check runs first (step 6 of §3.8). If that triggers Victory, recoil application still runs but `CombatActive` already false. Result stays `Victory`. Player wins on mutual faint. |
| Creature's target faints before their initiative slot | `if (creature.IsFainted) continue` skips fainted actors. If *target* fainted, UseMove still executes — PP consumed, `TakeDamage(0)` on fainted target is no-op. `CreatureActed` fires. |
| PP already 0 when UseMove queued | PP deducted (clamped to 0). Move executes normally. MVP has no Struggle fallback. Post-MVP: auto-substitute Struggle when all moves at 0 PP. |
| Creature gains Sleep mid-round from move effect | Sleep takes effect next RoundStart. Creature may still act this round if it hasn't been processed yet. |
| Paralysis suppresses creature | Suppression flag set at RoundStart. Both movement and action skipped. `CreatureActed` fires with `Suppressed = true`. No PP consumed. |
| Two creatures identical initiative + priority | Pre-computed tiebreak value determines order. Stable within round, may vary between rounds. |
| Flee in trainer battle | No-op. Fire `CreatureActed`. Combat continues. |
| Capture on trainer creature | `CaptureSystem.Attempt` returns false. `CreatureCaptured(target, false)` fired. No removal. |
| Flee with MovementTarget set | `MovementTarget` must be null for Flee. If somehow set, ignored — Flee consumes entire turn. |
| Stealth creature targeted | Most moves auto-miss (high evasion). AlwaysHits and AoE bypass Stealth. Stealth removed when stealthed creature attacks or is hit. |
| Confusion self-hit faints creature | `HandleFaint` called. Original action does not execute. PP not consumed. Check end condition. |
| Movement target occupied (ally moved there earlier this phase) | `Pathfinder.FindPath` returns null. Movement skipped. Action still executes from original position. |
| Movement target out of movement range | Path exceeds `movementRange` tiles. Movement skipped. Action still executes. |
| `_enemyParty` empty at construction | `All(c => c.IsFainted)` returns true for empty set. `CheckEndCondition` returns `Victory` immediately at first check. Misconfiguration — constructor should validate party sizes > 0 and log error. |
| `playerInputProvider` never resolves | Awaits indefinitely (MVP). Post-MVP: configurable timeout with auto-Wait. |
| Creature movement triggers no recalculation of other creatures' initiative | Initiative calculated once at phase start using starting positions. Movement during the phase does not change other creatures' turn order. |
| AI enemy uses Flee | `IAIDecisionSystem` should never return Flee for EncounterType.Trainer. For wild encounters, AI flee is valid — same behavior as player flee but from enemy side (combat ends, player does not get rewards). |

---

## 6. Dependencies

### 6.1 TurnManager Depends On (Inbound)

| System | What TurnManager Uses |
|---|---|
| `GridSystem` | `GetTile(Vector2Int)`, `ChebyshevDistance()`, tile occupancy. Constructor dependency. |
| `CreatureInstance` | Reads: `ComputedStats.SPD`, `IsFainted`, `GridPosition`, `ActiveStatusEffects`, `LearnedMovePP`. Mutates: `TakeDamage`, `Heal`, `DeductPP`, `ApplyStatusEffect`, `RemoveStatusEffect`, `SetGridPosition`, `SetFacing`, `SetMoved`, `SetActed`. |
| `MoveConfig` | Reads: `Priority`, `IsDamaging`, `AlwaysHits`, `Accuracy`, `Effects`. |
| `GameSettings` | Reads: `MovementDivisor`, `MaxRoundsPerCombat` (post-MVP). Via `ConfigLoader.Settings`. |
| `EncounterType` | Constructor param. Determines Flee/Capture behavior. |

### 6.2 TurnManager Delegates To (Outbound)

| System | Contract |
|---|---|
| `DamageCalculator` | `Calculate(move, attacker, target, grid)` → `int` damage. |
| `CaptureSystem` | `Attempt(target, actor)` → `bool` success. |
| `IAIDecisionSystem` | `DecideAction(creature, playerParty, grid)` → `TurnAction`. |
| `MoveEffectApplier` | `Apply(effect, actor, target, grid)` → void. |
| `StatusEffectProcessor` | `ApplyStartOfRound(creature)`, `ApplyEndOfRound(creature)`, `DecrementDurations(creature)`. |
| `Pathfinder` | `FindPath(grid, startTile, endTile, actor)` → `List<TileData>` or null. |

### 6.3 Systems That Subscribe to TurnManager Events

| System | Events Consumed | Purpose |
|---|---|---|
| Combat UI | `RoundStarted`, `RoundEnded`, `CreatureActed`, `CreatureFainted`, `CreatureCaptured` | Display updates, animations, HP bars |
| Audio Manager | `CreatureActed`, `CreatureFainted`, `CreatureCaptured` | SFX triggers |
| XP/Reward System | `RoundEnded`, `CreatureFainted` | Post-combat XP accumulation |
| Analytics | All events | Session telemetry |
| `GameStateManager` | `CombatResult` via `BattleStats` | Combat → Map state transition |

### 6.4 TurnManager Provides

- `BattleStats` instance queryable after combat.
- `CurrentRound` and `CurrentPhase` as public readable properties.
- `CombatActive` as public flag.
- Five C# events (§6.3).

### 6.5 TurnManager Requires

- `playerInputProvider` delegate → `Dictionary<CreatureInstance, TurnAction>` with one entry per non-fainted party creature.
- `IAIDecisionSystem` that returns valid (non-null) `TurnAction` for any creature.

---

## 7. Tuning Knobs

| Parameter | Location | Default | Safe Range | Affects |
|---|---|---|---|---|
| Initiative distance weight | `TurnManager.CalculateInitiative` | 1000 | 500–5000 | Distance tier separation. Below 500: SPD can override distance. Above 5000: SPD contribution negligible. |
| Movement divisor | `GameSettings` SO | 20 | 10–40 | Tiles per turn. At 10: high mobility (SPD 60 = 6 tiles). At 40: low mobility (SPD 60 = 1 tile). |
| Max rounds per combat | `GameSettings` SO | No limit (MVP) | 30–100 | Stalemate prevention. Requires `CombatResult.Draw` implementation. |
| Flee success rate (wild) | `GameSettings` SO | 100% (MVP) | 50–100% | Post-MVP: formula `clamp01(playerAvgSPD / enemyAvgSPD × 0.8)`. |
| Burn DoT fraction | `GameSettings` SO | 1/16 maxHP | 1/32–1/8 | Burn pressure. At 1/8, Burn = Poison. |
| Poison DoT fraction | `GameSettings` SO | 1/8 maxHP | 1/16–1/4 | Poison pressure. Above 1/6 very punishing. |
| Sleep duration | `GameSettings` SO | 3 rounds | 2–5 | Suppression rounds. |
| Freeze duration | `GameSettings` SO | 2 rounds | 1–4 | Suppression rounds. |
| Paralysis suppression chance | `GameSettings` SO | 25% | 15–40% | Frequency of lost turns. |
| Confusion self-hit chance | `GameSettings` SO | 33% | 20–50% | Risk per round while confused. |
| Confusion self-hit power | `GameSettings` SO | 40 | 30–50 | Self-damage severity. Above 60 rivals DoT effects. |

All tuning values belong in `GameSettings.asset` or a dedicated `CombatSettings.asset` ScriptableObject per ADR-001.

---

## 8. Acceptance Criteria

### Functional (EditMode/PlayMode tests)

**Phase sequencing:**
- [ ] Phases execute in order: RoundStart → PlayerCreatureSelect → PlayerAction → EnemyAction → RoundEnd
- [ ] `RoundStarted` fires once per round with correct round number (1, 2, 3...)
- [ ] `RoundEnded` fires once per round after RoundEnd effects
- [ ] `CreatureActed` fires once per executed turn (including Wait and suppressed turns)
- [ ] `CreatureFainted` fires exactly when HP reaches 0, before next creature's turn
- [ ] `CreatureCaptured` fires with correct success boolean

**Split turn (move + act):**
- [ ] Creature with MovementTarget set repositions before executing action
- [ ] Creature with MovementTarget = null executes action from current position
- [ ] Movement failure (invalid path) does not prevent action execution
- [ ] Movement range = `max(1, floor(SPD / MovementDivisor))`
- [ ] Movement to occupied tile fails; action still executes from original position
- [ ] Flee action ignores MovementTarget — no movement occurs
- [ ] Initiative calculated from positions at phase start, not after mid-phase movement

**Initiative ordering:**
- [ ] Creature at distance 2 acts before creature at distance 3 regardless of SPD
- [ ] At identical distance, higher SPD creature acts first
- [ ] Identical distance + SPD with seeded RNG produces same order on repeated runs
- [ ] Priority +1 move executes before Priority 0 regardless of distance
- [ ] Priority −1 executes after all Priority 0 moves
- [ ] Same priority bracket: initiative score determines order

**Status effects:**
- [ ] Burn applies `max(1, floor(maxHP/16))` at RoundStart
- [ ] Poison applies `max(1, floor(maxHP/8))` at RoundStart
- [ ] Sleep suppresses both movement and action. No PP consumed.
- [ ] Paralysis 25% proc suppresses both movement and action. No PP consumed.
- [ ] Freeze suppresses both movement and action.
- [ ] Confusion 33% self-hit: takes damage, original action skipped, no PP consumed
- [ ] Status DoT applied before PlayerAction begins

**Combat termination:**
- [ ] Victory when all enemies faint during PlayerAction (EnemyAction skipped)
- [ ] Defeat when all player creatures faint
- [ ] Mutual faint (recoil) returns Victory
- [ ] Flee in wild encounter sets `CombatResult.Fled`, exits loop
- [ ] Flee in trainer battle fails, combat continues
- [ ] Capture on trainer creature returns false

**Determinism:**
- [ ] TurnManager with seed `12345` and fixed scenario produces identical action sequence on repeated runs

### Experiential (playtest validation)

- [ ] Players predict initiative order correctly 80% of time after 3 encounters
- [ ] Move+act in same turn feels fluid — creatures don't feel stuck choosing between mobility and offense
- [ ] Priority moves feel like reliable "go first" tool
- [ ] Rounds complete in under 3 minutes at mid-game 4v4
- [ ] Status effects feel threatening but not arbitrary — Burn/Paralysis linked to player positioning mistakes

---

## Changes from v1.0

1. **Split turn** — creatures can move AND act each round (was: choose one or the other)
2. **Movement range formula** — SPD-derived tiles per turn
3. **TurnAction replaces CombatAction** — struct supports MovementTarget + Action
4. **ActionType.Move removed** — movement is inherent part of every turn
5. **Fix: tiebreak RNG** — pre-computed values before sort (was: inline `_rng.Next()` in comparator)
6. **Fix: EncounterType** — added to constructor for Flee/Capture validation
7. **Fix: BattleStats reset** — per-round counters reset at RoundStart
8. **Fix: empty party** — explicit edge case + constructor validation
9. **Fix: status duration** — documented need for `Dictionary<StatusEffect, int>` on CreatureInstance
10. **Fix: accuracy/evasion stages** — documented as not yet implemented, default 1.0
11. **Fix: PP on suppressed turn** — PP not consumed when suppressed (was: ambiguous)
12. **Fix: faint ordering** — explicit rules for phase abort vs continuation
