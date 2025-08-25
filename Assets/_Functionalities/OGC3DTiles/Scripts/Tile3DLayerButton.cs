using System.Threading.Tasks;
using Netherlands3D.Functionalities.ObjectLibrary;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine;

namespace Netherlands3D.Functionalities.OGC3DTiles
{
    public class Tile3DLayerButton : ObjectLibraryButton
    {
        protected override async Task<LayerData> CreateLayer()
        {
            if (await base.CreateLayer() is not ReferencedLayerData newLayer)
            {
                return null;
            }
            
            var layerParent = GameObject.FindWithTag("3DTileParent").transform;
            if (newLayer.Reference is not Tile3DLayerGameObject tile3dLayerGameObject)
            {
                tile3dLayerGameObject = newLayer.Reference.gameObject.AddComponent<Tile3DLayerGameObject>();
            }

            tile3dLayerGameObject.transform.SetParent(layerParent);
            tile3dLayerGameObject.Name = prefab.name;

            return newLayer;
        }
    }
}
