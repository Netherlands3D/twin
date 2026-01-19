using System;
using System.Threading.Tasks;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.DataTypeAdapters;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Services
{
    public class Layers : MonoBehaviour, ILayersServiceFacade
    {
        [SerializeField] private PrefabLibrary prefabLibrary;
        [SerializeField] private FileTypeAdapter fromFileImporter;
        [SerializeField] private DataTypeChain fromUrlImporter;
        private VisualizationSpawner spawner;
        
        public UnityEvent<Layer> LayerAdded { get; } = new();
        public UnityEvent<LayerData> LayerRemoved { get; } = new();

        private void Awake()
        {
            spawner = new VisualizationSpawner(prefabLibrary);
        }
        
        /// <summary>
        /// Adds a new layer to the current project using the given preset.
        /// </summary>
        public Layer Add(LayerPresetArgs args, UnityAction<LayerGameObject> callback = null)
        {
            return Add(LayerBuilder.Create(args), callback);
        }

        /// <summary>
        /// Adds a new layer to the current project using the given builder.
        /// </summary>
        public Layer Add(ILayerBuilder builder, UnityAction<LayerGameObject> callback = null)
        {
            if (builder is not LayerBuilder layerBuilder)
            {
                throw new NotSupportedException("Unsupported layer builder type: " + builder.GetType().Name);
            }

            var layerData = builder.Build();
            var layer = new Layer(layerData);
            Visualize(layer, spawner, callback);
            LayerAdded.Invoke(layer);
            return layer;
        }
        
        public void AddFromUrl(Uri uri, StoredAuthorization authorization, UnityAction<LayerGameObject> callback = null)
        {
            fromUrlImporter.DetermineAdapter(uri, authorization);
        }
        
        /// <summary>
        /// Force a LayerData object to be visualized as a specific prefab.
        ///
        /// Warning: this code does not check if the given prefab is compatible with this LayerData, make sure you know what you are doing.
        /// </summary>
        public Layer VisualizeAs(LayerData layerData, string prefabIdentifier, UnityAction<LayerGameObject> callback = null)
        {
            layerData.PrefabIdentifier = prefabIdentifier;
            var layer = new Layer(layerData);
            Visualize(layer, spawner, callback);
            return layer;
        }

        /// <summary>
        /// Removes the layer from the current project and ensures the visualisation is removed as well.
        /// </summary>
        public void Remove(LayerData layerData)
        {
            layerData.Dispose();  
            LayerRemoved.Invoke(layerData);
        }

        public void VisualizeData(LayerData layerData, UnityAction<LayerGameObject> callback = null)
        {
            Layer layer = new Layer(layerData);
            Visualize(layer, spawner, callback);
        }
        
        private static async void Visualize(Layer layer, ILayerSpawner spawner, UnityAction<LayerGameObject> callback = null) //todo: change callbacks for promises?
        {
            try
            {
                Task<LayerGameObject> visualizationTask = layer.LayerGameObjectTask;
                if (layer.LayerGameObjectTask == null)
                {
                    visualizationTask = spawner.Spawn(layer.LayerData);
                    layer.SetVisualizationTask(visualizationTask);
                }
                
                var visualization = await visualizationTask;

                if (layer.LayerData == null || layer.LayerData.IsDisposed)
                {
                    Debug.Log("Layer " + layer.LayerData.Name + " was disposed before the visualisation was spawned, destroying the visualisation");
                    Destroy(visualization.gameObject);
                    return;
                }
                
                visualization?.SetData(layer.LayerData);
                callback?.Invoke(visualization);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}