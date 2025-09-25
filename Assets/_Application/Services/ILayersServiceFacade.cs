using System.Threading.Tasks;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine;

namespace Netherlands3D.Twin.Services
{
    public interface ILayersServiceFacade
    {
        public Task<ReferencedLayerData> Add(string preset, LayerPresetArgs args);
        public Task<ReferencedLayerData> Add(ILayerBuilder layerBuilder);
        public Task<ReferencedLayerData> Add(ReferencedLayerData layerData);
        public Task<ReferencedLayerData> Add(ReferencedLayerData layerData, Vector3 position, Quaternion? rotation = null);
        public Task Remove(ReferencedLayerData layerData);
    }
}