using UnityEngine;

namespace GeneForge.Core
{
    /// <summary>Base class for all Gene Forge config ScriptableObjects.</summary>
    public abstract class ConfigBase : ScriptableObject
    {
        [SerializeField] string id;
        [SerializeField] string displayName;

        public string Id => id;
        public string DisplayName => displayName;
    }
}
