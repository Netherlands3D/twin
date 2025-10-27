using System.Threading.Tasks;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine;

namespace Netherlands3D.Twin.Services
{
    public interface ILayersServiceFacade
    {
        public Task<ReferencedLayerData> Add(LayerPresetArgs layerBuilder);
        public Task<ReferencedLayerData> Add(ILayerBuilder layerBuilder);
        public Task<ReferencedLayerData> Visualize(ReferencedLayerData layerData);
        public Task<ReferencedLayerData> Visualize(ReferencedLayerData layerData, Vector3 position, Quaternion? rotation = null);
        public Task<LayerGameObject> ReplaceVisualisation(ReferencedLayerData layerData, string prefabId);
        public Task Remove(ReferencedLayerData layerData);
    }
}