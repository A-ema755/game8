# Move Customization System

## 1. Overview

The Move Customization System allows DNA alterations and equipped body parts to modify a creature's moves beyond their base definitions. Type infusion splices can graft a second type onto a move, creating dual-type hybrid variants with new names (e.g., Flamethrower becomes "Frostflame" after Ice DNA infusion). Body parts add passive bonuses to basic attacks or unlock move variants. Creatures at 50+ instability gain random bonus effects on move resolution. Moves used 50+ times can "master evolve" into a permanently stronger version at the player's choice. All active modifications are tracked per move in a `MoveModification` record and displayed in the move details panel.

## 2. Player Fantasy

Your creature's moves are not fixed — they are a second layer of build expression on top of stats and parts. Turning a Fire creature's signature Flamethrower into a dual-type ice-fire beam feels like a mad science moment. Watching Venom Glands silently add a poison proc to every basic attack makes your creature feel bespoke. When a move you've been spamming for 50 battles suddenly offers to evolve into something more powerful, it rewards your investment in a concrete, visible way.

## 3. Detailed Rules

### 3.1 Type Infusion on Moves

When a Type Infusion DNA mod is applied to a creature, the player may optionally assign it to one of the creature's known moves. If assigned:

- The move gains a second type alongside its original type.
- Damage is calculated using the more favorable type effectiveness between the two types.
- The move's display name changes to the `infusedName` defined in the `MoveModification` (or auto-generated as `[TypePrefix][OriginalName]` if no override is set).
- The move's visual effect color blends between the two type colors.

A single type infusion can be assigned to only one move at a time. Removing the infusion DNA mod removes the modification from the move.

```csharp
public class MoveModification
{
    public string baseMoveId;            // Original move this modification applies to
    public string infusedTypeName;       // e.g. "Ice" — the grafted type
    public string displayNameOverride;   // e.g. "Frostflame"; empty = auto-generate
    public bool isDualType;              // True when type infusion is active
    public List<PartMoveBonus> partBonuses;     // Active bonuses from equipped parts
    public InstabilityEffectSet instabilityEffects; // Active at 50+ instability
    public int usageCount;               // Lifetime uses of this move
    public bool isMasterEvolved;         // True after move evolution confirmed
    public string masterEvolvedMoveId;   // Replaces base move on confirmation
}
```

Type effectiveness when dual-type: the system calls `TypeChart.GetMultiplier()` for both types against the target's type(s) and uses the higher result.

### 3.2 Part-Based Move Modifications

Certain body parts add passive bonuses to moves while equipped. These bonuses apply automatically and are removed when the part is unequipped.

| Part | Move Affected | Bonus |
|------|--------------|-------|
| `glands-venom` | Basic attack | +20% chance to inflict Poison on hit |
| `claws-venom` | Basic attack | +20% chance to inflict Poison on hit (stacks additively with Venom Glands, max 35%) |
| `wings-feathered` | Any move used after moving to elevated tile | +10% damage |
| `aura-flame` | Contact moves | Attacker takes 10% reflected damage (burns on contact) |
| `tail-blade` | Physical moves | +5% crit chance |
| `horns-crystal` | Special moves | +5% crit chance |
| `eyes-predator` | All moves | +10 flat accuracy bonus |
| `shell-carapace` | — | No move bonus; stat-only part |

Part bonuses are additive. The cap on stacked poison proc chance from parts is 35%.

```csharp
[System.Serializable]
public class PartMoveBonus
{
    public string sourcePartId;
    public MoveModType bonusType;     // PoisonChance, CritBonus, DamageBonus, AccuracyBonus
    public float bonusValue;
    public MoveFilter affectedMoves;  // BasicAttack, PhysicalMoves, SpecialMoves, AllMoves
}
```

### 3.3 Instability Effects (50+ Instability)

When a creature's instability is 50 or higher, each move resolution rolls against `volatileEffectChance` (default 0.15). On a trigger, one effect from the Volatile Effects table is applied randomly (equal weight unless overridden in `GameSettings`):

| Roll | Effect | Description |
|------|--------|-------------|
| 0 | Crit Surge | +25% crit chance for this attack resolution |
| 1 | Splash Damage | Deals 50% of move damage to all creatures on tiles adjacent to target |
| 2 | Status Proc | Inflicts a random status effect (Poison, Paralysis, or Burn) on the target |
| 3 | Power Boost | Move base power increased by 20% for this resolution |

These effects are rolled per move use, not per turn. A creature using two moves in one turn (e.g. via a combo) rolls twice independently.

The `InstabilityEffectSet` tracks which effects are possible for a given creature based on its mods:

```csharp
[System.Serializable]
public class InstabilityEffectSet
{
    public bool critSurgeEnabled    = true;
    public bool splashDamageEnabled = true;
    public bool statusProcEnabled   = true;
    public bool powerBoostEnabled   = true;
    // Post-MVP: specific mods can disable certain effects
}
```

### 3.4 Move Mastery and Evolution

Each `MoveModification` tracks `usageCount` — how many times the creature has used that move in battle. When `usageCount` reaches `masteryThreshold` (default 50):

1. The move details panel shows a "Mastery Reached" notification.
2. The player is presented with a choice:
   - **Evolve**: Replace the move with its `masterEvolvedMoveId` version (higher power, reduced PP cost, or enhanced effect). Irreversible.
   - **Keep**: Retain the original move. `usageCount` continues tracking but no further evolution prompt is shown.
3. If the player has not yet decided, the prompt reappears each time they open the creature's move panel.

The `masterEvolvedMoveId` is defined in the base `MoveConfig` and points to a separate MoveConfig asset for the evolved version. Not all moves have an evolved version — `masterEvolvedMoveId` is empty for non-evolvable moves.

```csharp
/// <summary>
/// Checks whether a move has reached mastery threshold and has an evolution available.
/// </summary>
public static bool IsMasteryEvolutionAvailable(MoveModification mod, MoveConfig baseMoveConfig)
{
    return mod.usageCount >= GameSettings.Instance.masteryThreshold
        && !mod.isMasterEvolved
        && !string.IsNullOrEmpty(baseMoveConfig.masterEvolvedMoveId);
}
```

### 3.5 Move Details Panel Display

The move details panel in Party Management UI shows a move's full modification stack:

```
[Move Name / Evolved Name]
Type: Fire / Ice (dual)          ← infused type shown if active
Power: 90  Accuracy: 100  PP: 10
─────────────────────────────────
DNA Mods Active:
  + Ice Infusion → Frostflame (dual Fire/Ice)
Part Bonuses Active:
  + Tail Blade: +5% crit chance
Instability Effects (at 50+):
  Volatile: crit surge, splash, status, power boost
Mastery: 37/50 uses
```

If no modifications are active, the panel shows "No active DNA modifications."

### 3.6 Modification Persistence

`MoveModification` records are stored on `CreatureInstance` in a `Dictionary<string, MoveModification>` keyed by `baseMoveId`. The dictionary is serialized to JSON as part of the save file. If a move is forgotten (replaced at 4-move cap), its `MoveModification` record is retained in a separate `suspendedModifications` list and reactivated if the move is re-learned via Move Reminder.

## 4. Formulas

### Dual-Type Damage Effectiveness

```
effectiveness = Max(TypeChart.GetMultiplier(type1, targetType),
                    TypeChart.GetMultiplier(type2, targetType))
```

### Poison Proc Stacking (Parts)

```
poisonChance = Sum(part.poisonProcBonus for all equipped parts with poison bonus)
poisonChance = Min(poisonChance, maxPoisonProcCap)   // maxPoisonProcCap = 0.35
```

### Volatile Effect Trigger

```
triggerRoll = Random[0, 1)
if triggerRoll < volatileEffectChance (0.15):
    effectIndex = Random.Range(0, enabledEffectCount)
    apply effect[effectIndex]
```

### Mastery Check

```
masteryReached = (usageCount >= masteryThreshold) && !isMasterEvolved && masterEvolvedMoveId != ""
masteryThreshold = 50   // Tunable in GameSettings
```

### Evolved Move PP

```
evolvedMovePP = Floor(baseMoveConfig.pp * 0.75)   // Evolved moves cost 25% less PP
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Type infusion applied but player doesn't assign it to a move | Infusion stays unassigned; no move is modified; player can assign it later at a research station |
| Infusion removed while move is mid-battle (theoretically) | Infusion mods only apply/remove at research stations; cannot change mid-battle |
| Dual-type move: both types are equally effective | Either value is valid; use the first type's result (deterministic) |
| Dual-type move: one type is immune (0x), other is super-effective (2x) | Use the higher (2x); immunity of one type does not override the other |
| Move evolution chosen; creature then learns new move replacing the evolved one | Evolved move ID is forgotten; `isMasterEvolved = true` persists on the modification but the move is gone; Move Reminder can restore it at the evolved tier |
| `masteryThreshold` reached but `masterEvolvedMoveId` is empty | No evolution prompt shown; `usageCount` still tracked for display |
| Part unequipped mid-session that provided a PartMoveBonus | Bonus removed immediately at unequip; `MoveModification.partBonuses` list updated |
| Instability drops below 50 (via stabilizer item, post-MVP) | Volatile effects deactivate; `instabilityEffects` remain in record for when instability rises again |
| Creature at 50+ instability uses a status move (no damage) | Volatile effects that require a damage target (splash, power boost) are skipped; only crit surge and status proc are eligible |
| Two type infusion mods both want to apply to the same move | Only one infusion can be assigned per move; second assignment replaces the first with a player confirmation prompt |

## 6. Dependencies

| System | Dependency Type | Notes |
|--------|----------------|-------|
| Move Database | Read | Base move power, type, PP, `masterEvolvedMoveId` |
| DNA Alteration System | Read/Write | Type infusion mods trigger modification creation; instability drives volatile effects |
| Body Part System | Read | Equipped parts determine active `partBonuses` |
| Type Chart System | Read | Dual-type effectiveness resolution |
| Damage & Health System | Read | Power boost and splash damage route through standard damage calculation |
| Creature Instance | Read/Write | `usageCount`, `isMasterEvolved`, instability value |
| Turn Manager | Read | Move use event increments `usageCount` |
| Party Management UI | Read | Displays `MoveModification` stack in move details panel |
| Save/Load System | Read/Write | Full `MoveModification` dictionary persisted per creature |

## 7. Tuning Knobs

| Parameter | Default | Range | Notes |
|-----------|---------|-------|-------|
| `volatileEffectChance` | 0.15 | 0.05–0.35 | Probability of volatile effect at 50+ instability |
| `masteryThreshold` | 50 | 20–100 | Move uses required for evolution prompt |
| `evolvedMovePpFraction` | 0.75 | 0.5–1.0 | PP cost of evolved move as fraction of base |
| `maxPoisonProcCap` | 0.35 | 0.2–0.5 | Stacked poison proc ceiling from parts |
| `splashDamageFraction` | 0.50 | 0.25–0.75 | Fraction of move damage dealt as splash |
| `powerBoostFraction` | 0.20 | 0.10–0.50 | Power boost percentage from volatile effect |
| `critSurgeBonus` | 0.25 | 0.10–0.50 | Additional crit chance from volatile effect |
| Volatile effect weights | Equal (0.25 each) | Per-effect tunable | Relative probability per effect type |

## 8. Acceptance Criteria

- [ ] Applying an Ice type infusion to Flamethrower renames it "Frostflame" (or auto-generated name) and shows dual Fire/Ice type badges in the move panel.
- [ ] Dual-type damage uses the higher effectiveness value; single-type immunity does not block the more favorable type.
- [ ] `glands-venom` equipped adds a 20% poison proc to basic attacks; visible in the move details panel.
- [ ] Two poison-proc parts stack to ≤35%; capped correctly.
- [ ] At 50+ instability, volatile effects trigger at approximately 15% rate over 1000 move uses (within ±3%).
- [ ] All four volatile effect types can trigger; splash damage routes through the damage system and hits adjacent tiles.
- [ ] `usageCount` increments correctly on each move use; persists through save/load.
- [ ] Mastery prompt appears when `usageCount >= 50` and `masterEvolvedMoveId` is set.
- [ ] Choosing "Evolve" replaces the move with the evolved version and sets `isMasterEvolved = true`; cannot be reversed.
- [ ] Choosing "Keep" dismisses the prompt; no evolution occurs; prompt reappears on next panel open.
- [ ] Removing a type infusion mod reverts the move name and removes dual-type behavior.
- [ ] Move details panel correctly shows all active modifications in layered format.
- [ ] `MoveModification` data survives a save/load cycle with no data loss.
- [ ] Move with no active modifications shows "No active DNA modifications" in the panel.
