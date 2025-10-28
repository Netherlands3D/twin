using System.Linq;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.UI.AddLayer
{
    public class StandardLayerToggle : LayerToggle
    {
        private CartesianTiles.TileHandler tileHandler;

        protected override void OnEnable()
        {
            tileHandler = FindAnyObjectByType<CartesianTiles.TileHandler>(FindObjectsInactive.Include);
            layerParent = tileHandler.transform;
            var prefabIdentifier = prefab.GetComponent<CartesianTileLayerGameObject>().PrefabIdentifier;

            layerGameObject = tileHandler.layers
                .Select(layer => layer.GetComponent<CartesianTileLayerGameObject>())
                .FirstOrDefault(l => l.PrefabIdentifier == prefabIdentifier);

            base.OnEnable();
        }
    }
}