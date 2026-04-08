using GeneForge.Core;
using GeneForge.Creatures;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.Combat
{
    /// <summary>
    /// Pure C# static validator for TurnAction invariants (GDD Turn Manager §3.3).
    /// Extracted from PlayerInputController for testability.
    /// Returns a validation result with a reason string on failure.
    ///
    /// Note: <see cref="TurnAction.Suppressed"/> is not validated here — suppression
    /// is a TurnManager execution concern, not a submission-time invariant.
    /// </summary>
    public static class TurnActionValidator
    {
        /// <summary>Maximum valid move PP slot index (0-based, 4 move slots).</summary>
        private const int MaxMoveSlotIndex = 3;

        /// <summary>Result of a TurnAction validation check.</summary>
        public readonly struct ValidationResult
        {
            /// <summary>True if the action passes all invariant checks.</summary>
            public readonly bool IsValid;

            /// <summary>Human-readable reason for failure. Null when valid.</summary>
            public readonly string Reason;

            /// <summary>Creates a validation result.</summary>
            public ValidationResult(bool isValid, string reason = null)
            {
                IsValid = isValid;
                Reason = reason;
            }

            /// <summary>Shorthand for a passing result.</summary>
            public static ValidationResult Valid => new ValidationResult(true);

            /// <summary>Shorthand for a failing result with reason.</summary>
            public static ValidationResult Invalid(string reason) => new ValidationResult(false, reason);
        }

        /// <summary>
        /// Validate a TurnAction against GDD §3.3 invariants.
        /// </summary>
        /// <param name="action">The action to validate.</param>
        /// <param name="actor">The creature performing the action.</param>
        /// <param name="encounterType">Current encounter type (affects Capture validity).</param>
        /// <param name="grid">Grid system for range validation. Required for UseMove range checks.</param>
        /// <returns>Validation result with reason on failure.</returns>
        public static ValidationResult Validate(
            TurnAction action, CreatureInstance actor,
            EncounterType encounterType, GridSystem grid)
        {
            if (actor == null) return ValidationResult.Invalid("Actor is null.");

            switch (action.Action)
            {
                case ActionType.UseMove:
                    return ValidateUseMove(action, actor, grid);

                case ActionType.Capture:
                    return ValidateCapture(action, encounterType);

                case ActionType.Flee:
                    return ValidateFlee(action);

                case ActionType.Wait:
                    return ValidateWait(action);

                case ActionType.Item:
                    // Post-MVP; silently accepted as Wait equivalent
                    return ValidationResult.Valid;

                default:
                    return ValidationResult.Invalid($"Unknown ActionType: {action.Action}");
            }
        }

        /// <summary>
        /// UseMove: Move non-null, MovePPSlot 0-3, PP > 0, form accessible,
        /// target valid per TargetType.
        /// </summary>
        private static ValidationResult ValidateUseMove(
            TurnAction action, CreatureInstance actor, GridSystem grid)
        {
            if (action.Move == null)
                return ValidationResult.Invalid("UseMove: Move is null.");

            if (action.MovePPSlot < 0 || action.MovePPSlot > MaxMoveSlotIndex)
                return ValidationResult.Invalid(
                    $"UseMove: MovePPSlot {action.MovePPSlot} out of range 0-{MaxMoveSlotIndex}.");

            if (action.MovePPSlot >= actor.LearnedMovePP.Count)
                return ValidationResult.Invalid(
                    $"UseMove: MovePPSlot {action.MovePPSlot} exceeds learned move count.");

            if (actor.LearnedMovePP[action.MovePPSlot] <= 0)
                return ValidationResult.Invalid("UseMove: No PP remaining for selected move.");

            // Form access check
            if (action.Move.Form != DamageForm.None &&
                !actor.AvailableForms.Contains(action.Move.Form))
                return ValidationResult.Invalid(
                    $"UseMove: Creature lacks body part for {action.Move.Form} form.");

            return ValidateUseMoveTarget(action, actor, grid);
        }

        /// <summary>
        /// Validates the target field based on the move's TargetType.
        /// </summary>
        private static ValidationResult ValidateUseMoveTarget(
            TurnAction action, CreatureInstance actor, GridSystem grid)
        {
            switch (action.Move.TargetType)
            {
                case TargetType.Single:
                case TargetType.SingleAlly:
                    if (action.Target == null)
                        return ValidationResult.Invalid(
                            "UseMove: Target creature is null for single-target move.");
                    return ValidateTargetRange(actor, action.Target.GridPosition, action.Move.Range, grid);

                case TargetType.Adjacent:
                    if (action.Target == null)
                        return ValidationResult.Invalid(
                            "UseMove: Target creature is null for adjacent move.");
                    // Adjacent moves must target distance == 1
                    if (grid != null)
                    {
                        int dist = GridSystem.ChebyshevDistance(
                            actor.GridPosition, action.Target.GridPosition);
                        if (dist != 1)
                            return ValidationResult.Invalid(
                                $"UseMove: Adjacent target at distance {dist}, must be exactly 1.");
                    }
                    break;

                case TargetType.AoE:
                case TargetType.Line:
                    if (action.TargetTile == null)
                        return ValidationResult.Invalid(
                            "UseMove: TargetTile is null for AoE/Line move.");
                    break;

                case TargetType.Self:
                case TargetType.AllAllies:
                    // No target needed
                    break;
            }

            return ValidationResult.Valid;
        }

        /// <summary>
        /// Checks Chebyshev distance between actor and target position against move range.
        /// </summary>
        private static ValidationResult ValidateTargetRange(
            CreatureInstance actor, Vector2Int targetPos, int moveRange, GridSystem grid)
        {
            if (grid == null) return ValidationResult.Valid;

            int dist = GridSystem.ChebyshevDistance(actor.GridPosition, targetPos);
            if (dist > moveRange)
                return ValidationResult.Invalid(
                    $"UseMove: Target at distance {dist} exceeds move range {moveRange}.");

            return ValidationResult.Valid;
        }

        /// <summary>
        /// Capture: Target non-null, encounter allows capture.
        /// </summary>
        private static ValidationResult ValidateCapture(
            TurnAction action, EncounterType encounterType)
        {
            if (action.Target == null)
                return ValidationResult.Invalid("Capture: Target is null.");

            if (encounterType == EncounterType.Trainer)
                return ValidationResult.Invalid("Capture: Cannot capture in trainer battles.");

            return ValidationResult.Valid;
        }

        /// <summary>
        /// Flee: MovementTarget must be null (flee consumes entire turn).
        /// Note: Flee in trainer battles is a valid submission — TurnManager handles the no-op
        /// per GDD §3.8. Validation does not block it.
        /// </summary>
        private static ValidationResult ValidateFlee(TurnAction action)
        {
            if (action.MovementTarget != null)
                return ValidationResult.Invalid(
                    "Flee: MovementTarget must be null (flee consumes entire turn).");

            return ValidationResult.Valid;
        }

        /// <summary>
        /// Wait: all optional fields null / -1.
        /// </summary>
        private static ValidationResult ValidateWait(TurnAction action)
        {
            if (action.Move != null)
                return ValidationResult.Invalid("Wait: Move should be null.");

            if (action.Target != null)
                return ValidationResult.Invalid("Wait: Target should be null.");

            if (action.TargetTile != null)
                return ValidationResult.Invalid("Wait: TargetTile should be null.");

            if (action.MovePPSlot != -1)
                return ValidationResult.Invalid(
                    $"Wait: MovePPSlot should be -1, got {action.MovePPSlot}.");

            return ValidationResult.Valid;
        }
    }
}
