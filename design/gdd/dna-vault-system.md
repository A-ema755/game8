# DNA Vault System

## 1. Overview

DNA Vaults are ancient research facilities hidden in each habitat zone, protected by environmental puzzles and a guardian creature. Vaults contain Forbidden Mods — experimental DNA modifications that are extremely powerful but unstable. Accessing a vault requires solving the puzzle (e.g., Electric creature powers the lab) and defeating a trophy-level guardian. Inside, players discover unique DNA recipes unavailable elsewhere and can extract Forbidden Mods. Vaults reward thorough exploration and preparation.

## 2. Player Fantasy

Finding a sealed vault feels like discovering ancient knowledge. Realizing you need a specific creature type to access it creates a goal. The vault guardian is a genuine threat — harder than normal encounters, with unique abilities. Claiming Forbidden Mod DNA feels like earning forbidden knowledge, making your creatures feel genuinely exotic. The vault is a treasure, not a free gift.

## 3. Detailed Rules

### 3.1 Vault Structure

Each vault contains:
- An entrance (sealed by environmental puzzle)
- A guardian creature (trophy-level, fixed spawn)
- 2-3 Forbidden Mod DNA materials (unique recipes)
- Optional: lore terminals explaining the vault's history

### 3.2 VaultConfig

```csharp
[CreateAssetMenu(menuName = "GeneForge/VaultConfig")]
public class VaultConfig : ScriptableObject
{
    public string vaultId;
    public string zoneId;
    public Vector3 location;
    
    public FieldAbilityType requiredAbility;  // e.g., Electric
    public string guardianCreatureId;
    public int guardianLevel;
    
    public List<string> forbiddenModIds;  // DNA mods available here
    public List<string> uniqueRecipeIds;  // Cross-breeding recipes
    
    public int runtimeDiscoveryRP = 200;
    public int guardianDefeatRP = 300;
}
```

### 3.3 Guardian Encounter

Vault guardian is encounter-based, not a trainer battle:

```csharp
public void EnterVault(VaultConfig vault, CreatureInstance player)
{
    // Solve puzzle to enter (handled by environmental system)
    var guardianConfig = CreatureDatabase.Get(vault.guardianCreatureId);
    var guardian = new CreatureInstance(guardianConfig, vault.guardianLevel);
    
    // One-on-one optional battle
    StartEncounter(vault, guardian, player.activeParty[0]);  // Default to first creature
}
```

Guardian can be:
- Defeated: unlock vault contents, gain RP
- Fled: vault is inaccessible until next visit
- Lost: standard loss outcome; vault remains sealed

### 3.4 Forbidden Mod Access

After defeating guardian, player can extract Forbidden Mod DNA:

```csharp
public void AccessVaultContents(VaultConfig vault)
{
    foreach (var modId in vault.forbiddenModIds)
    {
        var forbiddenMod = DNADatabase.Get(modId);
        inventory.AddDNAMaterial(forbiddenMod);
    }
    
    // Discover recipes
    foreach (var recipeId in vault.uniqueRecipeIds)
    {
        pokedexManager.DiscoverRecipe(recipeId);
    }
    
    // Mark vault as cleared
    gameState.ClearedVaults.Add(vault.vaultId);
}
```

### 3.5 Forbidden Mods

Forbidden Mods have:
- High rarity (Rare or Legendary)
- High instability grant (30-50 points)
- Powerful effects (e.g., +30% ATK, rare perk, type dual-boost)
- Availability only from vaults (post-discovery)

Forbidden Mods can only be installed at Apex Lab (Station Level 5).

## 4. Formulas

### Guardian Power Scaling

```
guardianPower = baseLevel * (1.0 + difficultyTier * 0.3)
guardianLevel = Min(playerAvgLevel + 5, maxLevelCap)
```

### Forbidden Mod Instability

```
instabilityGrant = 30 + (rarity == Legendary ? 20 : 0)
instabilityRange = 30–50
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Player defeats guardian, leaves vault, returns later | Vault contents are still available (not consumed on first access) |
| Guardian is defeated but player loses on rematch | Vault is still open; guardian does not respawn |
| Forbidden Mod discovered from vault is applied to creature | Instability jumps significantly; may push creature over 100 (clamped) |
| Player doesn't have Station Level 5 but has Forbidden Mod in inventory | Mod appears as "locked" in inventory with "Requires Apex Lab" tooltip |
| Vault location is in a dangerous zone but player hasn't cleared zone yet | Vault is still accessible; puzzle still required; guardian is still tough |
| Multiple vaults exist but player only has creatures to solve 1 puzzle | Other vaults remain sealed until player gets required types |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Environmental Puzzle System | Vault entrance sealed by puzzle (Fire, Electric, etc.) |
| Encounter System | Guardian encounter when puzzle solved |
| Station Upgrade System | Forbidden Mods require Level 5 to install |
| DNA Alteration System | Forbidden Mods are DNA materials |
| Creature Database | Guardian creature sourced here |
| Pokedex System | Unique recipes discovered from vault |
| Campaign Map | Vault locations |
| Save/Load System | Persists cleared vault state |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `vaultsPerZone` | 1 | One vault per habitat zone |
| `guardianLevelAdvantagae` | +5 | Guardian level relative to player average |
| `forbiddenModInstabilityGrant` | 30–50 | Range for vault-exclusive mods |
| `forbiddenModPowerScaling` | 1.3 | Power multiplier vs normal mods |
| `discoveryRP` | 200 | RP for finding vault |
| `guardianDefeatRP` | 300 | RP for defeating guardian |

## 8. Acceptance Criteria

- [ ] Vault location is gated by environmental puzzle (Electric, Fire, etc.).
- [ ] Guardian creature spawns at configured level on vault entry.
- [ ] Defeating guardian grants access to vault contents.
- [ ] Forbidden Mods are extracted and added to inventory.
- [ ] Unique recipes are discovered and added to Pokedex.
- [ ] Vault contents remain accessible after first clear (not consumed).
- [ ] Forbidden Mods require Apex Lab (Level 5) to install.
- [ ] Vault cleared state persists through save/load.
- [ ] Guardian encounter is separate from normal wild/trainer battles.
- [ ] Discovery RP and guardian defeat RP are awarded correctly.
