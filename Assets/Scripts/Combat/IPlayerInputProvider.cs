using System.Collections.Generic;
using GeneForge.Creatures;

namespace GeneForge.Combat
{
    /// <summary>
    /// Abstraction for player input collection during combat.
    /// Enables testing without UI — test implementations can pre-load actions
    /// and return immediately, while the real UI implementation buffers
    /// actions from MoveSelectionPanel clicks across multiple frames.
    ///
    /// Contract:
    /// 1. Caller invokes <see cref="BeginActionCollection"/> at the start of PlayerCreatureSelect.
    /// 2. Caller polls <see cref="AllActionsReady"/> each frame (or immediately in tests).
    /// 3. Once ready, caller retrieves actions via <see cref="GetActions"/>.
    ///
    /// TurnManager only calls <see cref="GetActions"/> — the CombatController
    /// coroutine handles steps 1–2 before advancing the round.
    /// </summary>
    public interface IPlayerInputProvider
    {
        /// <summary>
        /// Signal that action collection should begin for the given creatures.
        /// Clears any previously buffered actions. Called once per round
        /// during PlayerCreatureSelect phase.
        /// </summary>
        /// <param name="creatures">Non-fainted player creatures that need actions this round.</param>
        void BeginActionCollection(IReadOnlyList<CreatureInstance> creatures);

        /// <summary>
        /// True when all creature actions have been submitted for this round.
        /// Polled each frame by CombatController's coroutine.
        /// Test implementations may return true immediately.
        /// </summary>
        bool AllActionsReady { get; }

        /// <summary>
        /// Retrieve the collected actions. Only valid when <see cref="AllActionsReady"/> is true.
        /// Returns a snapshot — callers may mutate the returned dictionary without
        /// affecting the provider's internal state.
        /// </summary>
        IReadOnlyDictionary<CreatureInstance, TurnAction> GetActions();
    }
}
