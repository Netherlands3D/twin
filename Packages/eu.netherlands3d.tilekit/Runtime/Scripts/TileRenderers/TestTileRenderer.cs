using System.Linq;
using Netherlands3D.Tilekit.Changes;
using RSG;
using UnityEngine;

namespace Netherlands3D.Tilekit.TileRenderers
{
    
    [CreateAssetMenu(menuName = "Netherlands3D/Tilekit/TileRenderers/Test", fileName = "TestTileRenderer", order = 0)]
    public class TestTileRenderer : TileRenderer
    {
        public override Promise Add(Change change)
        {
            Debug.Log(change.Type);
            Debug.Log(change.Tile.TileContents.FirstOrDefault().Uri.ToString());
            
            return base.Add(change);
        }

        public override Promise Replace(Change change)
        {
            Debug.Log(change.Type);
            Debug.Log(change.Tile.TileContents.FirstOrDefault().Uri.ToString());

            return base.Replace(change);
        }

        public override Promise Remove(Change change)
        {
            Debug.Log(change.Type);
            Debug.Log(change.Tile.TileContents.FirstOrDefault().Uri.ToString());

            return base.Remove(change);
        }
    }
}