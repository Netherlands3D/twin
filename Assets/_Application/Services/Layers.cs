using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
        public Layer Add(LayerPresetArgs args)
        {
            return Add(LayerBuilder.Create(args));
        }

        /// <summary>
        /// Adds a new layer to the current project using the given builder.
        /// </summary>
        public Layer Add(ILayerBuilder builder)
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
            var layer = new Layer(layerData);
            Visualize(layer, spawner);
            LayerAdded.Invoke(layer);
            return layer;
        }

        private Layer AddFolderLayer(LayerBuilder layerBuilder)
        {
            var folderLayer = layerBuilder.Build();
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
            //todo: the fileTypeAdapter should be refactored to return the resulting objects
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
            //todo: the DataTypeChain should be refactored to return the resulting objects
            return null;
        }

        /// <summary>
        /// Force a LayerData object to be visualized as a specific prefab.
        ///
        /// Warning: this code does not check if the given prefab is compatible with this LayerData, make sure you know what you are doing.
        /// </summary>
        public async Task<Layer> VisualizeAs(LayerData layerData, string prefabIdentifier)
        {
            layerData.PrefabIdentifier = prefabIdentifier;
            return await VisualizeData(layerData);
        }

        /// <summary>
        /// Removes the layer from the current project and ensures the visualisation is removed as well.
        /// </summary>
        public void Remove(LayerData layerData)
        {
            layerData.Dispose();
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

            return urlPropertyData.Url;
        }

        public async Task<Layer> VisualizeData(LayerData layerData)
        {
            Layer layer = new Layer(layerData);
            await Visualize(layer, spawner);
            return layer;
        }

        private static async Task<LayerGameObject> Visualize(Layer layer, ILayerSpawner spawner)
        {
            LayerGameObject visualization = await spawner.Spawn(layer.LayerData);
            layer.SetVisualization(visualization);
            visualization.SetData(layer.LayerData);
            return visualization;
        }
    }
}