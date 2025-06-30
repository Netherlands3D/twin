using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLTFast;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Cameras;
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
        [Header("Required input")] [SerializeField]
        private Material baseMaterial;

        [Header("Settings")] 
        [SerializeField] private float cameraDistanceFromGeoReferencedObject = 150f;

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
            // the obj-importer deletes the obj-file after importing.
            // because we want to keep the file, we let the importer read a copy of the file
            // the copying can be removed after the code for the importer is changed

            string copiedObjFilename = glbPath + ".temp";
            File.Copy(glbPath, copiedObjFilename);

            StartCoroutine(LoadGlb(copiedObjFilename));
            // importer.objFilePath = copiedObjFilename;
            //
            // importer.imgFilePath = "";
            //
            // importer.BaseMaterial = baseMaterial;
            // importer.createSubMeshes = createSubMeshes;
            // importer.StartImporting(OnObjImported);
        }

        private IEnumerator LoadGlb(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                Debug.LogError("Invalid file path.");
                yield break;
            }

            var gltf = new GltfImport();

            // Start loading the file
            var loadTask = gltf.Load(file);
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            if (!loadTask.Result)
            {
                Debug.LogError("Failed to load GLB file.");
                yield break;
            }

            var instantiateTask = gltf.InstantiateMainSceneAsync(transform);
            while (!instantiateTask.IsCompleted)
            {
                yield return null;
            }

            if (!instantiateTask.Result)
            {
                Debug.LogError("Failed to instantiate GLB.");
                yield break;
            }

            Debug.Log("GLB loaded and instantiated successfully.");
        }

        private void OnObjImported(GameObject returnedGameObject)
        {
            // bool isGeoReferenced = !importer.createdGameobjectIsMoveable;
            var holgo = GetComponent<HierarchicalObjectLayerGameObject>();

            // if (isGeoReferenced)
            //     PositionGeoReferencedObj(returnedGameObject, holgo);
            // else
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

        private void PositionGeoReferencedGlb(GameObject returnedGameObject, HierarchicalObjectLayerGameObject holgo)
        {
            var targetPosition = new Coordinate(returnedGameObject.transform.position); //georeferenced position as coordinate. todo: there is already precision lost in the importer, this should be preserved while parsing, as there is nothing we can do now anymore.

            if (!holgo.TransformIsSetFromProperty) //move the camera only if this is is a user imported object, not if this is a project import. We know this because a project import has its Transform property set.
            {
                var cameraMover = Camera.main.GetComponent<MoveCameraToCoordinate>();
                cameraMover.LookAtTarget(targetPosition, cameraDistanceFromGeoReferencedObject); //move the camera to the georeferenced position, this also shifts the origin if needed.
            }

            holgo.WorldTransform.MoveToCoordinate(targetPosition); //set this object to the georeferenced position, since this is the correct position.
            returnedGameObject.transform.SetParent(transform, false); // we set the parent and reset its localPosition, since the origin might have changed.
            returnedGameObject.transform.localPosition = Vector3.zero;

            // imported object should stay where it is initially, and only then apply any user transformations if present.
            if (holgo.TransformIsSetFromProperty)
            {
                var transformPropterty = (TransformLayerPropertyData)((ILayerWithPropertyData)holgo).PropertyData;
                holgo.WorldTransform.MoveToCoordinate(transformPropterty.Position); //apply saved user changes to position.
            }
        }

        // private void DisposeImporter()
        // {
        //     if (importer != null)
        //     {
        //         // importer.MtlImportSucceeded.RemoveListener(MtlImportSuccess.Invoke);
        //         Destroy(importer.gameObject);
        //     }
        // }

        public void SetGlbPathInPropertyData(string fullPath)
        {
            var propertyData = PropertyData as GLBPropertyData;
            propertyData.GlbFile = AssetUriFactory.CreateProjectAssetUri(fullPath);
        }

        // public void SetMtlPathInPropertyData(string fullPath)
        // {
        //     var propertyData = PropertyData as OBJPropertyData;
        //     propertyData.MtlFile = AssetUriFactory.CreateProjectAssetUri(fullPath);
        // }

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