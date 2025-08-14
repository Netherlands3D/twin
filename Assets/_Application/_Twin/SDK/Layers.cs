using System;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.DataTypeAdapters;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D._Application._Twin.SDK
{
    public class Layers : MonoBehaviour
    {
        [SerializeField] private PrefabLibrary prefabLibrary;
        [SerializeField] private FileTypeAdapter fromFileImporter;
        [SerializeField] private DataTypeChain fromUrlImporter;
        private LayerSpawner spawner;

        private void Awake()
        {
            spawner = new LayerSpawner();
        }

        public async Task<LayerData> Add(Layer layer)
        {
            LayerGameObject layerGameObject = null;
            if (layer.Type == "url")
            {
                var url = RetrieveUrlForLayer(layer);
                fromUrlImporter.DetermineAdapter(url, layer.Credentials);
                
                // TODO: Capture created LayerData and return it
            }
            else if (layer.Type == "file")
            {
                var url = RetrieveUrlForLayer(layer);
                fromFileImporter.ProcessFile(url.ToString());

                // TODO: Capture created LayerData and return it
            }
            else
            {
                var layerGameObjectPrefab = prefabLibrary.GetPrefabById(layer.Type);

                // We use Async methods to support Addressables and Asset Bundles
                layerGameObject = await spawner.Spawn(layerGameObjectPrefab, layer.Position, layer.Rotation);
            }

            if (layerGameObject == null)
            {
                throw new Exception($"Could not find layer of type: {layer.Type}");
            }

            LayerData layerData = layerGameObject.LayerData;
            if (layerData == null) return null;

            if (!string.IsNullOrEmpty(layer.Name))
                layerData.Name = layer.Name;

            if (layer.Color.HasValue)
                layerData.Color = layer.Color.Value;

            if (layer.Parent != null)
                layerData.SetParent(layer.Parent);

            foreach (var property in layer.Properties)
            {
                layerData.AddProperty(property);
            }

            if (layer.DefaultSymbolizer != null)
            {
                layerData.DefaultStyle.AnyFeature.Symbolizer = layer.DefaultSymbolizer;
            }
            
            foreach (var style in layer.Styles)
            {
                layerData.AddStyle(style);
            }
            
            // We changed the properties - so we load them into the visualisations with this LayerGameObject
            layerGameObject.LoadPropertiesInVisualisations();
            
            return layerData;
        }

        private Uri RetrieveUrlForLayer(Layer layer)
        {
            var urlPropertyData = layer.Properties.Cast<LayerURLPropertyData>().SingleOrDefault(data => data != null);
            if (urlPropertyData == null)
            {
                throw new Exception("Cannot add layer with type 'auto' without a URL property");
            }

            return urlPropertyData.Data;
        }
    }
}