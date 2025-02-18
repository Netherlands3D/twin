using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.OBJImporter
{
    public class OBJSpawner : MonoBehaviour, ILayerWithPropertyData
    {
        [Header("Required input")]
        [SerializeField] private Material baseMaterial;
        [SerializeField] private Netherlands3D.ObjImporter.ObjImporter importerPrefab;

        [Header("Settings")]
        [SerializeField] private bool createSubMeshes = false;
        [SerializeField] private float cameraDistanceFromGeoReferencedObject = 150f;

        private OBJPropertyData propertyData = new();
        public LayerPropertyData PropertyData => propertyData;

        private Netherlands3D.ObjImporter.ObjImporter importer;
        private GameObject importedObject;

        public bool HasMtl => GetMtlPathFromPropertyData() != string.Empty;
        public UnityEvent<bool> MtlImportSuccess = new();

        private void Awake()
        {
            gameObject.transform.position = ObjectPlacementUtility.GetSpawnPoint();
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var propertyData = properties.OfType<OBJPropertyData>().FirstOrDefault();
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

        public void StartImport()
        {
            if (importedObject)
                Destroy(importedObject);

            DisposeImporter();

            importer = Instantiate(importerPrefab);

            var objPath = GetObjPathFromPropertyData();
            var mtlPath = GetMtlPathFromPropertyData();

            ImportObj(objPath, mtlPath);
        }

        private void ImportObj(string objPath, string mtlPath = "")
        {
            // the obj-importer deletes the obj-file after importing.
            // because we want to keep the file, we let the importer read a copy of the file
            // the copying can be removed after the code for the importer is changed

            string copiedObjFilename = objPath + ".temp";
            File.Copy(objPath, copiedObjFilename);
            importer.objFilePath = copiedObjFilename;

            importer.mtlFilePath = "";
            if (mtlPath != string.Empty)
            {
                string copiedMtlFilename = mtlPath + ".temp";
                File.Copy(mtlPath, copiedMtlFilename);
                importer.mtlFilePath = copiedMtlFilename;
                importer.MtlImportSucceeded.AddListener(MtlImportSuccess.Invoke);
            }

            importer.imgFilePath = "";

            importer.BaseMaterial = baseMaterial;
            importer.createSubMeshes = createSubMeshes;
            importer.StartImporting(OnObjImported);
        }

        private void OnObjImported(GameObject returnedGameObject)
        {
            bool isGeoReferenced = !importer.createdGameobjectIsMoveable;
            bool hasTransformProperty = false;

            var holgo = GetComponent<HierarchicalObjectLayerGameObject>();
            if (holgo)
                hasTransformProperty = holgo.TransformIsSetFromProperty;

            importedObject = returnedGameObject;
            if (isGeoReferenced)
            {
                SetObjectPosition(returnedGameObject, hasTransformProperty);
                Debug.Log("Geo-referenced object importer, moving camera to this position: " + returnedGameObject.transform.position);
                var cameraMover = Camera.main.GetComponent<MoveCameraToCoordinate>();
                cameraMover.LookAtTarget(new Coordinate(returnedGameObject.transform.position), cameraDistanceFromGeoReferencedObject);
            }

            // In case the returned object is georeferenced, or this (parent) object has its transform set from a property, we will use either of those positionings, and need to retain the world position.
            returnedGameObject.transform.SetParent(this.transform, isGeoReferenced || hasTransformProperty);
            returnedGameObject.AddComponent<MeshCollider>();

            DisposeImporter();
        }

        private void SetObjectPosition(GameObject returnedGameObject, bool hasTransformProperty)
        {
            // if we already have a position from the transform properties, match the returned object's positioning to this saved position, otherwise set it to the returned object's positioning, since this is the georeferenced position.
            if (hasTransformProperty)
                returnedGameObject.transform.position = transform.position;
            else
                transform.position = returnedGameObject.transform.position;
        }

        private void DisposeImporter()
        {
            if (importer != null)
            {
                importer.MtlImportSucceeded.RemoveListener(MtlImportSuccess.Invoke);
                Destroy(importer.gameObject);
            }
        }

        public void SetObjPathInPropertyData(string fullPath)
        {
            var propertyData = PropertyData as OBJPropertyData;
            propertyData.ObjFile = AssetUriFactory.CreateProjectAssetUri(fullPath);
        }

        public void SetMtlPathInPropertyData(string fullPath)
        {
            var propertyData = PropertyData as OBJPropertyData;
            propertyData.MtlFile = AssetUriFactory.CreateProjectAssetUri(fullPath);
        }

        private string GetObjPathFromPropertyData()
        {
            if (propertyData.ObjFile == null)
                return "";

            var localPath = propertyData.ObjFile.LocalPath.TrimStart('/', '\\');
            var path = Path.Combine(Application.persistentDataPath, localPath);
            return path;
        }

        private string GetMtlPathFromPropertyData()
        {
            if (propertyData.MtlFile == null)
                return "";

            var localPath = propertyData.MtlFile.LocalPath.TrimStart('/', '\\');
            var path = Path.Combine(Application.persistentDataPath, localPath);
            return path;
        }
    }
}