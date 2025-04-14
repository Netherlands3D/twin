using Netherlands3D.Functionalities.ObjectLibrary;
using UnityEngine;

namespace Netherlands3D.Functionalities.OGC3DTiles
{
    public class Tile3DLayerButton : ObjectLibraryButton
    {
        protected override void CreateObject()
        {
            var layerParent = GameObject.FindWithTag("3DTileParent").transform;
            var newObject = Instantiate(prefab, Vector3.zero, prefab.transform.rotation, layerParent);
            
            var layerComponent = newObject.GetComponent<Tile3DLayerGameObject>();
            if (!layerComponent)
                layerComponent = newObject.gameObject.AddComponent<Tile3DLayerGameObject>();            
            
            layerComponent.Name = prefab.name;
        }
    }
}
