using System;
using System.Threading.Tasks;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.DataTypeAdapters;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerPresets;
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

        public async Task<ReferencedLayerData> Add(LayerPresetArgs args)
        {
            return await Add(LayerBuilder.Create(args));
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
                    if (url.Scheme == "prefab-library")
                    {
                        // This is a stored prefab identifier from the prefab library, so let's try it again but
                        // then as a direct build
                        return await Add(layerBuilder.OfType(url.AbsolutePath.Trim('/')));
                    }
                    
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

        public async Task<LayerGameObject> Convert(ReferencedLayerData layerData, string prefabId)
        {
            var layerGameObject = await spawner.Spawn(prefabId);
            string previousId = layerData.Reference.PrefabIdentifier;
            layerData.SetReference(layerGameObject, false);
            layerGameObject.OnConvert(previousId);
            return layerGameObject;
        }

        public Task Remove(ReferencedLayerData layerData)
        {
            layerData.DestroyLayer();
            
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