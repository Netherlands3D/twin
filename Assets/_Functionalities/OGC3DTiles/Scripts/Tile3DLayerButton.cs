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
        protected override async Task<Layer> CreateLayer(ILayerBuilder layerBuilder = null)
        {
            layerBuilder ??= LayerBuilder.Create();
            layerBuilder.NamedAs(prefab.name);

            Layer layer = await base.CreateLayer(layerBuilder);

            if (layer.LayerData.Visualization == null)
            {
                Debug.LogError(
                    "Expected layer created by the Tile3DLayerButton to be ReferencedLayerData, but received a null"
                );

                return null;
            }
            
            if (layer.LayerGameObject is not Tile3DLayerGameObject tile3dLayerGameObject)
            {
                Debug.LogError(
                    "Expected layer created by the Tile3DLayerButton to be a Tile3DLayerGameObject, " 
                    + $"but received a {layer.LayerGameObject.GetType()}"
                );
            }

            return layer;
        }
    }
}
