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
            //List<LayerPropertyData> propertyList = new List<LayerPropertyData>();
            //Layers.Properties.OBJPropertyData propertyData = new OBJPropertyData();
            
            //propertyList.Add(propertyData);

           // LayerData = new ReferencedLayerData(localFile.LocalFilePath, "34882a73ff6122243a0e3e9811473e20", propertyList);

            
            
            ConnectToImporter();

            importer.objFilePath = Path.Combine(Application.persistentDataPath, propertyData.Data.LocalPath.TrimStart('/', '\\')); ;
            importer.mtlFilePath = "";
            importer.imgFilePath = "";

            importer.BaseMaterial = baseMaterial;
            importer.createSubMeshes = createSubMeshes;
            importer.StartImporting(OnOBJImported);
        }

        private void OnOBJImported(GameObject returnedGameObject)
        {
            //objfilename = string.Empty;
            //mtlfilename = string.Empty;
            //imgfilename = string.Empty;
            Debug.Log("finished obj-import");
            returnedGameObject.transform.parent = this.transform;

            if (importer != null) Destroy(importer.gameObject);
            //AddLayerScriptToObj(returnedGameObject, null);
        }

        private void ConnectToImporter()
        {
            if (importer != null) Destroy(importer.gameObject);

            importer = new GameObject().AddComponent<ObjImporter.ObjImporter>();

            Debug.Log("Connected to new ObjImporter");
        }

        //private LayerGameObject AddLayerScriptToObj(GameObject parsedObj, ReferencedLayerData existingLayer)
        //{
        //    var spawnPoint = ObjectPlacementUtility.GetSpawnPoint();

        //    parsedObj.transform.position = spawnPoint;

        //    //var instantiator = parsedObj.AddComponent<HierarchicalObjectPropertySectionInstantiator>();
        //    //instantiator.PropertySectionPrefab = defaultPropertySection;

        //    parsedObj.AddComponent<MeshCollider>();
        //    parsedObj.AddComponent<ToggleScatterPropertySectionInstantiator>();
        //    var layerGameObject = parsedObj.AddComponent<HierarchicalObjectLayerGameObject>();
        //    layerGameObject.LayerData = existingLayer;
        //    parsedObj.AddComponent<WorldTransform>();

        //    CreatedMoveableGameObject.Invoke(parsedObj);
        //    return layerGameObject;
        //}
    }
}
