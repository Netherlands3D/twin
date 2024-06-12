using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class Tile3DLayerButton : ObjectLibraryButton
    {
        protected override void CreateObject()
        {
            var layerParent = GameObject.FindWithTag("3DTileParent").transform;
            var newObject = Instantiate(prefab, Vector3.zero, Quaternion.Euler(initialRotation), layerParent);
            newObject.transform.localScale = initialScale;
            newObject.name = prefab.name;
            var layerComponent = newObject.GetComponent<Tile3DLayer2>();
            if (!layerComponent)
                newObject.AddComponent<Tile3DLayer2>();            
        }
    }
}
