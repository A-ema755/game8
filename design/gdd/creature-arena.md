# Creature Arena

## 1. Overview

The Creature Arena is an endgame battle tower with escalating floor-based challenges. Every 5 floors features a boss encounter with unique AI and exclusive part drops. Themed floor modifiers add rule variations (no items, double speed, one creature only, type-restricted, no DNA mods). Players can tackle floors in any order but progression unlocks sequentially. Weekly rotations (post-MVP) change modifier combinations, encouraging replays and leaderboard competition.

## 2. Player Fantasy

Climbing the arena feels like proving mastery. A "no DNA mods" floor forces players to rely on pure strategy and party building. Discovering a new rule modifier (double speed) changes how team comp works — maybe your slow but tanky creature becomes a liability. Arena bosses are genuine threats with personality and unique moves. Leaderboards show fastest times, adding replay value.

## 3. Detailed Rules

### 3.1 Floor Structure

| Floor | Type | Rule Modifier | Opponent | Reward |
|-------|------|---------------|----------|--------|
| 1–4 | Regular | None | Trainer (level 20) | RP + experience |
| 5 | Boss | None | Arena Boss 1 | Exclusive part + RP |
| 6–9 | Regular | No Items | Trainer (level 25) | RP |
| 10 | Boss | Double Speed | Arena Boss 2 | Exclusive part + RP |
| 11–14 | Regular | One Creature Only | Trainer (level 28) | RP |
| 15 | Boss | Type Restricted | Arena Boss 3 | Exclusive part + RP |
| 16–19 | Regular | No DNA Mods | Trainer (level 32) | RP |
| 20 | Boss | Combined Modifiers | Final Boss | Legendary part + RP |

### 3.2 ArenaFloorConfig

```csharp
[CreateAssetMenu(menuName = "GeneForge/ArenaFloorConfig")]
public class ArenaFloorConfig : ScriptableObject
{
    [System.Serializable]
    public class FloorData
    {
        public int floorNumber;
        public string opponentId;
        public int opponentLevel;
        public List<ArenaRuleModifier> modifiers;
        public bool isBossFloor;
        public string exclusivePartId;
        public int rpReward = 100;
    }
    
    public List<FloorData> floors;
}

public enum ArenaRuleModifier
{
    NoItems,
    DoubleSpeed,
    OneCreatureOnly,
    TypeRestricted,
    NoDnaMods,
    HalfStats
}
```

### 3.3 Boss Encounters

Arena bosses are fixed encounters with unique teams:

```csharp
[System.Serializable]
public class ArenaBoss
{
    public string bossId;
    public string displayName;
    public List<CreatureInstance> team;
    public string uniqueAbility;           // Boss-exclusive move or perk
    public int level;
    public string exclusivePartReward;
}
```

Boss team examples:
- **Arena Boss 1 (Fire-focused):** Emberfox (Lv30), Cinder Dragon (Lv28), Magma Turtle (Lv26)
- **Final Boss:** Diverse team of Lv35+ creatures with legendary parts

### 3.4 Rule Modifiers in Practice

**No Items:** Cannot use single-use items (potions, traps, battle items) during floor. DNA modifications still work.

**Double Speed:** All creatures' SPD stats doubled; turn order changes dramatically.

**One Creature Only:** Can only bring 1 creature; no swapping allowed. That one creature must handle all opponents.

**Type Restricted:** Cannot use creatures of a specific type (e.g., no Fire-type on Floor 15). Chosen randomly or per floor.

**No DNA Mods:** Creature stat boosts and perks from DNA alterations are disabled (innate stats only).

### 3.5 Progression & Leaderboards

```csharp
public class ArenaManager
{
    public int CurrentFloor { get; set; } = 1;
    public int MaxFloorUnlocked { get; set; } = 1;
    
    public void CompleteFloor(int floorNumber)
    {
        var timeElapsed = GetBattleTime();
        var leaderboardEntry = new LeaderboardEntry
        {
            playerName = gameState.playerName,
            floor = floorNumber,
            timeSeconds = timeElapsed,
            timestamp = System.DateTime.Now
        };
        
        SaveLeaderboardEntry(leaderboardEntry);
        MaxFloorUnlocked = Math.Max(MaxFloorUnlocked, floorNumber + 1);
    }
}
```

Leaderboards track:
- **Floors Cleared:** Highest floor reached
- **Fastest Times:** Per-floor speed running records
- **Boss Defeats:** Which bosses beaten

Post-MVP: Weekly rotation resets leaderboards.

## 4. Formulas

### Floor Difficulty Scaling

```
opponentLevel = 20 + (floor - 1) * 0.5
```

### RP Reward

```
baseRP = 100
bossFloorBonus = 1.5x
ruleModifierBonus = 1.2x per modifier
totalRP = baseRP * (bossFloorBonus?) * (ruleModifierBonus?)
```

### Speed Modifier Effect

```
doubleSpeedModifier = 2.0 * baseSPD
turnOrderRecalculated = true
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Player clears floor 10 (boss) but doesn't unlock floor 11 yet | Floors 11+ are locked; only completed floors and next floor available |
| Floor has "No DNA Mods" and creature relies on mod-based moveset | Move is disabled/greyed out; creature uses base moveset only |
| Type-restricted floor restricts player's only remaining creature type | Floor is skipped; player can attempt different floor or come back later |
| One Creature Only floor; that creature faints | Battle lost; arena run ends |
| Player reaches Floor 20 (Final Boss); loses | Can retry immediately or leave arena; progress stays at Floor 20 |
| Boss gives exclusive part but player inventory is full | Part is held in "pending rewards" and claimed after freeing space |
| Weekly rotation happens mid-run (post-MVP) | Current run completes under old modifier set; next run uses new set |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Encounter System | Boss and trainer encounters |
| Creature Instance | Creature data for opponents |
| Body Part System | Exclusive parts as rewards |
| DNA Alteration System | Rule modifiers disable mods when needed |
| Turn Manager | Implements speed modifier effects |
| Save/Load System | Persists floor progress and leaderboard entries |
| UI Shell | Arena menu, floor selection, leaderboard display |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `floorsPerBoss` | 5 | Boss appears every 5 floors |
| `baseFloorLevel` | 20 | Starting floor opponent level |
| `levelPerFloor` | 0.5 | Level increment per floor |
| `baseFloorRP` | 100 | Base RP per floor |
| `bossRPMultiplier` | 1.5 | Boss floor RP bonus |
| `ruleModifierRPBonus` | 1.2 | Per-modifier RP bonus |
| `speedModifierMultiplier` | 2.0 | SPD x2 for double-speed floors |
| `maxFloors` | 20+ | Can expand post-MVP |

## 8. Acceptance Criteria

- [ ] Arena floors are accessible from a hub location.
- [ ] Floors progress sequentially; next floor unlocks after completing previous.
- [ ] Bosses appear every 5 floors with unique teams and dialogue.
- [ ] Exclusive parts drop from boss encounters and appear in rewards panel.
- [ ] Rule modifiers (No Items, Double Speed, etc.) apply correctly per floor.
- [ ] One Creature Only floor enforces single-creature battles.
- [ ] Type-Restricted floor prevents use of restricted type.
- [ ] No DNA Mods floor disables stat bonuses and DNA-unlocked moves.
- [ ] RP rewards scale with floor difficulty and rule modifiers.
- [ ] Leaderboards display for floors cleared and fastest times.
- [ ] Floor progress persists through save/load.
- [ ] Boss defeats are tracked and celebrated in leaderboard.
