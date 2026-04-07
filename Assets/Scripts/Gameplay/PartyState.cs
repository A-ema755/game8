using System;
using System.Collections.Generic;
using GeneForge.Core;
using GeneForge.Creatures;

namespace GeneForge.Gameplay
{
    /// <summary>
    /// Runtime party state: active party (ordered, capped) and unlimited storage.
    /// Implements GDD: design/gdd/party-system.md sections 3.1-3.6.
    /// Pure C# — no MonoBehaviour. Serializable for JsonUtility save/load (ADR-002, ADR-005).
    /// </summary>
    [System.Serializable]
    public class PartyState
    {
        // ── Events ───────────────────────────────────────────────────────
        /// <summary>Fired after any mutation to active party or storage.</summary>
        [field: NonSerialized] public event Action PartyChanged;

        /// <summary>Fired when HasConscious transitions to false (all party members fainted).</summary>
        [field: NonSerialized] public event Action PartyWiped;

        // ── Settings shorthand ───────────────────────────────────────────
        private static GameSettings S => ConfigLoader.Settings;
        private const int DefaultMaxPartySize = 6;
        private int MaxPartySize => S != null ? S.MaxPartySize : DefaultMaxPartySize;

        // ── State ────────────────────────────────────────────────────────
        [UnityEngine.SerializeField] private List<CreatureInstance> _activeParty = new();
        [UnityEngine.SerializeField] private List<CreatureInstance> _storage = new();

        // ── Properties ───────────────────────────────────────────────────

        /// <summary>Lead creature (slot 0), or null if party is empty.</summary>
        public CreatureInstance Lead => _activeParty.Count > 0 ? _activeParty[0] : null;

        /// <summary>True if at least one active party creature is not fainted.</summary>
        public bool HasConscious
        {
            get
            {
                foreach (var c in _activeParty)
                    if (!c.IsFainted) return true;
                return false;
            }
        }

        /// <summary>True if active party is at or above MaxPartySize.</summary>
        public bool IsPartyFull => _activeParty.Count >= MaxPartySize;

        /// <summary>Number of creatures in the active party.</summary>
        public int ActiveCount => _activeParty.Count;

        /// <summary>Number of creatures in storage.</summary>
        public int StorageCount => _storage.Count;

        /// <summary>Read-only view of the active party (ordered).</summary>
        public IReadOnlyList<CreatureInstance> ActiveParty => _activeParty;

        /// <summary>Read-only view of storage (unordered, unlimited).</summary>
        public IReadOnlyList<CreatureInstance> Storage => _storage;

        // ── Mutations ────────────────────────────────────────────────────

        /// <summary>
        /// Add a creature to the active party.
        /// Returns false if the party is full (GDD section 3.1).
        /// </summary>
        public bool AddToParty(CreatureInstance creature)
        {
            if (creature == null || IsPartyFull) return false;
            _activeParty.Add(creature);
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// Add a creature directly to storage. Always succeeds (GDD section 3.2).
        /// </summary>
        public void AddToStorage(CreatureInstance creature)
        {
            if (creature == null) return;
            _storage.Add(creature);
            NotifyChanged();
        }

        /// <summary>
        /// Deposit active party creature at <paramref name="partySlot"/> to storage.
        /// Blocked if depositing would leave zero conscious creatures in the active party.
        /// Returns false if blocked or slot is out of range.
        /// </summary>
        public bool DepositToStorage(int partySlot)
        {
            if (partySlot < 0 || partySlot >= _activeParty.Count) return false;

            var creature = _activeParty[partySlot];

            if (!creature.IsFainted)
            {
                int consciousCount = 0;
                foreach (var c in _activeParty)
                    if (!c.IsFainted) consciousCount++;

                if (consciousCount <= 1) return false;
            }

            _activeParty.RemoveAt(partySlot);
            _storage.Add(creature);
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// Withdraw creature at <paramref name="storageIndex"/> from storage into the active party.
        /// Returns false if the party is full or index is out of range (GDD section 3.2).
        /// </summary>
        public bool WithdrawFromStorage(int storageIndex)
        {
            if (storageIndex < 0 || storageIndex >= _storage.Count) return false;
            if (IsPartyFull) return false;

            var creature = _storage[storageIndex];
            _storage.RemoveAt(storageIndex);
            _activeParty.Add(creature);
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// Swap two active party slots. No-op if either index is out of range.
        /// </summary>
        public void SwapPartySlots(int slotA, int slotB)
        {
            if (slotA < 0 || slotA >= _activeParty.Count) return;
            if (slotB < 0 || slotB >= _activeParty.Count) return;
            if (slotA == slotB) return;

            (_activeParty[slotA], _activeParty[slotB]) = (_activeParty[slotB], _activeParty[slotA]);
            NotifyChanged();
        }

        /// <summary>
        /// If slot 0 is fainted, rotate it to the back and advance the next conscious creature
        /// to slot 0. Guards against infinite loop when all creatures are fainted (GDD section 5).
        /// </summary>
        public void PromoteNextConscious()
        {
            int count = _activeParty.Count;
            if (count == 0) return;

            int rotations = 0;
            while (rotations < count && _activeParty[0].IsFainted)
            {
                var fainted = _activeParty[0];
                _activeParty.RemoveAt(0);
                _activeParty.Add(fainted);
                rotations++;
            }

            NotifyChanged();
        }

        /// <summary>
        /// Revive all fainted creatures in the active party to 1 HP (GDD section 3.6 party wipe recovery).
        /// </summary>
        public void ReviveAll()
        {
            foreach (var c in _activeParty)
                if (c.IsFainted) c.Revive(1);
            NotifyChanged();
        }

        /// <summary>
        /// Get the active party creature at <paramref name="slot"/>. Returns null if out of range.
        /// </summary>
        public CreatureInstance GetPartyMember(int slot)
        {
            if (slot < 0 || slot >= _activeParty.Count) return null;
            return _activeParty[slot];
        }

        // ── Internal ─────────────────────────────────────────────────────

        private void NotifyChanged()
        {
            PartyChanged?.Invoke();
            if (!HasConscious && _activeParty.Count > 0)
                PartyWiped?.Invoke();
        }
    }
}
