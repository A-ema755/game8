# Capture System

## 1. Overview

The Capture System governs how players collect wild creatures using Gene Traps — this world's equivalent of capture devices. When a wild creature is weakened or inflicted with a status condition, the player can throw a Gene Trap to attempt capture. The catch rate is calculated from a formula combining the creature's rarity, trap quality, remaining HP, and active status effects. A Catch Predictor UI displays the probability before committing. Failed attempts consume the trap (post-MVP) or leave it available (MVP). Certain rare creatures require specific environmental conditions before they can be captured at all.

## 2. Player Fantasy

You are a field researcher who earns creatures through skill, not luck. Whittling a powerful creature down to near-death, inflicting the right status condition, and pulling out the perfect trap feels like a precision tool-use moment — not a coin flip. The probability display respects the player's intelligence, showing them exactly what their preparation is worth. Every successful capture is a small victory of planning.

## 3. Detailed Rules

### 3.1 Gene Trap Types

| Trap ID | Display Name | `trapModifier` | Description |
|---------|-------------|----------------|-------------|
| `gene-trap-standard` | Standard Gene Trap | 1.0 | Default trap available from the start |
| `gene-trap-enhanced` | Enhanced Gene Trap | 1.5 | Crafted or purchased at Researcher rank |
| `gene-trap-specialist` | Specialist Gene Trap | 2.0 | Matches creature's primary type; requires Field Agent rank |

Specialist Gene Traps carry a `targetType` field. The 2x modifier only applies when the wild creature's primary type matches `targetType`. Otherwise it functions as a Standard Gene Trap (1.0x).

### 3.2 Catch Attempt Flow

1. Player selects "Throw Trap" from the combat action menu.
2. Player selects which Gene Trap to throw (inventory filtered to trap items).
3. Catch Predictor UI calculates and displays probability (see Section 4).
4. Player confirms. Turn is consumed.
5. Server resolves: generate `roll = Random.Range(0f, 1f)`.
6. If `roll < catchRate` → capture succeeds. Creature added to party or storage.
7. If `roll >= catchRate` → capture fails.
   - MVP: trap is returned to inventory.
   - Post-MVP: trap is consumed.
8. On failure, the wild creature's turn proceeds normally. It may attack or flee.

### 3.3 Status Bonus Values

| Status Effect | `statusBonus` |
|---------------|--------------|
| None | 1.0 |
| Poison | 1.2 |
| Burn | 1.2 |
| Paralysis | 1.5 |
| Freeze | 1.5 |
| Sleep | 2.5 |

Only the highest applicable bonus is used if multiple statuses are stacked (edge case — see Section 5).

### 3.4 Special Capture Conditions

Some rare creatures cannot be captured without meeting environmental prerequisites. These are defined per `CreatureConfig` in the `captureConditions` list. A creature with unsatisfied conditions is shown a lock icon in the Catch Predictor UI and cannot be targeted by a trap.

```csharp
[System.Serializable]
public class CaptureCondition
{
    public CaptureConditionType conditionType; // Weather, TimeOfDay, Terrain, MinLevel
    public string requiredValue;               // e.g. "Rain", "Night", "Water", "20"
    public string flavorText;                  // Shown in UI when condition is not met
}

public enum CaptureConditionType
{
    Weather,
    TimeOfDay,
    TerrainType,
    MinPlayerLevel,
    SpecificMove // player must have used a specific move type this battle
}
```

### 3.5 Capture Resolution C# Sketch

```csharp
public static class CaptureCalculator
{
    /// <summary>
    /// Returns a catch probability in [0, 1].
    /// </summary>
    public static float CalculateCatchRate(
        CreatureConfig config,
        int currentHp,
        int maxHp,
        float trapModifier,
        float statusBonus)
    {
        float hpFactor = (3f * maxHp - 2f * currentHp) / (3f * maxHp);
        float rawRate = (config.CatchRate * trapModifier * hpFactor * statusBonus) / 255f;
        return Mathf.Clamp01(rawRate);
    }

    /// <summary>
    /// Resolves a single capture attempt. Returns true on success.
    /// </summary>
    public static bool AttemptCapture(float catchRate)
    {
        return Random.value < catchRate;
    }
}
```

### 3.6 Catch Predictor UI

The UI panel displays:
- Probability percentage (rounded to nearest whole percent, minimum display 1% if `catchRate > 0`).
- A color-coded confidence band: red (<20%), orange (20-50%), yellow (50-80%), green (>80%).
- Trap modifier and status modifier shown as multiplier badges.
- A lock icon with flavor text if special capture conditions are not met.

The predictor updates live as the player cycles through trap types.

## 4. Formulas

### Catch Rate Formula

```
catchRate = (creatureBaseRate * trapModifier * hpFactor * statusBonus) / 255
catchRate = Clamp(catchRate, 0, 1)
```

### HP Factor

```
hpFactor = (3 * maxHP - 2 * currentHP) / (3 * maxHP)
```

`hpFactor` ranges from ~0.33 (full HP) to 1.0 (1 HP remaining).

### Variable Definitions

| Variable | Type | Source |
|----------|------|--------|
| `creatureBaseRate` | float [1–255] | `CreatureConfig.baseCatchRate` |
| `trapModifier` | float | Gene Trap item config |
| `hpFactor` | float [0.33–1.0] | Calculated from current/max HP |
| `statusBonus` | float | Active status effect table (Section 3.3) |

### Example Calculation

Target: creature with `baseCatchRate = 45`, at 25% HP, Paralyzed, using Enhanced Trap.

```
hpFactor = (3*100 - 2*25) / (3*100) = (300 - 50) / 300 = 0.833
catchRate = (45 * 1.5 * 0.833 * 1.5) / 255
catchRate = 84.3 / 255 = 0.331 → ~33%
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Multiple status effects active simultaneously | Use the single highest `statusBonus` only; do not multiply bonuses together |
| `currentHP == maxHP` | `hpFactor = 0.333...`; catch rate is at minimum |
| `currentHP == 1` | `hpFactor` approaches 1.0; effectively capped at 1.0 |
| `creatureBaseRate == 255` | Always capturable (rate = 1.0 before clamp) regardless of other factors |
| `creatureBaseRate == 0` | Uncatchable; predictor shows 0% and trap is greyed out |
| Capture attempted on a Trainer creature | Throw Trap action is not available in trainer encounters; combat UI hides the option |
| Capture attempted during Nest/Trophy/Horde encounter | Per-encounter flag `captureAllowed` on `EncounterConfig`; action hidden when false |
| Inventory is empty of traps | Throw Trap action is greyed out with tooltip "No Gene Traps in inventory" |
| Capture condition requires weather but weather system is disabled (MVP) | `captureConditions` with `conditionType == Weather` are ignored; capture proceeds normally |
| Party is full (6 creatures) and no storage available | Capture succeeds; player prompted to choose a creature to transfer to storage (storage is always available at this point) |
| Wild creature has `isBoss == true` | Capture is permanently blocked regardless of conditions; trap action hidden |

## 6. Dependencies

| System | Dependency Type | Notes |
|--------|----------------|-------|
| Creature Database | Read | `CreatureConfig.baseCatchRate`, `captureConditions` |
| Creature Instance | Read | Current HP, max HP, active status effects |
| Party System | Write | Adds captured creature to party or storage |
| Encounter System | Read | `captureAllowed` flag per encounter type |
| Inventory System | Read/Write | Consumes trap on failure (post-MVP); checks trap availability |
| Pokedex System | Write | Triggers Pokedex update on successful capture (Full Profile tier) |
| Type Chart System | Read | Specialist trap type-matching check |
| Combat UI | Read/Write | Catch Predictor panel, action availability |
| Weather System | Read | Optional; only required for weather-gated capture conditions |

## 7. Tuning Knobs

| Parameter | Default | Range | Notes |
|-----------|---------|-------|-------|
| `baseCatchRate` (common creature) | 180 | 1–255 | e.g. starter-zone commons |
| `baseCatchRate` (uncommon creature) | 90 | 1–255 | mid-zone creatures |
| `baseCatchRate` (rare creature) | 45 | 1–255 | bosses, trophy-adjacent |
| `baseCatchRate` (legendary) | 3 | 1–255 | near-impossible without prep |
| `statusBonus` (Sleep) | 2.5 | 1.0–4.0 | Primary incentive to use sleep moves |
| `statusBonus` (Paralysis/Freeze) | 1.5 | 1.0–3.0 | |
| `statusBonus` (Poison/Burn) | 1.2 | 1.0–2.0 | Minor assist |
| `trapModifier` (Standard) | 1.0 | — | Baseline |
| `trapModifier` (Enhanced) | 1.5 | 1.0–2.5 | |
| `trapModifier` (Specialist) | 2.0 | 1.5–3.0 | |
| MVP failed-trap return | true | bool | Set false post-MVP to consume on fail |

## 8. Acceptance Criteria

- [ ] `CaptureCalculator.CalculateCatchRate()` returns correct values for all documented example inputs (unit tested).
- [ ] `hpFactor` at full HP is approximately 0.333; at 1 HP is approximately 1.0.
- [ ] Specialist Gene Trap applies 2.0x only when creature type matches; falls back to 1.0x otherwise.
- [ ] Sleep status applies 2.5x bonus; all other bonuses match the table in Section 3.3.
- [ ] Only the highest status bonus is applied when multiple statuses are active.
- [ ] Catch Predictor UI updates in real time when the player cycles trap types.
- [ ] Probability display color changes correctly at <20%, 20-50%, 50-80%, and >80% thresholds.
- [ ] Throw Trap action is hidden entirely in Trainer encounters.
- [ ] Throw Trap action respects `captureAllowed` flag from `EncounterConfig`.
- [ ] Creatures with `baseCatchRate == 0` display 0% and cannot be targeted.
- [ ] Creatures with unsatisfied `captureConditions` show a lock icon and the condition's `flavorText`.
- [ ] Successful capture triggers Pokedex Full Profile tier unlock.
- [ ] Captured creature is added to party if space available, otherwise routes to storage.
- [ ] MVP: failed trap is returned to inventory and not consumed.
