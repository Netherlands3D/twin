using System;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.DataTypeAdapters;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.Services
{
    public class Layers : MonoBehaviour, ILayersServiceFacade
    {
        [SerializeField] private PrefabLibrary prefabLibrary;
        [SerializeField] private FileTypeAdapter fromFileImporter;
        [SerializeField] private DataTypeChain fromUrlImporter;
        private LayerSpawner spawner;

        private void Awake()
        {
            spawner = new LayerSpawner(prefabLibrary);
        }

        public async Task<ReferencedLayerData> Add(ILayerBuilder builder)
        {
            if (builder is not LayerBuilder layerBuilder)
            {
                throw new NotSupportedException("Unsupported layer builder type: " + builder.GetType().Name);
            }
            
            LayerGameObject layerGameObject = null;
            switch (layerBuilder.Type)
            {
                case "url":
                {
                    var url = RetrieveUrlForLayer(layerBuilder);
                    fromUrlImporter.DetermineAdapter(url, layerBuilder.Credentials);
                
                    // TODO: Capture created LayerData and return it
                    return null;
                }
                case "file":
                {
                    var url = RetrieveUrlForLayer(layerBuilder);
                    fromFileImporter.ProcessFile(url.ToString());

                    // TODO: Capture created LayerData and return it
                    return null;
                }
                default:
                    layerGameObject = await SpawnLayer(layerBuilder);
                    break;
            }

            if (layerGameObject == null)
            {
                throw new Exception($"Could not find layer of type: {layerBuilder.Type}");
            }

            return layerGameObject.LayerData;
        }

        private async Task<LayerGameObject> SpawnLayer(LayerBuilder layerBuilder)
        {
            var layerData = layerBuilder.Build(prefabLibrary.fallbackPrefab);

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

        private Uri RetrieveUrlForLayer(LayerBuilder layerBuilder)
        {
            var urlPropertyData = layerBuilder.Properties.Cast<LayerURLPropertyData>().SingleOrDefault(data => data != null);
            if (urlPropertyData == null)
            {
                throw new Exception("Cannot add layer with type 'auto' without a URL property");
            }

            return urlPropertyData.Data;
        }
    }
}