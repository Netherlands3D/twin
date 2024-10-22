using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
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

            var localPath = propertyData.ObjFile.LocalPath.TrimStart('/', '\\');
            var path = Path.Combine(Application.persistentDataPath, localPath);
            
            ImportObj(path);
        }

        private void ImportObj(string path)
        {
            // the obj-importer deletes the obj-file after importing.
            // because we want to keep the file, we let the importer read a copy of the file
            // the copying can be removed after the code for the importer is changed
            string copiedFilename = path + ".temp";
            File.Copy(path, copiedFilename);

            importer.objFilePath = copiedFilename;
            importer.mtlFilePath = "";
            importer.imgFilePath = "";

            importer.BaseMaterial = baseMaterial;
            importer.createSubMeshes = createSubMeshes;
            importer.StartImporting(OnObjImported);
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