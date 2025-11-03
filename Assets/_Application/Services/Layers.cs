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
        
        public UnityEvent<ReferencedLayerData> layerAdded = new();
        public UnityEvent<ReferencedLayerData> layerRemoved = new();

        private void Awake()
        {
            spawner = new LayerSpawner(prefabLibrary);
        }

        /// <summary>
        /// Adds a new layer to the current project using the given preset.
        /// </summary>
        public async Task<ReferencedLayerData> Add(LayerPresetArgs args)
        {
            return await Add(LayerBuilder.Create(args));
        }

        /// <summary>
        /// Adds a new layer to the current project using the given builder.
        /// </summary>
        [ItemCanBeNull]
        public async Task<ReferencedLayerData> Add(ILayerBuilder builder)
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
            
            var layerGameObject = await SpawnLayer(layerBuilder);
            if (layerGameObject == null)
            {
                throw new Exception($"Could not find layer of type: {layerBuilder.Type}");
            }

            var layerData = layerGameObject.LayerData;
            layerAdded.Invoke(layerData);

            return layerData;
        }

        private ReferencedLayerData ImportFromFile(LayerBuilder layerBuilder)
        {
            var url = RetrieveUrlForLayer(layerBuilder);
            fromFileImporter.ProcessFile(url.ToString());

            // Return null to indicate that adding this flow does not directly result in a Layer, it may do so
            // indirectly (DataTypeAdapters call this Layer service again).
            return null;
        }

        private async Task<ReferencedLayerData> ImportFromUrl(LayerBuilder layerBuilder)
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
        public async Task<ReferencedLayerData> Visualize(ReferencedLayerData layerData)
        {
            layerData.SetReference(SpawnPlaceholder(layerData), true);

            await spawner.Spawn(layerData);
            
            return layerData;
        }

        /// <summary>
        /// Visualizes an existing layer's data by spawning a placeholder and after that the actual visualisation
        /// (LayerGameObject).
        ///
        /// Usually used when loading a project file as this will restore the layer's data but the visualisation needs
        /// to be spawned. 
        /// </summary>
        public async Task<ReferencedLayerData> Visualize(ReferencedLayerData layerData, Vector3 position, Quaternion? rotation = null)
        {
            layerData.SetReference(SpawnPlaceholder(layerData), true);
            
            await spawner.Spawn(layerData, position, rotation ?? Quaternion.identity);
            
            return layerData;
        }

        /// <summary>
        /// Removes the layer from the current project and ensures the visualisation is removed as well.
        /// </summary>
        public Task Remove(ReferencedLayerData layerData)
        {
            layerData.DestroyLayer();
            
            layerRemoved.Invoke(layerData);
            return Task.CompletedTask;
        }

        private async Task<LayerGameObject> SpawnLayer(LayerBuilder layerBuilder)
        {
            var layerGameObject = SpawnPlaceholder(null);
            var layerData = layerBuilder.Build(layerGameObject);
            if (layerData is not ReferencedLayerData referencedLayerData)
            {
                throw new Exception("Cannot add layer");
            }

            if (!layerBuilder.Position.HasValue)
            {
                return await spawner.Spawn(referencedLayerData);
            }

            return await spawner.Spawn(
                referencedLayerData,
                layerBuilder.Position.Value,
                layerBuilder.Rotation ?? Quaternion.identity
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