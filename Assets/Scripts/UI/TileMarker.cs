using UnityEngine;

namespace GeneForge.UI
{
    /// <summary>Marker component on grid tile GameObjects for raycast identification.</summary>
    public class TileMarker : MonoBehaviour
    {
        /// <summary>Grid position of this tile (X = tileX, Y = tileZ).</summary>
        public Vector2Int GridPosition { get; set; }
    }
}
