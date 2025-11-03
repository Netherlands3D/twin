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
        private LayerSpawner spawner;
        
        public UnityEvent<Layer> layerAdded = new();
        public UnityEvent<Layer> layerRemoved = new();

        private void Awake()
        {
            spawner = new LayerSpawner(prefabLibrary);
        }

        /// <summary>
        /// Adds a new layer to the current project using the given preset.
        /// </summary>
        public async Task<Layer> Add(LayerPresetArgs args)
        {
            return await Add(LayerBuilder.Create(args));
        }

        /// <summary>
        /// Adds a new layer to the current project using the given builder.
        /// </summary>
        [ItemCanBeNull]
        public async Task<Layer> Add(ILayerBuilder builder)
        {
            if (builder is not LayerBuilder layerBuilder)
            {
                throw new NotSupportedException("Unsupported layer builder type: " + builder.GetType().Name);
            }
            
            switch (layerBuilder.Type)
            {
                case "url": return await ImportFromUrl(layerBuilder);
                case "file": return ImportFromFile(layerBuilder);
            }

            var layerData = builder.Build();            
            var layerGameObject = await SpawnVisualization(layerData);
            if (layerGameObject == null)
            {
                throw new Exception($"Could not find layer of type: {layerBuilder.Type}");
            }
            Layer layer = new Layer(layerData);
            layer.SetVisualization(layerGameObject);
            layerAdded.Invoke(layer);

            return layer;
        }

        private Layer ImportFromFile(LayerBuilder layerBuilder)
        {
            var url = RetrieveUrlForLayer(layerBuilder);
            fromFileImporter.ProcessFile(url.ToString());

            // Return null to indicate that adding this flow does not directly result in a Layer, it may do so
            // indirectly (DataTypeAdapters call this Layer service again).
            return null;
        }

        private async Task<Layer> ImportFromUrl(LayerBuilder layerBuilder)
        {
            var url = RetrieveUrlForLayer(layerBuilder);
            if (url.Scheme == "prefab-library")
            {
                // This is a stored prefab identifier from the prefab library, so let's try it again but
                // then as a direct build
                return await Add(layerBuilder.OfType(url.AbsolutePath.Trim('/')));
            }
                    
            fromUrlImporter.DetermineAdapter(url, layerBuilder.Credentials);
                
            // Return null to indicate that adding this flow does not directly result in a Layer, it may do so
            // indirectly (DataTypeAdapters call this Layer service again).
            return null;
        }

        /// <summary>
        /// Visualizes an existing layer's data by spawning a placeholder and after that the actual visualisation
        /// (LayerGameObject).
        ///
        /// Usually used when loading a project file as this will restore the layer's data but the visualisation needs
        /// to be spawned. 
        /// </summary>
        public async Task<Layer> SpawnLayer(LayerData layerData)
        {
            //TODO we need to remove the as ReferencedLayerData cast and make this work for all LayerData types
            if (layerData is not ReferencedLayerData)
            {
                throw new NotSupportedException("Only ReferencedLayerData visualization is supported currently.");
            }

            Layer layer = new Layer(layerData);
            LayerGameObject placeHolder = SpawnPlaceholder(layerData as ReferencedLayerData);
            layer.SetVisualization(placeHolder);
            LayerGameObject visualization = await spawner.Spawn(layerData as ReferencedLayerData);
            layer.SetVisualization(visualization);
            return layer;
        }

        /// <summary>
        /// Visualizes an existing layer's data by spawning a placeholder and after that the actual visualisation
        /// (LayerGameObject).
        ///
        /// Usually used when loading a project file as this will restore the layer's data but the visualisation needs
        /// to be spawned. 
        /// </summary>
        public async Task<Layer> SpawnLayer(LayerData layerData, Vector3 position, Quaternion? rotation = null)
        {   
            //TODO we need to remove the as ReferencedLayerData cast and make this work for all LayerData types
            if (layerData is not ReferencedLayerData)
            {
                throw new NotSupportedException("Only ReferencedLayerData visualization is supported currently.");
            }

            Layer layer = new Layer(layerData);
            LayerGameObject placeHolder = SpawnPlaceholder(layerData as ReferencedLayerData);

            layer.SetVisualization(placeHolder);
            LayerGameObject visualization = await spawner.Spawn(layerData as ReferencedLayerData, position, rotation ?? Quaternion.identity);
            layer.SetVisualization(visualization);
            return layer;
        }

        /// <summary>
        /// Force a LayerData object to be visualized as a specific prefab.
        ///
        /// Warning: this code does not check if the given prefab is compatible with this LayerData, make sure you know what you are doing.
        /// </summary>
        public async Task<Layer> VisualizeAs(LayerData layerData, string prefabIdentifier)
        {        
            //TODO we need to remove the as ReferencedLayerData cast and make this work for all LayerData types
            if (layerData is not ReferencedLayerData referencedLayerData)
            {
                throw new NotSupportedException("Only ReferencedLayerData visualization is supported currently.");
            }
            string previousId = referencedLayerData.PrefabIdentifier;

            Layer layer = new Layer(layerData);          
            LayerGameObject visualization = await spawner.Spawn(layerData as ReferencedLayerData, prefabIdentifier);
            layer.SetVisualization(visualization);
            if (previousId != prefabIdentifier) visualization.OnConvert(previousId);
            return layer;
        }

        /// <summary>
        /// Removes the layer from the current project and ensures the visualisation is removed as well.
        /// </summary>
        public void Remove(Layer layer)
        {
            layer.LayerData.DestroyLayer();            
            layerRemoved.Invoke(layer);
        }

        private async Task<LayerGameObject> SpawnVisualization(LayerData layerData)
        {
            if (layerData is not ReferencedLayerData referencedLayerData)
            {
                throw new Exception("Cannot add layer");
            }
            return await spawner.Spawn(
                referencedLayerData
            );
        }

        private LayerGameObject SpawnPlaceholder(ReferencedLayerData layerData)
        {
            return prefabLibrary.placeholderPrefab.Instantiate(layerData);
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
    }
}