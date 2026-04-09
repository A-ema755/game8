using GeneForge.Creatures;
using UnityEngine;

namespace GeneForge.UI
{
    /// <summary>Marker component on creature GameObjects for raycast identification.</summary>
    public class CreatureMarker : MonoBehaviour
    {
        /// <summary>The CreatureInstance represented by this GameObject.</summary>
        public CreatureInstance Creature { get; set; }
    }
}
