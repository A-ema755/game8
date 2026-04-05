# Turn Manager

## 1. Overview

The Turn Manager governs phase-based combat sequencing in Gene Forge. Each round flows through five phases: RoundStart, PlayerCreatureSelect, PlayerAction, EnemyAction, and RoundEnd. Initiative within the action phases is determined by proximity to the nearest enemy (closer creatures act first), with SPD as a tiebreaker. The system publishes C# events at every meaningful moment — RoundStarted, RoundEnded, CreatureActed, CreatureFainted, CreatureCaptured — allowing combat UI, audio, VFX, and the AI system to react without direct coupling. CombatAction is a value struct describing a single creature's chosen action; BattleStats tracks aggregate combat metrics per session.

## 2. Player Fantasy

Combat feels deliberate and readable. The player always knows whose turn it is, what phase they're in, and how long until the enemy acts. The phase structure creates a rhythm: pick your moves, watch them execute in initiative order, see the enemy respond. There's no ambiguity about sequencing — the player's creatures never surprise-lose a turn because the rules were unclear.

## 3. Detailed Rules

### 3.1 Phase Sequence

```
RoundStart
    │
    ▼
PlayerCreatureSelect   ← Player chooses action for each party creature
    │
    ▼
PlayerAction           ← Player creatures execute in initiative order
    │
    ▼
EnemyAction            ← Enemy creatures execute in initiative order
    │
    ▼
RoundEnd
    │
    └─► loop to RoundStart (or EndCombat if win/loss condition met)
```

### 3.2 Phase Definitions

| Phase | Description |
|-------|-------------|
| `RoundStart` | Apply start-of-round effects (Burn/Poison ticks, status duration decrements). Fire `RoundStarted` event. |
| `PlayerCreatureSelect` | UI presents move/move target/capture/item choices for each non-fainted party creature. No time limit (MVP). |
| `PlayerAction` | Execute queued player actions in initiative order. Each execution fires `CreatureActed`. |
| `EnemyAction` | AI resolves and executes enemy actions in initiative order. Each execution fires `CreatureActed`. |
| `RoundEnd` | Apply end-of-round effects. Check win/loss. Fire `RoundEnded`. Increment round counter. |

### 3.3 CombatAction Struct

```csharp
namespace GeneForge.Combat
{
    /// <summary>
    /// Describes one creature's action for the current round.
    /// Immutable value type — set during PlayerCreatureSelect, consumed during action phases.
    /// </summary>
    public readonly struct CombatAction
    {
        public readonly ActionType Type;
        public readonly CreatureInstance Actor;
        public readonly MoveConfig Move;          // null for non-move actions
        public readonly TileData TargetTile;      // For move or move targets
        public readonly CreatureInstance Target;  // null for tile-targeted moves
        public readonly int MovePPSlot;           // Index into Actor.LearnedMoves (0-3)

        public CombatAction(ActionType type, CreatureInstance actor,
            MoveConfig move = null, TileData targetTile = null,
            CreatureInstance target = null, int movePPSlot = -1)
        {
            Type = type;
            Actor = actor;
            Move = move;
            TargetTile = targetTile;
            Target = target;
            MovePPSlot = movePPSlot;
        }
    }

    public enum ActionType
    {
        UseMove = 0,
        Move = 1,       // Reposition on grid (costs action)
        Capture = 2,    // Throw Gene Trap at target
        Item = 3,       // Use held item (post-MVP)
        Flee = 4,       // Attempt to flee wild encounter
        Wait = 5        // Pass turn (no action)
    }
}
```

### 3.4 Initiative Calculation

Initiative order within each action phase (PlayerAction and EnemyAction are sorted independently):

```csharp
/// <summary>
/// Initiative = proximity score + SPD tiebreaker.
/// Creatures closer to the nearest enemy act FIRST.
/// Lower distance → lower initiative value → sorted first.
/// </summary>
private int CalculateInitiative(CreatureInstance creature, GridSystem grid, List<CreatureInstance> enemies)
{
    int minDist = int.MaxValue;
    foreach (var enemy in enemies)
    {
        if (enemy.IsFainted) continue;
        int dist = GridSystem.ChebyshevDistance(
            creature.GridPosition,
            enemy.GridPosition);
        if (dist < minDist) minDist = dist;
    }

    // Negate SPD so higher SPD = lower sort value (acts earlier in tie)
    return minDist * 1000 - creature.ComputedStats.SPD;
}
```

Tiebreak order (from highest to lowest priority):
1. Lower Chebyshev distance to nearest enemy
2. Higher SPD stat
3. Random (stable per round — seeded at RoundStart)

### 3.5 TurnManager Implementation

```csharp
namespace GeneForge.Combat
{
    /// <summary>
    /// Phase-based combat sequencer. Pure C# — no MonoBehaviour.
    /// Drives the round loop and publishes events for all combat state changes.
    /// </summary>
    public class TurnManager
    {
        // ── Events ───────────────────────────────────────────────────────
        public static event Action<int> RoundStarted;           // round number
        public static event Action<int> RoundEnded;             // round number
        public static event Action<CombatAction> CreatureActed; // the executed action
        public static event Action<CreatureInstance> CreatureFainted;
        public static event Action<CreatureInstance, bool> CreatureCaptured; // creature, success

        // ── State ─────────────────────────────────────────────────────────
        public int CurrentRound { get; private set; }
        public CombatPhase CurrentPhase { get; private set; }
        public bool CombatActive { get; private set; }

        private readonly GridSystem _grid;
        private readonly List<CreatureInstance> _playerParty;
        private readonly List<CreatureInstance> _enemyParty;
        private readonly BattleStats _stats;
        private readonly Dictionary<CreatureInstance, CombatAction> _queuedActions = new();
        private readonly System.Random _rng;

        public TurnManager(GridSystem grid,
            List<CreatureInstance> playerParty,
            List<CreatureInstance> enemyParty,
            int seed = 0)
        {
            _grid = grid;
            _playerParty = playerParty;
            _enemyParty = enemyParty;
            _stats = new BattleStats();
            _rng = seed == 0 ? new System.Random() : new System.Random(seed);
        }

        // ── Entry Point ───────────────────────────────────────────────────

        public async Task RunCombatAsync(
            Func<List<CreatureInstance>, Task<Dictionary<CreatureInstance, CombatAction>>> playerInputProvider,
            IAIDecisionSystem aiSystem)
        {
            CombatActive = true;
            CurrentRound = 0;

            while (CombatActive)
            {
                CurrentRound++;
                await RunRoundAsync(playerInputProvider, aiSystem);

                if (CheckEndCondition(out var result))
                {
                    CombatActive = false;
                    _stats.Result = result;
                }
            }
        }

        // ── Round Execution ───────────────────────────────────────────────

        private async Task RunRoundAsync(
            Func<List<CreatureInstance>, Task<Dictionary<CreatureInstance, CombatAction>>> playerInputProvider,
            IAIDecisionSystem aiSystem)
        {
            // Phase: RoundStart
            CurrentPhase = CombatPhase.RoundStart;
            ApplyStartOfRoundEffects();
            RoundStarted?.Invoke(CurrentRound);

            if (!CombatActive) return; // faint during start-of-round (DoT)

            // Phase: PlayerCreatureSelect
            CurrentPhase = CombatPhase.PlayerCreatureSelect;
            var playerActions = await playerInputProvider(GetActivePlayerCreatures());
            foreach (var (creature, action) in playerActions)
                _queuedActions[creature] = action;

            // Phase: PlayerAction
            CurrentPhase = CombatPhase.PlayerAction;
            var playerOrder = GetInitiativeOrder(GetActivePlayerCreatures(), _enemyParty);
            foreach (var creature in playerOrder)
            {
                if (creature.IsFainted) continue;
                if (_queuedActions.TryGetValue(creature, out var action))
                    await ExecuteActionAsync(action);
                if (!CombatActive) return;
            }

            // Phase: EnemyAction
            CurrentPhase = CombatPhase.EnemyAction;
            var enemyOrder = GetInitiativeOrder(GetActiveEnemyCreatures(), _playerParty);
            foreach (var creature in enemyOrder)
            {
                if (creature.IsFainted) continue;
                var aiAction = aiSystem.DecideAction(creature, _playerParty, _grid);
                await ExecuteActionAsync(aiAction);
                if (!CombatActive) return;
            }

            // Phase: RoundEnd
            CurrentPhase = CombatPhase.RoundEnd;
            ApplyEndOfRoundEffects();
            _queuedActions.Clear();
            RoundEnded?.Invoke(CurrentRound);
        }

        // ── Action Execution ──────────────────────────────────────────────

        private async Task ExecuteActionAsync(CombatAction action)
        {
            // Priority moves fire before non-priority in the same phase (re-sort if needed)
            switch (action.Type)
            {
                case ActionType.UseMove:
                    await ExecuteMoveAsync(action);
                    break;
                case ActionType.Move:
                    ExecuteReposition(action);
                    break;
                case ActionType.Capture:
                    ExecuteCapture(action);
                    break;
                case ActionType.Wait:
                    break; // no-op
                case ActionType.Flee:
                    ExecuteFlee(action);
                    break;
            }
            CreatureActed?.Invoke(action);
            _stats.ActionsThisRound++;
        }

        private async Task ExecuteMoveAsync(CombatAction action)
        {
            // Deduct PP
            action.Actor.DeductPP(action.MovePPSlot);

            // Hit check
            bool hit = MoveHitCheck(action.Move, action.Actor, action.Target);
            if (!hit)
            {
                // "Missed!" — no damage applied
                return;
            }

            if (action.Move.IsDamaging)
            {
                int damage = DamageCalculator.Calculate(action.Move, action.Actor, action.Target, _grid);
                action.Target.TakeDamage(damage);
                _stats.TotalDamageDealt += damage;

                if (action.Target.IsFainted)
                {
                    HandleFaint(action.Target);
                }
            }

            // Apply move effects
            foreach (var effect in action.Move.Effects)
                MoveEffectApplier.Apply(effect, action.Actor, action.Target, _grid);

            await Task.Yield(); // frame yield for animation
        }

        private void ExecuteCapture(CombatAction action)
        {
            // Resolved by CaptureSystem — TurnManager just fires the event
            bool success = CaptureSystem.Attempt(action.Target, action.Actor);
            CreatureCaptured?.Invoke(action.Target, success);
            if (success)
            {
                _enemyParty.Remove(action.Target);
                _grid.GetTile(action.Target.GridPosition).Occupant = null;
                _stats.CapturesThisRound++;
            }
        }

        private void ExecuteReposition(CombatAction action)
        {
            var path = Pathfinder.FindPath(_grid,
                _grid.GetTile(action.Actor.GridPosition),
                action.TargetTile,
                action.Actor);
            if (path != null) action.Actor.MoveTo(action.TargetTile.GridPosition, _grid);
        }

        private void ExecuteFlee(CombatAction action)
        {
            // MVP: flee always succeeds in wild encounters, never in trainer battles
            CombatActive = false;
            _stats.Result = CombatResult.Fled;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private List<CreatureInstance> GetInitiativeOrder(
            List<CreatureInstance> actors, List<CreatureInstance> opponents)
        {
            var sorted = actors.Where(c => !c.IsFainted).ToList();
            sorted.Sort((a, b) =>
            {
                int ia = CalculateInitiative(a, _grid, opponents);
                int ib = CalculateInitiative(b, _grid, opponents);
                if (ia != ib) return ia.CompareTo(ib);
                return _rng.Next().CompareTo(_rng.Next()); // random tiebreak
            });
            return sorted;
        }

        private void HandleFaint(CreatureInstance creature)
        {
            creature.Faint();
            CreatureFainted?.Invoke(creature);
            _grid.GetTile(creature.GridPosition).Occupant = null;
            _stats.FaintsThisRound++;
        }

        private void ApplyStartOfRoundEffects()
        {
            foreach (var c in _playerParty.Concat(_enemyParty))
            {
                if (c.IsFainted) continue;
                StatusEffectProcessor.ApplyStartOfRound(c);
                if (c.IsFainted) HandleFaint(c);
            }
        }

        private void ApplyEndOfRoundEffects()
        {
            foreach (var c in _playerParty.Concat(_enemyParty))
            {
                if (c.IsFainted) continue;
                StatusEffectProcessor.ApplyEndOfRound(c);
                StatusEffectProcessor.DecrementDurations(c);
            }
        }

        private bool CheckEndCondition(out CombatResult result)
        {
            bool playerWon  = _enemyParty.All(c => c.IsFainted);
            bool playerLost = _playerParty.All(c => c.IsFainted);

            if (playerWon)  { result = CombatResult.Victory; return true; }
            if (playerLost) { result = CombatResult.Defeat;  return true; }
            result = CombatResult.Ongoing;
            return false;
        }

        private bool MoveHitCheck(MoveConfig move, CreatureInstance attacker, CreatureInstance target)
        {
            if (move.AlwaysHits) return true;
            float hitChance = (move.Accuracy / 100f)
                            * attacker.AccuracyStageMultiplier
                            * (1f / target.EvasionStageMultiplier);
            return (float)_rng.NextDouble() < hitChance;
        }

        private int CalculateInitiative(CreatureInstance creature, GridSystem grid, List<CreatureInstance> opponents)
        {
            int minDist = int.MaxValue;
            foreach (var opp in opponents)
            {
                if (opp.IsFainted) continue;
                int d = GridSystem.ChebyshevDistance(creature.GridPosition, opp.GridPosition);
                if (d < minDist) minDist = d;
            }
            return minDist * 1000 - creature.ComputedStats.SPD;
        }

        private List<CreatureInstance> GetActivePlayerCreatures()
            => _playerParty.Where(c => !c.IsFainted).ToList();

        private List<CreatureInstance> GetActiveEnemyCreatures()
            => _enemyParty.Where(c => !c.IsFainted).ToList();
    }

    // ── Supporting Types ──────────────────────────────────────────────────

    public enum CombatPhase
    {
        RoundStart = 0,
        PlayerCreatureSelect = 1,
        PlayerAction = 2,
        EnemyAction = 3,
        RoundEnd = 4
    }

    public enum CombatResult
    {
        Ongoing = 0,
        Victory = 1,
        Defeat = 2,
        Fled = 3
    }

    /// <summary>Aggregate metrics tracked for a single combat session.</summary>
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
}
```

### 3.6 Priority Move Handling

Moves with `priority > 0` execute before standard moves within the same action phase. Re-sort is applied after all actions are queued but before execution begins:

```csharp
// Within the PlayerAction phase, re-sort to respect priority:
playerOrder.Sort((a, b) => {
    var ma = _queuedActions[a].Move;
    var mb = _queuedActions[b].Move;
    int pa = ma?.Priority ?? 0;
    int pb = mb?.Priority ?? 0;
    if (pa != pb) return pb.CompareTo(pa); // descending: higher priority first
    return CalculateInitiative(a, _grid, _enemyParty)
        .CompareTo(CalculateInitiative(b, _grid, _enemyParty));
});
```

## 4. Formulas

| Formula | Expression |
|---------|-----------|
| Initiative score | `(minDistToNearestEnemy × 1000) − SPD` |
| Action order | Sort ascending by initiative score; lower = acts first |
| Priority override | Higher `move.Priority` acts before lower within same phase |
| Hit chance | `(accuracy / 100) × accuracyMult × (1 / evasionMult)` |
| Round DoT (Burn) | `floor(maxHP / 16)` damage at RoundStart |
| Round DoT (Poison) | `floor(maxHP / 8)` damage at RoundStart |
| Status duration (Sleep) | Random 2–5 rounds, rolled at application |
| Status duration (Freeze) | Random 2–4 rounds, thaws immediately on Fire hit |

## 5. Edge Cases

| Situation | Behavior |
|-----------|----------|
| All player creatures faint during `PlayerAction` | `CheckEndCondition` returns Defeat; loop exits after `EnemyAction` phase completes |
| All enemies faint during `PlayerAction` | Combat ends before `EnemyAction` phase runs |
| Creature gains Sleep at start of round from another effect | That creature still queued an action; action is suppressed during execution |
| Capture attempt on trainer-owned creature | `CaptureSystem.Attempt` always returns false; "Can't capture trainer's creature" logged |
| Two player creatures have identical initiative score | Random tiebreak seeded at RoundStart, stable within round |
| Player queues a move but target faints before execution | Action resolves against empty tile; no damage; no PP consumed |
| Creature under Confusion hits itself | Self-damage uses actor stats, 40 base power, typeless, no type effectiveness |
| Flee attempted in trainer battle | Always fails; "You can't flee a trainer battle!" message |
| Priority move (+1) vs another priority move (+1) | Normal initiative tiebreaker applies between same-priority moves |
| `playerInputProvider` never resolves | System waits indefinitely (MVP has no timeout); add timeout post-MVP |

## 6. Dependencies

| Dependency | Direction | Notes |
|------------|-----------|-------|
| `GridSystem` | Inbound | Initiative calculation, pathfinding for reposition |
| `CreatureInstance` | Inbound | Actors, targets, stat reading, HP mutation |
| `MoveConfig` | Inbound | Move data for action execution |
| `DamageCalculator` | Outbound | Called per damaging move execution |
| `CaptureSystem` | Outbound | Called on capture action |
| `IAIDecisionSystem` | Outbound | Enemy action decisions |
| `MoveEffectApplier` | Outbound | Applies MoveEffect list per move |
| `StatusEffectProcessor` | Outbound | Start/end-of-round status ticks |
| `TypeChart` | Indirect | Via DamageCalculator |
| Combat UI | Inbound | Subscribes to all TurnManager events |

## 7. Tuning Knobs

| Parameter | Location | Default | Notes |
|-----------|----------|---------|-------|
| Initiative distance weight | `CalculateInitiative` | `× 1000` | Separates distance tiers cleanly |
| Max rounds per combat | `GameSettings` SO | 50 | Draw if exceeded (post-MVP) |
| Flee success rate (wild) | `GameSettings` SO | 100% (MVP) | Post-MVP: formula based on SPD ratio |
| Burn DoT fraction | `StatusEffectProcessor` | 1/16 max HP | Tunable per status config |
| Poison DoT fraction | `StatusEffectProcessor` | 1/8 max HP | Tunable per status config |
| Sleep duration range | `StatusEffectProcessor` | 2–5 rounds | Min/max configurable |
| Freeze thaw chance per round | `StatusEffectProcessor` | 20% | Alternative to fire-only thaw |

## 8. Acceptance Criteria

- [ ] Round phases execute in order: RoundStart → PlayerCreatureSelect → PlayerAction → EnemyAction → RoundEnd
- [ ] `RoundStarted` event fires at start of each round with correct round number
- [ ] `RoundEnded` event fires at end of each round
- [ ] `CreatureActed` event fires for every executed action
- [ ] `CreatureFainted` event fires when HP reaches 0, before next action resolves
- [ ] `CreatureCaptured` event fires with correct success boolean
- [ ] Player creatures closer to enemies act before farther ones in `PlayerAction`
- [ ] Higher SPD breaks initiative ties correctly
- [ ] Priority +1 moves execute before priority 0 moves in the same phase
- [ ] Combat ends immediately when all enemies faint (no EnemyAction phase that round)
- [ ] Combat ends when all player creatures faint (Defeat result)
- [ ] Flee action in wild encounter sets `CombatResult.Fled`
- [ ] Flee action in trainer battle fails and does not end combat
- [ ] EditMode test: deterministic round with seeded RNG produces expected action order
- [ ] EditMode test: Burn DoT applies `floor(maxHP/16)` damage at RoundStart
