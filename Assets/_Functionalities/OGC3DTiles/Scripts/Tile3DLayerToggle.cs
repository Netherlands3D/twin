using System.Linq;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.UI.AddLayer;

namespace Netherlands3D.Functionalities.OGC3DTiles
{
    public class Tile3DLayerToggle : LayerToggle
    {
        protected override void Awake()
        {
            base.Awake();

            var prefabIdentifier = prefab.GetComponent<Tile3DLayerGameObject>().PrefabIdentifier;

            var container = ServiceLocator.GetService<Tile3DLayerSet>();
            var layers = container.All();

            layerParent = container.transform;
            layerGameObject = layers.FirstOrDefault(l => l.PrefabIdentifier == prefabIdentifier);
        }
    }
}