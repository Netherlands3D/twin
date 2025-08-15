using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Tiles3D;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.CityJSON
{
    public class CityJSONSpawner : MonoBehaviour, ILayerWithPropertyData
    {
        private CityJSONPropertyData propertyData = new();
        public LayerPropertyData PropertyData { get; }
        private GameObject importedObject;

        private void Awake()
        {
            gameObject.transform.position = ObjectPlacementUtility.GetSpawnPoint();
        }


        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var propertyData = properties.OfType<CityJSONPropertyData>().FirstOrDefault();
            if (propertyData == null) return;

            // Property data is set here, and the parsing and loading of the actual data is done
            // in the start method, there a coroutine is started to load the data in a streaming fashion.
            // If we do that here, then this may conflict with the loading of the project file and it would
            // cause duplication when adding a layer manually instead of through the loading mechanism
            this.propertyData = propertyData;
        }

        private void Start()
        {
            StartImport(); //called after loading properties or after setting the file path through the import adapter
        }

        private void StartImport()
        {
            if (importedObject)
                Destroy(importedObject);

            var path = GetCityJsonPathFromPropertyData();
            StartCoroutine(LoadCityJson(path));
        }
        
        private IEnumerator LoadCityJson(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                Debug.LogError("Invalid file path.");
                yield break;
            }
            
            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
            byte[] data = new byte[fileStream.Length];
            int totalRead = 0;
            fileStream.Read(data, 0, data.Length);
            fileStream.Close();

            var loadTask = gltf.Load(data);
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            if (!loadTask.Result)
            {
                Debug.LogError("Failed to load GLB file.");
                yield break;
            }

            var root = new GameObject("GLBRoot");
            var instantiateTask = gltf.InstantiateMainSceneAsync(root.transform);
            while (!instantiateTask.IsCompleted)
            {
                yield return null;
            }

            if (!instantiateTask.Result)
            {
                Debug.LogError("Failed to instantiate GLB.");
                yield break;
            }
            root.transform.Rotate(Vector3.up, 180); // compensate for 180 degree rotation
            OnObjImported(root);
        }

        private void OnObjImported(GameObject returnedGameObject)
        {
            var holgo = GetComponent<HierarchicalObjectLayerGameObject>();

            var isGeoReferenced = false;
            if (returnedGameObject.transform.childCount > 0)
            {
                //GLB stores coordinates as 32 bit floats, and therefore cannot accurately be georeferenced.
                //However, we will still do a check to ensure at least the model will appear roughly where it should if it is still georeferenced despite this.
                var referencePosition = returnedGameObject.transform.GetChild(0).localPosition;
                if (IsValidRD(referencePosition.x, referencePosition.y, referencePosition.z, out var origin))
                {
                    PositionGeoReferencedGlb(returnedGameObject, holgo, origin);
                }
            }
            
            if(!isGeoReferenced)
                PositionNonGeoReferencedGlb(returnedGameObject, holgo);

            importedObject = returnedGameObject;
            foreach (var meshFilter in returnedGameObject.GetComponentsInChildren<MeshFilter>())
            {
                meshFilter.gameObject.AddComponent<MeshCollider>();
            }

            // Object is loaded / replaced - trigger the application of styling
            holgo.ApplyStyling();
        }
        
        private void PositionNonGeoReferencedGlb(GameObject returnedGameObject, HierarchicalObjectLayerGameObject holgo)
        {
            throw new NotImplementedException();
        }        
        
        private void PositionGeoReferencedGlb(GameObject returnedGameObject, HierarchicalObjectLayerGameObject holgo, Coordinate origin)
        {
            throw new NotImplementedException();
        }
        
        public void SetCityJSONPathInPropertyData(string fullPath)
        {
            var propertyData = PropertyData as CityJSONPropertyData;
            propertyData.CityJsonFile = AssetUriFactory.CreateProjectAssetUri(fullPath);
        }
        
        private string GetCityJsonPathFromPropertyData()
        {
            if (propertyData.CityJsonFile == null)
                return "";

            var localPath = AssetUriFactory.GetLocalPath(propertyData.CityJsonFile);
            return localPath;
        }
    }
}
