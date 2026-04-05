# DNA Alteration System

## 1. Overview

The DNA Alteration System is Gene Forge's core expression mechanic, replacing evolution as the primary growth vector. Players extract DNA Materials from captured and defeated creatures, then apply them to their party at research stations. Mods fall into three categories: stat boosts, perk grafts, and type infusions. Each mod adds Instability to the target creature's stability meter (0–100). At 50+ instability moves gain unpredictable bonus effects; at 80+ the creature risks disobedience or triggers rare breakthrough moments. Aggressive splices display a success percentage before committing, and failed splices leave permanent scar marks. A DNA Lineage Tree tracks every modification ever applied to a creature.

## 2. Player Fantasy

You are a genetic pioneer building creatures that are unmistakably yours. Slapping a fire resistance perk onto your water creature because you need it for the next zone feels clever. Pushing instability past 80 to chase a breakthrough feels dangerous and exhilarating. When a splice fails and leaves a scar, that creature has a story. The Lineage Tree is your lab journal — proof that you engineered something no one else has.

## 3. Detailed Rules

### 3.1 DNA Materials

DNA Materials are consumable items extracted from creatures. Each material has:
- `speciesSourceId` — which species it came from
- `materialType` — StatBoost, PerkGraft, TypeInfusion, or Personality
- `rarity` — Common, Uncommon, Rare, Legendary
- `instabilityGrant` — how much instability this material adds when applied (5–35 points)

Extraction sources:
- **Capture**: guarantees 1-2 materials matching the captured species.
- **Defeating in battle**: 50% chance of 1 material drop; rarity scales with creature level.
- **Expedition rewards**: see Expedition System.
- **DNA Vault discovery**: unique Forbidden Mod materials only available here.

### 3.2 Mod Categories

#### Stat Boosts
Direct numerical increases to a creature's base stats. Applied immediately and permanently.

Affected stats: `HP`, `ATK`, `DEF`, `SPD`, `ACC`.

Each stat boost material specifies `statTarget` and `bonusAmount`. Bonus is flat, not percentage.

#### Perk Grafts
Passive abilities transplanted from a donor species. Each creature can hold up to `MaxPerks` perks simultaneously (default 4). Perks are defined in `PerkConfig` ScriptableObjects.

Perk examples: `FireResistance`, `RegenOnWaterTile`, `FirstStrikeOnLowHP`, `TauntOnEntry`.

#### Type Infusions
Infuses the target creature with a genome type's DNA, granting two benefits:
1. **Defensive resistance**: the creature takes 0.5× damage from the infused type's incoming attacks.
2. **STAB (Same Type Attack Bonus)**: the creature deals 1.5× damage on moves matching the infused type — but only if it has body part access to the move's damage form.

This makes type infusion significantly more valuable than a simple resistance buff. A Mineral creature infused with Thermal DNA gains Thermal resistance (0.5× incoming) AND Thermal STAB (1.5× on Thermal moves it can use). The body part requirement prevents STAB from being free — the player must invest in the right parts to unlock the offensive benefit.

Up to 2 type infusions can be active at once. A third replaces the oldest.

Type infusion also slightly alters the creature's visual tint (handled by Color & Pattern System).

**Note**: Instability is gained ONLY through DNA engineering at research stations. Enemy moves in combat never apply instability or mutagenic effects.

### 3.3 Instability Mechanics

```csharp
[System.Serializable]
public class DnaMod
{
    public string modId;
    public DnaModCategory category;    // StatBoost, PerkGraft, TypeInfusion
    public string sourceSpeciesId;
    public DnaRarity rarity;
    public int instabilityGrant;       // Added to creature on apply
    public int appliedAtLevel;
    public bool isScarred;             // True if this mod slot has a scar from a failed splice
}

public enum DnaRarity { Common, Uncommon, Rare, Legendary }

public class CreatureInstance
{
    // ... other fields ...
    public int instability;            // 0-100
    public List<DnaMod> appliedMods;
}
```

Instability thresholds and their effects:

| Threshold | Effect |
|-----------|--------|
| 0–49 | Normal behavior |
| 50–79 | Moves gain random bonus effects (see Move Customization System) |
| 80–99 | **Blight genome type gained as secondary type** (with all defensive implications — weak to Ark and Organic). Creature may disobey (skip turn, use random move) OR trigger a Breakthrough |
| 100 | Capped at 100; Breakthrough chance maximized; disobedience is frequent; Blight secondary type active |

**Blight Secondary Type (Instability 80+):** When a creature's instability reaches 80 or higher, it gains `Blight` as a secondary genome type. This is applied automatically and cannot be removed except by reducing instability below 80 (post-MVP: instability management tools at Station Level 4). While Blight is active as a secondary type:
- The creature is weak to Ark (2.0×) and Organic (2.0×) attacks
- The creature resists Thermal, Bioelectric, Toxic, and Kinetic attacks (0.5×)
- The creature gains STAB on Blight-type moves (if it knows any)
- Type effectiveness is calculated multiplicatively with the creature's primary type
- This replaces any existing secondary genome type for defensive calculations

**Disobedience roll** (checked at the start of each of the creature's turns when instability >= 80):
```
disobeyChance = (instability - 80) / 20f   // 0.0 at 80, 1.0 at 100
```
If disobedience triggers, the creature either skips its turn (50%) or uses a random move targeting a random valid creature (50%).

**Breakthrough**: When a disobedience roll is triggered but a separate d100 roll beats `breakthroughThreshold` (default 15), a Breakthrough fires instead: the creature executes a powerful bonus action (max-power version of a random known move, area-of-effect variant, or applies a beneficial status to itself).

### 3.4 Mutation Risk (Aggressive Splices)

Mods of rarity Rare or Legendary display a `successChance` before the player commits. The player sees the potential outcome on success AND the scar consequence on failure.

```csharp
public static float CalculateSpliceSuccessChance(
    DnaMod mod,
    CreatureInstance target,
    int stationLevel)
{
    float baseChance = mod.rarity switch
    {
        DnaRarity.Common    => 1.0f,
        DnaRarity.Uncommon  => 0.90f,
        DnaRarity.Rare      => 0.70f,
        DnaRarity.Legendary => 0.45f,
        _ => 1.0f
    };

    // Higher instability reduces success chance
    float instabilityPenalty = target.instability * 0.002f; // -0.2% per instability point

    // Higher station level improves success chance
    float stationBonus = (stationLevel - 1) * 0.05f; // +5% per level above 1

    return Mathf.Clamp01(baseChance - instabilityPenalty + stationBonus);
}
```

**On failed splice:**
- The mod material is consumed (lost).
- The target creature gains a **scar** on one of its body regions (visual mark, permanent).
- A `ScarRecord` is appended to the creature's history with `sourceModId`, `timestamp`, and `region`.
- No instability is added from a failed splice.

### 3.5 Applying a Mod (Full Flow)

1. Player opens the DNA Alteration interface at a research station.
2. Player selects a creature from their party.
3. Player selects a DNA Material from their inventory.
4. System displays: mod effect preview, instability change, success chance (if risky).
5. Player confirms.
6. If success chance < 1.0: resolve dice roll against `successChance`.
   - Failure: consume material, apply scar, abort.
   - Success: proceed.
7. Apply mod effect to creature stats or perk list.
8. Add `instabilityGrant` to `creature.instability`.
9. Append `DnaMod` record to `creature.appliedMods`.
10. Update DNA Lineage Tree display.

### 3.6 DNA Lineage Tree

The Lineage Tree is a visual graph stored per creature. Each node represents one applied mod:
- Node color = rarity color.
- Node icon = mod category.
- Scar nodes are shown with a red X overlay.
- Synergy connections drawn between nodes whose combination unlocked a known recipe.

The tree is read-only from the creature's Pokedex full profile view and the Party Management screen.

Post-MVP: trees can be shared as a code string for DNA trading.

### 3.7 Station Level Gates

| Station Level | Mods Available |
|--------------|---------------|
| 1 (Field Lab) | Common stat boosts only |
| 2 (Gene Lab) | Uncommon stat boosts, Common perk grafts |
| 3 (Splice Lab) | Rare stat boosts, Uncommon perk grafts, Common type infusions |
| 4 (Mutation Lab) | Legendary mods, instability management tools |
| 5 (Apex Lab) | Forbidden Mods, all categories |

## 4. Formulas

### Instability Grant per Rarity

| Rarity | `instabilityGrant` Range | Default |
|--------|--------------------------|---------|
| Common | 5–10 | 7 |
| Uncommon | 10–18 | 14 |
| Rare | 18–28 | 23 |
| Legendary | 28–35 | 32 |

The exact value is set per DNA Material asset, within the rarity range.

### Disobedience Chance

```
disobeyChance = (instability - 80) / 20        [only when instability >= 80]
```

### Splice Success Chance

```
successChance = baseChance - (instability * 0.002) + ((stationLevel - 1) * 0.05)
successChance = Clamp(successChance, 0.05, 1.0)   // floor at 5% — never impossible
```

### Total Instability After N Mods

```
totalInstability = Sum(mod[i].instabilityGrant for i in appliedMods)
totalInstability = Min(totalInstability, 100)
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Applying a mod would push instability past 100 | Instability clamps at 100; surplus is discarded; player is warned |
| Player applies a stat boost to a stat already at max | Max stat cap (`MaxStatValue = 999`) enforced; excess is discarded |
| Perk list is full (4 perks) and player tries to graft a 5th | Player is prompted to remove one existing perk first; removal is free but permanent |
| Removed perk leaves a gap in the Lineage Tree | Node remains but is shown as "Removed" in grey; instability granted by that mod is NOT refunded |
| Type infusion slot 3 replaces oldest infusion | Player warned which infusion will be replaced before confirming |
| Legendary mod fails at very high instability | Success floor is 5%; a failure is always possible |
| Applying a mod to a creature with `isBoss == true` (wild trophy) | Not applicable; mods only apply to party creatures |
| DNA Material used for a type infusion that the creature already has natively | Rejected with message: creature already has this genome type natively |
| Creature at instability 79 receives a mod pushing it to 80+ | Blight secondary type activates immediately; player warned before confirming |
| Creature with existing secondary type reaches instability 80+ | Blight replaces the existing secondary type for defensive calculations |
| Instability drops below 80 (post-MVP via management tools) | Blight secondary type is removed; previous secondary type restored if applicable |
| Type infusion grants STAB but creature has no moves of that type | STAB is "latent" — no effect until a matching move is learned or a body part granting appropriate form access is equipped |
| Two mods from same species applied | Allowed; no restriction on same-source stacking unless explicitly noted in the `conflictsWith` field of the mod |
| Breakthrough fires and kills an enemy creature | Valid outcome; XP and drops are awarded normally |

## 6. Dependencies

| System | Dependency Type | Notes |
|--------|----------------|-------|
| Creature Instance | Read/Write | Instability, appliedMods, stats, perk list, secondary genome type (Blight at 80+) |
| Creature Database | Read | `CreatureConfig.baseCatchRate`, species info |
| Type Chart System | Read | Blight type effectiveness lookups; type infusion resistance validation (14 genome types) |
| Damage & Health System | Read | STAB calculation includes infused types; Blight secondary type affects type effectiveness multiplier |
| Station Upgrade System | Read | Station level gates mod access |
| Move Customization System | Write | Instability >= 50 triggers move bonus effects |
| Body Part System | Read | Form access determines whether infused-type STAB can be used offensively |
| Pokedex System | Write | DNA discovery updates lineage data |
| Combat UI | Read | Instability-driven disobedience handled in Turn Manager; Blight icon shown when active |
| Turn Manager | Read/Write | Checks instability at turn start for disobedience |
| Battle Scar System | Write | Failed splices generate scars |
| Save/Load System | Read/Write | Full mod history and instability persisted in save |

## 7. Tuning Knobs

| Parameter | Default | Range | Notes |
|-----------|---------|-------|-------|
| `instabilityGrant` (Common) | 7 | 5–10 | Per-asset override allowed |
| `instabilityGrant` (Uncommon) | 14 | 10–18 | |
| `instabilityGrant` (Rare) | 23 | 18–28 | |
| `instabilityGrant` (Legendary) | 32 | 28–35 | |
| `instabilityThresholdBonus` | 50 | 30–70 | When move bonus effects begin |
| `instabilityThresholdDisobey` | 80 | 60–90 | When disobedience begins |
| `instabilityPenaltyPerPoint` | 0.002 | 0.001–0.005 | Splice success penalty per instability point |
| `stationLevelBonusPerLevel` | 0.05 | 0.02–0.10 | Splice success bonus per station level above 1 |
| `breakthroughThreshold` | 15 | 5–30 | d100 must beat this for breakthrough vs disobey |
| `MaxPerks` | 4 | 2–6 | Max simultaneous perk grafts per creature |
| `successFloor` | 0.05 | 0.01–0.15 | Minimum splice success probability |
| `disobeySkipChance` | 0.5 | 0.3–0.7 | Fraction of disobeys that skip turn (rest = random move) |

## 8. Acceptance Criteria

- [ ] `CalculateSpliceSuccessChance()` returns correct values for all four rarities at 0 instability and station level 1 (unit tested).
- [ ] Instability clamps at 100 and cannot exceed it.
- [ ] Disobedience does not trigger below instability 80.
- [ ] Disobedience chance is exactly 0.0 at instability 80 and 1.0 at instability 100.
- [ ] Failed splice consumes the material, does not add instability, and records a scar on the creature.
- [ ] Successful splice applies all stat/perk/type changes and adds the correct `instabilityGrant`.
- [ ] Station Level 1 blocks Uncommon, Rare, and Legendary mods from the UI.
- [ ] Perk list enforces the `MaxPerks` limit; player cannot graft beyond it without removal.
- [ ] Third type infusion prompts the player about replacement before proceeding.
- [ ] Creatures with instability >= 50 have move bonus effects active (verified in Move Customization System).
- [ ] Creature at instability 80+ gains Blight as secondary genome type.
- [ ] Blight secondary type makes creature weak to Ark (2.0×) and Organic (2.0×).
- [ ] Blight secondary type grants resistance to Thermal, Bioelectric, Toxic, Kinetic (0.5×).
- [ ] Type infusion grants STAB (1.5×) on moves matching the infused type.
- [ ] Type infusion grants defensive resistance (0.5×) to the infused type.
- [ ] STAB from type infusion only applies if creature has body part access to the move's damage form.
- [ ] Instability is never applied by enemy moves in combat — only at research stations.
- [ ] DNA Lineage Tree shows all applied mods with correct rarity colors and scar markers.
- [ ] All mod data persists correctly through a save/load cycle.
- [ ] Applying a mod to a stat already at max cap silently clamps; no crash.
