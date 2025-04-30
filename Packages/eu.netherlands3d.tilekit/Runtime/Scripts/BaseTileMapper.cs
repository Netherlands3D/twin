using System.Collections.Generic;
using Netherlands3D.Tilekit.ExtensionMethods;
using UnityEngine;

namespace Netherlands3D.Tilekit.TileSets
{
    public abstract class BaseTileMapper : MonoBehaviour
    {
        public BaseTileSetProvider TileSetProvider;

        public List<Tile> TilesInView { get; } = new();

        private void OnDrawGizmosSelected()
        {
            foreach (var tile in TilesInView)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawCube(tile.BoundingVolume.Center.ToVector3(), tile.BoundingVolume.Size.ToVector3());
                foreach (var tileContent in tile.TileContents)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawCube(tileContent.BoundingVolume.Center.ToVector3(), tileContent.BoundingVolume.Size.ToVector3());
                }
            }
        }
    }
}