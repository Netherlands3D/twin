using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [RequireComponent(typeof(LayerGameObject))]
    public class ObjSpawner : MonoBehaviour, ILayerWithPropertyData
    {
        [Header("Required input")] 
        [SerializeField] private Material baseMaterial;
        [SerializeField] private ObjImporter.ObjImporter importerPrefab;

        [Header("Settings")] 
        [SerializeField] private bool createSubMeshes = false;
        
        private ObjPropertyData propertyData = new();
        public LayerPropertyData PropertyData => propertyData;

        private ObjImporter.ObjImporter importer;

        private void Awake()
        {
            gameObject.transform.position = ObjectPlacementUtility.GetSpawnPoint();
        }

        private void Start()
        {
            StartImport();
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

        private void StartImport()
        {
            DisposeImporter();

            importer = Instantiate(importerPrefab);

            var localObjPath = propertyData.ObjFile.LocalPath.TrimStart('/', '\\');
            var objPath = Path.Combine(Application.persistentDataPath, localObjPath);

            var mtlPath = propertyData.MtlFile.LocalPath.TrimStart('/', '\\'); 
            
            ImportObj(objPath, mtlPath);
        }

        private void ImportObj(string objPath, string mtlPath)
        {
            // the obj-importer deletes the obj-file after importing.
            // because we want to keep the file, we let the importer read a copy of the file
            // the copying can be removed after the code for the importer is changed
            string copiedFilename = objPath + ".temp";
            File.Copy(objPath, copiedFilename);

            importer.objFilePath = copiedFilename;
            importer.mtlFilePath = mtlPath;
            importer.imgFilePath = "";

            importer.BaseMaterial = baseMaterial;
            importer.createSubMeshes = createSubMeshes;
            importer.StartImporting(OnObjImported);
        }

        private void ImportMtl(string path)
        {
            propertyData.MtlFile = AssetUriFactory.CreateProjectAssetUri(path);

            importer.mtlFilePaht = path;
            importer.ImportMtl();
            // propertyData.ObjFile = AssetUriFactory.CreateProjectAssetUri(fullPath);
        }

        private void OnObjImported(GameObject returnedGameObject)
        {
            // By explicitly stating the worldPositionStays to false, we ensure Obj is spawned and it will retain the
            // position and scale in this parent object
            returnedGameObject.transform.SetParent(this.transform, false);
            returnedGameObject.AddComponent<MeshCollider>();

            DisposeImporter();
        }

        private void DisposeImporter()
        {
            if (importer != null) Destroy(importer.gameObject);
        }
    }
}