using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeneForge.Core
{
    /// <summary>
    /// Primary game state enum. Defined locally per GDD Section 3.1 —
    /// this is a state-machine control enum, not a gameplay-data enum.
    /// </summary>
    public enum GameState
    {
        Boot = 0,
        MainMenu = 1,
        CampaignMap = 2,
        Combat = 3,
        BattleResults = 4,
        ResearchStation = 5,
        PartyManagement = 6,
        Pokedex = 7,
        Arena = 8
    }

    /// <summary>
    /// Singleton game state machine. Primary state-driving singleton (ADR-003).
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
        // WARNING: Static event survives scene loads. Subscribers MUST deregister
        // in OnDisable/OnDestroy to prevent stale reference leaks.
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
            if (handler == null) return;
            if (!_handlers.Contains(handler))
                _handlers.Add(handler);
        }

        /// <summary>Deregister a handler. Call from OnDestroy.</summary>
        public void Deregister(IStateHandler handler)
        {
            _handlers.Remove(handler);
        }

        // ── Valid Transitions (advisory enforcement in MVP) ────────────
        // Source of truth: GDD Section 3.4 Valid Transitions table.
        private static readonly Dictionary<GameState, HashSet<GameState>> ValidTransitions = new()
        {
            { GameState.Boot,            new() { GameState.MainMenu } },
            { GameState.MainMenu,        new() { GameState.CampaignMap } },
            { GameState.CampaignMap,     new() { GameState.Combat, GameState.ResearchStation, GameState.PartyManagement, GameState.Pokedex, GameState.Arena } },
            { GameState.Combat,          new() { GameState.BattleResults } },
            { GameState.BattleResults,   new() { GameState.CampaignMap, GameState.ResearchStation } },
            { GameState.ResearchStation, new() { GameState.CampaignMap, GameState.PartyManagement, GameState.Pokedex } },
            { GameState.PartyManagement, new() { GameState.CampaignMap, GameState.ResearchStation } },
            { GameState.Pokedex,         new() { GameState.CampaignMap, GameState.ResearchStation, GameState.PartyManagement } },
            { GameState.Arena,           new() { GameState.CampaignMap, GameState.BattleResults } }
        };

        // ── Transition ───────────────────────────────────────────────────

        /// <summary>
        /// Async transition to a new state using Unity 6 Awaitable.
        /// Notifies exit handlers, loads scene, then notifies enter handlers.
        /// Logs warning and no-ops if a transition is already in progress.
        /// </summary>
        public async Awaitable TransitionTo(GameState newState)
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

            // Advisory transition validation (MVP: warn only, post-MVP: hard block)
            if (ValidTransitions.TryGetValue(CurrentState, out var allowed) && !allowed.Contains(newState))
                Debug.LogWarning($"[GSM] Invalid transition: {CurrentState} → {newState}. See Section 3.4 valid transitions.");

            _transitioning = true;
            try
            {
                var fromState = CurrentState;
                PreviousState = fromState;

                // Notify exit — catch per-handler to ensure all handlers are called
                NotifyHandlers(h => h.OnExit(fromState), "OnExit");

                // Load scene if mapped
                if (StateScenes.TryGetValue(newState, out var sceneName))
                    await LoadSceneAsync(sceneName);

                CurrentState = newState;
                StateChanged?.Invoke(fromState, newState);

                // Notify enter — catch per-handler to ensure all handlers are called
                NotifyHandlers(h => h.OnEnter(newState), "OnEnter");
            }
            finally
            {
                _transitioning = false;
            }
        }

        /// <summary>
        /// Calls an action on each handler with per-handler exception safety.
        /// Snapshots the handler list before iterating to avoid InvalidOperationException
        /// if a handler's callback triggers registration/deregistration.
        /// </summary>
        private void NotifyHandlers(Action<IStateHandler> action, string phase)
        {
            var snapshot = _handlers.ToArray();
            foreach (var h in snapshot)
            {
                try { action(h); }
                catch (Exception e)
                {
                    Debug.LogError($"[GSM] Handler {h.GetType().Name}.{phase} threw: {e.Message}");
                }
            }
        }

        private async Awaitable LoadSceneAsync(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError($"[GSM] Scene '{sceneName}' could not be loaded — not in build settings?");
                return;
            }

            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
                await Awaitable.NextFrameAsync();

            op.allowSceneActivation = true;

            // Wait one frame for scene Awake/Start to complete
            await Awaitable.NextFrameAsync();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                StateChanged = null;
                Instance = null;
            }
        }

        // ── Convenience helpers ──────────────────────────────────────────

        /// <summary>Returns true if currently in the specified state.</summary>
        public bool IsInState(GameState state) => CurrentState == state;

        /// <summary>Returns true if a transition is currently in progress.</summary>
        public bool IsTransitioning => _transitioning;

        /// <summary>Returns to the previous state. Does not work from Boot.</summary>
        public Awaitable ReturnToPrevious()
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
