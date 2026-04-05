# Battle Scar System

## 1. Overview

The Battle Scar System applies permanent visible marks to creatures that survive near-death experiences (< 10% HP). Scars are type-colored (burn orange, frost pale-blue, claw red marks, impact grey) and cannot be removed. Scarred creatures are recognized as battle veterans by NPCs and rivals. Scars are visible in battle UI, Creature Forge, Pokedex entries, and async PvP. A creature's scar history is listed in its Pokedex profile, creating a chronicle of struggles.

## 2. Player Fantasy

A heavily scarred creature tells a story of hard-fought battles. Seeing a veteran creature with multiple scars feels prestigious — you've pushed it to the brink and it survived. Rivals acknowledge and respect scarred creatures. The visual marks are permanent badges of honor, not punishment. A creature's scars are as defining as its DNA modifications.

## 3. Detailed Rules

### 3.1 Scar Trigger

A scar is applied when a creature:
1. Survives a battle (not fainted)
2. At any point during battle, HP dropped below 10% of max HP
3. Is healed back above 10% (or battle ends with < 10% HP at some point)

```csharp
public void OnCreatureTakeDamage(CreatureInstance creature, int damageDealt)
{
    int newHP = creature.CurrentHP - damageDealt;
    
    // Trigger scar check at < 10% HP
    if (newHP < creature.maxHP * 0.10f && !creature.HasSurvivalScarThisBattle)
    {
        creature.HasSurvivalScarThisBattle = true;
        creature.LastScarSource = GetDamageSource(damageDealt);  // Fire, Ice, Physical, etc.
    }
    
    creature.CurrentHP = Mathf.Max(0, newHP);
}

public void OnBattleEnd(List<CreatureInstance> participants)
{
    foreach (var creature in participants)
    {
        if (!creature.fainted && creature.HasSurvivalScarThisBattle)
        {
            ApplyScar(creature, creature.LastScarSource);
            creature.HasSurvivalScarThisBattle = false;
        }
    }
}
```

### 3.2 Scar Types & Colors

| Scar Type | Trigger | Color | Visual |
|-----------|---------|-------|--------|
| Burn | Thermal-type damage at <10% HP | Orange-red (#FF4500) | Charred patch |
| Frost | Cryo-type damage at <10% HP | Pale blue (#B0E0E6) | Frost crystalline mark |
| Claw | Physical-form damage at <10% HP | Red (#DC143C) | Parallel scratch lines |
| Impact | Kinetic/Mineral-type damage at <10% HP | Grey (#808080) | Dent or crater |
| Toxic | Toxic-type damage at <10% HP | Green (#32CD32) | Chemical burn |
| Shock | Bioelectric-type damage at <10% HP | Yellow (#FFD700) | Burn marks, branching paths |

### 3.3 ScarData

```csharp
[System.Serializable]
public class ScarData
{
    public string scarId;                    // Unique identifier
    public ScarType scarType;                // Burn, Frost, Claw, Impact, Poison, Electric
    public Color scarColor;
    public Vector3 position;                 // Normalized position on model (0–1)
    public float scale;                      // Size of scar (0.5–1.5)
    public long timestampApplied;            // When scar was applied
    public string battleLocationId;          // Zone where scar was earned
    public int creatureLevelAtScar;          // Level when scar was applied
    
    public ScarData(ScarType type, Vector3 pos, float scl)
    {
        scarType = type;
        scarColor = GetColorForType(type);
        position = pos;
        scale = scl;
        timestampApplied = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}

public class CreatureInstance
{
    public List<ScarData> scars = new();
    
    public void ApplyScar(ScarData scar)
    {
        scars.Add(scar);
        
        // Update creature material to show scar
        UpdateScarTexture();
    }
}
```

### 3.4 Visual Implementation

Scars are rendered as overlays on the creature model:

```csharp
public void RenderScars(CreatureInstance creature)
{
    var material = creature.renderer.material;
    
    // Create procedural scar texture
    Texture2D scarTexture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
    
    foreach (var scar in creature.scars)
    {
        Vector2 screenPos = WorldToTextureCoord(scar.position, scarTexture.width);
        DrawScarOnTexture(scarTexture, scar, screenPos);
    }
    
    material.SetTexture("_ScarMap", scarTexture);
    material.SetFloat("_ScarBlend", 0.4f);  // 40% visibility
}
```

### 3.5 NPC Recognition

NPCs and rivals comment on scars:

```csharp
public string GetScarAcknowledgement(CreatureInstance creature)
{
    int scarCount = creature.scars.Count;
    
    if (scarCount == 0) return "";
    if (scarCount == 1) return "That creature's seen some action...";
    if (scarCount == 3) return "A veteran. Respect.";
    if (scarCount >= 5) return "That creature's been through wars.";
    
    return "";
}
```

## 4. Formulas

### Scar Position & Scale

```
scarPosition = randomizedOnCreatureModel (0–1 normalized)
scarScale = Random(0.5, 1.5)
```

### Scar Visibility

```
scarBlendFactor = 0.4  [40% visible over creature texture]
totalScarCount affects visual density; more scars = darker patches
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Creature survives <10% HP but is healed mid-battle; battle ends normally | Scar is applied (trigger was <10% HP, regardless of final HP) |
| Creature with 5+ scars looks very scarred | Scars overlap naturally; additional scars increase visual density |
| Scar is applied to a creature with cosmetic patterns/glow | Scar texture layers on top; both visible |
| Creature is captured/reset at station; scars remain | Scars are permanent; cannot be removed |
| Creature faints and is swapped for another; fainting creature doesn't get scar | Fainted creatures don't get scars; only survivors |
| Scar from friendly fire (dual battle, ally damage) | Scar still applies; source is marked as ally (rare visual indicator) |
| Async PvP: opponent's scarred creature battles | Scars visible on opponent creature in replay |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Creature Instance | Stores scar list |
| Combat System | Triggers scar logic on damage |
| Damage & Health System | Calculates < 10% threshold |
| Renderer/Graphics | Renders scar texture overlay |
| Pokedex System | Displays scar history in creature profile |
| NPC Dialogue | References scars in conversation |
| Async PvP (post-MVP) | Shows scars in opponent creatures |
| Save/Load System | Persists scar data |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `scarTriggerThreshold` | 0.10 | 10% HP to trigger scar |
| `scarBlendFactor` | 0.40 | 40% visibility over creature |
| `maxVisibleScars` | 10 | Hard cap for visual clarity |
| `scarPositionRandomRadius` | 0.1 | Randomization range on model |
| `scarMinScale` | 0.5 | Minimum scar size |
| `scarMaxScale` | 1.5 | Maximum scar size |

## 8. Acceptance Criteria

- [ ] Scars are applied when creature survives < 10% HP.
- [ ] Scar type (Burn, Frost, Claw, Impact, Poison, Electric) matches damage source.
- [ ] Scar color is type-specific and matches visual scar color.
- [ ] Scars are permanent and cannot be removed.
- [ ] Scars are visible on creature in battle UI.
- [ ] Scars are visible on creature in Creature Forge/Party Management.
- [ ] Scars are listed in Pokedex full profile with timestamp and location.
- [ ] Multiple scars accumulate and are visible together.
- [ ] NPCs comment on scarred creatures (veteran recognition).
- [ ] Rivals acknowledge scars in dialogue.
- [ ] Scar data persists through save/load.
- [ ] Scars visible in async PvP (post-MVP).
