using System.Linq;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using UnityEngine;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class StandardLayerToggle : LayerToggle
    {
        private CartesianTiles.TileHandler tileHandler;

        protected override void OnEnable()
        { 
            tileHandler = FindAnyObjectByType<CartesianTiles.TileHandler>(FindObjectsInactive.Include);
            layerParent = tileHandler.transform;
            layerGameObject = tileHandler.layers.FirstOrDefault(l => l.GetComponent<CartesianTileLayerGameObject>().PrefabIdentifier == prefab.GetComponent<CartesianTileLayerGameObject>().PrefabIdentifier)?.GetComponent<CartesianTileLayerGameObject>();
            base.OnEnable();
        }
    }
}