using System.Threading.Tasks;
using Netherlands3D.Functionalities.ObjectLibrary;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Services;
using UnityEngine;

namespace Netherlands3D.Functionalities.OGC3DTiles
{
    public class Tile3DLayerButton : ObjectLibraryButton
    {
        protected override async Task<LayerData> CreateLayer(ILayerBuilder layerBuilder = null)
        {
            layerBuilder ??= LayerBuilder.Create();
            layerBuilder.NamedAs(prefab.name);
            
            if (await base.CreateLayer(layerBuilder) is not ReferencedLayerData newLayer)
            {
                Debug.LogError(
                    "Expected layer created by the Tile3DLayerButton to be ReferencedLayerData, but received a null"
                );

                return null;
            }
            
            if (newLayer.Reference is not Tile3DLayerGameObject tile3dLayerGameObject)
            {
                Debug.LogError(
                    "Expected layer created by the Tile3DLayerButton to be a Tile3DLayerGameObject, " 
                    + $"but received a {newLayer.Reference.GetType()}"
                );
            }

            return newLayer;
        }
    }
}
