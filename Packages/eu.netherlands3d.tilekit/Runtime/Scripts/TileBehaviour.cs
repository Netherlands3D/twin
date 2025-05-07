using Netherlands3D.Tilekit;
using Netherlands3D.Tilekit.TileSets;
using UnityEngine;

namespace Netherlands3D.Twin.Tilekit
{
    public class TileBehaviour : MonoBehaviour
    {
        public Tile Tile { get; set; }
        [SerializeField] private TileContentRegistry tileContentRegistry;
    }
}