# AI Decision System

## 1. Overview

The AI Decision System controls enemy creature behavior in combat using a scoring-based evaluation model. At the start of each enemy turn, the AI evaluates every legal action (move + target + destination tile combination) and scores it across several weighted dimensions: expected damage, type effectiveness, kill potential, threat priority, positioning value, and terrain synergy. The action with the highest composite score is executed. Trainer AI uses personality weight presets defined in `AIPersonalityConfig` ScriptableObjects. Wild AI uses a simpler threat/aggro model inherited from the Threat/Aggro System. Both share the same underlying scorer.

## 2. Player Fantasy

Enemies feel purposeful, not random. A fire creature on a lava tile pressing its advantage, an enemy healer retreating to high ground, a speed attacker targeting your weakest creature — these behaviors make the player feel like they are reading and countering a real opponent. When the AI makes an unexpected-but-correct play, it earns respect. When the player outmaneuvers it through positioning or type prep, the victory feels earned.

## 3. Detailed Rules

### 3.1 Action Enumeration

At the start of each enemy turn, the system generates a list of `CandidateAction` objects representing every legal combination of:
- Move (from the creature's known move pool, with sufficient PP)
- Target (all valid targets given the move's targeting pattern)
- Destination tile (if the move requires repositioning first)

Waiting (passing the turn) is always a valid candidate with a base score of 0.

```csharp
public class CandidateAction
{
    public MoveConfig move;           // null = Wait
    public CreatureInstance target;   // null for self-targeting or AOE
    public Vector2Int destinationTile;
    public float compositeScore;
}
```

### 3.2 Scoring Dimensions

Each candidate is scored across six dimensions. The final score is a weighted sum.

```csharp
public static float ScoreAction(
    CandidateAction action,
    CreatureInstance actor,
    BattleState battleState,
    AIPersonalityConfig personality)
{
    float scoreDamage      = ScoreDamage(action, actor, battleState);
    float scoreKill        = ScoreKillPotential(action, actor, battleState);
    float scoreThreat      = ScoreThreatTarget(action, actor, battleState);
    float scorePosition    = ScorePosition(action, actor, battleState);
    float scoreTerrain     = ScoreTerrainSynergy(action, actor, battleState);
    float scoreSelf        = ScoreSelfPreservation(action, actor, battleState);

    return (scoreDamage      * personality.weightDamage)
         + (scoreKill        * personality.weightKill)
         + (scoreThreat      * personality.weightThreat)
         + (scorePosition    * personality.weightPosition)
         + (scoreTerrain     * personality.weightTerrain)
         + (scoreSelf        * personality.weightSelfPreservation);
}
```

#### ScoreDamage
Estimates damage the move would deal to the target. Uses the same formula as `DamageCalculator` but without random variance (uses midpoint). Normalized to the target's max HP to get a 0–1 value. Multiplied by type effectiveness.

```
scoreDamage = estimatedDamage / target.maxHp   (clamped 0–2 to allow overkill reward)
```

#### ScoreKillPotential
Strongly rewards actions that can faint the target this turn.

```
scoreKill = (estimatedDamage >= target.currentHp) ? 1.0 : 0.0
```

This is multiplied by `weightKill` which is high in Aggressive personalities.

#### ScoreThreatTarget
Prioritizes targets that pose higher threat to the AI team. Uses the Threat/Aggro System's current threat values.

```
scoreThreat = target.currentThreat / maxThreatInBattle   (normalized 0–1)
```

For Trainer AI, this is replaced by a strategic target priority based on role:
- Enemy healer: highest priority
- Enemy strongest attacker: second
- Closest enemy: tiebreaker

#### ScorePosition
Rewards moving to tiles that are closer to preferred targets (approach) or farther from dangerous enemies (retreat). Uses A* path distance.

```
approachScore = 1.0 - (distanceToTarget / maxBattleDistance)
retreatScore  = distanceFromHighestThreat / maxBattleDistance
scorePosition = (approachScore * personality.aggressionBias) 
              + (retreatScore * (1 - personality.aggressionBias))
```

#### ScoreTerrainSynergy
Rewards moving to terrain tiles that synergize with the creature's type.

```
scoreTerrainSynergy = destinationTile.hasSynergyWith(actor.primaryType) ? 1.0 : 0.0
```

Also penalizes landing on tiles harmful to the creature's type (e.g. water creature on fire tile):

```
scoreTerrainSynergy -= destinationTile.hasHarmFor(actor.primaryType) ? 0.5 : 0.0
```

#### ScoreSelfPreservation
Rewards actions that keep the creature safe. Strong in Cautious personalities.

- Rewards using a healing move or buff when below `lowHpThreshold`.
- Rewards retreating to cover or high-DEF tiles.
- Penalizes actions that would leave the creature exposed to a likely KO next turn.

```
selfPreservScore = (actor.currentHp / actor.maxHp < LowHpThreshold) ? 1.0 : 0.0
```

### 3.3 AIPersonalityConfig ScriptableObject

```csharp
[CreateAssetMenu(menuName = "GeneForge/AIPersonalityConfig")]
public class AIPersonalityConfig : ScriptableObject
{
    public string id;
    public string displayName;

    [Header("Scoring Weights (should sum to ~6 for normalization)")]
    [Range(0f, 3f)] public float weightDamage          = 1.0f;
    [Range(0f, 3f)] public float weightKill             = 1.2f;
    [Range(0f, 3f)] public float weightThreat           = 0.8f;
    [Range(0f, 3f)] public float weightPosition         = 0.7f;
    [Range(0f, 3f)] public float weightTerrain          = 0.5f;
    [Range(0f, 3f)] public float weightSelfPreservation = 0.8f;

    [Header("Behavioral Biases")]
    [Range(0f, 1f)] public float aggressionBias    = 0.6f;  // 1.0 = always approach
    [Range(0f, 1f)] public float focusFireBias     = 0.5f;  // 1.0 = always target same enemy
    [Range(0f, 1f)] public float abilityPreference = 0.5f;  // 1.0 = always use special ability
    [Range(0f, 1f)] public float randomnessFactor  = 0.1f;  // Adds slight unpredictability

    [Header("Thresholds")]
    [Range(0f, 1f)] public float lowHpThreshold    = 0.30f; // Below this HP, self-preservation spikes
    [Range(0f, 1f)] public float retreatHpThreshold = 0.15f; // Below this, always try to retreat
}
```

### 3.4 Named Personality Presets

| Preset ID | Name | Character | Key Weights |
|-----------|------|-----------|-------------|
| `aggressive` | Berserker | Attacks first, ignores self | weightDamage=2.0, weightKill=2.5, weightSelf=0.1 |
| `cautious` | Tactician | Preserves self, positions well | weightSelf=2.0, weightPosition=1.5, weightDamage=0.8 |
| `balanced` | Standard | All-around | All weights ~1.0 |
| `focus-fire` | Hunter | Always targets same creature | focusFireBias=1.0, weightKill=2.0 |
| `terrain-seeker` | Naturalist | Prioritizes synergy tiles | weightTerrain=2.5, weightPosition=1.5 |
| `self-buff` | Enhancer | Buffs before attacking | abilityPreference=0.9, weightDamage=0.5 |

### 3.5 Wild AI vs Trainer AI

**Wild AI:**
- Uses `threat/aggro` values from the Threat/Aggro System to determine target selection.
- Personality defaults to `balanced` unless the species has a `defaultWildPersonality` set in `CreatureConfig`.
- Predator-type species use `aggressive` by default.
- Does not use strategic multi-turn planning; evaluates each turn independently.

**Trainer AI:**
- Uses the personality assigned to the trainer's `AIPersonalityConfig`.
- Has access to a simple 2-turn lookahead: if executing action A this turn, what is the expected best action next turn? The lookahead is not exhaustive — it checks only the top 3 current candidates.
- Trainer AI respects switch logic: if the active creature is at a type disadvantage and a better-typed creature is available, it will consider switching (scored as a candidate action with `weightSwitch` modifier).

### 3.6 Randomness

A small random jitter is added to the final score to prevent perfectly deterministic AI (which players can memorize):

```
finalScore = compositeScore + Random.Range(-randomnessFactor, randomnessFactor)
```

`randomnessFactor` default is 0.1. This means the AI can occasionally make a suboptimal choice of up to 10% score value — enough to feel alive, not enough to feel stupid.

### 3.7 Tie-Breaking

If two candidates score within 0.01 of each other after jitter, the tiebreaker is:
1. Move with higher base power.
2. Move with higher accuracy.
3. Random selection.

## 4. Formulas

### Composite Score

```
score = (scoreDamage * wDamage) + (scoreKill * wKill) + (scoreThreat * wThreat)
      + (scorePosition * wPosition) + (scoreTerrain * wTerrain) + (scoreSelf * wSelf)
score += Random.Range(-randomnessFactor, randomnessFactor)
```

### Damage Estimate (for scoring only)

```
estimatedDamage = ((2 * attackerLevel / 5 + 2) * movePower * (ATK / DEF)) / 50 + 2
estimatedDamage *= typeEffectiveness
estimatedDamage *= terrainSynergyBonus
estimatedDamage *= heightBonus
```

(Mirrors the Damage & Health System formula but without critical hit or random roll variance.)

### Normalized Damage Score

```
scoreDamage = Clamp(estimatedDamage / target.maxHp, 0, 2)
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| All moves are out of PP | AI selects Struggle (base power 10 physical move, no type, self-damage 25% of dealt) |
| No valid targets exist for any move | AI waits (score 0 action selected) |
| All candidate scores are negative | Wait action (score 0) is always a fallback candidate; selected if it outscores all others |
| Creature has instability >= 80 (disobedience check) | Handled by Creature Instance before AI runs; if disobedience triggers, AI scoring is skipped for this turn |
| Two tiles have identical composite score | Tiebreak by closest to current position (minimize movement cost) |
| AI trainer has no conscious creatures to switch to | Switch candidate is not generated; AI cannot switch if bench is empty |
| AI attempts to use a move that targets allies on a single-target move | Ally targeting is only valid for buff/heal moves explicitly flagged `canTargetAlly = true` |
| Wild creature has no `defaultWildPersonality` assigned | Falls back to `balanced` preset |
| Lookahead evaluation causes stack overflow (nested scoring) | Lookahead is explicitly depth-limited to 1 level; no recursive calls beyond first depth |

## 6. Dependencies

| System | Dependency Type | Notes |
|--------|----------------|-------|
| Move Database | Read | Move power, accuracy, type, targeting pattern, PP |
| Creature Instance | Read | HP, stats, instability, current PP, known moves |
| Type Chart System | Read | Type effectiveness for damage estimation |
| Damage & Health System | Read | Shared damage formula for score estimation |
| Threat / Aggro System | Read | Wild AI target selection |
| Grid / Tile System | Read | Tile positions, movement cost, height, terrain type |
| Terrain System | Read | Synergy tile checks per creature type |
| Turn Manager | Read | Turn order, action execution pipeline |
| Party System | Read | Trainer switch candidate enumeration |

## 7. Tuning Knobs

| Parameter | Default | Range | Notes |
|-----------|---------|-------|-------|
| `randomnessFactor` | 0.1 | 0.0–0.3 | Higher = less predictable AI |
| `weightDamage` (balanced) | 1.0 | 0–3 | Per-personality asset |
| `weightKill` (balanced) | 1.2 | 0–3 | |
| `weightThreat` (balanced) | 0.8 | 0–3 | |
| `weightPosition` (balanced) | 0.7 | 0–3 | |
| `weightTerrain` (balanced) | 0.5 | 0–3 | |
| `weightSelfPreservation` (balanced) | 0.8 | 0–3 | |
| `lowHpThreshold` | 0.30 | 0.1–0.5 | Below this fraction of maxHp, self-preservation activates |
| `retreatHpThreshold` | 0.15 | 0.05–0.3 | Below this, creature always tries to retreat |
| `lookaheadDepth` | 1 | 0–2 | 0 = no lookahead; 2 = two-turn planning (expensive) |
| `scoreTieTolerance` | 0.01 | 0.0–0.05 | Score difference below which tiebreaker logic triggers |
| TrainerXpMultiplier (shared) | 1.5 | 1.0–3.0 | Defined in Leveling/XP System |

## 8. Acceptance Criteria

- [ ] `ScoreAction()` returns a deterministic result (excluding random jitter) for identical inputs (unit tested).
- [ ] All six scoring dimensions contribute non-zero values for relevant scenarios (verified per dimension).
- [ ] AI selects Struggle when all moves are out of PP.
- [ ] AI falls back to Wait when no valid targets exist and Wait is the highest-scoring action.
- [ ] `AIPersonalityConfig` ScriptableObjects can be created and assigned to encounters via the Unity Inspector.
- [ ] Aggressive personality demonstrably attacks more and retreats less than Cautious in simulation (playtested).
- [ ] Wild AI uses threat values from the Threat/Aggro System for target selection.
- [ ] Trainer AI uses 2-turn lookahead on the top 3 candidates without stack overflow.
- [ ] Trainer AI correctly considers switching when type disadvantage scoring favors a bench creature.
- [ ] Random jitter is within the configured `randomnessFactor` range.
- [ ] Disobedient creatures (instability >= 80, disobey triggered) skip AI scoring entirely for that turn.
- [ ] Terrain synergy scoring correctly rewards moving to matching-type tiles and penalizes harmful tiles.
