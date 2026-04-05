# Tutorial System

## 1. Overview

The Tutorial System guides new players through core mechanics progressively during their first session. Steps unlock sequentially: selecting a creature, moving on the grid, attacking with type explanation, capturing, applying DNA mods, and discovering terrain synergy. Each step has optional contextual tooltips and can be skipped. A TutorialManager tracks seen steps in the save file. A pause-menu Glossary provides reference definitions. The system is non-intrusive — tutorials don't block player actions, only suggest them.

## 2. Player Fantasy

New players feel guided but not railroaded. Learning to capture feels like an accomplishment because the game explains the stakes (catch rate formula, trap types). Understanding terrain synergy comes from seeing the bonus damage firsthand, not reading a wall of text. Tooltips appear exactly when needed — when hovering over a capture button for the first time, a popup explains catch probability. Players can dismiss tooltips and never see them again, or review them in the Glossary anytime.

## 3. Detailed Rules

### 3.1 Tutorial Steps

| Step | Trigger | Tooltip | Completion |
|------|---------|---------|-----------|
| Intro | Game start | "Select your starting creature" | Any creature selected |
| Movement | First creature selected | "Click a tile within range to move" | Creature moves once |
| Attack | Creature in range of enemy | "Attacks deal type-based damage. Fire beats Grass." | Any attack lands |
| Type Advantage | (auto-fire if player used effective type) | "That's super effective! [Type] beats [Type]." | Dismissed or timer |
| Capture | Wild creature at low HP | "Throw a trap when HP is low for higher catch rate." | Trap thrown (success not required) |
| DNA Mod | First research station | "Combine DNA to customize your creatures." | DNA modification interface opened |
| Terrain Synergy | Any creature on matching terrain | "This creature gets a damage boost on [Terrain]!" | Acknowledged or 5 turns pass |

### 3.2 TutorialManager

```csharp
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }
    
    private HashSet<string> _seenSteps;
    
    public bool HasSeenStep(string stepId) => _seenSteps.Contains(stepId);
    
    public void MarkStepSeen(string stepId)
    {
        _seenSteps.Add(stepId);
        SaveTutorialProgress();
    }
    
    public void ShowTooltip(string stepId, string message, Vector3 screenPos);
    public void HideTooltip();
    public bool ShouldShowTooltip(string stepId) => !HasSeenStep(stepId);
}
```

### 3.3 Tooltip Display

Tooltips are small UI panels that appear near the referenced element with:
- A 2-3 sentence explanation
- An optional icon (attack icon for attack tutorial, etc.)
- A dismiss button or auto-hide timer (4 seconds)

Tooltips respect a "Don't show again" checkbox; checking it marks the step as seen.

### 3.4 Glossary (Pause Menu)

A searchable reference accessible from the pause menu showing all game terms:
- **Creature:** What a creature is, types, stats, moves
- **DNA Mod:** Instability, perks, stat boosts
- **Terrain:** Type synergy, height, movement costs
- **Status Effects:** Burn, poison, paralyze, etc.
- **Capture:** Trap types, catch rate formula, rarity

Glossary entries are unlocked progressively as the player encounters mechanics in-game.

### 3.5 Tutorial Config

```csharp
[CreateAssetMenu(menuName = "GeneForge/TutorialConfig")]
public class TutorialConfig : ScriptableObject
{
    [System.Serializable]
    public class TutorialStep
    {
        public string stepId;
        public string displayText;
        public Sprite icon;
        public float displayDuration = 4.0f;  // 0 = manual dismiss
        public bool showSkipButton = true;
    }
    
    public List<TutorialStep> steps;
    public Dictionary<string, GlossaryEntry> glossaryEntries;
}

[System.Serializable]
public class GlossaryEntry
{
    public string term;
    public string definition;
    public Sprite icon;
    public string relatedStepId;  // auto-highlight this glossary entry when step is shown
}
```

## 4. Formulas

### Tooltip Display Condition

```
shouldShow = !TutorialManager.HasSeenStep(stepId)
          AND isFirstTime encounter for this mechanic
```

### Glossary Unlock Condition

```
entryUnlocked = currentGameSession.hasEncountered(relatedStepId)
             OR playerHasManuallyUnlockedEntry
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Player dismisses tooltip before reading it | Step is marked as seen; no way to unsee (but Glossary remains available) |
| Player skips to advanced tactics before seeing basic tutorial | Later tooltips (DNA Mod, Terrain Synergy) still appear normally |
| Tooltip UI appears off-screen | Reposition to stay within viewport bounds |
| Player opens Glossary before any steps are triggered | All entries are locked with "???" placeholder text |
| Save file corrupted, `_seenSteps` is invalid | Restore from default empty set; all tutorials will replay |
| Player restarts the game (new save) after completing first zone | New save file gets fresh tutorial sequence |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Game State Manager | Detects first game session vs. new game after progress |
| Capture System | Triggers capture tutorial when wild creature is at low HP |
| Turn Manager | Fires attack tutorial when creature attacks for first time |
| DNA Alteration System | Opens DNA Mod tutorial on first station visit |
| Grid Tile System | Detects terrain synergy bonus for terrain tutorial |
| Pokedex System | Unlocks glossary entries as creatures are discovered |
| Save/Load System | Persists `_seenSteps` set in save file |
| UI Shell | Pause menu integration for glossary |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `tooltipDisplayDuration` | 4.0s | Auto-hide time (0 = manual dismiss) |
| `tooltipMaxScreenDistance` | 200px | Reposition if further than this from center |
| `skipButtonAvailable` | true | Allow players to skip tutorials |
| `glossaryAutoUnlockOnStep` | true | Auto-unlock glossary entry when step triggers |
| `maxConcurrentTooltips` | 1 | Never stack more than 1 tooltip |

## 8. Acceptance Criteria

- [ ] First-time player sees "Select a creature" tooltip on game start.
- [ ] Dismissing a tooltip marks that step as seen in the save file.
- [ ] Movement tutorial appears after creature selection and completes on first move.
- [ ] Attack type advantage message appears automatically after first super-effective hit.
- [ ] Capture tutorial appears when wild creature reaches < 30% HP.
- [ ] DNA Mod tutorial appears on first visit to research station.
- [ ] Terrain synergy tooltip appears when creature deals bonus damage on matching terrain.
- [ ] Glossary is accessible from pause menu and shows all unlocked entries.
- [ ] Glossary entries are initially locked with placeholder text until encountered.
- [ ] Tutorial progress persists through save/load cycles.
- [ ] Skipping a tutorial marks it as seen and never shows it again (unless manually reset).
- [ ] Tooltips stay within screen bounds and reposition if needed.
