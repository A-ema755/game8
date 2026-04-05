# Save / Load System

## 1. Overview

The Save / Load System provides JSON persistence for all durable game state in Gene Forge using Unity's `JsonUtility`. Four distinct save documents exist: `RunState` (the active campaign run — party, Pokedex progress, campaign position, ecosystem state), `CreatureSaveData` (full per-creature snapshot including DNA modifications, body parts, instability, scars, and affinity), `MetaState` (cross-run progression — Institute rank, station upgrade level, Arena progress), and `Settings` (player preferences). Saves write to `Application.persistentDataPath` on every state-machine exit from Combat, ResearchStation, and CampaignMap; they also write on explicit quit. Loading occurs once during Boot before the MainMenu transition.

## 2. Player Fantasy

The player never fears losing progress. After every battle, every DNA tweak, every map movement, the game is saved. Returning after days away drops them right back where they left off — same creatures, same scars, same DNA modifications, same position on the map. The save system is invisible when it works and catastrophic when it doesn't, so it must be rock-solid and fast.

## 3. Detailed Rules

### 3.1 Save File Layout

All files are stored under `Application.persistentDataPath/GeneForge/`:

| File | Contents | Written When |
|------|----------|-------------|
| `run.json` | `RunState` | Exit Combat, exit ResearchStation, exit CampaignMap, quit |
| `meta.json` | `MetaState` | Exit ResearchStation (rank/station changes), Arena floor cleared |
| `settings.json` | `Settings` | Any settings change |
| `run.json.bak` | Previous `RunState` | Before every `run.json` write (single backup) |

### 3.2 Data Structures

```csharp
namespace GeneForge.SaveLoad
{
    // ── Run State ────────────────────────────────────────────────────────

    /// <summary>Full snapshot of the active campaign run.</summary>
    [Serializable]
    public class RunState
    {
        public string runId;                        // GUID assigned at new game
        public string saveVersion = "1.0";          // For migration
        public long savedAtUnixMs;                  // UTC timestamp

        public List<CreatureSaveData> party;        // Active party (max 6)
        public List<CreatureSaveData> storage;      // Station storage (unlimited)
        public PokedexSaveData pokedex;
        public CampaignSaveData campaign;
        public EcosystemSaveData ecosystem;
        public int researchPoints;
    }

    // ── Creature Save Data ────────────────────────────────────────────────

    /// <summary>Complete snapshot of one creature instance.</summary>
    [Serializable]
    public class CreatureSaveData
    {
        public string instanceId;          // GUID — unique per creature
        public string speciesId;           // Matches CreatureConfig.Id
        public string nickname;            // Empty string = use species display name

        public int level;
        public int currentXp;
        public int currentHp;

        // Active move loadout (up to 4 move IDs)
        public List<string> activeMoveIds;
        // All learned move IDs (superset of active)
        public List<string> learnedMoveIds;

        // Equipped body parts: slot enum int → part config ID
        public List<SlotPartPair> equippedParts;

        // DNA modifications applied (ordered, most recent last)
        public List<DnaModSaveData> dnaMods;

        public int instability;            // 0-100

        public PersonalityTrait personalityTrait;

        public int affinityLevel;          // 0-100

        // Permanent scars
        public List<ScarSaveData> scars;

        // Cumulative battle stats for Pokedex / permadeath log
        public int battlesParticipated;
        public int totalDamageDealt;
        public int enemiesFainted;
    }

    [Serializable]
    public class SlotPartPair
    {
        public int slot;        // BodySlot cast to int
        public string partId;   // BodyPartConfig.Id
    }

    [Serializable]
    public class DnaModSaveData
    {
        public string modId;            // DNA mod config ID
        public int instabilityCost;     // Recorded at time of application
        public long appliedAtUnixMs;
    }

    [Serializable]
    public class ScarSaveData
    {
        public int damageSourceType;    // CreatureType cast to int
        public string battleId;         // Which battle caused it
        public long acquiredAtUnixMs;
    }

    // ── Pokedex Save Data ─────────────────────────────────────────────────

    [Serializable]
    public class PokedexSaveData
    {
        public List<PokedexEntrySaveData> entries;
    }

    [Serializable]
    public class PokedexEntrySaveData
    {
        public string speciesId;
        public int tier;               // PokedexTier cast to int
        public int battleCount;
        public bool allDnaRecipesFound;
        public List<string> discoveredRecipeIds;
    }

    // ── Campaign Save Data ────────────────────────────────────────────────

    [Serializable]
    public class CampaignSaveData
    {
        public string currentNodeId;           // Campaign map node ID
        public List<string> clearedNodeIds;    // Completed encounter nodes
        public List<string> unlockedZoneIds;   // Accessible habitat zones
    }

    // ── Ecosystem Save Data ───────────────────────────────────────────────

    [Serializable]
    public class EcosystemSaveData
    {
        public List<SpeciesPopulationData> populations;
        public int conservationScore;   // 0-100
    }

    [Serializable]
    public class SpeciesPopulationData
    {
        public string speciesId;
        public int captureCount;        // How many captured by player
        public int currentPopulation;   // Relative abundance (100 = full)
    }

    // ── Meta State ────────────────────────────────────────────────────────

    [Serializable]
    public class MetaState
    {
        public string saveVersion = "1.0";
        public int instituteRank;          // 0-4 (maps to rank enum)
        public int stationUpgradeLevel;    // 1-5
        public int arenaHighFloor;
        public List<string> unlockedRecipeIds;
        public List<string> achievementIds;
    }

    // ── Settings ──────────────────────────────────────────────────────────

    [Serializable]
    public class Settings
    {
        public float masterVolume = 1.0f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1.0f;
        public int combatSpeed = 1;        // 1, 2, or 4
        public bool permadeathEnabled;
        public bool autoBattleDefault;
        public int targetFrameRate = 60;
        public int qualityLevel = 2;
    }
}
```

### 3.3 SaveLoadManager

```csharp
namespace GeneForge.SaveLoad
{
    /// <summary>
    /// Handles all read/write operations for Gene Forge save files.
    /// Registered as IStateHandler to auto-save on state exits.
    /// </summary>
    public class SaveLoadManager : MonoBehaviour, IStateHandler
    {
        public static SaveLoadManager Instance { get; private set; }

        public RunState CurrentRun { get; private set; }
        public MetaState CurrentMeta { get; private set; }
        public Settings CurrentSettings { get; private set; }

        private static string SaveDir =>
            Path.Combine(Application.persistentDataPath, "GeneForge");

        private const string RunFile      = "run.json";
        private const string RunBackup    = "run.json.bak";
        private const string MetaFile     = "meta.json";
        private const string SettingsFile = "settings.json";
        private const string SaveVersion  = "1.0";

        // States that trigger auto-save on exit
        private static readonly HashSet<GameState> AutoSaveOnExit = new()
        {
            GameState.Combat,
            GameState.ResearchStation,
            GameState.CampaignMap
        };

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Directory.CreateDirectory(SaveDir);
        }

        void Start()
        {
            GameStateManager.Instance.Register(this);
        }

        void OnDestroy()
        {
            GameStateManager.Instance?.Deregister(this);
        }

        // ── IStateHandler ────────────────────────────────────────────────

        public void OnExit(GameState state)
        {
            if (AutoSaveOnExit.Contains(state))
                SaveRun();
        }

        public void OnEnter(GameState state) { }

        // ── Save ─────────────────────────────────────────────────────────

        public void SaveRun()
        {
            if (CurrentRun == null) return;
            CurrentRun.savedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            string runPath = Path.Combine(SaveDir, RunFile);
            string backupPath = Path.Combine(SaveDir, RunBackup);

            // Rotate backup before overwrite
            if (File.Exists(runPath))
                File.Copy(runPath, backupPath, overwrite: true);

            WriteJson(runPath, CurrentRun);
        }

        public void SaveMeta()
        {
            if (CurrentMeta == null) return;
            WriteJson(Path.Combine(SaveDir, MetaFile), CurrentMeta);
        }

        public void SaveSettings()
        {
            if (CurrentSettings == null) return;
            WriteJson(Path.Combine(SaveDir, SettingsFile), CurrentSettings);
        }

        // ── Load ─────────────────────────────────────────────────────────

        /// <summary>Load all save files. Creates defaults if not found.</summary>
        public void LoadAll()
        {
            CurrentRun      = LoadJson<RunState>(RunFile)       ?? new RunState { runId = Guid.NewGuid().ToString() };
            CurrentMeta     = LoadJson<MetaState>(MetaFile)     ?? new MetaState();
            CurrentSettings = LoadJson<Settings>(SettingsFile)  ?? new Settings();
        }

        /// <summary>
        /// Attempt to restore from backup if run.json is corrupted.
        /// Returns true if backup restore succeeded.
        /// </summary>
        public bool TryRestoreFromBackup()
        {
            string backupPath = Path.Combine(SaveDir, RunBackup);
            if (!File.Exists(backupPath)) return false;
            try
            {
                var json = File.ReadAllText(backupPath);
                CurrentRun = JsonUtility.FromJson<RunState>(json);
                return CurrentRun != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoad] Backup restore failed: {e.Message}");
                return false;
            }
        }

        public void StartNewRun()
        {
            CurrentRun = new RunState
            {
                runId = Guid.NewGuid().ToString(),
                saveVersion = SaveVersion,
                party = new List<CreatureSaveData>(),
                storage = new List<CreatureSaveData>(),
                pokedex = new PokedexSaveData { entries = new List<PokedexEntrySaveData>() },
                campaign = new CampaignSaveData { clearedNodeIds = new List<string>(), unlockedZoneIds = new List<string>() },
                ecosystem = new EcosystemSaveData { populations = new List<SpeciesPopulationData>() },
                researchPoints = 0
            };
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private void WriteJson<T>(string path, T data)
        {
            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(data, prettyPrint: false));
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoad] Write failed at {path}: {e.Message}");
            }
        }

        private T LoadJson<T>(string filename) where T : class
        {
            string path = Path.Combine(SaveDir, filename);
            if (!File.Exists(path)) return null;
            try
            {
                return JsonUtility.FromJson<T>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoad] Parse failed for {filename}: {e.Message}");
                return null;
            }
        }
    }
}
```

### 3.4 Auto-Save Triggers

| Trigger | File Written |
|---------|-------------|
| Exit `Combat` state | `run.json` |
| Exit `ResearchStation` state | `run.json` |
| Exit `CampaignMap` state | `run.json` |
| Rank or station level change | `meta.json` |
| Arena floor cleared | `meta.json` |
| Any settings change | `settings.json` |
| Application quit (`OnApplicationQuit`) | `run.json`, `meta.json` |

### 3.5 Version Migration

`saveVersion` field enables future format changes. Migration is handled before deserialization returns data to callers:

```csharp
private static RunState MigrateRunState(RunState data)
{
    if (data.saveVersion == SaveVersion) return data;
    // Future: data.saveVersion == "0.9" → apply field renames
    Debug.LogWarning($"[SaveLoad] Migrating save from version {data.saveVersion} to {SaveVersion}");
    data.saveVersion = SaveVersion;
    return data;
}
```

## 4. Formulas

No mathematical formulas. All operations are serialization/deserialization and file I/O.

## 5. Edge Cases

| Situation | Behavior |
|-----------|----------|
| Save file missing on first launch | Creates default `RunState`, `MetaState`, `Settings` |
| `run.json` corrupted (bad JSON) | Log error, attempt backup restore; if backup also bad, start new run |
| Disk full during write | Log error, leave previous file intact (backup already exists) |
| Application crashes mid-write | Previous backup is intact; restore on next boot |
| `CreatureSaveData` references deleted `speciesId` | Log warning on load; creature is omitted from party/storage |
| `activeMoveIds` contains unknown move ID | Log warning; slot left empty, creature learns a default move |
| Instability value out of range in save | Clamped to 0–100 on load |
| `saveVersion` mismatch | Migration function called; if migration fails, log error and start new run |
| Pokedex entry for unknown species | Entry skipped; species may have been removed between versions |
| Settings file missing | Default `Settings` created and saved immediately |
| `currentNodeId` not found in campaign data | Placed at campaign start node |

## 6. Dependencies

| Dependency | Direction | Notes |
|------------|-----------|-------|
| `GameStateManager` | Inbound | Registers as handler for auto-save on exit |
| `CreatureInstance` | Outbound | Serializes to/from `CreatureSaveData` |
| `ConfigLoader` | Outbound | Validates speciesId, moveIds, partIds on load |
| `UnityEngine.JsonUtility` | External | Serialization engine |
| `System.IO` | External | File read/write |
| Pokedex System | Inbound | Reads/writes `PokedexSaveData` |
| Campaign Map | Inbound | Reads/writes `CampaignSaveData` |
| DNA Alteration System | Inbound | Reads/writes `DnaModSaveData` per creature |

## 7. Tuning Knobs

| Parameter | Location | Default | Notes |
|-----------|----------|---------|-------|
| `SaveVersion` | `SaveLoadManager` const | `"1.0"` | Bump on format change |
| `SaveDir` subfolder | `SaveLoadManager` | `"GeneForge"` | Under `persistentDataPath` |
| Backup file count | `SaveLoadManager` | 1 (`.bak`) | Could extend to rolling N backups |
| `prettyPrint` in `WriteJson` | `SaveLoadManager` | `false` | Set true for debug builds |
| `AutoSaveOnExit` states | `SaveLoadManager` `HashSet` | Combat, ResearchStation, CampaignMap | Add/remove trigger states here |
| Max party size | `GameSettings` SO | `6` | Validated on load |

## 8. Acceptance Criteria

- [ ] `SaveLoadManager.LoadAll()` creates default data when no files exist
- [ ] `SaveRun()` writes `run.json` and rotates previous to `run.json.bak`
- [ ] Exiting `Combat` state triggers `SaveRun()` via `IStateHandler.OnExit`
- [ ] A corrupt `run.json` triggers backup restore attempt
- [ ] `StartNewRun()` produces valid `RunState` with new GUID `runId`
- [ ] `CreatureSaveData` round-trips all fields through JSON without data loss
- [ ] `SlotPartPair` serializes and deserializes enum-as-int correctly
- [ ] Unknown `speciesId` on load logs warning and creature is excluded
- [ ] Instability value of 150 in save file is clamped to 100 on load
- [ ] EditMode test: write RunState, read it back, assert all fields equal
- [ ] EditMode test: corrupt JSON returns null from `LoadJson` with error log
- [ ] `settings.json` written immediately on first launch with defaults
