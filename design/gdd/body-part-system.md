# Body Part System

## 1. Overview

The Body Part System gives creatures modular anatomy defined by their archetype. Each creature archetype (Bipedal, Quadruped, Serpentine, Avian, Amorphous) exposes a different set of named body slots. Players acquire part blueprints through DNA splicing, battle rewards, and DNA Vault discoveries, then equip them to matching slots. Parts are not cosmetic: they unlock moves, shift type affinities, modify stats, interact with grid terrain, and conflict or synergize with each other. Parts gain XP through battle use and level up independently, growing stronger over time. Three or more parts from the same type category trigger a set bonus.

## 2. Player Fantasy

Your creature's body is a canvas and a build system simultaneously. Swapping Wings onto your heavy ground-type fighter so it can leap elevated tiles, then discovering that Wings + Claws + Tail creates the "Aerial Predator" set bonus, feels like unlocking a hidden combo. Every part decision has a tradeoff. A creature covered in heavy armor plating looks powerful and is — but it will never fly. The visible part geometry makes your build choices legible at a glance.

## 3. Detailed Rules

### 3.1 Archetypes and Slot Layouts

| Archetype | Available Slots |
|-----------|----------------|
| Bipedal | Head, Back, LeftArm, RightArm, Tail, Legs |
| Quadruped | Head, Back, Tail, Legs, Hide |
| Serpentine | Head, BodyUpper, BodyLower, Tail |
| Avian | Head, Wings, Tail, Talons |
| Amorphous | CoreA, CoreB, CoreC, Appendage |

Each slot accepts only parts whose `slotType` matches the slot name. A `Wings` part has `slotType = "Wings"` and can only go into a `Wings` slot. Bipedal creatures have no Wings slot by default — a DNA mod can unlock a Wings slot (post-MVP upgrade path).

### 3.2 BodyPartConfig ScriptableObject

```csharp
[CreateAssetMenu(menuName = "GeneForge/BodyPartConfig")]
public class BodyPartConfig : ScriptableObject
{
    [Header("Identity")]
    public string id;                          // kebab-case, e.g. "venom-glands"
    public string displayName;                 // "Venom Glands"
    public BodyPartCategory category;          // Offensive, Defensive, Utility, Aura
    public string slotType;                    // Must match a slot name on the archetype

    [Header("Stats")]
    public StatModifierSet statModifiers;      // Flat bonuses to creature stats
    public float weight;                       // Affects movement cost and flight conflicts

    [Header("Moves and Type")]
    public List<string> movesUnlocked;         // Move IDs unlocked while this part is equipped
    public TypeAffinityChange typeAffinityChange; // Adds resistance, weakness, or immunity

    [Header("Synergy and Conflict")]
    public List<string> synergySet;            // Set name(s) this part contributes to
    public List<string> conflictsWith;         // Part IDs that cannot coexist with this part

    [Header("Progression")]
    public DnaRarity rarity;
    public int xpToNextLevel;                  // XP required to reach part level 2 (scales per level)
    public int maxLevel;                       // Default 5

    [Header("Species")]
    public string signatureSpeciesId;          // If non-empty, this species gets bonus stat values
}

[System.Serializable]
public class TypeAffinityChange
{
    public CreatureType affectedType;
    public AffinityModifier modifier;          // Resistance (0.5x), Immunity (0x), Weakness (2x)
}
```

### 3.3 Part Catalog (MVP Set)

| Part ID | Display Name | Category | Slot | Key Effect |
|---------|-------------|----------|------|-----------|
| `wings-feathered` | Feathered Wings | Utility | Wings | Unlocks Aerial Dash; ignores height movement cost |
| `wings-membrane` | Membrane Wings | Utility | Wings | Unlocks Glide; +Evasion on elevated tiles |
| `horns-bone` | Bone Horns | Offensive | Head | +ATK; unlocks Horn Charge |
| `horns-crystal` | Crystal Horns | Offensive | Head | +ATK; unlocks Crystal Pierce; Rock resistance |
| `claws-rending` | Rending Claws | Offensive | LeftArm/RightArm | +ATK; unlocks Rend; inflicts Bleed |
| `claws-venom` | Venom Claws | Offensive | LeftArm/RightArm | Basic attack gains 20% poison chance |
| `shell-carapace` | Carapace Shell | Defensive | Back/Hide | +DEF; Fire weakness |
| `shell-crystal` | Crystal Shell | Defensive | Back/Hide | +DEF; +DEF; Rock resistance |
| `fangs-serrated` | Serrated Fangs | Offensive | Head | +ATK; unlocks Fang Strike |
| `eyes-predator` | Predator Eyes | Utility | Head | +accuracy; cannot be blinded |
| `eyes-compound` | Compound Eyes | Utility | Head | +Evasion; reveals stealth creatures |
| `aura-flame` | Flame Aura | Aura | Back | Burns attackers on contact; conflicts with Frost Aura |
| `aura-frost` | Frost Aura | Aura | Back | Slows adjacent enemies; conflicts with Flame Aura |
| `glands-venom` | Venom Glands | Utility | BodyUpper/Back | Basic attack gains poison chance; unlocks Toxic Spray |
| `tail-blade` | Blade Tail | Offensive | Tail | +ATK; unlocks Tail Whip; counter-attack on physical hit |
| `tail-weight` | Heavy Tail | Defensive | Tail | +DEF; -SPD; unlocks Slam |
| `limbs-extra` | Extra Limbs | Offensive | Arms | +ATK; conflicts with heavy shells |

### 3.4 Equipping and Unequipping Parts

- Parts can only be equipped and unequipped at a research station.
- Unequipping a part removes its stat bonuses, moves, and type affinity changes immediately.
- If a creature has learned moves that require the part (via `movesUnlocked`), those moves are suspended (greyed out) while the part is unequipped. They return when the part is re-equipped.
- Unequipping does not refund part XP or reset part level.
- A creature cannot have the same part equipped twice simultaneously.

### 3.5 Part Conflicts

A part with a non-empty `conflictsWith` list cannot be equipped if any listed part is already in a slot on the same creature. The equip button is greyed out with a tooltip naming the conflicting part.

Hard-coded conflict rules (also expressible via `conflictsWith`):
- Any part with `weight > HeavyWeightThreshold` (default 15) conflicts with any Wings part.
- `aura-flame` conflicts with `aura-frost`.
- Two parts of category `Aura` cannot occupy two different slots simultaneously (max 1 Aura per creature).

### 3.6 Part Synergy Sets

If a creature has 3 or more parts whose `synergySet` list contains the same set name, a set bonus activates. Set bonuses are defined in `PartSynergyConfig` ScriptableObjects and apply passively.

Example sets:

| Set Name | Required Parts (3 of) | Bonus |
|----------|-----------------------|-------|
| `aerial-predator` | wings-*, claws-*, tail-blade | +15% damage on the turn after moving to elevated tile |
| `inferno-body` | aura-flame, shell-carapace, glands-venom (flame variant) | Fire moves +20% power; immune to Burn self-infliction |
| `toxic-chassis` | glands-venom, claws-venom, eyes-compound | Poison applied by this creature stacks to Toxic automatically |
| `crystal-fortress` | shell-crystal, horns-crystal, eyes-compound | +25% DEF; incoming magic damage reflected 5% |

Set bonuses are additive with other bonuses. Only one set can be active at a time (if criteria for two sets are met, the one with more matching parts wins; tie = alphabetical id).

### 3.7 Part Leveling

Parts earn XP at the end of each battle in which the creature participated and the part's effects were relevant (any move from `movesUnlocked` was used, OR a stat modifier was active during damage taken/dealt).

```csharp
public class EquippedPart
{
    public BodyPartConfig config;
    public int level;      // 1–5 (config.maxLevel)
    public int currentXp;

    public int XpToNextLevel => config.xpToNextLevel * level; // Scales linearly
}
```

At each level-up, stat bonuses from `statModifiers` increase by 20% of their base value (rounded down). Move power for `movesUnlocked` moves increases by 5% per level above 1.

Signature species bonus: if `config.signatureSpeciesId` matches the creature's species, all stat modifiers are 25% higher and level-up bonuses are 30% higher.

## 4. Formulas

### Part Stat Bonus at Level N

```
effectiveStat = baseStat + (statModifier.flatBonus * (1 + 0.20 * (partLevel - 1)))
```

### Signature Species Bonus

```
effectiveStat = baseStat + (statModifier.flatBonus * 1.25 * (1 + 0.20 * (partLevel - 1)))
```

### Part XP to Next Level

```
xpRequired(level) = config.xpToNextLevel * level
```

e.g. if `xpToNextLevel = 100`: Level 1→2 costs 100 XP, Level 2→3 costs 200 XP, etc.

### Weight Conflict Threshold

```
isHeavy = part.weight > HeavyWeightThreshold    // HeavyWeightThreshold = 15 (tunable)
isFlying = part.slotType == "Wings"
conflictTriggers = isHeavy && anyWingPartEquipped
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Part is unequipped while a suspended move is the creature's only move | Last remaining move cannot be suspended; unequip is blocked with warning "This creature would have no usable moves" |
| Creature archetype has no matching slot for a part | Equip option not shown for this creature; part remains in inventory |
| Set bonus is active; player removes one part dropping count below 3 | Set bonus deactivates immediately; no partial bonus |
| Two sets both qualify (tie) | Alphabetically first set name wins |
| Part levels up mid-battle | Level-up is deferred until end of battle to avoid mid-combat stat recalculations |
| Player equips a part whose `movesUnlocked` move they have already manually forgotten | Move is re-added to the creature's move pool; creature may need to drop a move if at the 4-move cap |
| Signature species bonus applied to a creature that had its type changed via DNA mod | Bonus checks original `speciesId`, not current type; bonus still applies |
| Part with `conflictsWith` list tries to equip but the conflicting part is currently suspended (slot empty) | Conflict check ignores suspended parts — only checks currently equipped parts |
| Part XP overflow at max level (level 5) | XP pool freezes; no further XP accumulates |

## 6. Dependencies

| System | Dependency Type | Notes |
|--------|----------------|-------|
| Creature Instance | Read/Write | Equip slot state, part XP tracking |
| Creature Database | Read | Archetype slot layout per species |
| Move Database | Read | Validate that `movesUnlocked` IDs exist |
| DNA Alteration System | Read | Parts may be unlocked via DNA splicing blueprints |
| Move Customization System | Write | Part equip/unequip triggers move pool update |
| Type Chart System | Read | Type affinity changes must reference valid types |
| Combat System (Damage & Health) | Read | Weight/conflict rules affect grid movement cost |
| Party Management UI | Read/Write | Part equip/unequip interface |
| Save/Load System | Read/Write | Equipped parts, part levels, and XP persisted |

## 7. Tuning Knobs

| Parameter | Default | Range | Notes |
|-----------|---------|-------|-------|
| `MaxPartsPerCreature` | 1 per slot (slot count varies by archetype) | — | Enforced by slot layout |
| `MaxAurasPerCreature` | 1 | 1–2 | Hard cap on Aura category parts |
| `HeavyWeightThreshold` | 15 | 10–25 | Above this weight, part conflicts with Wings |
| `SynergyMinParts` | 3 | 2–4 | Parts required to activate a set bonus |
| `PartLevelStatScaling` | 0.20 | 0.10–0.35 | Stat bonus increase per level (fraction of base) |
| `PartLevelMoveScaling` | 0.05 | 0.02–0.10 | Move power increase per level above 1 |
| `SignatureStatBonus` | 1.25 | 1.10–1.50 | Stat multiplier for signature species |
| `SignatureLevelBonus` | 1.30 | 1.10–1.50 | Level-up bonus multiplier for signature species |
| `BaseXpPerLevel` | 100 | 50–200 | `config.xpToNextLevel` default for new assets |
| `MaxPartLevel` | 5 | 3–7 | Per-part override allowed via asset |

## 8. Acceptance Criteria

- [ ] Each archetype exposes the correct slot names and counts as defined in Section 3.1.
- [ ] `BodyPartConfig` ScriptableObjects can be created via the Create Asset menu without errors.
- [ ] Equipping a part immediately applies its `statModifiers`, `movesUnlocked`, and `typeAffinityChange` to the creature.
- [ ] Unequipping a part removes all its effects; suspended moves are greyed out in the move panel.
- [ ] Conflict rules block equip when a conflicting part is already equipped; tooltip names the conflict.
- [ ] Heavy weight + Wings conflict is enforced regardless of `conflictsWith` list.
- [ ] Only one Aura part can be equipped per creature.
- [ ] Set bonus activates when 3+ parts share a `synergySet` name; deactivates when count drops below 3.
- [ ] Part XP accumulates correctly and triggers level-up at the correct threshold.
- [ ] Stat bonuses increase by the correct percentage at each level-up.
- [ ] Signature species stat bonus is 25% higher than the base mod value (unit tested).
- [ ] Part equip/unequip data survives a save/load cycle intact.
- [ ] Move pool correctly reflects suspended moves after part removal.
