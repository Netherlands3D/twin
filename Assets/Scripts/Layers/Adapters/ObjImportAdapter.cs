using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/OBJImportAdapter", fileName = "OBJImportAdapter", order = 0)]
    public class ObjImportAdapter : ScriptableObject
    {
        [Header("Required input")]
        [SerializeField] private Material baseMaterial;

        [Header("Settings")]
        [SerializeField] private bool createSubMeshes = false;
        [Tooltip("The default properties section to show when opening the property panel of a layer")]
        [SerializeField] private AbstractHierarchicalObjectPropertySection defaultPropertySection;
        
        [Header("Result")] public UnityEvent<GameObject> CreatedMoveableGameObject = new();

        private string objFileName = "";
        private string objfilename
        {
            get { return objFileName; }
            set
            {
                objFileName = value;

                Debug.Log("received objFile: " + objFileName);

            }
        }

        private string mtlFileName = "";
        private string mtlfilename
        {
            get => mtlFileName;
            set => mtlFileName = value;
        }

        private string imgFileName = "";
        private string imgfilename
        {
            get => imgFileName;
            set => imgFileName = value;
        }

        private ObjImporter.ObjImporter importer;

        public void ParseFiles(string value)
        {
            Debug.Log("receiveid files: " + value);

            if (value == "") //empty string received, so no selection was made
            {
                return;
            }

            string[] filenames = value.Split(',');

            foreach (var file in filenames)
            {
                string fileextention = System.IO.Path.GetExtension(file).ToLower();
                switch (fileextention)
                {
                    case ".obj":
                        objfilename = System.IO.Path.Combine(Application.persistentDataPath, file);
                        break;

                    case ".mtl":
                        mtlfilename = System.IO.Path.Combine(Application.persistentDataPath, file);
                        break;

                    case ".jpg":
                    case ".png":
                    case ".jpeg":
                        imgfilename = System.IO.Path.Combine(Application.persistentDataPath, file);
                        break;
                }
            }

            if (objfilename != "")
            {
                OnStartImporting();
            }
        }

        private void OnStartImporting()
        {
            ConnectToImporter();

            importer.objFilePath = objfilename;
            importer.mtlFilePath = mtlfilename;
            importer.imgFilePath = imgfilename;

            importer.BaseMaterial = baseMaterial;
            importer.createSubMeshes = createSubMeshes;
            importer.StartImporting(OnOBJImported);
        }

        private void OnOBJImported(GameObject returnedGameObject)
        {
            objfilename = string.Empty;
            mtlfilename = string.Empty;
            imgfilename = string.Empty;

            if (importer != null) Destroy(importer.gameObject);
            AddLayerScriptToObj(returnedGameObject);
        }

        private void ConnectToImporter()
        {
            if (importer != null) Destroy(importer.gameObject);

            importer = new GameObject().AddComponent<ObjImporter.ObjImporter>();

            Debug.Log("Connected to new ObjImporter");
        }

        private void AddLayerScriptToObj(GameObject parsedObj)
        {
            var spawnPoint = ObjectPlacementUtility.GetSpawnPoint();

            parsedObj.transform.position = spawnPoint;
            
            var instantiator = parsedObj.AddComponent<HierarchicalObjectPropertySectionInstantiator>();
            instantiator.PropertySectionPrefab = defaultPropertySection;
            
            parsedObj.AddComponent<HierarchicalObjectLayer>();
            parsedObj.AddComponent<MeshCollider>();
            CreatedMoveableGameObject.Invoke(parsedObj);
        }
    }
}
