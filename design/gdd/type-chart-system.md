# Type Chart System

## 1. Overview

The Type Chart System provides a static 2D effectiveness matrix for all 14 genome types plus None (15 total values) and computes the combined type effectiveness multiplier for any attack against a target with one or two types. The matrix is a static float array initialized once at startup — not a ScriptableObject — for O(1) lookup performance in the hot combat path (ADR-007). STAB (Same-Type Attack Bonus) is also calculated here. The system exposes a single static method `GetMultiplier(attackType, targetPrimaryType, targetSecondaryType)` used exclusively by the Damage & Health System.

The 14 types are organized in three tiers:
- **Standard (8):** Thermal, Aqua, Organic, Bioelectric, Cryo, Mineral, Toxic, Neural
- **Extended (4):** Ferro, Kinetic, Aero, Sonic
- **Apex (2):** Ark (genetic stability), Blight (genetic instability)

All 14 types are MVP scope. The chart contains 36 super-effective relationships, no immunities, and every type resists itself.

## 2. Player Fantasy

Type matchups are intuitive and grounded in biology and physics. Thermal burns Organic tissue, Aqua quenches Thermal reactions, Mineral grounds Bioelectric charge — the logic is rooted in real-world intuition. The 14-type system creates a rich web of relationships without requiring a second matrix to memorize (Pillar 3 — Discovery Through Play: one type chart, learnable through combat). When a super-effective hit lands, the UI calls it out boldly. When a resisted attack does chip damage instead of nothing, the player still feels the interaction was meaningful — there are no immunities to learn, just advantages and disadvantages. Mastering the type chart rewards consistently better damage output and smarter team building.

## 3. Detailed Rules

### 3.1 CreatureType Enum

Types are mapped to integer indices for O(1) array lookup:

```csharp
namespace GeneForge.Combat
{
    public enum CreatureType
    {
        None        = 0,
        Thermal     = 1,
        Aqua        = 2,
        Organic     = 3,
        Bioelectric = 4,
        Cryo        = 5,
        Mineral     = 6,
        Toxic       = 7,
        Neural      = 8,
        Ferro       = 9,
        Kinetic     = 10,
        Aero        = 11,
        Sonic       = 12,
        Ark         = 13,
        Blight      = 14
    }
}
```

### 3.2 Effectiveness Values

| Value | Label | UI Display |
|-------|-------|-----------|
| `0.5f` | Resisted | "Not very effective..." |
| `1.0f` | Neutral | (no callout) |
| `2.0f` | Super Effective | "Super effective!" |

There are **no immunities (0.0x)** in the Gene Forge type chart. Every type deals at least 0.5x to every other type.

### 3.3 Triangle Structure

The 14 types are built on **4 core triangles** with cross-links connecting them:

**Triangle 1 — Elemental:** Thermal > Organic > Aqua > Thermal
**Triangle 2 — Physical:** Mineral > Bioelectric > Aqua > Mineral
**Triangle 3 — Mental:** Neural > Kinetic > Ferro > Neural
**Triangle 4 — Atmospheric:** Cryo > Aero > Sonic > Cryo

Cross-links (15 relationships) bridge the triangles. The 2 apex types (Ark, Blight) sit above with 9 relationships of their own.

### 3.4 All 36 Super-Effective Relationships

#### Core Triangles (12 relationships)

| # | Attacker | Defender | Intuition |
|---|----------|----------|-----------|
| 1 | Thermal | Organic | Heat burns plants and living tissue |
| 2 | Organic | Aqua | Plants absorb and filter water |
| 3 | Aqua | Thermal | Water quenches fire and cools heat |
| 4 | Mineral | Bioelectric | Earth grounds electrical charge |
| 5 | Bioelectric | Aqua | Electricity conducts through water |
| 6 | Aqua | Mineral | Water erodes stone over time |
| 7 | Neural | Kinetic | Mind overcomes brute muscle |
| 8 | Kinetic | Ferro | Raw force bends and breaks metal |
| 9 | Ferro | Neural | Faraday cage blocks psychic waves |
| 10 | Cryo | Aero | Ice grounds flyers, freezes wings |
| 11 | Aero | Sonic | Wind scatters and disperses sound |
| 12 | Sonic | Cryo | Resonance shatters brittle ice |

#### Cross-Links (15 relationships)

| # | Attacker | Defender | Intuition |
|---|----------|----------|-----------|
| 13 | Thermal | Cryo | Heat melts ice |
| 14 | Thermal | Ferro | Extreme heat smelts metal |
| 15 | Cryo | Organic | Frost kills plants, freezes tissue |
| 16 | Cryo | Kinetic | Cold numbs muscles, slows movement |
| 17 | Mineral | Toxic | Dense minerals neutralize chemicals |
| 18 | Mineral | Sonic | Dense stone absorbs vibration |
| 19 | Mineral | Thermal | Earth smothers fire |
| 20 | Toxic | Organic | Poison kills living tissue |
| 21 | Toxic | Ferro | Acid corrodes metal |
| 22 | Neural | Toxic | Mental discipline purges poison |
| 23 | Ferro | Cryo | Metal tools shatter frozen things |
| 24 | Kinetic | Mineral | Brute force cracks stone |
| 25 | Bioelectric | Aero | Lightning strikes airborne targets |
| 26 | Aero | Thermal | Wind snuffs flames |
| 27 | Organic | Mineral | Roots break through stone |

#### Apex Relationships (9 relationships)

| # | Attacker | Defender | Intuition |
|---|----------|----------|-----------|
| 28 | Ark | Blight | Order contains chaos |
| 29 | Ark | Toxic | Genetic purity neutralizes corruption |
| 30 | Ark | Kinetic | Perfection renders brute force meaningless |
| 31 | Blight | Ark | Entropy cracks perfection |
| 32 | Blight | Bioelectric | Chaos disrupts precision bio-systems |
| 33 | Blight | Neural | Genetic chaos overwhelms structured minds |
| 34 | Thermal | Ark | Heat = entropy, breaks molecular order |
| 35 | Organic | Blight | Life absorbs and metabolizes chaos |
| 36 | Sonic | Neural | Sonic disruption overloads neural pathways |

### 3.5 Resistance Map (Defender Takes 0.5x)

Every type resists itself. Additional resistances:

| Type | Also Resists Incoming From |
|------|---------------------------|
| Thermal | Organic, Cryo, Ferro |
| Aqua | Thermal, Cryo |
| Organic | Aqua, Bioelectric, Mineral |
| Bioelectric | Aero, Ferro |
| Cryo | Aqua |
| Mineral | Thermal, Bioelectric, Toxic, Sonic |
| Toxic | Organic |
| Neural | Toxic |
| Ferro | Cryo, Organic, Neural, Sonic, Aero |
| Kinetic | Sonic |
| Aero | Organic, Kinetic |
| Sonic | Kinetic |
| Ark | Toxic, Neural, Cryo, Sonic, Mineral |
| Blight | Thermal, Bioelectric, Toxic, Kinetic |

### 3.6 Full 15×15 Effectiveness Matrix

Rows = Attacking type. Columns = Defending type. None row/column is always 1.0.

| ATK \ DEF | The | Aqu | Org | Bio | Cry | Min | Tox | Neu | Fer | Kin | Aer | Son | Ark | Bli |
|-----------|-----|-----|-----|-----|-----|-----|-----|-----|-----|-----|-----|-----|-----|-----|
| **Thermal** | 0.5 | 0.5 | 2.0 | 1.0 | 2.0 | 0.5 | 1.0 | 1.0 | 2.0 | 1.0 | 1.0 | 1.0 | 2.0 | 0.5 |
| **Aqua** | 2.0 | 0.5 | 0.5 | 1.0 | 0.5 | 2.0 | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 |
| **Organic** | 0.5 | 2.0 | 0.5 | 1.0 | 1.0 | 2.0 | 0.5 | 1.0 | 0.5 | 1.0 | 0.5 | 1.0 | 1.0 | 2.0 |
| **Bioelectric** | 1.0 | 2.0 | 0.5 | 0.5 | 1.0 | 0.5 | 1.0 | 1.0 | 1.0 | 1.0 | 2.0 | 1.0 | 1.0 | 0.5 |
| **Cryo** | 0.5 | 0.5 | 2.0 | 1.0 | 0.5 | 1.0 | 1.0 | 1.0 | 0.5 | 2.0 | 2.0 | 1.0 | 0.5 | 1.0 |
| **Mineral** | 2.0 | 1.0 | 0.5 | 2.0 | 1.0 | 0.5 | 2.0 | 1.0 | 1.0 | 1.0 | 1.0 | 2.0 | 0.5 | 1.0 |
| **Toxic** | 1.0 | 1.0 | 2.0 | 1.0 | 1.0 | 0.5 | 0.5 | 0.5 | 2.0 | 1.0 | 1.0 | 1.0 | 0.5 | 0.5 |
| **Neural** | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 2.0 | 0.5 | 0.5 | 2.0 | 1.0 | 1.0 | 0.5 | 1.0 |
| **Ferro** | 0.5 | 1.0 | 1.0 | 0.5 | 2.0 | 1.0 | 1.0 | 2.0 | 0.5 | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 |
| **Kinetic** | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 2.0 | 1.0 | 1.0 | 2.0 | 0.5 | 0.5 | 0.5 | 1.0 | 0.5 |
| **Aero** | 2.0 | 1.0 | 1.0 | 0.5 | 1.0 | 1.0 | 1.0 | 1.0 | 0.5 | 1.0 | 0.5 | 2.0 | 1.0 | 1.0 |
| **Sonic** | 1.0 | 1.0 | 1.0 | 1.0 | 2.0 | 0.5 | 1.0 | 2.0 | 0.5 | 0.5 | 1.0 | 0.5 | 0.5 | 1.0 |
| **Ark** | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 2.0 | 1.0 | 1.0 | 2.0 | 1.0 | 1.0 | 0.5 | 2.0 |
| **Blight** | 1.0 | 1.0 | 1.0 | 2.0 | 1.0 | 1.0 | 1.0 | 2.0 | 1.0 | 1.0 | 1.0 | 1.0 | 2.0 | 0.5 |

### 3.7 Balance Summary

| Type | SEs | Weaknesses | Resists | Net | Role |
|------|-----|------------|---------|-----|------|
| Thermal | 4 | 3 | 4 | +1 | Offensive generalist |
| Aqua | 2 | 2 | 3 | 0 | Balanced pivot |
| Organic | 3 | 3 | 4 | 0 | Fragile but essential — only standard Blight counter |
| Bioelectric | 2 | 2 | 3 | 0 | Lore-strong — only Mineral + Blight crack it |
| Cryo | 3 | 3 | 2 | 0 | Aggressive disruptor |
| Mineral | 4 | 3 | 5 | +1 | Anchor — most connected type |
| Toxic | 2 | 3 | 2 | -1 | Surgical aggressor |
| Neural | 2 | 3 | 2 | -1 | Surgical specialist |
| Ferro | 2 | 3 | 6 | -1 | Defensive tank — most resistances |
| Kinetic | 2 | 3 | 2 | -1 | Physical powerhouse |
| Aero | 2 | 2 | 3 | 0 | Fast and evasive |
| Sonic | 2 | 2 | 2 | 0 | Disruptive specialist |
| Ark | 3 | 2 | 6 | +1 | Stability apex — tanky purifier |
| Blight | 3 | 2 | 5 | +1 | Instability apex — chaotic aggressor |

### 3.8 Apex Type Design Notes

**Ark (Genetic Stability)**
- Pristine ancient DNA, the uncorrupted genetic template
- Rare wild encounters — "living fossils," template species found in DNA vaults and sealed temples
- Tank identity: many resistances (6), fewer offensive targets (3)
- Standard counter: Thermal (heat = entropy, breaks molecular order)
- Scales with new types by gaining more resistances (gets tankier)

**Blight (Genetic Instability)**
- Uncontrolled mutation, genetic entropy, DNA in freefall
- Creatures can gain Blight as a secondary type when instability reaches 80+
- Aggressor identity: targets precision types (Bioelectric, Neural), fewer resistances
- Standard counter: Organic (life absorbs and metabolizes chaos)
- Scales with new types by gaining more offensive targets (gets scarier but more fragile)

### 3.9 TypeChart Implementation

```csharp
namespace GeneForge.Combat
{
    /// <summary>
    /// Static type effectiveness matrix for 14 genome types.
    /// Initialized once at startup. Thread-safe after initialization.
    /// ADR-007: static array, not ScriptableObject.
    /// </summary>
    public static class TypeChart
    {
        // Dimensions: [attackingTypeIndex, defendingTypeIndex]
        // Index 0 = None (neutral), indices 1-14 = Genome types
        private static float[,] _matrix;
        private static bool _initialized;

        private const int TypeCount = 15; // 0=None, 1-14=Genome types

        public static void Initialize()
        {
            if (_initialized) return;

            _matrix = new float[TypeCount, TypeCount];

            // Default all to 1.0 (neutral)
            for (int a = 0; a < TypeCount; a++)
            for (int d = 0; d < TypeCount; d++)
                _matrix[a, d] = 1.0f;

            // ── Self-resist: every type resists itself ──────────────────
            for (int i = 1; i < TypeCount; i++)
                _matrix[i, i] = 0.5f;

            // ── Core Triangle 1: Elemental ──────────────────────────────
            Set(CreatureType.Thermal, CreatureType.Organic,     2.0f); // #1
            Set(CreatureType.Organic, CreatureType.Aqua,        2.0f); // #2
            Set(CreatureType.Aqua,    CreatureType.Thermal,     2.0f); // #3

            // ── Core Triangle 2: Physical ───────────────────────────────
            Set(CreatureType.Mineral,     CreatureType.Bioelectric, 2.0f); // #4
            Set(CreatureType.Bioelectric, CreatureType.Aqua,        2.0f); // #5
            Set(CreatureType.Aqua,        CreatureType.Mineral,     2.0f); // #6

            // ── Core Triangle 3: Mental ─────────────────────────────────
            Set(CreatureType.Neural,  CreatureType.Kinetic, 2.0f); // #7
            Set(CreatureType.Kinetic, CreatureType.Ferro,   2.0f); // #8
            Set(CreatureType.Ferro,   CreatureType.Neural,  2.0f); // #9

            // ── Core Triangle 4: Atmospheric ────────────────────────────
            Set(CreatureType.Cryo, CreatureType.Aero,  2.0f); // #10
            Set(CreatureType.Aero, CreatureType.Sonic,  2.0f); // #11
            Set(CreatureType.Sonic, CreatureType.Cryo,  2.0f); // #12

            // ── Cross-Links ─────────────────────────────────────────────
            Set(CreatureType.Thermal,     CreatureType.Cryo,     2.0f); // #13
            Set(CreatureType.Thermal,     CreatureType.Ferro,    2.0f); // #14
            Set(CreatureType.Cryo,        CreatureType.Organic,  2.0f); // #15
            Set(CreatureType.Cryo,        CreatureType.Kinetic,  2.0f); // #16
            Set(CreatureType.Mineral,     CreatureType.Toxic,    2.0f); // #17
            Set(CreatureType.Mineral,     CreatureType.Sonic,    2.0f); // #18
            Set(CreatureType.Mineral,     CreatureType.Thermal,  2.0f); // #19
            Set(CreatureType.Toxic,       CreatureType.Organic,  2.0f); // #20
            Set(CreatureType.Toxic,       CreatureType.Ferro,    2.0f); // #21
            Set(CreatureType.Neural,      CreatureType.Toxic,    2.0f); // #22
            Set(CreatureType.Ferro,       CreatureType.Cryo,     2.0f); // #23
            Set(CreatureType.Kinetic,     CreatureType.Mineral,  2.0f); // #24
            Set(CreatureType.Bioelectric, CreatureType.Aero,     2.0f); // #25
            Set(CreatureType.Aero,        CreatureType.Thermal,  2.0f); // #26
            Set(CreatureType.Organic,     CreatureType.Mineral,  2.0f); // #27

            // ── Apex ────────────────────────────────────────────────────
            Set(CreatureType.Ark,     CreatureType.Blight,      2.0f); // #28
            Set(CreatureType.Ark,     CreatureType.Toxic,       2.0f); // #29
            Set(CreatureType.Ark,     CreatureType.Kinetic,     2.0f); // #30
            Set(CreatureType.Blight,  CreatureType.Ark,         2.0f); // #31
            Set(CreatureType.Blight,  CreatureType.Bioelectric, 2.0f); // #32
            Set(CreatureType.Blight,  CreatureType.Neural,      2.0f); // #33
            Set(CreatureType.Thermal, CreatureType.Ark,         2.0f); // #34
            Set(CreatureType.Organic, CreatureType.Blight,      2.0f); // #35
            Set(CreatureType.Sonic,   CreatureType.Neural,      2.0f); // #36

            // ── Additional Resistances (0.5x) ───────────────────────────
            // Thermal resists: Organic, Cryo, Ferro
            Set(CreatureType.Organic, CreatureType.Thermal, 0.5f);
            Set(CreatureType.Cryo,    CreatureType.Thermal, 0.5f);
            Set(CreatureType.Ferro,   CreatureType.Thermal, 0.5f);

            // Aqua resists: Thermal, Cryo
            Set(CreatureType.Thermal, CreatureType.Aqua, 0.5f);
            Set(CreatureType.Cryo,    CreatureType.Aqua, 0.5f);

            // Organic resists: Aqua, Bioelectric, Mineral
            Set(CreatureType.Aqua,        CreatureType.Organic, 0.5f);
            Set(CreatureType.Bioelectric, CreatureType.Organic, 0.5f);
            Set(CreatureType.Mineral,     CreatureType.Organic, 0.5f);

            // Bioelectric resists: Aero, Ferro
            Set(CreatureType.Aero,  CreatureType.Bioelectric, 0.5f);
            Set(CreatureType.Ferro, CreatureType.Bioelectric, 0.5f);

            // Cryo resists: Aqua
            Set(CreatureType.Aqua, CreatureType.Cryo, 0.5f);

            // Mineral resists: Thermal, Bioelectric, Toxic, Sonic
            Set(CreatureType.Thermal,     CreatureType.Mineral, 0.5f);
            Set(CreatureType.Bioelectric, CreatureType.Mineral, 0.5f);
            Set(CreatureType.Toxic,       CreatureType.Mineral, 0.5f);
            Set(CreatureType.Sonic,       CreatureType.Mineral, 0.5f);

            // Toxic resists: Organic
            Set(CreatureType.Organic, CreatureType.Toxic, 0.5f);

            // Neural resists: Toxic
            Set(CreatureType.Toxic, CreatureType.Neural, 0.5f);

            // Ferro resists: Cryo, Organic, Neural, Sonic, Aero
            Set(CreatureType.Cryo,    CreatureType.Ferro, 0.5f);
            Set(CreatureType.Organic, CreatureType.Ferro, 0.5f);
            Set(CreatureType.Neural,  CreatureType.Ferro, 0.5f);
            Set(CreatureType.Sonic,   CreatureType.Ferro, 0.5f);
            Set(CreatureType.Aero,    CreatureType.Ferro, 0.5f);

            // Kinetic resists: Sonic
            Set(CreatureType.Sonic, CreatureType.Kinetic, 0.5f);

            // Aero resists: Organic, Kinetic
            Set(CreatureType.Organic, CreatureType.Aero, 0.5f);
            Set(CreatureType.Kinetic, CreatureType.Aero, 0.5f);

            // Sonic resists: Kinetic
            Set(CreatureType.Kinetic, CreatureType.Sonic, 0.5f);

            // Ark resists: Toxic, Neural, Cryo, Sonic, Mineral
            Set(CreatureType.Toxic,   CreatureType.Ark, 0.5f);
            Set(CreatureType.Neural,  CreatureType.Ark, 0.5f);
            Set(CreatureType.Cryo,    CreatureType.Ark, 0.5f);
            Set(CreatureType.Sonic,   CreatureType.Ark, 0.5f);
            Set(CreatureType.Mineral, CreatureType.Ark, 0.5f);

            // Blight resists: Thermal, Bioelectric, Toxic, Kinetic
            Set(CreatureType.Thermal,     CreatureType.Blight, 0.5f);
            Set(CreatureType.Bioelectric, CreatureType.Blight, 0.5f);
            Set(CreatureType.Toxic,       CreatureType.Blight, 0.5f);
            Set(CreatureType.Kinetic,     CreatureType.Blight, 0.5f);

            _initialized = true;
        }

        private static void Set(CreatureType atk, CreatureType def, float value)
            => _matrix[(int)atk, (int)def] = value;

        /// <summary>
        /// Returns the combined type effectiveness multiplier for an attack.
        /// If target has two types, both multipliers are multiplied together.
        /// </summary>
        /// <param name="attackType">The genome type of the move.</param>
        /// <param name="primaryType">The target's primary genome type.</param>
        /// <param name="secondaryType">The target's secondary genome type (None if single-type).</param>
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
        /// No Immune label — minimum multiplier is 0.25x (dual resist).
        /// </summary>
        public static EffectivenessLabel GetLabel(float multiplier)
        {
            if (multiplier < 1f)    return EffectivenessLabel.Resisted;
            if (multiplier > 1f)    return EffectivenessLabel.SuperEffective;
            return EffectivenessLabel.Neutral;
        }

        /// <summary>
        /// Calculate STAB multiplier.
        /// Returns StabMultiplier if move type matches either creature type; 1.0 otherwise.
        /// Also applies to types granted via DNA type infusion (see DNA Alteration System).
        /// </summary>
        public static float GetStab(
            CreatureType moveType,
            CreatureType creaturePrimaryType,
            CreatureType creatureSecondaryType = CreatureType.None)
        {
            if (moveType == CreatureType.None) return 1.0f;

            bool hasStab = moveType == creaturePrimaryType
                        || (creatureSecondaryType != CreatureType.None
                            && moveType == creatureSecondaryType);
            return hasStab ? StabMultiplier : 1.0f;
        }

        // ── Constants ──────────────────────────────────────────────────
        public const float StabMultiplier = 1.5f;
        public const float SuperEffectiveMultiplier = 2.0f;
        public const float ResistedMultiplier = 0.5f;
    }

    public enum EffectivenessLabel
    {
        Resisted         = 0,
        Neutral          = 1,
        SuperEffective   = 2
    }
}
```

### 3.10 Dual-Type Interaction Examples

| Move Type | Target Types | Calculation | Result |
|-----------|-------------|-------------|--------|
| Thermal | Organic / Aqua | 2.0 × 0.5 | 1.0 (neutral — cancel out) |
| Aqua | Thermal / Mineral | 0.5 × 2.0 | 1.0 (neutral — cancel out) |
| Cryo | Organic / Aero | 2.0 × 2.0 | 4.0 (double super effective) |
| Mineral | Bioelectric / Toxic | 2.0 × 2.0 | 4.0 (double super effective) |
| Organic | Ferro / Toxic | 0.5 × 0.5 | 0.25 (double resisted — minimum) |
| Thermal | Cryo / Ferro | 2.0 × 2.0 | 4.0 (double super effective) |
| Blight | Bioelectric / Neural | 2.0 × 2.0 | 4.0 (Blight devastates precision dual-types) |
| Ark | Toxic / Kinetic | 2.0 × 2.0 | 4.0 (Ark purifies chaos dual-types) |

### 3.11 STAB + Type Effectiveness Combined

Both multipliers are applied independently and then multiplied together in the Damage & Health System. TypeChart only computes each factor; combination is done by the damage formula.

```
finalMultiplier = STAB × typeEffectiveness
```

Example: Thermal creature uses Thermal move against Organic target:
- STAB = 1.5 (Thermal creature + Thermal move)
- TypeEffectiveness = 2.0 (Thermal vs Organic)
- Combined = 1.5 × 2.0 = **3.0**

Example: Thermal creature uses Thermal move against Organic/Aero dual-type:
- STAB = 1.5
- TypeEffectiveness = 2.0 × 0.5 = 1.0 (Organic weak, Aero neutral — wait, Thermal vs Aero is 1.0)
- Combined = 1.5 × 1.0 = **1.5** (STAB only)

## 4. Formulas

| Formula | Expression |
|---------|-----------|
| Single-type effectiveness | `matrix[attackType, defenseType]` |
| Dual-type effectiveness | `matrix[attackType, primaryType] × matrix[attackType, secondaryType]` |
| STAB bonus | `1.5` if move type matches any creature type (including infused types), else `1.0` |
| Combined STAB + effectiveness | `STAB × typeEffectiveness` (computed in DamageSystem) |
| Max possible multiplier (dual-type) | `2.0 × 2.0 × 1.5 = 6.0` (dual super effective + STAB) |
| Min possible multiplier (dual resist) | `0.5 × 0.5 = 0.25` (no immunities — floor is 0.25) |

## 5. Edge Cases

| Situation | Behavior |
|-----------|----------|
| `GetMultiplier` called before `Initialize()` | Returns 1.0, logs error |
| Move type is `None` | Returns 1.0 (neutral) for all target types |
| Target secondary type is `None` | Only primary type effectiveness applied |
| Same type in both primary and secondary slots | Both applied: e.g. Thermal vs Cryo/Cryo = 2.0 × 2.0 = 4.0 |
| Attacking type not in matrix bounds | Array index out of range — guarded by enum int range (0–14) |
| Post-MVP type added without matrix expansion | Matrix initialized with `TypeCount`; new type at index > TypeCount-1 throws; must increment `TypeCount` and re-populate |
| Dual-type where one type is SE and the other resists | Multiply: 2.0 × 0.5 = 1.0 (cancels to neutral) |
| Dual-type where both types resist | Multiply: 0.5 × 0.5 = 0.25 (double resisted — minimum possible) |
| Creature gains Blight secondary type at instability 80+ | GetMultiplier uses both primary and Blight; defensive weaknesses of Blight now apply |
| DNA type infusion grants STAB | GetStab checks both primary and secondary type (infused type counts as secondary for STAB) |

## 6. Dependencies

| Dependency | Direction | Notes |
|------------|-----------|-------|
| `CreatureType` enum | Inbound | Integer values used as matrix indices; defined in this system |
| `ConfigLoader` | Sibling | Both initialized in Boot before game starts |
| Damage & Health System | Outbound | Sole consumer of `GetMultiplier` and `GetStab` |
| Combat UI / Feedback | Outbound | Uses `GetLabel` for effectiveness callout display |
| DNA Alteration System | Outbound | Type infusion grants STAB for infused type via secondary type slot |
| Creature Instance | Outbound | Secondary type field updated when instability >= 80 (Blight) |
| AI Decision System | Outbound | Uses `GetMultiplier` for damage estimation scoring |

## 7. Tuning Knobs

| Parameter | Location | Default | Notes |
|-----------|----------|---------|-------|
| `StabMultiplier` | `TypeChart` const | `1.5f` | Standard STAB bonus; applies to primary and secondary types |
| `SuperEffectiveMultiplier` | `TypeChart` const | `2.0f` | Reference value; actual matrix entries set directly |
| `ResistedMultiplier` | `TypeChart` const | `0.5f` | Reference value |
| `TypeCount` | `TypeChart` const | `15` | Must be updated when adding new types (current: 0=None + 14 types) |
| Matrix values | `TypeChart.Initialize()` | See Section 3.6 | Any matchup can be rebalanced by changing a single `Set()` call |

## 8. Acceptance Criteria

- [ ] `TypeChart.Initialize()` completes without errors
- [ ] `GetMultiplier(Thermal, Organic, None)` returns `2.0f`
- [ ] `GetMultiplier(Thermal, Aqua, None)` returns `0.5f`
- [ ] `GetMultiplier(Aqua, Thermal, None)` returns `2.0f`
- [ ] `GetMultiplier(Mineral, Bioelectric, None)` returns `2.0f`
- [ ] `GetMultiplier(Cryo, Organic, Aero)` returns `4.0f` (dual SE)
- [ ] `GetMultiplier(Organic, Ferro, Toxic)` returns `0.25f` (dual resist)
- [ ] `GetMultiplier(Ark, Blight, None)` returns `2.0f`
- [ ] `GetMultiplier(Thermal, Ark, None)` returns `2.0f`
- [ ] `GetMultiplier(Organic, Blight, None)` returns `2.0f`
- [ ] `GetMultiplier(Sonic, Neural, None)` returns `2.0f`
- [ ] `GetStab(Thermal, Thermal, None)` returns `1.5f`
- [ ] `GetStab(Thermal, Aqua, Organic)` returns `1.0f`
- [ ] `GetStab(Thermal, Aqua, Thermal)` returns `1.5f` (secondary type STAB)
- [ ] `GetStab(None, Thermal, None)` returns `1.0f`
- [ ] `GetMultiplier` before `Initialize()` returns `1.0f` and logs error
- [ ] `GetLabel(2.0f)` returns `SuperEffective`
- [ ] `GetLabel(0.5f)` returns `Resisted`
- [ ] `GetLabel(1.0f)` returns `Neutral`
- [ ] No `EffectivenessLabel.Immune` value exists in the enum
- [ ] Matrix has no 0.0 entries (no immunities — verified by iterating all cells)
- [ ] EditMode tests cover all 36 SE relationships from Section 3.4
- [ ] EditMode tests verify all resistance entries from Section 3.5
- [ ] No allocation per call (static array, no list/dictionary)
- [ ] All 14 types match the balance summary in Section 3.7 (SE count, weakness count, resistance count)
