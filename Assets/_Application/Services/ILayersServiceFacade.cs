using System;
using System.Threading.Tasks;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Services
{
    public interface ILayersServiceFacade
    {
        // public Task<Layer> Add(LayerPresetArgs layerBuilder);
        // public Task<Layer> Add(ILayerBuilder layerBuilder);
        // public Task<Layer> VisualizeData(LayerData layerData);
        public Layer Add(LayerPresetArgs layerBuilder, UnityAction<LayerGameObject> callback = null, UnityAction<Exception> errorCallback = null);
        public Layer Add(ILayerBuilder layerBuilder, UnityAction<LayerGameObject> callback = null, UnityAction<Exception> errorCallback = null);
        // public Layer VisualizeData(LayerData layerData, UnityAction<LayerGameObject> callback = null);
        
        //public Task<Layer> SpawnLayer(LayerData layerData);
        // public Task<Layer> SpawnLayer(LayerData layerData, Vector3 position, Quaternion rotation = default);
        public void VisualizeData(LayerData layerData, UnityAction<LayerGameObject> callback = null, UnityAction<Exception> errorCallback = null);
        public Layer VisualizeAs(LayerData layerData, string prefabIdentifier, UnityAction<LayerGameObject> callback = null, UnityAction<Exception> errorCallback = null);
        public void Remove(LayerData layer);
        
        public UnityEvent<Layer> LayerAdded { get; }
        public UnityEvent<LayerData> LayerRemoved { get; }
    }

    public record Layer
    {
        public readonly LayerData LayerData;
        public LayerGameObject LayerGameObject { get; private set; }
        
        public Layer(LayerData layerData)
        {
            LayerData = layerData;
        }

        public void SetVisualization(LayerGameObject layerGameObject)
        {
            LayerGameObject = layerGameObject;
        }
    }
}