using System;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.DataTypeAdapters;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;
using Object = UnityEngine.Object;

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
            if (builder is not BaseLayerBuilder layerBuilder)
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

        public async Task<ReferencedLayerData> Add(ReferencedLayerData layerData)
        {
            layerData.SetReference(SpawnPlaceholder(layerData), true);

            await spawner.Spawn(layerData);
            
            return layerData;
        }

        public async Task<ReferencedLayerData> Add(ReferencedLayerData layerData, Vector3 position, Quaternion? rotation = null)
        {
            layerData.SetReference(SpawnPlaceholder(layerData), true);
            
            await spawner.Spawn(layerData, position, rotation ?? Quaternion.identity);
            
            return layerData;
        }

        private async Task<LayerGameObject> SpawnLayer(BaseLayerBuilder layerBuilder)
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

        private Uri RetrieveUrlForLayer(BaseLayerBuilder layerBuilder)
        {
            // TODO: Can't we do this another way? This feels leaky
            var urlPropertyData = layerBuilder.Properties.Get<LayerURLPropertyData>();
            if (urlPropertyData == null)
            {
                throw new Exception("Cannot add layer with type 'auto' without a URL property");
            }

            return urlPropertyData.Data;
        }
    }
}