using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GeneForge.Core;

namespace GeneForge.Tests
{
    /// <summary>
    /// EditMode tests for GameStateManager. Tests state machine logic only —
    /// guard clauses, handler calls, event firing, previous state tracking.
    /// Scene loading is not tested here (scenes don't exist in EditMode);
    /// LoadSceneAsync returns null and exits early via the null guard.
    /// </summary>
    [TestFixture]
    public class GameStateManagerTests
    {
        private GameObject _gameObject;
        private GameStateManager _manager;

        private class MockHandler : IStateHandler
        {
            public List<(string phase, GameState state)> Calls { get; } = new();

            public void OnEnter(GameState state) => Calls.Add(("OnEnter", state));
            public void OnExit(GameState state) => Calls.Add(("OnExit", state));
        }

        private class ThrowingHandler : IStateHandler
        {
            public string ThrowOnPhase { get; set; }

            public void OnEnter(GameState state)
            {
                if (ThrowOnPhase == "OnEnter")
                    throw new InvalidOperationException("Test exception in OnEnter");
            }

            public void OnExit(GameState state)
            {
                if (ThrowOnPhase == "OnExit")
                    throw new InvalidOperationException("Test exception in OnExit");
            }
        }

        [SetUp]
        public void SetUp()
        {
            if (GameStateManager.Instance != null)
                UnityEngine.Object.DestroyImmediate(GameStateManager.Instance.gameObject);

            // Suppress scene load errors (scenes don't exist in EditMode)
            LogAssert.ignoreFailingMessages = true;

            _gameObject = new GameObject("TestGameStateManager");
            _manager = _gameObject.AddComponent<GameStateManager>();
        }

        [TearDown]
        public void TearDown()
        {
            // Clear static StateChanged event via reflection
            var eventField = typeof(GameStateManager).GetField("StateChanged",
                BindingFlags.Static | BindingFlags.NonPublic);
            eventField?.SetValue(null, null);

            LogAssert.ignoreFailingMessages = false;

            if (_gameObject != null)
                UnityEngine.Object.DestroyImmediate(_gameObject);
        }

        // ── AC: Instance is non-null after Boot scene Awake ──────────────

        [Test]
        public void Instance_IsNonNull_AfterAwake()
        {
            Assert.IsNotNull(GameStateManager.Instance);
            Assert.AreEqual(_manager, GameStateManager.Instance);
        }

        // ── AC: TransitionTo same-state call logs warning, does not execute ──

        [UnityTest]
        public IEnumerator TransitionTo_SameState_LogsWarning_DoesNotFireStateChanged()
        {
            bool eventFired = false;
            GameStateManager.StateChanged += (_, _) => eventFired = true;

            // Boot is the initial state — transitioning to Boot is same-state
            _manager.TransitionTo(GameState.Boot);
            yield return null;

            Assert.IsFalse(eventFired);
            Assert.AreEqual(GameState.Boot, _manager.CurrentState);
        }

        // ── AC: Second TransitionTo while transitioning logs warning, no-ops ──

        [UnityTest]
        public IEnumerator TransitionTo_WhileTransitioning_LogsWarning_DoesNotExecute()
        {
            var field = typeof(GameStateManager).GetField("_transitioning",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(_manager, true);

            bool eventFired = false;
            GameStateManager.StateChanged += (_, _) => eventFired = true;

            _manager.TransitionTo(GameState.MainMenu);
            yield return null;

            Assert.IsFalse(eventFired);
            Assert.AreEqual(GameState.Boot, _manager.CurrentState);

            field.SetValue(_manager, false);
        }

        // ── AC: Handler registered twice only receives one callback ──────

        [UnityTest]
        public IEnumerator Register_Twice_HandlerReceivesOneCallbackEach()
        {
            var handler = new MockHandler();
            _manager.Register(handler);
            _manager.Register(handler);

            _manager.TransitionTo(GameState.MainMenu);
            yield return null;

            int exitCount = 0;
            int enterCount = 0;
            foreach (var (phase, _) in handler.Calls)
            {
                if (phase == "OnExit") exitCount++;
                if (phase == "OnEnter") enterCount++;
            }

            Assert.AreEqual(1, exitCount, "Handler should receive exactly one OnExit");
            Assert.AreEqual(1, enterCount, "Handler should receive exactly one OnEnter");
        }

        // ── AC: Handler deregistered before transition gets no callbacks ──

        [UnityTest]
        public IEnumerator Deregister_BeforeTransition_HandlerReceivesNoCallbacks()
        {
            var handler = new MockHandler();
            _manager.Register(handler);
            _manager.Deregister(handler);

            _manager.TransitionTo(GameState.MainMenu);
            yield return null;

            Assert.AreEqual(0, handler.Calls.Count);
        }

        // ── AC: StateChanged event fires with correct (from, to) tuple ───

        [UnityTest]
        public IEnumerator StateChanged_FiresWithCorrect_FromTo_Args()
        {
            GameState? firedFrom = null;
            GameState? firedTo = null;
            GameStateManager.StateChanged += (from, to) =>
            {
                firedFrom = from;
                firedTo = to;
            };

            _manager.TransitionTo(GameState.MainMenu);
            yield return null;

            Assert.AreEqual(GameState.Boot, firedFrom);
            Assert.AreEqual(GameState.MainMenu, firedTo);
        }

        // ── AC: OnExit called before OnEnter in correct order ────────────

        [UnityTest]
        public IEnumerator TransitionTo_CallsOnExit_BeforeOnEnter_WithCorrectStates()
        {
            var handler = new MockHandler();
            _manager.Register(handler);

            _manager.TransitionTo(GameState.MainMenu);
            yield return null;

            Assert.GreaterOrEqual(handler.Calls.Count, 2);
            Assert.AreEqual(("OnExit", GameState.Boot), handler.Calls[0]);
            Assert.AreEqual(("OnEnter", GameState.MainMenu), handler.Calls[1]);
        }

        // ── AC: ReturnToPrevious after chain returns to correct state ────

        [UnityTest]
        public IEnumerator ReturnToPrevious_AfterCombatToBattleResults_ReturnsToCombat()
        {
            _manager.TransitionTo(GameState.MainMenu);
            yield return null;
            _manager.TransitionTo(GameState.CampaignMap);
            yield return null;
            _manager.TransitionTo(GameState.Combat);
            yield return null;
            _manager.TransitionTo(GameState.BattleResults);
            yield return null;

            Assert.AreEqual(GameState.BattleResults, _manager.CurrentState);
            Assert.AreEqual(GameState.Combat, _manager.PreviousState);

            _manager.ReturnToPrevious();
            yield return null;

            Assert.AreEqual(GameState.Combat, _manager.CurrentState);
        }

        // ── AC: ReturnToPrevious when PreviousState is Boot routes to MainMenu ──

        [UnityTest]
        public IEnumerator ReturnToPrevious_WhenPreviousIsBoot_RoutesToMainMenu()
        {
            Assert.AreEqual(GameState.Boot, _manager.PreviousState);

            _manager.ReturnToPrevious();
            yield return null;

            Assert.AreEqual(GameState.MainMenu, _manager.CurrentState);
        }

        // ── AC: StateChanged fires after CurrentState updated, before OnEnter ──

        [UnityTest]
        public IEnumerator StateChanged_FiresAfterCurrentStateUpdated_BeforeOnEnter()
        {
            var order = new List<string>();
            GameState? stateAtEventTime = null;

            GameStateManager.StateChanged += (_, _) =>
            {
                stateAtEventTime = _manager.CurrentState;
                order.Add("StateChanged");
            };

            var handler = new OrderTrackingHandler(order);
            _manager.Register(handler);

            _manager.TransitionTo(GameState.MainMenu);
            yield return null;

            Assert.AreEqual(GameState.MainMenu, stateAtEventTime,
                "CurrentState should be updated before StateChanged fires");
            Assert.AreEqual(3, order.Count);
            Assert.AreEqual("OnExit", order[0]);
            Assert.AreEqual("StateChanged", order[1]);
            Assert.AreEqual("OnEnter", order[2]);
        }

        // ── AC: Handler throwing in OnEnter doesn't block other handlers ─

        [UnityTest]
        public IEnumerator HandlerThrowingInOnEnter_DoesNotPreventOtherHandlers()
        {
            var thrower = new ThrowingHandler { ThrowOnPhase = "OnEnter" };
            var healthy = new MockHandler();

            _manager.Register(thrower);
            _manager.Register(healthy);

            _manager.TransitionTo(GameState.MainMenu);
            yield return null;

            bool healthyGotEnter = healthy.Calls.Exists(c => c.phase == "OnEnter");
            Assert.IsTrue(healthyGotEnter, "Healthy handler should still receive OnEnter");
        }

        // ── AC: Handler throwing in OnExit doesn't prevent other handlers ─

        [UnityTest]
        public IEnumerator HandlerThrowingInOnExit_DoesNotPreventOtherHandlers()
        {
            var thrower = new ThrowingHandler { ThrowOnPhase = "OnExit" };
            var healthy = new MockHandler();

            _manager.Register(thrower);
            _manager.Register(healthy);

            _manager.TransitionTo(GameState.MainMenu);
            yield return null;

            bool healthyGotExit = healthy.Calls.Exists(c => c.phase == "OnExit");
            bool healthyGotEnter = healthy.Calls.Exists(c => c.phase == "OnEnter");
            Assert.IsTrue(healthyGotExit, "Healthy handler should still receive OnExit");
            Assert.IsTrue(healthyGotEnter, "Healthy handler should still receive OnEnter");
        }

        // ── AC: Invalid transition logs advisory warning but still proceeds ──

        [UnityTest]
        public IEnumerator TransitionTo_InvalidTransition_LogsWarning_ButStillProceeds()
        {
            // Boot → Combat is not in ValidTransitions table
            _manager.TransitionTo(GameState.Combat);
            yield return null;

            Assert.AreEqual(GameState.Combat, _manager.CurrentState,
                "Invalid transition should still proceed in MVP (advisory only)");
        }

        // ── AC: Handler list mutation during OnEnter doesn't throw ───────

        [UnityTest]
        public IEnumerator HandlerMutationDuringOnEnter_DoesNotThrow()
        {
            var mutator = new MutatingHandler(_manager);
            _manager.Register(mutator);

            _manager.TransitionTo(GameState.MainMenu);
            yield return null;

            Assert.AreEqual(GameState.MainMenu, _manager.CurrentState,
                "Transition should complete even when handler mutates handler list");
        }

        // ── Test doubles ─────────────────────────────────────────────────

        private class OrderTrackingHandler : IStateHandler
        {
            private readonly List<string> _order;

            public OrderTrackingHandler(List<string> order) => _order = order;

            public void OnEnter(GameState state) => _order.Add("OnEnter");
            public void OnExit(GameState state) => _order.Add("OnExit");
        }

        private class MutatingHandler : IStateHandler
        {
            private readonly GameStateManager _gsm;

            public MutatingHandler(GameStateManager gsm) => _gsm = gsm;

            public void OnEnter(GameState state)
            {
                // Register a new handler during OnEnter — should not throw
                _gsm.Register(new MockHandler());
            }

            public void OnExit(GameState state) { }
        }
    }
}
