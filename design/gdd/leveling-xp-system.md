# Leveling / XP System

## 1. Overview

The Leveling and XP System tracks creature growth from level 1 to level 50. Creatures earn XP by defeating wild creatures, winning trainer battles, and capturing targets. XP is shared across the active party with full shares going to participants and half shares to bench creatures. Each level-up increases stats according to per-species growth curves. Moves are learned automatically at level thresholds defined in the creature's move pool. No evolution exists — DNA alteration is the parallel growth vector, operating alongside levels rather than replacing them.

## 2. Player Fantasy

Watching a creature you've invested in grow stronger through real battles feels earned. The XP share system means your bench never falls hopelessly behind, so swapping in a new capture doesn't feel punishing. Learning a powerful new move at a specific level gives you a clear milestone to look forward to — "three more fights and Emberfox learns Inferno Burst." Leveling and DNA alteration are complementary: levels give reliable, predictable growth; DNA gives wild, customizable jumps.

## 3. Detailed Rules

### 3.1 XP Sources

| Source | Base XP Formula | Notes |
|--------|----------------|-------|
| Defeating a wild creature | `baseXP * levelDiffModifier * participationShare` | See formulas |
| Capturing a wild creature | `flatCaptureBonus + (rarityBonus * creatureRarity)` | Bonus on top of any defeat XP |
| Winning a trainer battle | Full party shares `trainerBattleXpPool` evenly after win | Pool = sum of all defeated trainer creatures' XP values |
| Horde encounter clear | XP per creature defeated individually, then a `hordeClearBonus` | |

### 3.2 Level Difference Modifier

The modifier scales XP based on the level gap between the player's creature and the defeated creature.

| Level Difference (player - enemy) | Modifier |
|-----------------------------------|----------|
| Enemy is 6+ levels higher | 2.0 |
| Enemy is 3–5 levels higher | 1.5 |
| Within ±2 levels | 1.0 |
| Player is 3–5 levels higher | 0.7 |
| Player is 6–10 levels higher | 0.4 |
| Player is 11+ levels higher | 0.1 |

### 3.3 Participation Share

At the end of each battle, the XP pool is distributed:

- **Participants** (acted at least once during the battle): receive 100% of their share.
- **Non-participants** (in party but never switched in): receive 50% of their share.
- **Stored creatures**: receive 0% XP.

XP is divided equally among all active party members, then the participation modifier is applied per creature.

```csharp
public static Dictionary<CreatureInstance, int> CalculateXpDistribution(
    int rawXp,
    List<CreatureInstance> activeParty,
    HashSet<CreatureInstance> participants)
{
    float baseShare = rawXp / (float)activeParty.Count;
    var result = new Dictionary<CreatureInstance, int>();

    foreach (var creature in activeParty)
    {
        float share = participants.Contains(creature) ? baseShare : baseShare * 0.5f;
        result[creature] = Mathf.FloorToInt(share);
    }
    return result;
}
```

### 3.4 Capture XP Bonus

On successful capture, the capturing creature and all participants receive a flat bonus plus a rarity bonus. The defeated creature's XP is also awarded normally.

```csharp
public static int CalculateCaptureBonus(DnaRarity capturedRarity)
{
    return capturedRarity switch
    {
        DnaRarity.Common    => 50,
        DnaRarity.Uncommon  => 120,
        DnaRarity.Rare      => 280,
        DnaRarity.Legendary => 600,
        _ => 50
    };
}
```

### 3.5 Level-Up Stat Growth

Each creature species has a `StatGrowthCurve` — a per-stat array of multipliers indexed by level. At each level-up, the stat is recalculated from the base stat and the new level's growth curve value.

```csharp
[System.Serializable]
public class StatGrowthCurve
{
    public AnimationCurve hpCurve;
    public AnimationCurve atkCurve;
    public AnimationCurve defCurve;
    public AnimationCurve spdCurve;
}

public static int CalculateStat(int baseStat, AnimationCurve growthCurve, int level)
{
    // growthCurve is defined from level 1 (value ~1.0) to level 50 (value ~2.5-4.0)
    float multiplier = growthCurve.Evaluate(level);
    return Mathf.FloorToInt(baseStat * multiplier);
}
```

Growth curve shapes (per-species archetype defaults):

| Archetype | Stat Focus | HP Curve | ATK Curve | SPD Curve |
|-----------|-----------|----------|-----------|-----------|
| Tank | DEF, HP | Steep | Shallow | Flat |
| Attacker | ATK | Moderate | Steep | Moderate |
| Speed | SPD, EVA | Shallow | Moderate | Steep |
| Balanced | Even | Moderate | Moderate | Moderate |

The `StatGrowthCurve` is defined on `CreatureConfig` as a ScriptableObject field.

### 3.6 Move Learning

Each species has a `MovePool` — an ordered list of `MoveLearnEntry` records:

```csharp
[System.Serializable]
public class MoveLearnEntry
{
    public int learnLevel;     // Level at which this move is offered
    public string moveId;      // References MoveConfig
}
```

At level-up, if the new level matches a `learnLevel`, the player is prompted to learn the move:
- If the creature has fewer than 4 moves: move is added automatically.
- If the creature has 4 moves: player chooses to learn the new move (replacing an existing one) or skip.
- Skipped moves can be re-learned at any research station via the Move Reminder service.

### 3.7 Level Cap and XP at Max Level

The level cap is 50. A creature at level 50 still receives XP from battles (for display purposes and morale), but no stat gains or new move prompts occur. XP beyond level 50 is discarded.

### 3.8 Level-Up Flow

1. XP added to creature.
2. While `currentXp >= xpToNextLevel` and `level < MaxLevel`:
   a. Subtract `xpToNextLevel` from `currentXp`.
   b. Increment `level`.
   c. Recalculate all stats via `CalculateStat()`.
   d. Check `MovePool` for any `learnLevel == level` entries.
   e. Queue move-learn prompt if applicable.
3. If multiple levels gained at once, all stat calculations run before move prompts are shown.
4. Level-up visual notification shown in battle (if mid-battle) or in the XP summary screen (post-battle).

## 4. Formulas

### XP from Defeating a Wild Creature

```
rawXP = creatureConfig.baseXpYield * levelDiffModifier
xpPerPartyMember = Floor(rawXP / activePartyCount)
participantXP = xpPerPartyMember * 1.0
benchXP = xpPerPartyMember * 0.5
```

### XP to Next Level

```
xpToNextLevel(level) = Floor(0.8 * level^3 + 10 * level + 50)
```

Sample values:
| Level | XP to Next |
|-------|-----------|
| 1 | 61 |
| 5 | 150 |
| 10 | 910 |
| 20 | 6,658 |
| 30 | 22,406 |
| 49 | 96,070 |

### Stat at Level N

```
stat(level) = Floor(baseStat * growthCurve.Evaluate(level))
```

### Trainer Battle XP Pool

```
trainerBattleXpPool = Sum(creature.baseXpYield * creature.level * TrainerXpMultiplier
                          for creature in defeatedTrainerCreatures)
TrainerXpMultiplier = 1.5    // Trainer battles yield more XP than wild encounters
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Multiple level-ups from a single XP award | Process levels sequentially; all stat gains applied; move prompts queued and shown after final level is resolved |
| Move learn prompt skipped; player later wants the move | Re-learn available at research station Move Reminder for a small RP cost (default 25 RP) |
| Creature gains XP in a battle it did not participate in (bench) | Bench share (50%) applied; creature still cannot gain levels beyond their participation level (no restriction — bench creatures level normally, just slower) |
| Party has only 1 creature (solo run) | 100% of XP goes to that creature; no division |
| XP awarded to a fainted creature | Fainted creatures still receive their XP share (they participated before fainting); revived with those levels applied |
| Captured creature's level is higher than the player's lead creature | No restriction; creature joins at its existing level and gains XP normally |
| Level 50 creature in party | XP distributed normally; level 50 creature receives 0 effective gain; XP discarded |
| All party creatures are level 50 | No XP is meaningful; distributed and discarded silently |
| Trainer battle ends mid-way (player flees) | No XP awarded from that battle |
| Move pool entry references a move ID that doesn't exist in Move Database | Skip that learn entry silently; log error to developer console; do not crash |

## 6. Dependencies

| System | Dependency Type | Notes |
|--------|----------------|-------|
| Creature Instance | Read/Write | Current level, currentXp, stat values |
| Creature Database | Read | `baseXpYield`, `StatGrowthCurve`, `MovePool` per species |
| Move Database | Read | Validates move IDs in `MoveLearnEntry` |
| Party System | Read | Active party list, participation tracking |
| Encounter System | Read | Battle outcome, defeated creature data |
| Capture System | Read | Successful capture triggers bonus XP |
| Combat UI | Write | Level-up notification, move-learn prompt |
| Party Management UI | Write | Move Reminder service at research station |
| Save/Load System | Read/Write | Level, currentXp, and learned moves persisted |

## 7. Tuning Knobs

| Parameter | Default | Range | Notes |
|-----------|---------|-------|-------|
| `MaxLevel` | 50 | 30–99 | Level cap |
| `BenchXpFraction` | 0.50 | 0.0–1.0 | Fraction of share bench creatures receive |
| `TrainerXpMultiplier` | 1.5 | 1.0–3.0 | XP bonus for trainer battle creatures |
| `HordeClearBonus` | 200 | 0–500 | Flat XP bonus for clearing a horde encounter |
| `CaptureBonus` (Common) | 50 | 0–200 | |
| `CaptureBonus` (Uncommon) | 120 | 0–400 | |
| `CaptureBonus` (Rare) | 280 | 0–800 | |
| `CaptureBonus` (Legendary) | 600 | 0–2000 | |
| `MoveReminderCost` | 25 | 0–100 | RP cost to re-learn a forgotten move |
| XP curve coefficients | 0.8, 10, 50 | Tunable | Used in `xpToNextLevel` formula |
| `LevelDiffModifier` table | See Section 3.2 | Per-row tunable | All 6 rows configurable |

## 8. Acceptance Criteria

- [ ] `CalculateStat()` returns correct values for known baseStat + curve + level inputs (unit tested).
- [ ] `xpToNextLevel()` formula produces the correct values for levels 1, 10, 20, 30, and 49 (unit tested).
- [ ] `CalculateXpDistribution()` assigns 100% share to participants and 50% to bench members (unit tested).
- [ ] Level difference modifier applies the correct multiplier across all 6 tier boundaries (unit tested).
- [ ] Capture XP bonus is correctly awarded in addition to defeat XP on a successful capture.
- [ ] Multiple level-ups from a single award process all levels before showing move prompts.
- [ ] Move-learn prompt appears at the correct level threshold; auto-adds if fewer than 4 moves.
- [ ] If creature has 4 moves, player is asked to replace or skip; skip does not lose the move permanently.
- [ ] Skipped moves appear in the Move Reminder list at research stations.
- [ ] Fainted creatures receive their XP share and level up correctly when revived.
- [ ] Level 50 creatures receive no stat gain; XP is discarded silently.
- [ ] All XP and level data persists through a save/load cycle.
- [ ] Stat recalculations after level-up account for equipped part bonuses on top of the base growth.
