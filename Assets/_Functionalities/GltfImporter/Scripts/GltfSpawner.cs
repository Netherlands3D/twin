using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLTFast;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Functionalities.GltfImporter
{
    public class GltfSpawner : MonoBehaviour, ILayerWithPropertyData
    {
        private GltfPropertyData propertyData = new();
        public LayerPropertyData PropertyData => propertyData;

        private void Awake()
        {
            gameObject.transform.position = ObjectPlacementUtility.GetSpawnPoint();
        }

        private async void Start()
        {
            var localPath = propertyData.Uri.LocalPath.TrimStart('/', '\\');
            var path = Path.Combine(Application.persistentDataPath, localPath);
            
            Debug.Log("Reading GLB/GLTF file");
            byte[] data = await File.ReadAllBytesAsync(path);
            var gltf = new GltfImport();
            Debug.Log("Loading GLB/GLTF binary data");
            bool success = await gltf.LoadGltfBinary(data, new Uri(path));
            if (success) {
                Debug.Log("Creating Scene object(s) for GLB/GLTF");
                await gltf.InstantiateMainSceneAsync(transform);
                Debug.Log("Created Scene object(s) for GLB/GLTF");
            }
            else
            {
                Debug.LogError("Failed to load GLB/GLTF binary data");
            }
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var propertyData = properties.OfType<GltfPropertyData>().FirstOrDefault();
            if (propertyData == null) return;

            // Property data is set here, and the parsing and loading of the actual data is done
            // in the start method, there a coroutine is started to load the data in a streaming fashion.
            // If we do that here, then this may conflict with the loading of the project file and it would
            // cause duplication when adding a layer manually instead of through the loading mechanism
            this.propertyData = propertyData;
        }
    }
}