# Implementation Prompt: AI Decision System (#16)

## Agent

Invoke **team-combat** skill: `/team-combat AI Decision System`

Team composition for this feature:
- **game-designer** — Verify scoring dimensions align with GDD personality presets
- **ai-programmer** — Implement AIDecisionSystem scorer and candidate evaluation
- **gameplay-programmer** — Wire AIDecisionSystem into TurnManager, ensure DamageCalculator.Estimate() integration
- **qa-tester** — Write deterministic scoring tests and personality behavior tests

> **Note**: This is a logic-heavy system. ai-programmer leads implementation. Skip technical-artist and sound-designer phases. The system must be fully testable with seeded RNG and mock battle states.

## Objective

Implement the **AI Decision System** — scoring-based enemy creature behavior for combat. This is system #16 in the systems index, a Feature Layer system that evaluates every legal action and selects the highest-scoring one. Trainer AI uses personality weight presets from `AIPersonalityConfig` ScriptableObjects. Wild AI uses a simplified balanced preset.

## Authoritative Design Source

`design/gdd/ai-decision-system.md` — all scoring dimensions, personality presets, edge cases, and acceptance criteria live there. Do NOT deviate from the GDD without explicit approval.

## What Already Exists

### Completed systems this builds on:
- `Assets/Scripts/Combat/IAIDecisionSystem.cs` — interface contract: `TurnAction DecideAction(CreatureInstance creature, IReadOnlyList<CreatureInstance> opponents, IReadOnlyList<CreatureInstance> allies, GridSystem grid)`
- `Assets/Scripts/Core/ConfigStubs.cs` — `AIPersonalityConfig : ConfigBase` stub (replace with full implementation)
- `Assets/Scripts/Core/Enums.cs` — `AIPersonalityType` enum (Predator, Territorial, Defensive, Berserker, Support, Trainer)
- `Assets/Scripts/Core/ConfigLoader.cs` — already has `GetAIPersonality(string id)` method, loads from `Resources/Data/AIPersonalities/`
- `Assets/Scripts/Combat/TurnManager.cs` — injects `IAIDecisionSystem` via constructor, calls `DecideAction()` during `ExecuteEnemyAction()` phase
- `Assets/Scripts/Combat/TurnAction.cs` — action data class (ActionType, move, target, destination tile)
- `Assets/Scripts/Combat/Enums/ActionType.cs` — UseMove, Capture, Item, Flee, Wait
- `Assets/Scripts/Combat/DamageCalculator.cs` — has `static Estimate()` method for AI damage scoring (no variance, no crit)
- `Assets/Scripts/Combat/TypeChart.cs` — `GetMultiplier()`, `GetStab()`, `TerrainMatchesCreatureType()`
- `Assets/Scripts/Gameplay/Grid/GridSystem.cs` — `GetReachableTiles()`, `GetTile()`, pathfinding
- `Assets/Scripts/Creatures/CreatureInstance.cs` — all creature state (HP, stats, moves, forms, status effects, ActiveSecondaryType)
- `Assets/Scripts/Creatures/MoveConfig.cs` — move data (GenomeType, Form, Power, Accuracy, PP, Effects, TargetType)

### Assembly layout:
- `Assets/Scripts/GeneForge.Core.asmdef` (GUID: `a2365be9`) — Core namespace
- `Assets/Scripts/Combat/GeneForge.Combat.asmdef` (GUID: `ac121ad6`) — Combat namespace, references Core
- `Assets/Scripts/Gameplay/Grid/GeneForge.Grid.asmdef` — Grid namespace
- `Assets/Tests/EditMode/GeneForge.Tests.EditMode.asmdef` — references Core + Combat

### Key architectural decisions:
- **ADR-002**: Pure C# classes for logic systems
- **ADR-003**: No singletons for gameplay logic — inject via constructor
- **ADR-004**: C# events for system decoupling

## Scope — What to Implement

### MVP scope (implement these):

#### 1. `Assets/Scripts/Core/AIPersonalityConfig.cs` (replaces stub in ConfigStubs.cs)

**ScriptableObject** in `GeneForge.Core` namespace. Replace the stub in `ConfigStubs.cs`.

**Fields (from GDD §3.3):**
```csharp
[CreateAssetMenu(menuName = "GeneForge/AIPersonalityConfig")]
public class AIPersonalityConfig : ConfigBase
{
    // Scoring Weights (should sum to ~8 for normalization)
    [SerializeField, Range(0f, 3f)] float weightDamage;
    [SerializeField, Range(0f, 3f)] float weightKill;
    [SerializeField, Range(0f, 3f)] float weightThreat;
    [SerializeField, Range(0f, 3f)] float weightPosition;
    [SerializeField, Range(0f, 3f)] float weightTerrain;
    [SerializeField, Range(0f, 3f)] float weightSelfPreservation;
    [SerializeField, Range(0f, 3f)] float weightGenomeMatch;
    [SerializeField, Range(0f, 3f)] float weightFormTactic;

    // Behavioral Biases
    [SerializeField, Range(0f, 1f)] float aggressionBias;
    [SerializeField, Range(0f, 1f)] float focusFireBias;
    [SerializeField, Range(0f, 1f)] float randomnessFactor;

    // Thresholds
    [SerializeField, Range(0f, 1f)] float lowHpThreshold;
    [SerializeField, Range(0f, 1f)] float retreatHpThreshold;

    // Public property getters for all fields
}
```

#### 2. `Assets/Scripts/Combat/AIActionScorer.cs`

**Static class** in `GeneForge.Combat` namespace. Pure math — all scoring functions.

**Methods (from GDD §3.2):**
```csharp
/// Scores a candidate action across all dimensions. Returns weighted composite score.
public static float ScoreAction(
    CandidateAction action, CreatureInstance actor,
    IReadOnlyList<CreatureInstance> opponents,
    GridSystem grid, AIPersonalityConfig personality)

/// Estimates normalized damage (0–2 range). Uses DamageCalculator.Estimate().
public static float ScoreDamage(CandidateAction action, CreatureInstance actor, GridSystem grid)

/// Returns 1.0 if estimated damage >= target current HP, else 0.0.
public static float ScoreKillPotential(CandidateAction action, CreatureInstance actor, GridSystem grid)

/// Scores position value: approach vs retreat based on aggressionBias.
public static float ScorePosition(CandidateAction action, CreatureInstance actor, GridSystem grid)

/// Returns 1.0 for terrain synergy match, penalizes harmful terrain.
public static float ScoreTerrainSynergy(CandidateAction action, CreatureInstance actor, GridSystem grid)

/// Self-preservation score: rewards retreat/healing when below lowHpThreshold.
public static float ScoreSelfPreservation(CandidateAction action, CreatureInstance actor, float lowHpThreshold)

/// Genome type matchup score using TypeChart. Normalized -0.25 to +1.0.
public static float ScoreGenomeMatchup(CandidateAction action, CreatureInstance actor)

/// Form tactical advantage based on grid positioning and stat pairings.
public static float ScoreFormTactics(CandidateAction action, CreatureInstance actor, GridSystem grid)
```

#### 3. `Assets/Scripts/Combat/CandidateAction.cs`

**Data class** for action candidates.

```csharp
public class CandidateAction
{
    public MoveConfig Move { get; }          // null = Wait
    public CreatureInstance Target { get; }   // null for self/AOE
    public Vector2Int DestinationTile { get; }
    public float CompositeScore { get; set; }
}
```

#### 4. `Assets/Scripts/Combat/AIDecisionSystem.cs`

**Class** implementing `IAIDecisionSystem` in `GeneForge.Combat` namespace.

**Constructor takes:**
- `AIPersonalityConfig personality` — scoring weights
- `System.Random rng` — for jitter (testability)

**Methods:**
```csharp
/// Implements IAIDecisionSystem. Enumerates all legal actions,
/// scores each, adds jitter, returns the highest-scoring TurnAction.
public TurnAction DecideAction(
    CreatureInstance creature,
    IReadOnlyList<CreatureInstance> opponents,
    IReadOnlyList<CreatureInstance> allies,
    GridSystem grid)

/// Generates all legal CandidateActions for the creature.
private List<CandidateAction> EnumerateCandidates(
    CreatureInstance creature,
    IReadOnlyList<CreatureInstance> opponents,
    GridSystem grid)
```

**MVP simplifications:**
- No 2-turn lookahead (Trainer AI feature — post-MVP)
- No switch candidate evaluation (depends on full party bench access)
- No Struggle fallback move needed yet (ensure Wait is always a candidate)
- Threat scoring simplified: target lowest HP opponent (Threat/Aggro System not implemented)

#### 5. `Assets/Tests/EditMode/AIActionScorerTests.cs`

**Required tests (~18):**
- ScoreDamage returns normalized value (estimated damage / target maxHP)
- ScoreDamage returns 0 for non-damaging moves
- ScoreKillPotential returns 1.0 when damage >= target HP
- ScoreKillPotential returns 0.0 when damage < target HP
- ScoreGenomeMatchup positive for super-effective move
- ScoreGenomeMatchup negative for resisted move
- ScoreGenomeMatchup zero for neutral move
- ScoreTerrainSynergy returns 1.0 for matching terrain
- ScoreTerrainSynergy penalizes harmful terrain
- ScoreSelfPreservation activates below lowHpThreshold
- ScoreSelfPreservation inactive above threshold
- ScoreAction weighted sum matches manual calculation
- Aggressive personality scores higher on damage-heavy actions
- Cautious personality scores higher on self-preservation actions
- Wait action always has score 0 (baseline)
- Random jitter stays within randomnessFactor bounds
- Seeded RNG produces deterministic results
- DecideAction returns valid TurnAction (never null)

### Out of scope (skip these):
- 2-turn lookahead for Trainer AI
- Switch candidate evaluation
- Struggle move (all-PP-depleted fallback)
- Threat/Aggro System integration (use simplified targeting)
- Disobedience check (instability >= 80)
- Focus-fire bias tracking across turns
- ScoreFormTactics LoS validation (depends on full LoS implementation)

## Constraints

- **AIPersonalityConfig**: ScriptableObject extending `ConfigBase`
- **AIActionScorer**: Static class, pure math, no state
- **AIDecisionSystem**: Instance class implementing `IAIDecisionSystem`, RNG injected
- **Namespace**: `GeneForge.Core` for config, `GeneForge.Combat` for scorer and system
- **No hardcoded weights** — all scoring weights from AIPersonalityConfig
- **XML doc comments** on all public API
- Follow existing patterns from `DamageCalculator.cs` (instance with RNG injection)
- Remove the `AIPersonalityConfig` stub from `ConfigStubs.cs` after creating the real class

## Collaboration Protocol

Follow Question -> Options -> Decision -> Draft -> Approval:
1. Ask before creating any file
2. Show draft or summary before writing
3. Get explicit approval for the full changeset
4. No commits without user instruction

## Branch

Create branch `feature/AI-Decision-System` from `main`.
