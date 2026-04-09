using GeneForge.Combat;
using GeneForge.Creatures;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GeneForge.UI
{
    /// <summary>
    /// Translates mouse clicks on the 3D scene into combat input during the
    /// PlayerSelect phase. Raycasts from the main camera through the mouse
    /// position and routes hits to PlayerInputController.
    ///
    /// Tile GameObjects must carry a TileMarker component (preferred) or the
    /// "Tile" tag. Creature GameObjects must carry a CreatureMarker component
    /// (preferred) or the "Creature" tag.
    ///
    /// Attach to any persistent scene object alongside CombatController and
    /// PlayerInputController. Wire the serialized fields in the Inspector, or
    /// let CombatHUDController call Initialize() after scene setup.
    /// </summary>
    public class CombatInputBridge : MonoBehaviour
    {
        // ── Serialized Fields ─────────────────────────────────────────────

        [SerializeField] private CombatController _combatController;
        [SerializeField] private PlayerInputController _playerInputController;

        /// <summary>
        /// Layer mask for the raycast. Set to "Default" (everything) if left
        /// unset; restrict to tile/creature layers for performance in large scenes.
        /// </summary>
        [SerializeField] private LayerMask _raycastMask = ~0;

        /// <summary>
        /// Maximum raycast distance. Increase if the isometric camera is far
        /// from the grid (default 200 covers height 0–4 tiles at typical zoom).
        /// </summary>
        [SerializeField] private float _raycastDistance = 200f;

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Wire dependencies at runtime. Called by CombatHUDController or scene
        /// bootstrap when the combat scene is ready.
        /// </summary>
        /// <param name="combatController">Active CombatController.</param>
        /// <param name="playerInputController">Active PlayerInputController.</param>
        public void Initialize(
            CombatController combatController,
            PlayerInputController playerInputController)
        {
            _combatController = combatController;
            _playerInputController = playerInputController;
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────

        private void Update()
        {
            if (_combatController == null || _playerInputController == null)
                return;

            // Only active during PlayerSelect phase.
            if (_combatController.CurrentUIPhase != CombatUIPhase.PlayerSelect)
                return;

            if (!Mouse.current.leftButton.wasPressedThisFrame)
                return;

            ProcessClick();
        }

        // ── Private Helpers ───────────────────────────────────────────────

        private void ProcessClick()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[CombatInputBridge] No main camera found.");
                return;
            }

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (!Physics.Raycast(ray, out RaycastHit hit, _raycastDistance, _raycastMask))
                return;

            GameObject hitObj = hit.collider.gameObject;

            // ── Creature check (higher priority — creatures sit on top of tiles) ──
            CreatureInstance creature = ResolveCreature(hitObj);
            if (creature != null)
            {
                _playerInputController.OnCreatureClicked(creature);
                return;
            }

            // ── Tile check ────────────────────────────────────────────────
            Vector2Int? tilePos = ResolveTile(hitObj, hit.point);
            if (tilePos.HasValue)
            {
                _playerInputController.OnTileClicked(tilePos.Value);
            }
        }

        /// <summary>
        /// Returns the CreatureInstance for a hit GameObject, or null if not a creature.
        /// Checks CreatureMarker component first, then "Creature" tag as fallback.
        /// </summary>
        private static CreatureInstance ResolveCreature(GameObject obj)
        {
            // Component is the canonical path — set by CreatureVisualizer.
            var marker = obj.GetComponentInParent<CreatureMarker>();
            if (marker != null)
                return marker.Creature;

            return null;
        }

        /// <summary>
        /// Returns the grid position for a hit tile GameObject, or null if not a tile.
        /// Prefers TileMarker.GridPosition; falls back to rounding hit.point for
        /// "Tile"-tagged objects that have no marker.
        /// </summary>
        private static Vector2Int? ResolveTile(GameObject obj, Vector3 hitPoint)
        {
            // Component is the canonical path — set by GridVisualizer.
            var marker = obj.GetComponentInParent<TileMarker>();
            if (marker != null)
                return marker.GridPosition;

            return null;
        }
    }
}
