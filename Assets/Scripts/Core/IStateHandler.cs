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
