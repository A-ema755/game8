# Threat & Aggro System

## 1. Overview

The Threat & Aggro System determines which enemy creatures target which player creatures during combat. Wild creatures use a threat score based on damage dealt, proximity, and health status; this threat score can be manipulated via moves like Taunt and Stealth. Trainer-controlled creatures use personality weights and strategic AI instead of threat. The `ThreatManager` class tracks threat scores across all active creatures, applies aggro-manipulating effects, and provides the AI system with targeting recommendations. Threat is recalculated after each action resolves.

## 2. Player Fantasy

Combat feels responsive to your tactics. If you send in a high-damage dealer, enemies focus fire on them — you learn to protect that creature or spread threat around. A defensive tank can draw aggro away from fragile creatures. Using Taunt forces enemies to attack you, creating meaningful tactical moments. Using Stealth lets a creature fade from enemy attention. The system rewards positioning and move choice — not just raw stats.

## 3. Detailed Rules

### 3.1 Threat Scoring (Wild Creatures)

```csharp
namespace GeneForge.Combat
{
    /// <summary>
    /// Manages threat scores for combat creatures.
    /// Used by wild creature AI to determine targets.
    /// Trainer AI uses personality weights instead.
    /// </summary>
    public class ThreatManager
    {
        private readonly Dictionary<CreatureInstance, ThreatData> _threatScores = new();

        [System.Serializable]
        private class ThreatData
        {
            public float ThreatScore;
            public int RoundsTaunted; // 0 = not taunted
            public int RoundsStealth; // 0 = not stealthed
        }

        public ThreatManager(IEnumerable<CreatureInstance> allCreatures)
        {
            foreach (var creature in allCreatures)
            {
                _threatScores[creature] = new ThreatData { ThreatScore = 0f };
            }
        }

        /// <summary>
        /// Calculate threat score for a target creature.
        /// 
        /// Formula:
        /// threatScore = damageDealtThisBattle × 1.0 
        ///             + (1 / distanceToNearestEnemy) × 5.0 
        ///             + (1 - currentHP/maxHP) × preyFactor
        ///             - (taunted ? 0 : 50) [if taunted, BOOST threat]
        ///             - (stealthed ? 200 : 0) [if stealthed, REDUCE threat to near-zero]
        /// 
        /// Taunted creatures become high-threat targets regardless of other factors.
        /// Stealthed creatures become low-threat targets (threat ≈ 0).
        /// </summary>
        public float GetThreatScore(
            CreatureInstance target,
            CreatureInstance fromPerspective,
            GridSystem grid,
            float damageDealtThisBattle,
            float preyFactor = 10f)
        {
            var threatData = _threatScores[target];

            // Base threat from damage dealt this battle
            float threat = damageDealtThisBattle * 1.0f;

            // Proximity threat: closer creatures are higher threat
            int distance = GridSystem.ChebyshevDistance(
                target.GridPosition,
                fromPerspective.GridPosition);
            distance = Mathf.Max(1, distance); // Avoid division by zero
            threat += (1f / distance) * 5.0f;

            // Prey factor: low HP creatures are more threatening (predator instinct)
            float hpRatio = (float)target.CurrentHP / Mathf.Max(1, target.MaxHP);
            threat += (1f - hpRatio) * preyFactor;

            // Taunt effect: increase threat
            if (threatData.RoundsTaunted > 0)
            {
                threat += 50f; // Massive boost to ensure targeting
            }

            // Stealth effect: reduce threat to near-zero
            if (threatData.RoundsStealth > 0)
            {
                threat -= 200f; // Effectively nullifies all other threat
            }

            threat = Mathf.Max(0f, threat);
            return threat;
        }

        /// <summary>
        /// Get the highest-threat creature from a list.
        /// Used by wild AI to pick a target.
        /// </summary>
        public CreatureInstance GetHighestThreatTarget(
            List<CreatureInstance> potentialTargets,
            CreatureInstance fromPerspective,
            GridSystem grid,
            float damageDealtThisBattle,
            float preyFactor = 10f)
        {
            if (potentialTargets.Count == 0) return null;

            CreatureInstance highestThreat = potentialTargets[0];
            float maxThreat = GetThreatScore(highestThreat, fromPerspective, grid, damageDealtThisBattle, preyFactor);

            foreach (var target in potentialTargets)
            {
                if (target.IsFainted) continue;

                float score = GetThreatScore(target, fromPerspective, grid, damageDealtThisBattle, preyFactor);
                if (score > maxThreat)
                {
                    maxThreat = score;
                    highestThreat = target;
                }
            }

            return highestThreat;
        }

        /// <summary>
        /// Apply Taunt status to a target creature.
        /// Taunt lasts 2 rounds; taunted creatures are forced targets.
        /// </summary>
        public void ApplyTaunt(CreatureInstance target, int durationRounds = 2)
        {
            if (_threatScores.ContainsKey(target))
            {
                _threatScores[target].RoundsTaunted = durationRounds;
            }
        }

        /// <summary>
        /// Apply Stealth status to a target creature.
        /// Stealth lasts 1 round; stealthed creatures have near-zero threat.
        /// </summary>
        public void ApplyStealth(CreatureInstance target, int durationRounds = 1)
        {
            if (_threatScores.ContainsKey(target))
            {
                _threatScores[target].RoundsStealth = durationRounds;
            }
        }

        /// <summary>
        /// Record damage dealt by a creature this battle.
        /// Called after each attack resolves.
        /// </summary>
        public void RecordDamageDealt(CreatureInstance attacker, int damageAmount)
        {
            if (_threatScores.ContainsKey(attacker))
            {
                _threatScores[attacker].ThreatScore += damageAmount;
            }
        }

        /// <summary>
        /// Decrement all status durations (Taunt, Stealth).
        /// Called at end of round.
        /// </summary>
        public void DecrementDurations()
        {
            foreach (var data in _threatScores.Values)
            {
                if (data.RoundsTaunted > 0) data.RoundsTaunted--;
                if (data.RoundsStealth > 0) data.RoundsStealth--;
            }
        }

        /// <summary>
        /// Reset all threat scores. Called at start of new combat.
        /// </summary>
        public void ResetThreats()
        {
            foreach (var data in _threatScores.Values)
            {
                data.ThreatScore = 0f;
                data.RoundsTaunted = 0;
                data.RoundsStealth = 0;
            }
        }

        /// <summary>
        /// Get current threat score for debugging/UI.
        /// </summary>
        public float GetCurrentThreatScore(CreatureInstance creature)
        {
            return _threatScores.ContainsKey(creature) ? _threatScores[creature].ThreatScore : 0f;
        }
    }
}
```

### 3.2 Move Effects: Taunt & Stealth

**Taunt Move** (Status, range 3):
- Duration: 2 rounds
- Effect: Target is forced to attack the Taunt user for 2 turns (ignores normal threat calculation)
- Prevents status moves; target can only use damaging moves
- Implementation: `MoveEffectType.ApplyStatus(Taunt)` → ThreatManager.ApplyTaunt()

**Stealth Move** (Status, range 0 = self):
- Duration: 1 round
- Effect: User's threat score drops to near-zero; enemies largely ignore this creature
- Breaks on: User attacks (threat returns to normal), taking damage (threat returns to normal)
- Implementation: `MoveEffectType.ApplyStatus(Stealth)` → ThreatManager.ApplyStealth()

### 3.3 Threat Interaction with Combat

During `EnemyAction` phase in TurnManager:

```csharp
// In TurnManager.RunRoundAsync, EnemyAction phase:
var enemyOrder = GetInitiativeOrder(GetActiveEnemyCreatures(), _playerParty);
foreach (var creature in enemyOrder)
{
    if (creature.IsFainted) continue;
    
    // AI decides action: for wild creatures, use threat-based targeting
    var aiAction = aiSystem.DecideAction(
        creature,
        _playerParty,
        _grid,
        _threatManager); // Pass threat manager to AI
    
    await ExecuteActionAsync(aiAction);
    
    // Update threat scores after action resolves
    if (aiAction.Type == ActionType.UseMove && aiAction.Move.IsDamaging)
    {
        int damageDealt = ...; // Retrieved from combat result
        _threatManager.RecordDamageDealt(creature, damageDealt);
    }
}

// At end of round:
_threatManager.DecrementDurations();
```

### 3.4 AI Decision System Integration

The `IAIDecisionSystem` interface accepts a ThreatManager:

```csharp
public interface IAIDecisionSystem
{
    /// <summary>
    /// Decide an action for a creature.
    /// For wild creatures, use threat manager to pick targets.
    /// For trainer creatures, use personality weights.
    /// </summary>
    CombatAction DecideAction(
        CreatureInstance actor,
        List<CreatureInstance> opponents,
        GridSystem grid,
        ThreatManager threatManager = null);
}

// Example wild creature AI implementation:
public class WildCreatureAI : IAIDecisionSystem
{
    public CombatAction DecideAction(
        CreatureInstance actor,
        List<CreatureInstance> opponents,
        GridSystem grid,
        ThreatManager threatManager = null)
    {
        if (threatManager == null)
            return new CombatAction(ActionType.Wait, actor);

        // Get highest-threat target
        var target = threatManager.GetHighestThreatTarget(
            opponents,
            actor,
            grid,
            threatManager.GetCurrentThreatScore(actor),
            preyFactor: 10f);

        if (target == null)
            return new CombatAction(ActionType.Wait, actor);

        // Pick a move (simplified: first damaging move)
        var moveId = actor.LearnedMoveIds.FirstOrDefault();
        if (moveId == null)
            return new CombatAction(ActionType.Wait, actor);

        var moveConfig = ConfigLoader.GetMove(moveId);
        return new CombatAction(
            ActionType.UseMove,
            actor,
            moveConfig,
            null,
            target,
            0); // Move slot 0
    }
}

// Trainer creature AI would ignore threat and use personality instead:
public class TrainerCreatureAI : IAIDecisionSystem
{
    public CombatAction DecideAction(
        CreatureInstance actor,
        List<CreatureInstance> opponents,
        GridSystem grid,
        ThreatManager threatManager = null)
    {
        // Personality-based targeting:
        // Aggressive: pick highest ATK opponent
        // Cautious: pick weakest opponent (to eliminate quickly)
        // Territorial: pick closest opponent
        // etc.
        
        CreatureInstance target = actor.Personality switch
        {
            PersonalityTrait.Aggressive => opponents.OrderByDescending(o => o.ComputedStats.ATK).FirstOrDefault(),
            PersonalityTrait.Cautious => opponents.OrderBy(o => o.CurrentHP).FirstOrDefault(),
            _ => opponents.OrderBy(o => GridSystem.ChebyshevDistance(actor.GridPosition, o.GridPosition)).FirstOrDefault(),
        };

        if (target == null)
            return new CombatAction(ActionType.Wait, actor);

        var moveId = actor.LearnedMoveIds.FirstOrDefault();
        if (moveId == null)
            return new CombatAction(ActionType.Wait, actor);

        var moveConfig = ConfigLoader.GetMove(moveId);
        return new CombatAction(
            ActionType.UseMove,
            actor,
            moveConfig,
            null,
            target,
            0);
    }
}
```

## 4. Formulas

| Formula | Expression | Notes |
|---------|-----------|-------|
| Base threat | `damageDealtThisBattle × 1.0` | Accumulates over battle |
| Proximity threat | `(1 / distance) × 5.0` | Closer = higher threat |
| Prey factor | `(1 - currentHP/maxHP) × 10` | Low HP = higher threat (predator instinct) |
| Taunt boost | `+50` | Forces targeting |
| Stealth reduction | `-200` | Nullifies all other threat |
| Final threat | `base + proximity + prey - taunt/stealth effects` | Clamped to 0 minimum |

## 5. Edge Cases

| Situation | Behavior |
|-----------|----------|
| All player creatures have 0 threat | Wild creatures pick randomly (or pick closest by distance) |
| Target creature taunted while stealthed | Taunt overwrites stealth effect; target becomes high-threat |
| Stealth expires during creature's turn | Threat returns to normal; on next enemy action, threat recalculates |
| Creature faints while taunted | Threat duration decrements but has no effect (creature can't be attacked) |
| Attacker records damage against creature that faints same round | Damage is recorded and contributes to threat if creature revives (post-MVP) |
| Distance to target is 0 (same tile) | Distance clamped to 1; no division by zero |
| Multiple creatures with identical threat score | Tiebreak by SPD (via GetInitiativeOrder in TurnManager) |
| Threat recorded for non-damaging move | Damage is 0; threat unchanged |
| Taunt duration set to 0 explicitly | Creature no longer taunted; threat returns to normal calculation |
| Stealth applied but creature doesn't move | Threat remains near-zero next round until stealth expires or creature attacks |

## 6. Dependencies

| Dependency | Direction | Notes |
|------------|-----------|-------|
| `CreatureInstance` | Inbound | HP, position, moves, personality read |
| `GridSystem` | Inbound | Distance calculation via ChebyshevDistance |
| `MoveConfig` | Inbound | Move effects checked for Taunt/Stealth |
| `TurnManager` | Outbound | ThreatManager instantiated; called after actions resolve |
| `IAIDecisionSystem` | Outbound | Threat scores provided to AI for decision-making |
| `StatusEffect` enum | Inbound | Taunt/Stealth status effects |
| `ConfigLoader` | Inbound | Move configs loaded for AI decision support |

## 7. Tuning Knobs

| Parameter | Location | Default | Notes |
|-----------|----------|---------|-------|
| Damage threat weight | Formula | 1.0× | Damage dealt = threat gained |
| Proximity multiplier | Formula | 5.0× | Scales (1/distance) threat |
| Prey factor | GetThreatScore | 10.0 | Low HP = higher threat |
| Taunt threat boost | ApplyTaunt | +50 | Makes target dominant threat |
| Stealth threat reduction | ApplyStealth | -200 | Effectively hides creature |
| Taunt duration | ApplyTaunt | 2 rounds | How long creature is forced to attack |
| Stealth duration | ApplyStealth | 1 round | How long creature is hidden |
| Distance weight in AI | `GetHighestThreatTarget` | Integrated | Affects target priority |
| Wild creature aggression | Design | Default | All wild creatures use threat-based AI |
| Trainer creature strategy | Design | Personality-based | Varies per personality type |

## 8. Acceptance Criteria

- [ ] Threat score increases when creature deals damage
- [ ] Threat score increases for creatures closer to attacker
- [ ] Threat score increases for creatures with low HP
- [ ] Taunted creature has threat score +50 (becomes priority target)
- [ ] Stealthed creature has threat score near-zero
- [ ] GetHighestThreatTarget returns creature with highest threat
- [ ] Taunt duration decrements each round
- [ ] Stealth duration decrements each round
- [ ] Taunt expires after 2 rounds; creature returns to normal threat
- [ ] Stealth expires after 1 round; creature returns to normal threat
- [ ] ResetThreats() clears all threat data at start of new combat
- [ ] Wild AI uses ThreatManager to pick targets
- [ ] Trainer AI uses personality weights (ignores threat)
- [ ] Taunted creature cannot use status moves (enforced by status effect application)
- [ ] Stealthed creature threat breaks if creature attacks (threat recalculates)
- [ ] No division by zero when distance is 0
- [ ] EditMode test: threat formula produces expected scores for known inputs
- [ ] PlayMode test: wild creature prioritizes high-threat player creature

