using UnityEngine;

namespace Netherlands3D.WMSTiles3D
{
    public abstract class TileFunctions : MonoBehaviour
    {
  
       public abstract void AddSubtree(Tile parentTile);
       public abstract void CreateContent(Tile parentTile);


    }
}
