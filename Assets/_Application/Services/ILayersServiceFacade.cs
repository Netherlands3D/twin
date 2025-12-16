using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Services
{
    public interface ILayersServiceFacade
    {
        public Layer Add(LayerPresetArgs layerBuilder, UnityAction<LayerGameObject> callback = null, UnityAction<Exception> errorCallback = null);
        public Layer Add(ILayerBuilder layerBuilder, UnityAction<LayerGameObject> callback = null, UnityAction<Exception> errorCallback = null);
        public void VisualizeData(LayerData layerData, UnityAction<LayerGameObject> callback = null, UnityAction<Exception> errorCallback = null);
        public Layer VisualizeAs(LayerData layerData, string prefabIdentifier, UnityAction<LayerGameObject> callback = null, UnityAction<Exception> errorCallback = null);
        public void Remove(LayerData layer);

        public UnityEvent<Layer> LayerAdded { get; }
        public UnityEvent<LayerData> LayerRemoved { get; }
    }
}