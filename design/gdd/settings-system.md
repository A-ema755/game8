# Settings System

## 1. Overview

The Settings System manages all player preferences — audio volumes, visual quality, gameplay options, and accessibility features — through a centralized SettingsManager singleton that persists settings to a JSON file. Settings are accessible from the pause menu during gameplay and from the main menu. All settings take effect immediately on change with no restart required. A permadeath mode toggle is a special one-time setting available only on new game creation.

## 2. Player Fantasy

Settings should feel responsive and transparent. Dragging the music volume slider immediately affects the background music. Toggling screen shake off should be instantaneous. Accessibility options like colorblind mode should transform the entire type color system without requiring a reload. The player should feel in control of how the game looks, sounds, and plays — and confident that their preferences persist across sessions.

## 3. Detailed Rules

### Settings Categories

#### Audio
| Setting | Type | Range | Default |
|---------|------|-------|---------|
| Master Volume | Slider | 0–100 | 80 |
| Music Volume | Slider | 0–100 | 70 |
| SFX Volume | Slider | 0–100 | 85 |
| Ambient Volume | Slider | 0–100 | 60 |

All volume sliders map to Unity Audio Mixer group exposed parameters. Conversion from 0–100 range to dB: `dB = Mathf.Log10(volume / 100f) * 20f` (with volume = 0 set to -80dB).

#### Visuals
| Setting | Type | Options | Default |
|---------|------|---------|---------|
| VFX Quality | Dropdown | Low / Medium / High | High |
| Screen Shake | Toggle | On / Off | On |
| Fullscreen | Toggle | On / Off | On |
| Resolution | Dropdown | All detected resolutions | Native |

VFX Quality maps to particle system max particle counts and shadow distance in the URP asset. Low: 50% particles, shadows off. Medium: 75% particles, shadows medium. High: 100% particles, shadows high.

Resolution change applies via `Screen.SetResolution()`. Fullscreen toggle applies via `Screen.fullScreen`.

#### Gameplay
| Setting | Type | Options | Default |
|---------|------|---------|---------|
| Combat Speed | Buttons (1x / 2x / 4x) | 1x / 2x / 4x | 1x |
| Auto-Battle | Toggle | On / Off | Off |
| Catch Predictor | Toggle | On / Off | On |
| Damage Numbers | Toggle | On / Off | On |

Combat Speed multiplier is stored as a float (1.0 / 2.0 / 4.0) and applied to `Time.timeScale` during combat animations only — not during UI navigation or turn resolution (which are instantaneous). Auto-Battle enables the AI decision system for player-controlled creatures.

#### Accessibility
| Setting | Type | Options | Default |
|---------|------|---------|---------|
| UI Scale | Slider | 75% – 150% | 100% |
| Colorblind Mode | Dropdown | None / Deuteranopia / Protanopia / Tritanopia | None |
| High Contrast | Toggle | On / Off | Off |

UI Scale applies to the root Canvas `scaleFactor`. Colorblind Mode replaces the type color palette with accessible alternatives (defined in `TypeColorConfig` — each type has a base color and per-mode override). High Contrast increases UI border weight and background opacity.

### Pause Menu
The pause menu is a modal overlay accessible via ESC during combat or campaign map navigation.

Menu items:
1. **Resume** — closes the pause menu and returns to gameplay
2. **Settings** — opens the Settings panel (nested within the pause menu)
3. **Help** — opens the Help Glossary overlay
4. **Quit to Map** — saves current state and transitions to Campaign Map (only available during combat)
5. **Quit to Menu** — saves current state and transitions to Main Menu

Destructive actions (Quit to Map, Quit to Menu) require a confirmation dialog: "Any unsaved progress this combat will be lost. Continue?"

In MVP: "Quit to Map" during combat counts the combat as a loss (no rewards).

### Permadeath Mode Toggle
- Only presented during New Game flow, not accessible from Settings during an active run.
- Displayed as a distinct toggle card with a skull icon and warning text: "Fainted creatures are permanently lost."
- Cannot be changed after a run has started — the toggle is locked to the value chosen at run creation.
- Stored in the save file's `gameSettings.permadeathEnabled` boolean.

### SettingsManager
```csharp
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [System.Serializable]
    public class GameSettings
    {
        public int masterVolume = 80;
        public int musicVolume = 70;
        public int sfxVolume = 85;
        public int ambientVolume = 60;
        public int vfxQuality = 2;          // 0=Low, 1=Medium, 2=High
        public bool screenShakeEnabled = true;
        public bool fullscreen = true;
        public int resolutionIndex = -1;    // -1 = native
        public float combatSpeed = 1f;
        public bool autoBattle = false;
        public bool catchPredictor = true;
        public bool damageNumbers = true;
        public float uiScale = 1f;
        public int colorblindMode = 0;      // 0=None,1=Deut,2=Prot,3=Trit
        public bool highContrast = false;
        public bool permadeathEnabled = false;
    }

    public GameSettings Current { get; private set; } = new GameSettings();

    private const string SettingsFileName = "settings.json";

    public void Save()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, SettingsFileName);
        System.IO.File.WriteAllText(path, JsonUtility.ToJson(Current, prettyPrint: true));
    }

    public void Load()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, SettingsFileName);
        if (System.IO.File.Exists(path))
            Current = JsonUtility.FromJson<GameSettings>(System.IO.File.ReadAllText(path));
        ApplyAll();
    }

    public void ApplyAll() { /* apply each setting to the engine */ }
}
```

### JSON Persistence
- Settings file: `Application.persistentDataPath/settings.json`
- Saved: on every settings change (debounced 0.5s to avoid thrashing on slider drag).
- Loaded: on application startup before any scene loads (Boot scene).
- If settings file is missing or corrupt, defaults are used and a fresh file is written.

## 4. Formulas

**Volume to dB conversion:**
```
dB = volume == 0 ? -80f : Mathf.Log10(volume / 100f) * 20f
```

**UI Scale application:**
```
canvas.scaleFactor = baseScaleFactor * (uiScaleSetting / 100f)
```

**Combat speed application:**
During animation coroutines only:
```
animationDuration = baseDuration / combatSpeedMultiplier
```

**VFX Quality particle count:**
```
maxParticles = baseMaxParticles * vfxQualityMultiplier
// Low=0.5, Medium=0.75, High=1.0
```

## 5. Edge Cases

- **Settings file corrupt (invalid JSON):** Catch `JsonException`, log warning, use defaults, overwrite corrupt file with defaults.
- **Resolution not found in detected list:** Fall back to native resolution. Log warning.
- **Combat speed changed mid-animation:** The new speed applies to the next animation, not the current one in progress. No mid-animation speed changes.
- **Colorblind mode changed mid-combat:** Type color palette updates immediately. All currently displayed UI elements re-query colors. Screen may flicker briefly (one frame) as materials update.
- **Permadeath toggle shown for returning player (loaded existing save):** New Game flow checks if a save exists — if so, permadeath option is grayed with "Start a new run to change this setting."
- **High Contrast + Colorblind Mode simultaneously:** Both apply independently. High contrast changes border/opacity; colorblind mode changes hue palette. No conflict.
- **Master volume set to 0:** All audio groups silenced regardless of individual slider values.
- **VFX Quality changed mid-combat:** Apply at start of next combat to avoid mid-fight pool resizing.

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Audio System | Reads master/music/SFX/ambient volume from SettingsManager |
| VFX System | Reads VFX quality setting to configure particle pool sizes |
| Combat Feedback | Reads screenShakeEnabled and combatSpeed |
| Combat UI | Reads catchPredictor and damageNumbers toggles |
| Type Chart System | Reads colorblindMode to supply correct type colors to UI |
| UI Shell | Hosts pause menu and settings panel |
| Save/Load System | Permadeath flag stored in both settings.json and save file |
| Game State Manager | Boot scene triggers SettingsManager.Load() on startup |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `defaultMasterVolume` | 80 | |
| `defaultMusicVolume` | 70 | |
| `defaultSFXVolume` | 85 | |
| `defaultAmbientVolume` | 60 | |
| `defaultCombatSpeed` | 1.0x | |
| `defaultVFXQuality` | High | |
| `uiScaleMin` | 75% | |
| `uiScaleMax` | 150% | |
| `settingsSaveDebounce` | 0.5s | Delay before writing to disk on slider drag |
| `vfxQualityLowMultiplier` | 0.5 | Particle count multiplier at Low |
| `vfxQualityMedMultiplier` | 0.75 | Particle count multiplier at Medium |

## 8. Acceptance Criteria

- [ ] All audio sliders immediately affect their respective mixer groups with correct dB conversion.
- [ ] Master volume = 0 silences all audio regardless of individual sliders.
- [ ] Screen shake toggle immediately disables all camera shake when turned off.
- [ ] Combat speed 2x and 4x correctly halve and quarter animation durations respectively.
- [ ] Settings persist to settings.json and reload correctly on next application start.
- [ ] Corrupt or missing settings.json falls back to defaults and writes a new valid file.
- [ ] Colorblind mode changes type colors everywhere in the UI immediately on toggle.
- [ ] Permadeath toggle is only available on the New Game screen and is locked during an active run.
- [ ] UI Scale slider updates canvas scale factor in real time between 75% and 150%.
- [ ] Pause menu Quit to Map/Menu shows a confirmation dialog before executing.
- [ ] VFX Quality Low reduces particle counts by 50% compared to High.
- [ ] Resolution change applies immediately via Screen.SetResolution().
