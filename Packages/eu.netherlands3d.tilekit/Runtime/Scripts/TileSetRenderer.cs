using UnityEngine;

namespace Netherlands3D.Tilekit.TileSets
{
    public abstract class TileSetRenderer : MonoBehaviour
    {
        public virtual void Stage(TileSet tileSet)
        {
        }

        public abstract void Render(TileSet tileSet);
    }
}