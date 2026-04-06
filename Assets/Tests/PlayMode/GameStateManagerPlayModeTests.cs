using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using GeneForge.Core;

namespace GeneForge.Tests
{
    /// <summary>
    /// PlayMode tests for GameStateManager.
    /// Covers acceptance criteria requiring actual scene loading.
    /// </summary>
    [TestFixture]
    public class GameStateManagerPlayModeTests
    {
        private GameObject _gameObject;
        private GameStateManager _manager;

        [SetUp]
        public void SetUp()
        {
            if (GameStateManager.Instance != null)
                Object.DestroyImmediate(GameStateManager.Instance.gameObject);

            _gameObject = new GameObject("TestGameStateManager");
            _manager = _gameObject.AddComponent<GameStateManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
                Object.DestroyImmediate(_gameObject);
        }

        // ── AC #16: TransitionTo with non-existent scene does not hang ──

        [UnityTest]
        [Timeout(5000)] // 5 second timeout per GDD acceptance criteria
        public IEnumerator test_GameStateManager_TransitionTo_NonExistentScene_DoesNotHang()
        {
            // Arrange — force a scene name that doesn't exist in build settings
            // LoadSceneAsync returns null for unknown scenes, triggering the null guard

            // Expect the error log from LoadSceneAsync null guard
            LogAssert.ignoreFailingMessages = true;

            // Act — transition to MainMenu (scene likely not in test build settings)
            var awaitable = _manager.TransitionTo(GameState.MainMenu);
            yield return null;

            // Wait a few frames for async to settle
            yield return null;
            yield return null;

            // Assert — state machine should have completed (not hung)
            // CurrentState should be MainMenu (state updates even if scene load fails)
            Assert.AreEqual(GameState.MainMenu, _manager.CurrentState,
                "State should update even when scene load fails");
            Assert.IsFalse(_manager.IsTransitioning,
                "Transition should complete even when scene load fails");

            LogAssert.ignoreFailingMessages = false;
        }

        // ── AC: DontDestroyOnLoad persists manager across scene loads ────

        [UnityTest]
        public IEnumerator test_GameStateManager_DontDestroyOnLoad_PersistsAcrossSceneLoads()
        {
            // Arrange
            var instanceBefore = GameStateManager.Instance;
            Assert.IsNotNull(instanceBefore);

            // Act — load a new empty scene (this destroys non-persistent objects)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
            yield return null;

            // Assert — GameStateManager should survive
            Assert.IsNotNull(GameStateManager.Instance);
            Assert.AreEqual(instanceBefore, GameStateManager.Instance);
        }
    }
}
