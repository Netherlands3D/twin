using System.Collections.Generic;
using Netherlands3D.Tilekit.ExtensionMethods;
using UnityEngine;

namespace Netherlands3D.Tilekit.TileSets
{
    public abstract class BaseTileMapper : MonoBehaviour
    {
        public BaseTileSetProvider TileSetProvider;

        public HashSet<Tile> TilesInView { get; } = new();

        protected virtual void OnDrawGizmosSelected()
        {
            foreach (var tile in TilesInView)
            {
                DrawTileGizmo(tile, Color.blue, Color.green);
            }
        }

        protected void DrawTileGizmo(Tile tile, Color tileColor, Color tileContentColor, float sizeFactor = 1f)
        {
            Gizmos.color = tileColor;
            Gizmos.DrawWireCube(tile.BoundingVolume.Center.ToVector3(), tile.BoundingVolume.Size.ToVector3() * sizeFactor);
            Gizmos.color = tileContentColor;
            foreach (var tileContent in tile.TileContents)
            {
                // Draw content boxes at 99% the size to see them inside the main tile gizmo
                Gizmos.DrawWireCube(
                    tileContent.BoundingVolume.Center.ToVector3(), 
                    tileContent.BoundingVolume.Size.ToVector3() * 0.99f * sizeFactor
                );
            }
        }
    }
}