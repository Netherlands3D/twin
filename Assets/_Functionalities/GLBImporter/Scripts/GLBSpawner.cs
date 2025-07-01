using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLTFast;
using Netherlands3D.Tiles3D;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.GLBImporter
{
    public class GLBSpawner : MonoBehaviour, ILayerWithPropertyData
    {
        private GLBPropertyData propertyData = new();
        public LayerPropertyData PropertyData => propertyData;
        private GameObject importedObject;

        private void Awake()
        {
            gameObject.transform.position = ObjectPlacementUtility.GetSpawnPoint();
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var propertyData = properties.OfType<GLBPropertyData>().FirstOrDefault();
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

            var objPath = GetGlbPathFromPropertyData();
            ImportGlb(objPath);
        }

        private void ImportGlb(string glbPath)
        {
            StartCoroutine(LoadGlb(glbPath));
        }

        private IEnumerator LoadGlb(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                Debug.LogError("Invalid file path.");
                yield break;
            }

            var consoleLogger = new GLTFast.Logging.ConsoleLogger();
            var materialGenerator = new NL3DMaterialGenerator();
            GltfImport gltf = new GltfImport(null, null, materialGenerator, consoleLogger);
            
            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
            byte[] data = new byte[fileStream.Length];
            int totalRead = 0;
            fileStream.Read(data, 0, data.Length);            
            
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

            OnObjImported(root);
        }
        
        private void OnObjImported(GameObject returnedGameObject)
        {
            var holgo = GetComponent<HierarchicalObjectLayerGameObject>();

            PositionNonGeoReferencedGlb(returnedGameObject, holgo);

            importedObject = returnedGameObject;
            returnedGameObject.AddComponent<MeshCollider>();

            // Object is loaded / replaced - trigger the application of styling
            holgo.ApplyStyling();
        }

        private void PositionNonGeoReferencedGlb(GameObject returnedGameObject, HierarchicalObjectLayerGameObject holgo)
        {
            //if we have saved transform data, we will use that position, otherwise we will use this object's current position.
            if (holgo.TransformIsSetFromProperty)
            {
                //apply any transformation if present in the data
                var transformPropterty = (TransformLayerPropertyData)((ILayerWithPropertyData)holgo).PropertyData;
                transform.position = transformPropterty.Position.ToUnity();
                returnedGameObject.transform.SetParent(transform, false); // imported object should move to saved (parent's) position
            }
            else
            {
                //no transform property or georeference present, this object should just take on the parent's position
                returnedGameObject.transform.SetParent(transform, false); // imported object should move to saved (parent's) position
            }
        }

        public void SetGlbPathInPropertyData(string fullPath)
        {
            var propertyData = PropertyData as GLBPropertyData;
            propertyData.GlbFile = AssetUriFactory.CreateProjectAssetUri(fullPath);
        }

        private string GetGlbPathFromPropertyData()
        {
            if (propertyData.GlbFile == null)
                return "";

            var localPath = propertyData.GlbFile.LocalPath.TrimStart('/', '\\');
            var path = Path.Combine(Application.persistentDataPath, localPath);
            return path;
        }
    }
}