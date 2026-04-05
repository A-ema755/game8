# Environmental Puzzle System

## 1. Overview

Environmental Puzzles use creature field abilities to unlock content on the campaign map. Puzzles require specific creature types: Fire clears thorny walls, Water extinguishes barriers, Electric powers devices, Grass grows bridges, Flying scouts ahead, and Psychic senses rare creatures. Each puzzle gates content (nesting sites, DNA vaults, shortcuts, rare habitats) that rewards exploration. Puzzles are not timed or failed — they either cannot be solved with current party or can be.

## 2. Player Fantasy

Discovering a blocked path and realizing "I need a fire creature to burn through this" creates a meaningful team-building goal. Finding a shortcut with the right party member feels like the world rewards player preparation. Unlocking a vault hidden by an electric puzzle feels earned, not granted. Field abilities make every creature useful outside combat, not just in it.

## 3. Detailed Rules

### 3.1 Field Ability Categories

| Ability | Creature Type | Effect | Unlocks |
|---------|-------------|--------|---------|
| Burn/Clear | Fire | Burn through thorny walls, melted barriers | Hidden area |
| Freeze/Bridge | Ice | Freeze water to create bridge | Crossing point |
| Power Device | Electric | Activate dead devices, open doors | Laboratory, vault entrance |
| Grow/Cross | Grass | Grow vines across chasm | Canyon shortcuts |
| Scout Ahead | Flying | Reveal hidden encounters on map | Secret zones |
| Sense Rare | Psychic | Sense nearby rare creatures | Rare spawn zones |
| Dive/Swim | Water | Dive underwater, cross deep water | Underwater cave |

### 3.2 Puzzle Config

```csharp
[CreateAssetMenu(menuName = "GeneForge/PuzzleConfig")]
public class EnvironmentalPuzzle : ScriptableObject
{
    public string puzzleId;
    public Vector3 location;
    public FieldAbilityType requiredAbility;
    public CreatureType requiredType;  // Can be fulfilled by any creature with this type
    public string puzzleDescription;   // "Thorny wall blocks path"
    
    public string unlocksContentId;    // Links to nesting site, vault, etc.
    public bool revealedOnCompletion;  // Show map feature after solving
    public AudioClip solveSFX;
}
```

### 3.3 Solving a Puzzle

When player approaches a puzzle location with the required creature type in active party:

```csharp
public bool TrySolvePuzzle(EnvironmentalPuzzle puzzle, CreatureInstance creature)
{
    if (creature.primaryType != puzzle.requiredType)
        return false;
    
    // Puzzle solved
    SolvePuzzle(puzzle.puzzleId);
    VFXPoolManager.PlayEffect("puzzle-solve-" + puzzle.requiredAbility, puzzle.location);
    AudioManager.PlaySFX(puzzle.solveSFX);
    
    // Unlock content
    UnlockContent(puzzle.unlocksContentId);
    
    return true;
}
```

### 3.4 Puzzle Types & Locations

- **Nesting Site Barriers:** Fire puzzle blocks entry to bird nesting site; water puzzle to underwater nest
- **DNA Vault Entrance:** Electric puzzle powers a sealed lab containing vault
- **Shortcut Bridges:** Grass puzzle grows vines across chasms; Ice puzzle freezes water
- **Rare Habitat:** Psychic puzzle senses hidden creature spawning zone
- **Scout Mission:** Flying creature reveals hidden trainer encounter

## 4. Formulas

### Puzzle Requirement Matching

```
canSolve = hasActiveCreature AND creature.primaryType == puzzle.requiredType
```

### Content Unlock

```
isUnlocked = puzzle.isSolved
contentIsVisible = isUnlocked OR playerExploresWhileLocked
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Player has required type but creature is fatigued/fainted | Cannot solve; creature must be in active party and healthy |
| Puzzle solved once; player leaves area | Puzzle remains solved; content stays unlocked |
| Player swaps creature and returns to solved puzzle area | Puzzle is already solved; content is accessible |
| Puzzle has multiple solutions (e.g., Fire OR Electric both work) | Config lists all valid types; any one unlocks |
| Rare habitat revealed by Psychic; rare creature not actually present | Creature still has elevated spawn chance in zone (not guaranteed) |
| Content gated by puzzle is also gated by Pokedex requirement | Both must be satisfied (e.g., Vault requires both Electric puzzle AND Researcher rank) |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Creature Database | Reads primary type for puzzle matching |
| Campaign Map | Stores puzzle locations and unlocked content |
| Encounter System | Rare habitats alter spawn tables |
| Nesting System | Nesting sites unlocked by puzzles |
| DNA Vault System | Vault entrances gated by puzzles |
| Save/Load System | Persists puzzle solve state |
| Combat UI | Displays field ability prompt when creature in range |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `puzzleCount` | 8–12 | Per zone, scalable |
| `multiSolutionChance` | 0.2 | 20% of puzzles have alternate solutions |
| `requiredCreatureTypeOnly` | true | Can only any creature of the type solve (no ability items) |
| `puzzleAutoCompleteSFXVolume` | 0.8 | Solve sound effect volume |

## 8. Acceptance Criteria

- [ ] Environmental puzzles are placed on campaign map and have clear visual indicators.
- [ ] Puzzle requirements (Fire, Electric, Psychic, etc.) are displayed when player hovers over.
- [ ] Creature with required type in active party can solve puzzle by interacting.
- [ ] Solving puzzle unlocks associated content (nesting site, vault, shortcut, rare habitat).
- [ ] Puzzle solved state persists through save/load.
- [ ] Content unlocked by puzzle remains accessible after solving.
- [ ] Puzzle solve SFX and VFX play correctly.
- [ ] Multiple puzzles with same requirement can exist independently.
- [ ] Content gated by multiple conditions (puzzle + rank) enforces all requirements.
- [ ] UI prompts "Use field ability?" when appropriate creature is in range.
