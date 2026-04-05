# Type Chart System

## 1. Overview

The Type Chart System provides a static 2D effectiveness matrix for all 8 MVP elemental types (Fire, Water, Grass, Electric, Ice, Rock, Dark, Psychic) and computes the combined type effectiveness multiplier for any attack against a target with one or two types. The matrix is a static float array initialized once at startup — not a ScriptableObject — for O(1) lookup performance in the hot combat path (ADR-007). STAB (Same-Type Attack Bonus) is also calculated here. The system exposes a single static method `GetMultiplier(attackType, targetPrimaryType, targetSecondaryType)` used exclusively by the Damage & Health System.

## 2. Player Fantasy

Type matchups are intuitive and learnable. Fire beats Grass, Water beats Fire, Electric shocks Water — the logic is rooted in real-world intuition. When a super-effective hit lands, the UI calls it out boldly. When an immune target shrugs off an attack, the player learns something. Mastery of the type chart is rewarded through consistently better damage output, not through brute-force grinding.

## 3. Detailed Rules

### 3.1 Type Index

Types are mapped to array indices for O(1) lookup:

```csharp
namespace GeneForge.Combat
{
    // Internal index mapping — matches CreatureType enum integer values for MVP types
    // Fire=1, Water=2, Grass=3, Electric=4, Ice=5, Rock=6, Dark=7, Psychic=8
    // Index 0 (None) = neutral against everything
}
```

### 3.2 Effectiveness Values

| Value | Label | UI Display |
|-------|-------|-----------|
| `0.0f` | Immune | "No effect!" |
| `0.5f` | Not Very Effective | "Not very effective..." |
| `1.0f` | Neutral | (no callout) |
| `2.0f` | Super Effective | "Super effective!" |

### 3.3 Effectiveness Matrix (MVP 8 Types)

Rows = Attacking type. Columns = Defending type.

| ATK \ DEF | Fire | Water | Grass | Electric | Ice | Rock | Dark | Psychic |
|-----------|------|-------|-------|----------|-----|------|------|---------|
| **Fire** | 0.5 | 0.5 | 2.0 | 1.0 | 2.0 | 0.5 | 1.0 | 1.0 |
| **Water** | 2.0 | 0.5 | 0.5 | 1.0 | 1.0 | 2.0 | 1.0 | 1.0 |
| **Grass** | 0.5 | 2.0 | 0.5 | 1.0 | 1.0 | 2.0 | 1.0 | 1.0 |
| **Electric** | 1.0 | 2.0 | 1.0 | 0.5 | 1.0 | 1.0 | 1.0 | 1.0 |
| **Ice** | 0.5 | 0.5 | 2.0 | 1.0 | 0.5 | 2.0 | 1.0 | 1.0 |
| **Rock** | 2.0 | 1.0 | 1.0 | 1.0 | 2.0 | 1.0 | 1.0 | 1.0 |
| **Dark** | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 0.5 | 2.0 |
| **Psychic** | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 0.0 | 0.5 |

Notes:
- Psychic is **immune** (0.0) against Dark — Dark types are immune to psychic manipulation
- Electric has **no immunity** in the MVP set (post-MVP: Ground immune to Electric)
- All same-type matchups are 0.5 (resistant to their own type) except Dark vs Dark

### 3.4 TypeChart Implementation

```csharp
namespace GeneForge.Combat
{
    /// <summary>
    /// Static type effectiveness matrix.
    /// Initialized once at startup. Thread-safe after initialization.
    /// ADR-007: static array, not ScriptableObject.
    /// </summary>
    public static class TypeChart
    {
        // Dimensions: [attackingTypeIndex, defendingTypeIndex]
        // Index 0 = None (neutral), indices 1-8 = Fire through Psychic
        private static float[,] _matrix;
        private static bool _initialized;

        // Number of type slots including None
        private const int TypeCount = 9; // 0=None, 1-8=MVP types

        public static void Initialize()
        {
            if (_initialized) return;

            _matrix = new float[TypeCount, TypeCount];

            // Default all to 1.0 (neutral)
            for (int a = 0; a < TypeCount; a++)
            for (int d = 0; d < TypeCount; d++)
                _matrix[a, d] = 1.0f;

            // Populate non-neutral entries
            // Row index = (int)CreatureType attacking
            // Col index = (int)CreatureType defending

            // Fire (1) attacking
            Set(CreatureType.Fire, CreatureType.Fire,     0.5f);
            Set(CreatureType.Fire, CreatureType.Water,    0.5f);
            Set(CreatureType.Fire, CreatureType.Grass,    2.0f);
            Set(CreatureType.Fire, CreatureType.Ice,      2.0f);
            Set(CreatureType.Fire, CreatureType.Rock,     0.5f);

            // Water (2) attacking
            Set(CreatureType.Water, CreatureType.Fire,    2.0f);
            Set(CreatureType.Water, CreatureType.Water,   0.5f);
            Set(CreatureType.Water, CreatureType.Grass,   0.5f);
            Set(CreatureType.Water, CreatureType.Rock,    2.0f);

            // Grass (3) attacking
            Set(CreatureType.Grass, CreatureType.Fire,    0.5f);
            Set(CreatureType.Grass, CreatureType.Water,   2.0f);
            Set(CreatureType.Grass, CreatureType.Grass,   0.5f);
            Set(CreatureType.Grass, CreatureType.Rock,    2.0f);

            // Electric (4) attacking
            Set(CreatureType.Electric, CreatureType.Water,    2.0f);
            Set(CreatureType.Electric, CreatureType.Electric, 0.5f);

            // Ice (5) attacking
            Set(CreatureType.Ice, CreatureType.Fire,    0.5f);
            Set(CreatureType.Ice, CreatureType.Water,   0.5f);
            Set(CreatureType.Ice, CreatureType.Grass,   2.0f);
            Set(CreatureType.Ice, CreatureType.Ice,     0.5f);
            Set(CreatureType.Ice, CreatureType.Rock,    2.0f);

            // Rock (6) attacking
            Set(CreatureType.Rock, CreatureType.Fire,   2.0f);
            Set(CreatureType.Rock, CreatureType.Ice,    2.0f);

            // Dark (7) attacking
            Set(CreatureType.Dark, CreatureType.Dark,    0.5f);
            Set(CreatureType.Dark, CreatureType.Psychic, 2.0f);

            // Psychic (8) attacking
            Set(CreatureType.Psychic, CreatureType.Dark,    0.0f); // immune
            Set(CreatureType.Psychic, CreatureType.Psychic, 0.5f);

            _initialized = true;
        }

        private static void Set(CreatureType atk, CreatureType def, float value)
            => _matrix[(int)atk, (int)def] = value;

        /// <summary>
        /// Returns the combined type effectiveness multiplier for an attack.
        /// If target has two types, both multipliers are multiplied together.
        /// </summary>
        /// <param name="attackType">The elemental type of the move.</param>
        /// <param name="primaryType">The target's primary type.</param>
        /// <param name="secondaryType">The target's secondary type (None if single-type).</param>
        public static float GetMultiplier(
            CreatureType attackType,
            CreatureType primaryType,
            CreatureType secondaryType = CreatureType.None)
        {
            if (!_initialized)
            {
                Debug.LogError("[TypeChart] Not initialized. Call TypeChart.Initialize() first.");
                return 1.0f;
            }

            float mult = _matrix[(int)attackType, (int)primaryType];

            if (secondaryType != CreatureType.None)
                mult *= _matrix[(int)attackType, (int)secondaryType];

            return mult;
        }

        /// <summary>
        /// Returns the effectiveness label for UI display.
        /// Uses the combined multiplier from GetMultiplier.
        /// </summary>
        public static EffectivenessLabel GetLabel(float multiplier)
        {
            if (multiplier == 0f)   return EffectivenessLabel.Immune;
            if (multiplier < 1f)    return EffectivenessLabel.NotVeryEffective;
            if (multiplier > 1f)    return EffectivenessLabel.SuperEffective;
            return EffectivenessLabel.Neutral;
        }

        /// <summary>
        /// Calculate STAB multiplier.
        /// Returns StabMultiplier if move type matches either creature type; 1.0 otherwise.
        /// </summary>
        public static float GetStab(
            CreatureType moveType,
            CreatureType creaturePrimaryType,
            CreatureType creatureSecondaryType = CreatureType.None)
        {
            bool hasStab = moveType == creaturePrimaryType
                        || (creatureSecondaryType != CreatureType.None
                            && moveType == creatureSecondaryType);
            return hasStab ? StabMultiplier : 1.0f;
        }

        // ── Constants ──────────────────────────────────────────────────
        public const float StabMultiplier = 1.5f;
        public const float SuperEffectiveMultiplier = 2.0f;
        public const float NotVeryEffectiveMultiplier = 0.5f;
        public const float ImmuneMultiplier = 0.0f;
    }

    public enum EffectivenessLabel
    {
        Immune = 0,
        NotVeryEffective = 1,
        Neutral = 2,
        SuperEffective = 3
    }
}
```

### 3.5 Dual-Type Interaction Examples

| Move Type | Target Types | Calculation | Result |
|-----------|-------------|-------------|--------|
| Fire | Grass / Rock | 2.0 × 0.5 | 1.0 (neutral) |
| Water | Fire / Rock | 2.0 × 2.0 | 4.0 (double super effective) |
| Electric | Water / Water | 2.0 × 2.0 | 4.0 (same type both slots) |
| Psychic | Dark / Grass | 0.0 × 1.0 | 0.0 (immune — Dark negates) |
| Ice | Grass / Rock | 2.0 × 2.0 | 4.0 |
| Dark | Dark / Psychic | 0.5 × 2.0 | 1.0 (cancel out) |

### 3.6 STAB + Type Effectiveness Combined

Both multipliers are applied independently and then multiplied together in the Damage & Health System. TypeChart only computes each factor; combination is done by the damage formula.

```
finalMultiplier = STAB × typeEffectiveness
```

Example: Fire creature uses Fire move against Grass target:
- STAB = 1.5 (Fire creature + Fire move)
- TypeEffectiveness = 2.0 (Fire vs Grass)
- Combined = 1.5 × 2.0 = **3.0**

## 4. Formulas

| Formula | Expression |
|---------|-----------|
| Single-type effectiveness | `matrix[attackType, defenseType]` |
| Dual-type effectiveness | `matrix[attackType, primaryType] × matrix[attackType, secondaryType]` |
| STAB bonus | `1.5` if move type matches any creature type, else `1.0` |
| Combined STAB + effectiveness | `STAB × typeEffectiveness` (computed in DamageSystem) |
| Max possible multiplier (dual-type) | `2.0 × 2.0 × 1.5 = 6.0` (dual super effective + STAB) |
| Min possible multiplier (immune) | `0.0` (immune negates all damage) |

## 5. Edge Cases

| Situation | Behavior |
|-----------|----------|
| `GetMultiplier` called before `Initialize()` | Returns 1.0, logs error |
| Move type is `None` | Returns 1.0 (neutral) for all target types |
| Target secondary type is `None` | Only primary type effectiveness applied |
| Dual-type target where one type is immune | Result is 0.0 (immune negates the super effective) |
| Same type in both primary and secondary slots | Both applied, e.g. Electric vs Water/Water = 4.0 |
| Attacking type not in matrix bounds | Array index out of range — guarded by enum int range |
| Post-MVP type added without matrix expansion | Matrix initialized with `TypeCount`; new type at index > TypeCount-1 throws; must increment `TypeCount` and re-populate |

## 6. Dependencies

| Dependency | Direction | Notes |
|------------|-----------|-------|
| `CreatureType` enum | Inbound | Integer values used as matrix indices |
| `ConfigLoader` | Sibling | Both initialized in Boot before game starts |
| Damage & Health System | Outbound | Sole consumer of `GetMultiplier` and `GetStab` |
| Combat UI / Feedback | Outbound | Uses `GetLabel` for effectiveness callout display |

## 7. Tuning Knobs

| Parameter | Location | Default | Notes |
|-----------|----------|---------|-------|
| `StabMultiplier` | `TypeChart` const | `1.5f` | Standard STAB bonus |
| `SuperEffectiveMultiplier` | `TypeChart` const | `2.0f` | Reference value; actual matrix entries set directly |
| `NotVeryEffectiveMultiplier` | `TypeChart` const | `0.5f` | Reference value |
| `TypeCount` | `TypeChart` const | `9` | Must be updated when adding new types |
| Matrix values | `TypeChart.Initialize()` | See Section 3.3 | Any matchup can be rebalanced here |
| Psychic vs Dark immunity | Matrix entry | `0.0f` | Tune to 0.5 if immunity feels too punishing |

## 8. Acceptance Criteria

- [ ] `TypeChart.Initialize()` completes without errors
- [ ] `GetMultiplier(Fire, Grass, None)` returns `2.0f`
- [ ] `GetMultiplier(Fire, Water, None)` returns `0.5f`
- [ ] `GetMultiplier(Psychic, Dark, None)` returns `0.0f`
- [ ] `GetMultiplier(Water, Fire, Rock)` returns `4.0f`
- [ ] `GetMultiplier(Psychic, Dark, Grass)` returns `0.0f` (immunity)
- [ ] `GetStab(Fire, Fire, None)` returns `1.5f`
- [ ] `GetStab(Fire, Water, Grass)` returns `1.0f`
- [ ] `GetStab(Fire, Water, Fire)` returns `1.5f` (secondary type STAB)
- [ ] `GetMultiplier` before `Initialize()` returns `1.0f` and logs error
- [ ] `GetLabel(2.0f)` returns `SuperEffective`
- [ ] `GetLabel(0.0f)` returns `Immune`
- [ ] EditMode tests cover all matrix entries from Section 3.3
- [ ] No allocation per call (static array, no list/dictionary)
