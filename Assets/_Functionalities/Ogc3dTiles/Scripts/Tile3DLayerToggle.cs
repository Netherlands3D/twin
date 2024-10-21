using System.Linq;
using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class Tile3DLayerToggle : LayerToggle
    {
        protected override void Awake()
        {
            base.Awake();
            layerParent = GameObject.FindWithTag("3DTileParent").transform;
            layerGameObject = layerParent.GetComponentsInChildren<Tile3DLayerGameObject>().FirstOrDefault(l => l.PrefabIdentifier == prefab.GetComponent<Tile3DLayerGameObject>().PrefabIdentifier);
        }
    }
}