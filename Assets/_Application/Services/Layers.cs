using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.DataTypeAdapters;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.LayerTypes;
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
        public Layer Add(LayerPresetArgs args, UnityAction<LayerGameObject> callback = null, UnityAction<Exception> errorCallback = null)
        {
            return Add(LayerBuilder.Create(args), callback);
        }

        /// <summary>
        /// Adds a new layer to the current project using the given builder.
        /// </summary>
        [ItemCanBeNull]
        // public async Task<Layer> Add(ILayerBuilder builder)
        public Layer Add(ILayerBuilder builder, UnityAction<LayerGameObject> callback = null, UnityAction<Exception> errorCallback = null)
        {
            if (builder is not LayerBuilder layerBuilder)
            {
                throw new NotSupportedException("Unsupported layer builder type: " + builder.GetType().Name);
            }
            
            switch (layerBuilder.Type)
            {
                case "url": return ImportFromUrl(layerBuilder);
                case "file": return ImportFromFile(layerBuilder);
                case "folder": return AddFolderLayer(layerBuilder);
            }

            var layerData = builder.Build();
            // ProjectData.Current.AddStandardLayer(layerData);
            var layer = new Layer(layerData);
            //AddPlaceholder(layer)
            Visualize(layer, spawner, callback, errorCallback);
            // Layer layer = await VisualizeData(layerData);
            LayerAdded.Invoke(layer);
            return layer;
        }

        private Layer AddFolderLayer(LayerBuilder layerBuilder)
        {
            var folderLayer = layerBuilder.Build();
            // ProjectData.Current.AddStandardLayer(folderLayer);
            var folder = new Layer(folderLayer);
            LayerAdded.Invoke(folder);
            return folder;
        }

        private Layer ImportFromFile(LayerBuilder layerBuilder)
        {
            var url = RetrieveUrlForLayer(layerBuilder);
            fromFileImporter.ProcessFile(url.ToString());

            // Return null to indicate that adding this flow does not directly result in a Layer, it may do so
            // indirectly (DataTypeAdapters call this Layer service again).
            Debug.LogWarning("the fileTypeAdapter currently does not return anything, the returned object is null. This should be refactored in the future"); //todo: the fileTypeAdapter should be refactored to return the resulting objects
            return null;
        }

        private Layer ImportFromUrl(LayerBuilder layerBuilder)
        {
            var url = RetrieveUrlForLayer(layerBuilder);
            if (url.Scheme == "prefab-library")
            {
                // This is a stored prefab identifier from the prefab library, so let's try it again but
                // then as a direct build
                return Add(layerBuilder.OfType(url.AbsolutePath.Trim('/')));
            }
                    
            fromUrlImporter.DetermineAdapter(url, layerBuilder.Credentials);
                
            // Return null to indicate that adding this flow does not directly result in a Layer, it may do so
            // indirectly (DataTypeAdapters call this Layer service again).
            Debug.LogWarning("the urlImportAdapter currently does not return anything, the returned object is null. This should be refactored in the future"); //todo: the DataTypeChain should be refactored to return the resulting objects
            return null;
        }

       

        /// <summary>
        /// Visualizes an existing layer's data by spawning a placeholder and after that the actual visualisation
        /// (LayerGameObject).
        ///
        /// Usually used when loading a project file as this will restore the layer's data but the visualisation needs
        /// to be spawned. 
        /// </summary>
        // public async Task<Layer> SpawnLayer(LayerData layerData, Vector3 position, Quaternion? rotation = null)
        // {   
        //     //TODO we need to remove the as ReferencedLayerData cast and make this work for all LayerData types
        //     if (layerData is not ReferencedLayerData)
        //     {
        //         throw new NotSupportedException("Only ReferencedLayerData visualization is supported currently.");
        //     }
        //
        //     Layer layer = new Layer(layerData);           
        //     LayerGameObject visualization = await spawner.Spawn(layerData as ReferencedLayerData, position, rotation ?? Quaternion.identity);
        //     layer.SetVisualization(visualization);
        //     visualization.SetData(layerData);
        //     return layer;
        // }

        /// <summary>
        /// Force a LayerData object to be visualized as a specific prefab.
        ///
        /// Warning: this code does not check if the given prefab is compatible with this LayerData, make sure you know what you are doing.
        /// </summary>
        public Layer VisualizeAs(LayerData layerData, string prefabIdentifier, UnityAction<LayerGameObject> callback = null, UnityAction<Exception> errorCallback = null)
        {
            // string previousId = layerData.PrefabIdentifier;
            layerData.PrefabIdentifier = prefabIdentifier;
            var layer = new Layer(layerData);
            Visualize(layer, spawner, callback, errorCallback);
            // if (previousId != prefabIdentifier) layer.LayerGameObject.OnConvert(previousId); //todo: this should not be done here but in the future VisualizationPropertyData (ticket 3/4)
            return layer;
        }

        /// <summary>
        /// Removes the layer from the current project and ensures the visualisation is removed as well.
        /// </summary>
        public void Remove(LayerData layerData)
        {
            layerData.DestroyLayer();  
            LayerRemoved.Invoke(layerData);
        }

        private Uri RetrieveUrlForLayer(LayerBuilder layerBuilder)
        {
            // We prefer the direct approach
            if (layerBuilder.Url != null)
            {
                return layerBuilder.Url;
            }

            // But we need a fallback for project-loaded layers
            var urlPropertyData = layerBuilder.Properties.Get<LayerURLPropertyData>();
            if (urlPropertyData == null)
            {
                throw new Exception("Cannot add layer with type 'url' without a URL");
            }

            return urlPropertyData.Data;
        }

        public void VisualizeData(LayerData layerData, UnityAction<LayerGameObject> callback = null, UnityAction<Exception> errorCallback = null)
        {
            Layer layer = new Layer(layerData);
            Visualize(layer, spawner, callback, errorCallback);
        }
        
        private static async void Visualize(Layer layer, ILayerSpawner spawner, UnityAction<LayerGameObject> callback = null, UnityAction<Exception> errorCallback = null) //todo: change callbacks for promises
        {
            try
            {
                LayerGameObject visualization = await spawner.Spawn(layer.LayerData);
                layer.SetVisualization(visualization);
                visualization.SetData(layer.LayerData);
                callback?.Invoke(visualization);
            }
            catch (Exception e)
            {
                errorCallback?.Invoke(e);
            }
        }
    }
}