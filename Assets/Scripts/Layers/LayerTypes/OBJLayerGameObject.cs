using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public class OBJLayerGameObject : LayerGameObject, ILayerWithPropertyData
    {
        [Header("Required input")]
        [SerializeField] private Material baseMaterial;

        [Header("Settings")]
        [SerializeField] private bool createSubMeshes = false;

        private OBJPropertyData propertyData = new();
        public LayerPropertyData PropertyData=>propertyData;

        private ObjImporter.ObjImporter importer;
        

        protected override void Start()
        {
            base.Start();
            StartImport();
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

        private void StartImport()
        {

            
            ConnectToImporter();

            /// the obj-importer deletes the obj-file after importing.
            /// because we want to keep the file, we let the importer read a copy of the file
            /// the copying can be removed after the code for the importer is changed
            string originalFilename = Path.Combine(Application.persistentDataPath, propertyData.Data.LocalPath.TrimStart('/', '\\'));
            string copiedFilename = Path.Combine(Application.persistentDataPath, propertyData.Data.LocalPath.TrimStart('/', '\\'))+"_temp";
            File.Copy(originalFilename, copiedFilename);

            importer.objFilePath = copiedFilename ;
            importer.mtlFilePath = "";
            importer.imgFilePath = "";

            

            importer.BaseMaterial = baseMaterial;
            importer.createSubMeshes = createSubMeshes;
            importer.StartImporting(OnOBJImported);
        }

        private void OnOBJImported(GameObject returnedGameObject)
        {

            Debug.Log("finished obj-import");
            returnedGameObject.transform.parent = this.transform;

            if (importer != null) Destroy(importer.gameObject);
           
        }

        private void ConnectToImporter()
        {
            if (importer != null) Destroy(importer.gameObject);

            importer = new GameObject().AddComponent<ObjImporter.ObjImporter>();

            Debug.Log("Connected to new ObjImporter");
        }

       
    }
}
