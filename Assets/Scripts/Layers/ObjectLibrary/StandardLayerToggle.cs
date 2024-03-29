using System.Linq;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class StandardLayerToggle : LayerToggle
    {
        private CartesianTiles.TileHandler tileHandler;

        protected override void OnEnable()
        { 
            tileHandler = FindAnyObjectByType<CartesianTiles.TileHandler>(FindObjectsInactive.Include);
            layerParent = tileHandler.transform;
            layer = tileHandler.layers.FirstOrDefault(l => l.name == prefab.name)?.GetComponent<CartesianTileLayer>();

            base.OnEnable();
        }
    }
}