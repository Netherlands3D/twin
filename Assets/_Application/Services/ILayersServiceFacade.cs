using System;
using System.Threading.Tasks;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Services
{
    public interface ILayersServiceFacade
    {
        public Layer Add(LayerPresetArgs layerBuilder);
        public Layer Add(ILayerBuilder layerBuilder);
        public Task<Layer> VisualizeData(LayerData layerData);
        public Task<Layer> VisualizeAs(LayerData layerData, string prefabIdentifier);
        public void Remove(LayerData layer);

        public UnityEvent<Layer> LayerAdded { get; }
        public UnityEvent<LayerData> LayerRemoved { get; }
    }
}