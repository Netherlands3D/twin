using UnityEngine;

namespace Netherlands3D.Tilekit.TileSets
{
    public abstract class TileMapper : MonoBehaviour
    {
        protected TileSet tileSet;

        public virtual void Load(TileSet tileSet)
        {
            this.tileSet = tileSet;
        }
        
        public virtual void Stage()
        {
        }

        public abstract void Render();
    }
}