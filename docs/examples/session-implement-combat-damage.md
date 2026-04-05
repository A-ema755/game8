# Example Session: Implementing Combat Damage Formula

**Session Type:** Implementation
**Agent:** gameplay-programmer
**System:** Combat Damage Calculation
**GDD Reference:** `design/gdd/combat-damage.md`
**Output Files:**
- `Assets/Scripts/Combat/DamageCalculator.cs`
- `Assets/Scripts/Combat/CombatConfig.cs`
- `Assets/Resources/Data/CombatSettings.asset`
- `Assets/Scripts/Creatures/CreatureStats.cs`
- `Assets/Scripts/Creatures/AbilityData.cs`
- `tests/EditMode/Combat/DamageCalculatorTests.cs`
- `design/gdd/combat-damage.md` (updated with implementation notes)

---

## Context

Gene Forge uses a turn-based combat system on a height-variable isometric grid. Damage calculation must account for creature base stats, DNA-derived modifiers, type effectiveness (the type chart), ability power, height advantage, and status conditions. This session walks through implementing the damage formula from a partially specified GDD.

---

## Session Transcript

**User:**
```
The combat-damage GDD has a formula but I need to implement it. Here's the core:

  damage = (attacker.attack * ability.power * type_modifier) / defender.defense

But I haven't decided a few things yet. Can you implement this?
```

**Gameplay-Programmer:**
```
I've read design/gdd/combat-damage.md. Before I write any code I need to clarify
seven gaps that will affect the architecture:

1. **Rounding:** Does damage floor to int, round to nearest int, or stay float?
   (Matters for minimum-damage edge cases.)

2. **Minimum damage:** Can damage resolve to 0? Or is there a floor of 1?

3. **Type modifier source:** Is the type chart a static lookup table (JSON), a
   ScriptableObject per creature type, or computed at runtime?

4. **DNA modifier integration:** Do DNA trait bonuses apply before or after type
   effectiveness? (Multiplicative order matters for balance.)

5. **Height advantage:** The GDD mentions "height bonus" but gives no value.
   What is the multiplier for attacking from higher ground?

6. **Status conditions:** Are conditions like Burned or Weakened Gene applied as
   flat stat reductions, percentage reductions, or additive formula terms?

7. **Architecture pattern:** Should DamageCalculator be a static utility class,
   a MonoBehaviour singleton, or a pure service injected via dependency injection?
```

**User:**
```
Good catches. Here are the answers:

1. Floor to int always
2. Minimum damage is 1 (never 0)
3. Static JSON lookup in Assets/Data/Combat/
4. DNA bonuses apply after type effectiveness (so type chart isn't diluted)
5. Height advantage: 1.25x multiplier when attacker is on higher tile
6. Status conditions are percentage reductions to the relevant stat before formula
7. Static utility class — keep it simple
```

**Gameplay-Programmer:**
```
Understood. Here's the proposed file structure before I write a line:

Assets/Scripts/Combat/
  DamageCalculator.cs      — static utility, pure functions, no MonoBehaviour
  CombatConfig.cs          — typed config loader, reads from JSON on first call

Assets/Scripts/Creatures/
  CreatureStats.cs         — typed struct: attack, defense, speed, hp + DNA mods
  AbilityData.cs           — typed class: power, type, category (physical/special)

Assets/Data/Combat/
  CombatSettings ScriptableObject       — type chart matrix, height multiplier, min damage floor

tests/EditMode/Combat/
  DamageCalculatorTests.cs — 8 targeted unit tests (see below)

Formula as I understand it:
  raw = floor((attacker.EffectiveAttack * ability.power * type_modifier
               * height_bonus) / defender.EffectiveDefense)
  final = max(raw, 1)

Where:
  EffectiveAttack  = attacker.base_attack * (1.0 - status_debuff_percent)
  EffectiveDefense = defender.base_defense * (1.0 - status_debuff_percent)
  height_bonus     = 1.25f if attacker.GridHeight > defender.GridHeight else 1.0f
  type_modifier    = CombatConfig.TypeChart[ability.type][defender.primaryType]
                     * CombatConfig.TypeChart[ability.type][defender.secondaryType]
                     (1.0 if secondaryType is None)

Does this match your intent? Any corrections before I implement?
```

**User:**
```
Yes, that's right. One change: CreatureStats should use typed properties, not a
plain Dictionary. I want compile-time safety.
```

**Gameplay-Programmer:**
```
Agreed — typed properties are the right call. Implementing now.

[Implements DamageCalculator.cs, CombatConfig.cs, CreatureStats.cs, AbilityData.cs,
CombatSettings ScriptableObject, DamageCalculatorTests.cs]

Two rule violations flagged automatically by the hooks:

VIOLATION 1: I initially wrote `1.25f` as a literal in DamageCalculator.cs.
FIXED: Moved to CombatSettings.asset as "height_advantage_multiplier": 1.25

VIOLATION 2: A debug Debug.Log remained in the type chart lookup path.
FIXED: Removed. No debug output in production code.

---

UNIT TESTS (all passing):

1. BasicDamageFormula — standard attack vs standard defense, no modifiers
2. TypeEffective — 2x modifier doubles result before floor
3. TypeResistant — 0.5x modifier halves result before floor
4. TypeImmune — 0x modifier still floors to 1 (min damage rule)
5. HeightAdvantage — attacker on higher tile applies 1.25x multiplier
6. HeightDisadvantage — attacker on lower tile applies 1.0x (no penalty, no bonus)
7. StatusDebuff — 25% attack debuff reduces effective attack before formula
8. MinimumDamage — extremely high defense does not reduce damage below 1

All 8 tests pass. Implementation matches all clarified requirements.
```

**User:**
```
Looks good. Can you also make sure the type chart JSON is easy to extend when I
add new creature types later?
```

**Gameplay-Programmer:**
```
Already structured for extension. The type chart in CombatSettings ScriptableObject is a
2D dictionary keyed by type name strings:

{
  "type_chart": {
    "Fire": {
      "Grass": 2.0,
      "Water": 0.5,
      "Fire": 0.5,
      "Rock": 0.5,
      "Steel": 2.0
    },
    ...
  }
}

Adding a new type requires:
1. Add a new top-level key in "type_chart" (the attacking type)
2. Add the new type as a value key in every existing attacking type entry
3. No code changes required — CombatConfig.cs reads dynamically

I've added a comment block in CombatSettings ScriptableObject documenting this extension
pattern for future reference.

All files written. Ready for integration into the combat system.
```

---

## Key Observations

1. **Seven spec gaps identified before any code was written** — prevented costly rework on formula order and rounding behavior
2. **Architecture proposed and approved** — user corrected "typed properties" preference before implementation locked it in
3. **Violations reported transparently** — the hardcoded multiplier and debug log were caught and fixed, not hidden
4. **Tests cover edge cases, not just the happy path** — TypeImmune → min damage 1, and StatusDebuff before formula are the cases most likely to break
5. **Extension pattern documented** — future new creature types require no code changes, only JSON edits
6. **Seven files produced in one session** — with all acceptance criteria verified

---

## Architecture Notes (Gene Forge Specific)

- `DamageCalculator` is a static utility — no scene dependency, fully testable in EditMode
- `CombatConfig` loads once on first call (lazy static init) — no redundant JSON parsing per combat
- `CreatureStats` is a struct — passed by value into `DamageCalculator`, never mutated by the formula
- Height data comes from the Grid system (`GridTile.Height: int`) — `DamageCalculator` receives it as a parameter, not by reading the grid directly
- DNA modifiers are applied to `EffectiveAttack` / `EffectiveDefense` by the creature's own stat layer before `DamageCalculator` is called — damage formula itself knows nothing about DNA
