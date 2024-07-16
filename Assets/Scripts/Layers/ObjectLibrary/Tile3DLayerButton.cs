using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class Tile3DLayerButton : ObjectLibraryButton
    {
        protected override void CreateObject()
        {
            var layerParent = GameObject.FindWithTag("3DTileParent").transform;
            var newObject = Instantiate(prefab, Vector3.zero, Quaternion.Euler(initialRotation), layerParent);
            newObject.transform.localScale = initialScale;
            var layerComponent = newObject.GetComponent<Tile3DLayer>();
            if (!layerComponent)
                layerComponent = newObject.AddComponent<Tile3DLayer>();            
            
            layerComponent.Name = prefab.name;
        }
    }
}
