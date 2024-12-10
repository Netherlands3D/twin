using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers
{
    public class ObjSpawner : MonoBehaviour, ILayerWithPropertyData
    {
        [Header("Required input")] 
        [SerializeField] private Material baseMaterial;
        [SerializeField] private ObjImporter.ObjImporter importerPrefab;

        [Header("Settings")] 
        [SerializeField] private bool createSubMeshes = false;
        [SerializeField] private float cameraDistanceFromGeoReferencedObject = 150f;

        private ObjPropertyData propertyData = new();
        public LayerPropertyData PropertyData => propertyData;

        private ObjImporter.ObjImporter importer;
        private GameObject importedObject;

        public bool HasMtl => GetMtlPathFromPropertyData() != string.Empty;
        public UnityEvent<bool> MtlImportSuccess = new();

        private void Awake()
        {
            gameObject.transform.position = ObjectPlacementUtility.GetSpawnPoint();
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var propertyData = properties.OfType<ObjPropertyData>().FirstOrDefault();
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
            // By explicitly stating the worldPositionStays to false, we ensure Obj is spawned and it will retain the
            // position and scale in this parent object
            bool isGeoReferenced = !importer.createdGameobjectIsMoveable;
            importedObject = returnedGameObject;
            if (isGeoReferenced)
            {
                transform.position = returnedGameObject.transform.position;
                Debug.Log("Geo-referenced object importer, moving camera to this position: " + returnedGameObject.transform.position);
                var mainCam = Camera.main;
                mainCam.transform.position = returnedGameObject.transform.position + (-cameraDistanceFromGeoReferencedObject * mainCam.transform.forward);
            }

            returnedGameObject.transform.SetParent(this.transform, isGeoReferenced);
            returnedGameObject.AddComponent<MeshCollider>();

            DisposeImporter();
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
            var propertyData = PropertyData as ObjPropertyData;
            propertyData.ObjFile = AssetUriFactory.CreateProjectAssetUri(fullPath);
        }

        public void SetMtlPathInPropertyData(string fullPath)
        {
            var propertyData = PropertyData as ObjPropertyData;
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