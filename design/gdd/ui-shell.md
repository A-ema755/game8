# UI Shell

## 1. Overview

The UI Shell covers all top-level navigation surfaces: the main menu, new game setup flow, scene transitions, loading screens, and confirmation dialogs. It acts as the structural frame around all other UI systems. The UI Shell is managed by the Game State Manager and implemented with Unity UI Toolkit. Scene transitions use async loading with a loading screen interstitial. All destructive actions (quit, delete save) require confirmation dialogs before execution.

## 2. Player Fantasy

The main menu should feel polished and confident — a game that knows what it is. The new game flow builds excitement: choosing difficulty, naming your researcher, and picking your starter creature are rituals that create investment before the first encounter. Scene transitions should feel smooth and intentional, never jarring. The loading screen turns a technical necessity into a world-building moment with creature silhouettes and field notes.

## 3. Detailed Rules

### Main Menu

Layout: Full-screen with a background that shows a slow parallax isometric scene (creatures moving through a forest habitat).

Menu items (vertical list, center-bottom):
1. **Continue** — loads the most recent save. Grayed with "No Save Found" if no save exists.
2. **New Game** — opens the New Game flow. If a save exists, shows confirmation: "Starting a new game will overwrite your current save. Continue?"
3. **Pokedex** — opens a read-only Pokedex viewer (only available if a save exists with any Pokedex data).
4. **Settings** — opens the Settings panel overlay.
5. **Quit** — exits the application. Confirmation dialog: "Quit Gene Forge?"

Game version string displayed bottom-right corner.

### New Game Flow

Step-by-step wizard, non-skippable:

**Step 1 — Difficulty Selection**
Three options displayed as cards:
| Option | Description |
|--------|-------------|
| Normal | Standard experience. Party revival between zones. |
| Hard | No free healing. Gene Traps consumed on failed capture. Reduced RP rewards. |
| Permadeath | Fainted = permanent death. First encounter only per area. Memorial Wall unlocked. |

Each card shows a brief flavor description and a difficulty icon. Permadeath card has a skull icon and is grayed with tooltip "Complete Verdant Basin in Normal to unlock" until the unlock condition is met.

**Step 2 — Researcher Name Entry**
- Text input field, max 16 characters, alphanumeric + spaces only.
- Default name: "Researcher" (editable).
- Validation: empty name blocked ("Please enter a name.").
- Profanity filter applied (simple blocklist for MVP).

**Step 3 — Starter Creature Selection**
- Three starter creatures presented as rotating 3D model cards (same RenderTexture preview technique as Creature Forge).
- Each card shows: creature name, type badge(s), a flavor biography sentence, and base stat radar chart.
- Player selects one; the other two are set aside (not accessible in normal play — post-MVP rival gets one of the remaining two).
- Confirm button: "Choose [Name]" — requires one selection before enabling.

**Step 4 — Confirmation Summary**
- Shows: difficulty chosen, researcher name, chosen starter, and a "Begin Research" button.
- Back button returns to Step 3.
- "Begin Research" triggers new save creation and transitions to the first encounter in Verdant Basin.

### Scene Transitions

All scene transitions use additive async loading (`LoadSceneAsync` with `LoadSceneMode.Additive` for the loading screen, then unload/load target scene).

| Transition Type | Trigger | Visual |
|----------------|---------|--------|
| Fade to Black | Default (map navigation, menu transitions) | Black overlay alpha 0→1 (0.3s), load, alpha 1→0 (0.3s) |
| White Flash | Capture success | White overlay alpha 0→0.6→0 over 0.5s, simultaneous with capture sparkle |
| Red Fade | Combat defeat | Red overlay alpha 0→0.8 (0.5s), hold 0.5s, fade 0→0 (0.3s) |

Transition implementation:
```csharp
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    public enum TransitionType { FadeToBlack, WhiteFlash, RedFade }

    public async Task TransitionTo(string sceneName, TransitionType type)
    {
        await PlayOutAnimation(type);
        await LoadSceneAsync(sceneName);
        await PlayInAnimation(type);
    }
}
```

### Loading Screen

Displayed during any async scene load that takes > 0.3s.

Content:
- Creature silhouette (randomly selected from discovered species; uses Silhouette-tier Pokedex art).
- Loading tip below silhouette: a rotating set of field notes / gameplay hints (one per load).
- Progress bar at bottom: fills 0→100% based on `AsyncOperation.progress` (mapped 0–0.9 range to 0–100% visually, with final 10% held until scene is ready).
- Game logo top-left corner (small).

Loading tips are sourced from a `LoadingTipsConfig` ScriptableObject (list of strings). Tips rotate randomly without repeating until all have been shown.

If load completes before 0.3s threshold, loading screen is skipped entirely.

### Confirmation Dialogs

Reusable `ConfirmationDialog` component used for all destructive actions.

Properties:
- `titleText`: bold header
- `bodyText`: description of consequences
- `confirmLabel`: confirm button text (default "Confirm")
- `cancelLabel`: cancel button text (default "Cancel")
- Confirm button styled in danger red for irreversible actions.

Usage:
```csharp
ConfirmationDialog.Show(
    title: "Quit to Main Menu?",
    body: "Any unsaved progress will be lost.",
    confirmLabel: "Quit",
    cancelLabel: "Cancel",
    onConfirm: () => SceneTransitionManager.Instance.TransitionTo("MainMenu", TransitionType.FadeToBlack)
);
```

Dialog blocks all input behind it (modal). Background dims to 50% opacity.

### Pause Menu (in-game)
See Settings System GDD for full pause menu item list. UI Shell owns the pause menu container and activation (ESC key binding). Settings System owns the content.

The pause menu slides in from the right (translate X, 0.2s ease-out). Dismissal slides out right.

## 4. Formulas

**Loading Progress Bar:**
```
displayProgress = Clamp(asyncOperation.progress / 0.9f, 0f, 1f) * 100
```
(Unity's `AsyncOperation.progress` reports 0–0.9 during load; 0.9–1.0 during activation.)

**Loading Screen Visibility:**
```
showLoadingScreen = estimatedLoadTime > loadScreenThreshold (0.3s)
```
Estimated via last recorded load time for the same scene (cached). First load always shows.

## 5. Edge Cases

- **Continue with no save:** "Continue" button is grayed. Tooltip: "No save data found." Clicking does nothing.
- **New Game overwrites existing save:** Confirmation dialog required. Player must explicitly acknowledge save deletion. If canceled, existing save is untouched.
- **Starter selection with 0 starters selected:** "Begin Research" button remains disabled with pulse animation drawing attention to the creature selection cards.
- **Pokedex from main menu with empty Pokedex:** Shows the Pokedex screen with an "empty" state: "No creatures discovered yet. Begin your research!" Button is available as long as a save file exists.
- **Scene load fails:** Display an error screen with "Failed to load scene. Return to Main Menu?" — always shows a recovery path.
- **Transition interrupted (player alt-tabs during fade):** OnApplicationFocus pause handling freezes the transition coroutine until focus returns.
- **Permadeath unlock condition not met:** Card is visible but non-selectable with a lock icon and the unlock requirement tooltip.
- **Back button in New Game flow at Step 1:** Back from Step 1 returns to Main Menu. Confirmation: "Cancel new game setup?"
- **Researcher name with only spaces:** Trim whitespace; if result is empty, show validation error.

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Game State Manager | Controls which scene/state is active; UI Shell reads state to show correct menu |
| Settings System | Settings panel hosted within UI Shell's overlay layer |
| Save/Load System | Continue button reads save existence; New Game triggers save creation |
| Pokedex System | Main menu Pokedex button reads save data for species silhouettes |
| Creature Database | Starter creature 3D previews loaded from creature configs |
| Combat Feedback | White flash and red fade transitions coordinate with combat feedback events |
| Audio System | Menu music starts in Boot scene, transitions on scene change |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `fadeToBlackDuration` | 0.3s | Each direction (out + in) |
| `whiteFlashPeakAlpha` | 0.6 | Max alpha for capture success flash |
| `redFadePeakAlpha` | 0.8 | Max alpha for defeat fade |
| `redFadeHoldDuration` | 0.5s | Hold at peak before fading |
| `loadScreenThreshold` | 0.3s | Minimum load time to show loading screen |
| `pauseMenuSlideInDuration` | 0.2s | Pause overlay slide animation |
| `maxResearcherNameLength` | 16 | Character limit |
| `loadingTipRotation` | Non-repeating random | Cycle through all before repeating |
| `starterCreatureCount` | 3 | Number of starters to choose from |
| `mainMenuParallaxSpeed` | 0.05 units/s | Background parallax rate |

## 8. Acceptance Criteria

- [ ] Main menu displays all 5 items; Continue is grayed when no save exists.
- [ ] New Game flow progresses through all 4 steps in order; Back navigation works from steps 2–4.
- [ ] Permadeath option is locked until the player has completed Verdant Basin in Normal mode.
- [ ] Researcher name field rejects empty and whitespace-only names with a validation message.
- [ ] Selecting a starter creature enables the "Begin Research" button; no selection keeps it disabled.
- [ ] Fade to black transition plays on all standard scene changes with correct timing.
- [ ] White flash plays on capture success without blocking the post-capture UI.
- [ ] Red fade plays on combat defeat before transitioning to the map.
- [ ] Loading screen shows a creature silhouette and tip text during loads exceeding 0.3s.
- [ ] Loading screen is skipped entirely for loads under 0.3s.
- [ ] All confirmation dialogs block input correctly and require explicit confirmation for destructive actions.
- [ ] Overwriting an existing save during New Game requires and respects the confirmation dialog.
