# Combo Move System

## 1. Overview

The Combo Move System allows two adjacent creatures with Affinity 3+ to execute fusion attacks that cost both their turns. Combo moves are unlocked based on genome type pairings (Thermal+Aqua=Steam Blast, Cryo+Thermal=Frost Flame, etc.) and are discovered as players battle with different creature type combinations. A ComboMoveConfig defines 10-15 combos with power, accuracy, effects, and any animation/VFX requirements.

## 2. Player Fantasy

Discovering that your Thermal creature and Aqua creature together can unleash "Steam Blast" feels like unlocking a secret technique. The combo is powerful enough to justify the cost of both turns, making high-affinity pairs strategically valuable. Executing a planned combo in a tight fight feels like teamwork paying off.

## 3. Detailed Rules

### 3.1 Combo Prerequisites

A combo move can be used when:
1. Two creatures in party are adjacent on the grid
2. Both are alive and not stunned
3. Their Affinity level >= 3
4. A ComboMoveConfig exists for their type pairing
5. It's one of the creatures' turns in the turn order

Both creatures' turns are consumed; the combo executes as a single action.

### 3.2 Combo Catalog (10-15 examples)

| Combo ID | Type 1 | Type 2 | Name | Power | Effect | Notes |
|----------|--------|--------|------|-------|--------|-------|
| `steam-blast` | Thermal | Aqua | Steam Blast | 90 | Burn + Confusion 30% | Area damage, 2-tile radius |
| `frost-flame` | Cryo | Thermal | Frost Flame | 85 | Burn + Freeze 25% | Hits 2-3 tiles in line |
| `electric-tsunami` | Bioelectric | Aqua | Electric Tsunami | 100 | Paralyze 40% | Full grid damage |
| `nature-rage` | Organic | Mineral | Nature's Wrath | 80 | Trap opponent 1 turn | Terrain creates obstacles |
| `poison-cloud` | Toxic | Organic | Toxic Bloom | 70 | Poison + Spore sleep 30% | Area denial |
| `aero-strike` | Aero | Kinetic | Aerial Cataclysm | 120 | Knockback + Vulnerable | High power, leaves enemy elevated |
| `ground-quake` | Mineral | Kinetic | Continental Shift | 95 | Damage + tile alteration | Changes all tiles in area |
| `shadow-strike` | Neural | Blight | Void Echo | 90 | Reduce target DEF -2 | Ranged + debuff |
| `light-judgment` | Ark | Neural | Divine Blessing | 85 | Heal allies 20% HP | Healing combo, support focus |
| `crystal-resonance` | Sonic | Bioelectric | Shatter Pulse | 80 | Stun + damage | Both creatures must be Neural-type compatible |

### 3.3 ComboMoveConfig

```csharp
[CreateAssetMenu(menuName = "GeneForge/ComboMoveConfig")]
public class ComboMoveConfig : ScriptableObject
{
    [System.Serializable]
    public class ComboMove
    {
        public string comboId;
        public CreatureType type1;
        public CreatureType type2;
        public string displayName;
        public int power;
        public float accuracy = 1.0f;
        public List<StatusEffect> onHitEffects;
        public int aoeTileRadius = 0;  // 0 = single target, 1+ = area
        public int maxRange = 6;
        public string particleEffect;
        public AudioClip soundEffect;
        public bool canHeal;
        public int healAmount;  // if canHeal = true
        public bool isKnockback;
        public int knockbackRange = 1;
    }
    
    public List<ComboMove> combos;
}
```

### 3.4 Combo Discovery

Combos are discovered on first use (or auto-discovered):

```csharp
public class ComboMoveSystem
{
    public void OnComboMoveUsed(string comboId, CreatureInstance creature1, CreatureInstance creature2)
    {
        var pokedexEntry1 = PokedexManager.GetOrCreate(creature1.speciesId);
        var pokedexEntry2 = PokedexManager.GetOrCreate(creature2.speciesId);
        
        pokedexEntry1.discoveredCombos.Add(comboId);
        pokedexEntry2.discoveredCombos.Add(comboId);
        
        OnComboDiscovered?.Invoke(comboId, creature1, creature2);
    }
}
```

### 3.5 Targeting & Execution

```csharp
public bool TryExecuteCombo(string comboId, CreatureInstance caster1, CreatureInstance caster2, Vector3Int targetTile)
{
    var combo = ComboDatabase.Get(comboId);
    
    // Range check
    float distance = Vector3.Distance(caster1.GridPos, targetTile);
    if (distance > combo.maxRange) return false;
    
    // Accuracy roll
    if (Random.value > combo.accuracy) return false;  // Miss
    
    // Calculate damage for both creatures
    int damage = CalculateComboDamage(combo, caster1, caster2, targetTile);
    
    // Apply effects
    ApplyDamage(targetTile, damage);
    ApplyStatusEffects(targetTile, combo.onHitEffects);
    
    if (combo.isKnockback)
        KnockbackCreature(targetTile, combo.knockbackRange);
    
    if (combo.canHeal)
        HealAllies(caster1, caster2, combo.healAmount);
    
    // VFX & SFX
    VFXPoolManager.PlayEffect(combo.particleEffect, targetTile);
    AudioManager.PlaySFX(combo.soundEffect);
    
    return true;
}
```

## 4. Formulas

### Combo Damage

```
baseDamage = combo.power
STAB = (caster1.type matches type1 OR caster2.type matches type2) ? 1.5 : 1.0
typeMultiplier = GetTypeEffectiveness(combo.type1, targetCreatureType)
finalDamage = baseDamage * STAB * typeMultiplier
```

### Combo Discovery

```
discovered = first successful hit with combo pair
OR auto-discovered after affinity >= 3 (optional)
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Combo move targets 2 creatures in AoE but only 1 is in range | AoE applies only to in-range targets |
| Combo move targets ally creature by mistake | Damage/effects still apply (no friendly fire toggle needed for combos) |
| One creature in combo pair faints before execution completes | Combo still executes; both participate |
| Combo accuracy roll fails (miss) | No damage; effects don't apply; both turns still consumed |
| Creatures are adjacent but one cannot move (paralyzed, etc.) | Combo cannot be initiated; pair is grayed out |
| Combo has never been discovered but affinity is 3+ | Combo is available but not shown in move list until first use |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Creature Affinity System | Requires affinity >= 3 to unlock combos |
| Grid Tile System | Checks adjacency for combo availability |
| Turn Manager | Consumes both creatures' turns on combo execution |
| Damage & Health System | Calculates combo damage with type effectiveness |
| Pokedex System | Tracks discovered combos per creature |
| VFX System | Displays combo particle effects |
| Audio System | Plays combo-specific sound effects |
| Save/Load System | Persists discovered combos in Pokedex |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `minAffinityForCombo` | 3 | Affinity level required |
| `comboCount` | 15 | Total combos in game |
| `comboDamageMultiplier` | 1.0 | Power scaling factor |
| `comboSTABMultiplier` | 1.5 | STAB bonus on typed combos |
| `maxComboRange` | 6 | Tiles max distance to target |
| `comboPowerRange` | 70–120 | Power values across combos |
| `comboAccuracyRange` | 0.8–1.0 | Accuracy range across combos |

## 8. Acceptance Criteria

- [ ] Combo moves are unavailable until affinity >= 3.
- [ ] Two adjacent creatures can initiate a combo if a ComboMoveConfig exists.
- [ ] Combo move consumes both creatures' turns.
- [ ] Combo damage is calculated with correct STAB and type effectiveness.
- [ ] Accuracy roll is checked; miss prevents execution but still consumes turns.
- [ ] AoE combos damage all creatures in radius (including allies if no friendly fire setting).
- [ ] Combo effects (burns, freezes, heals, knockback) apply correctly.
- [ ] Combos are discovered on first use and appear in Pokedex.
- [ ] VFX and SFX play correctly for each combo.
- [ ] Combo data persists in save (discovered combos, creature affinity states).
- [ ] UI shows combo move option only when prerequisites are met.
