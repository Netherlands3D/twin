using System.Linq;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.UI.AddLayer;
using UnityEngine;

namespace Netherlands3D.Functionalities.OGC3DTiles
{
    public class Tile3DLayerToggle : LayerToggle
    {
        protected override void Awake()
        {
            base.Awake();
            layerParent = ServiceLocator.GetService("3DTileParent").transform;
            layerGameObject = layerParent.GetComponentsInChildren<Tile3DLayerGameObject>().FirstOrDefault(l => l.PrefabIdentifier == prefab.GetComponent<Tile3DLayerGameObject>().PrefabIdentifier);
        }
    }
}