using System.Threading.Tasks;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine;

namespace Netherlands3D.Twin.Services
{
    public interface ILayersServiceFacade
    {
        public Task<Layer> Add(LayerPresetArgs layerBuilder);
        public Task<Layer> Add(ILayerBuilder layerBuilder);
        public Task<Layer> SpawnLayer(LayerData layerData);
        // public Task<Layer> SpawnLayer(LayerData layerData, Vector3 position, Quaternion rotation = default);
        public Task<Layer> VisualizeAs(LayerData layerData, string prefabIdentifier);
        public void Remove(Layer layer);
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