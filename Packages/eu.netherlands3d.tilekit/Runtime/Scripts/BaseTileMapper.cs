using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Tilekit.TileSets
{
    public abstract class BaseTileMapper : MonoBehaviour
    {
        protected TileSet? tileSet;

        public virtual void Load(TileSet tileSet)
        {
            this.tileSet = tileSet;
        }
    }
}