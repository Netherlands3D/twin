using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Functionalities.OGC3DTiles
{
    public class Tile3DLayerSet : MonoBehaviour
    {
        public IEnumerable<Tile3DLayerGameObject> All()
        {
            return transform.GetComponentsInChildren<Tile3DLayerGameObject>();
        }

        public void Attach(Tile3DLayerGameObject layerGameObject)
        {
            layerGameObject.transform.SetParent(transform);
        }

        public void Detach(Tile3DLayerGameObject layerGameObject)
        {
            layerGameObject.transform.SetParent(null);
        }
    }
}