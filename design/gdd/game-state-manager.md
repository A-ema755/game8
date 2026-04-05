# Game State Manager

## 1. Overview

The Game State Manager is the sole singleton in Gene Forge (per ADR-003). It owns the active game state enum, drives async transitions between states, and notifies registered `IStateHandler` implementors via a registration pattern rather than hard-coded scene coupling. States map one-to-one with major game phases: Boot, MainMenu, CampaignMap, Combat, BattleResults, ResearchStation, PartyManagement, Pokedex, and Arena. All scene loads, music swaps, and UI panel activations are triggered through state transitions — never by direct cross-system calls.

## 2. Player Fantasy

Transitions feel instant and purposeful. Moving from the campaign map into a battle, then back to results, then to the research station is a fluid rhythm — not a sequence of jarring loading screens. The state machine is invisible; the player simply experiences a world that responds immediately to their choices.

## 3. Detailed Rules

### 3.1 GameState Enum

```csharp
namespace GeneForge.Core
{
    public enum GameState
    {
        Boot = 0,              // Initial load: ConfigLoader, TypeChart init
        MainMenu = 1,          // Title screen, continue/new game
        CampaignMap = 2,       // Overworld navigation, encounter nodes
        Combat = 3,            // Active turn-based battle
        BattleResults = 4,     // Post-battle summary: XP, captures, DNA materials
        ResearchStation = 5,   // DNA alteration, healing, storage, crafting
        PartyManagement = 6,   // View/swap party creatures, inspect details
        Pokedex = 7,           // Browse research journal entries
        Arena = 8              // Endgame battle tower
    }
}
```

### 3.2 IStateHandler Interface

Any system that needs to react to state changes registers itself. The state manager calls `OnEnter` and `OnExit` in registration order.

```csharp
namespace GeneForge.Core
{
    /// <summary>
    /// Implement on any MonoBehaviour or system class that reacts to game state changes.
    /// Register via GameStateManager.Register() and deregister on destroy.
    /// </summary>
    public interface IStateHandler
    {
        /// <summary>Called after TransitionTo completes for the new state.</summary>
        void OnEnter(GameState state);

        /// <summary>Called before TransitionTo begins leaving the current state.</summary>
        void OnExit(GameState state);
    }
}
```

### 3.3 GameStateManager

```csharp
namespace GeneForge.Core
{
    /// <summary>
    /// Singleton game state machine. Only singleton in the project (ADR-003).
    /// Drives scene loading, state transitions, and handler notifications.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Boot;
        public GameState PreviousState { get; private set; } = GameState.Boot;

        private bool _transitioning;
        private readonly List<IStateHandler> _handlers = new();

        // ── Events ──────────────────────────────────────────────────────
        public static event Action<GameState, GameState> StateChanged; // (from, to)

        // ── State → Scene name mapping ───────────────────────────────────
        private static readonly Dictionary<GameState, string> StateScenes = new()
        {
            { GameState.Boot,             "Boot"             },
            { GameState.MainMenu,         "MainMenu"         },
            { GameState.CampaignMap,      "CampaignMap"      },
            { GameState.Combat,           "Combat"           },
            { GameState.BattleResults,    "BattleResults"    },
            { GameState.ResearchStation,  "ResearchStation"  },
            { GameState.PartyManagement,  "PartyManagement"  },
            { GameState.Pokedex,          "Pokedex"          },
            { GameState.Arena,            "Arena"            }
        };

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Registration ─────────────────────────────────────────────────

        /// <summary>Register a handler to receive state enter/exit callbacks.</summary>
        public void Register(IStateHandler handler)
        {
            if (!_handlers.Contains(handler))
                _handlers.Add(handler);
        }

        /// <summary>Deregister a handler. Call from OnDestroy.</summary>
        public void Deregister(IStateHandler handler)
        {
            _handlers.Remove(handler);
        }

        // ── Transition ───────────────────────────────────────────────────

        /// <summary>
        /// Async transition to a new state.
        /// Notifies exit handlers, loads scene, then notifies enter handlers.
        /// Logs warning and no-ops if a transition is already in progress.
        /// </summary>
        public async Task TransitionTo(GameState newState)
        {
            if (_transitioning)
            {
                Debug.LogWarning($"[GSM] TransitionTo({newState}) ignored — transition already in progress.");
                return;
            }
            if (newState == CurrentState)
            {
                Debug.LogWarning($"[GSM] TransitionTo({newState}) ignored — already in that state.");
                return;
            }

            _transitioning = true;
            var fromState = CurrentState;
            PreviousState = fromState;

            // Notify exit
            foreach (var h in _handlers) h.OnExit(fromState);

            // Load scene if mapped
            if (StateScenes.TryGetValue(newState, out var sceneName))
                await LoadSceneAsync(sceneName);

            CurrentState = newState;
            StateChanged?.Invoke(fromState, newState);

            // Notify enter
            foreach (var h in _handlers) h.OnEnter(newState);

            _transitioning = false;
        }

        private async Task LoadSceneAsync(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
                await Task.Yield();

            op.allowSceneActivation = true;

            // Wait one frame for scene Awake/Start to complete
            await Task.Yield();
        }

        // ── Convenience helpers ──────────────────────────────────────────

        public bool IsInState(GameState state) => CurrentState == state;

        public bool IsTransitioning => _transitioning;

        /// <summary>Returns to the previous state. Does not work from Boot.</summary>
        public Task ReturnToPrevious()
        {
            if (PreviousState == GameState.Boot)
            {
                Debug.LogWarning("[GSM] ReturnToPrevious called from Boot — routing to MainMenu.");
                return TransitionTo(GameState.MainMenu);
            }
            return TransitionTo(PreviousState);
        }
    }
}
```

### 3.4 Valid Transitions

Not all transitions are legal. The table below defines allowed flows:

| From | Allowed To |
|------|-----------|
| Boot | MainMenu |
| MainMenu | CampaignMap |
| CampaignMap | Combat, ResearchStation, PartyManagement, Pokedex, Arena |
| Combat | BattleResults |
| BattleResults | CampaignMap, ResearchStation |
| ResearchStation | CampaignMap, PartyManagement, Pokedex |
| PartyManagement | CampaignMap, ResearchStation |
| Pokedex | CampaignMap, ResearchStation, PartyManagement |
| Arena | CampaignMap, BattleResults |

Transitions not in this table log a warning and still proceed (enforcement is advisory in MVP; hard-block can be added post-MVP).

### 3.5 Boot Sequence

```csharp
// BootController.cs — on the Boot scene's controller object
public class BootController : MonoBehaviour, IStateHandler
{
    async void Start()
    {
        GameStateManager.Instance.Register(this);

        // Foundation init before any state transition
        ConfigLoader.Initialize();
        TypeChart.Initialize();

        await GameStateManager.Instance.TransitionTo(GameState.MainMenu);
    }

    public void OnEnter(GameState state) { }
    public void OnExit(GameState state) => GameStateManager.Instance.Deregister(this);
}
```

### 3.6 Handler Registration Pattern

Each scene's root manager registers on `Awake` and deregisters on `OnDestroy`:

```csharp
public class CombatSceneManager : MonoBehaviour, IStateHandler
{
    void Awake() => GameStateManager.Instance.Register(this);
    void OnDestroy() => GameStateManager.Instance?.Deregister(this);

    public void OnEnter(GameState state)
    {
        if (state == GameState.Combat) InitializeCombat();
    }

    public void OnExit(GameState state)
    {
        if (state == GameState.Combat) CleanupCombat();
    }
}
```

## 4. Formulas

No mathematical formulas. All logic is control flow.

## 5. Edge Cases

| Situation | Behavior |
|-----------|----------|
| `TransitionTo` called while transitioning | Log warning, no-op the second call |
| `TransitionTo` called with current state | Log warning, no-op |
| Scene name not in `StateScenes` dictionary | Log error, skip scene load, still update state |
| Handler throws exception in `OnEnter`/`OnExit` | Caught per-handler; remaining handlers still called; exception logged |
| `ReturnToPrevious` called from Boot | Routes to MainMenu, logs warning |
| Handler registered twice | `Contains` check prevents duplicate registration |
| Scene load fails (missing scene) | Unity logs error; `op.progress` never reaches 0.9; add timeout guard post-MVP |
| GameStateManager destroyed mid-transition | `_transitioning` flag stranded; next boot resets it via fresh instance |
| `Instance` accessed before Boot scene Awake | Returns null; callers must null-check in early boot code |

## 6. Dependencies

| Dependency | Direction | Notes |
|------------|-----------|-------|
| `ConfigLoader` | Outbound | Must complete before first transition |
| `TypeChart` | Outbound | Initialized in Boot before MainMenu transition |
| `UnityEngine.SceneManagement` | External | Used for async scene loading |
| `IStateHandler` implementors | Inbound | Any system wanting state callbacks |
| Save/Load System | Inbound | Registers as handler to save on exit states |
| Combat UI | Inbound | Registers to show/hide on Combat enter/exit |
| Campaign Map | Inbound | Registers to initialize on CampaignMap enter |

## 7. Tuning Knobs

| Parameter | Location | Default | Notes |
|-----------|----------|---------|-------|
| Scene name map | `GameStateManager` dictionary | See Section 3.3 | Change scene names here |
| Transition enforcement | `TransitionTo` | Warning only (MVP) | Upgrade to hard-block post-MVP |
| Loading screen minimum display time | `LoadSceneAsync` | 0ms (MVP) | Add fade delay here |
| Max concurrent transitions | `_transitioning` bool | 1 | Hardcoded; single-threaded state machine |

## 8. Acceptance Criteria

- [ ] `GameStateManager.Instance` is non-null after Boot scene Awake
- [ ] `DontDestroyOnLoad` persists the manager across all scene loads
- [ ] `TransitionTo(MainMenu)` from Boot loads MainMenu scene
- [ ] `TransitionTo(Combat)` from CampaignMap calls `OnExit(CampaignMap)` then `OnEnter(Combat)` on all handlers
- [ ] Second `TransitionTo` call while transitioning logs warning and does not execute
- [ ] `TransitionTo` same-state call logs warning and does not execute
- [ ] Handler registered twice only receives one callback
- [ ] Handler deregistered before transition does not receive callbacks
- [ ] `StateChanged` event fires with correct (from, to) tuple
- [ ] EditMode test: mock handler receives correct OnEnter/OnExit sequence for a two-step transition chain
- [ ] `ReturnToPrevious` from BattleResults returns to CampaignMap after a CampaignMap→Combat→BattleResults chain
