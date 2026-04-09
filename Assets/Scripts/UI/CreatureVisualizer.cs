using System.Collections.Generic;
using GeneForge.Combat;
using GeneForge.Creatures;
using GeneForge.Grid;
using UnityEngine;

namespace GeneForge.UI
{
    /// <summary>
    /// Placeholder visualizer for creatures in combat. Spawns a capsule per
    /// creature and syncs world position to GridPosition after each action.
    /// Intended for playtesting only — not production visuals.
    ///
    /// Implements minimal visual feedback described in the combat playtest brief.
    /// Subscribe to CombatController events to keep visuals in sync with game state.
    /// </summary>
    public class CreatureVisualizer : MonoBehaviour
    {
        // ── Serialized Fields ─────────────────────────────────────────────

        [SerializeField] private CombatController _combatController;

        // ── Private State ─────────────────────────────────────────────────

        private readonly Dictionary<CreatureInstance, GameObject> _creatureObjects = new();
        private bool _spawned;

        // Colors
        private static readonly Color PlayerColor = new Color(0.2f, 0.4f, 0.9f);
        private static readonly Color EnemyColor  = new Color(0.9f, 0.2f, 0.2f);

        // ── Unity Lifecycle ───────────────────────────────────────────────

        private void OnEnable()
        {
            if (_combatController == null) return;
            _combatController.PhaseChanged    += OnPhaseChanged;
            _combatController.CreatureActed   += OnCreatureActed;
            _combatController.CreatureFainted += OnCreatureFainted;
        }

        private void OnDisable()
        {
            if (_combatController == null) return;
            _combatController.PhaseChanged    -= OnPhaseChanged;
            _combatController.CreatureActed   -= OnCreatureActed;
            _combatController.CreatureFainted -= OnCreatureFainted;
        }

        // ── Phase Handling ────────────────────────────────────────────────

        private void OnPhaseChanged(CombatUIPhase phase)
        {
            if (!_spawned && phase != CombatUIPhase.Inactive)
                SpawnCreatures();
        }

        // ── Creature Spawning ─────────────────────────────────────────────

        private void SpawnCreatures()
        {
            if (_combatController == null || _combatController.Grid == null)
            {
                Debug.LogWarning("[CreatureVisualizer] CombatController or Grid not ready.");
                return;
            }

            _spawned = true;

            if (_combatController.PlayerParty != null)
                foreach (var creature in _combatController.PlayerParty)
                    SpawnCreature(creature, PlayerColor);

            if (_combatController.EnemyParty != null)
                foreach (var creature in _combatController.EnemyParty)
                    SpawnCreature(creature, EnemyColor);
        }

        private void SpawnCreature(CreatureInstance creature, Color color)
        {
            if (creature == null) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = creature.DisplayName;
            go.transform.SetParent(transform);
            go.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            go.transform.position = ResolveWorldPosition(creature);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            go.GetComponent<Renderer>().material = mat;

            // Floating name label
            AttachNameLabel(go, creature.DisplayName);

            _creatureObjects[creature] = go;
        }

        private void AttachNameLabel(GameObject parent, string displayName)
        {
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(parent.transform);
            labelGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            labelGo.transform.localScale    = new Vector3(0.5f, 0.5f, 0.5f);

            var tm = labelGo.AddComponent<TextMesh>();
            tm.text      = displayName;
            tm.fontSize  = 24;
            tm.alignment = TextAlignment.Center;
            tm.anchor    = TextAnchor.MiddleCenter;
            tm.color     = Color.white;
        }

        // ── Event Handlers ────────────────────────────────────────────────

        private void OnCreatureActed(CreatureActedArgs args)
        {
            SyncAllPositions();
        }

        private void OnCreatureFainted(CreatureInstance creature)
        {
            if (creature == null) return;
            if (_creatureObjects.TryGetValue(creature, out var go) && go != null)
                go.SetActive(false);
        }

        // ── Position Sync ─────────────────────────────────────────────────

        private void SyncAllPositions()
        {
            foreach (var kvp in _creatureObjects)
            {
                var creature = kvp.Key;
                var go       = kvp.Value;

                if (go == null || !go.activeSelf) continue;

                go.transform.position = ResolveWorldPosition(creature);
            }
        }

        private Vector3 ResolveWorldPosition(CreatureInstance creature)
        {
            var grid = _combatController.Grid;
            var tile = grid?.GetTile(creature.GridPosition);
            float tileHeight = tile != null ? tile.Height : 0;

            return new Vector3(
                creature.GridPosition.x,
                1f + tileHeight * 0.5f,
                creature.GridPosition.y);
        }

        // ── Query API ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns the creature whose capsule is within 0.5 world units of
        /// <paramref name="worldPos"/>, or null if none are that close.
        /// </summary>
        public CreatureInstance GetCreatureAtWorldPosition(Vector3 worldPos)
        {
            CreatureInstance closest = null;
            float closestDist = 0.5f; // threshold

            foreach (var kvp in _creatureObjects)
            {
                var go = kvp.Value;
                if (go == null || !go.activeSelf) continue;

                float dist = Vector3.Distance(go.transform.position, worldPos);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = kvp.Key;
                }
            }

            return closest;
        }
    }
}
