using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
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
            var holgo = GetComponent<HierarchicalObjectLayerGameObject>();
            
            if (isGeoReferenced)
                PositionGeoReferencedObj(returnedGameObject, holgo);
            else
                PositionNonGeoReferencedObj(returnedGameObject, holgo);

            importedObject = returnedGameObject;
            returnedGameObject.AddComponent<MeshCollider>();
            DisposeImporter();
            
            // Object is loaded / replaced - trigger the application of styling
            holgo.ApplyStyling();
        }

        private void PositionNonGeoReferencedObj(GameObject returnedGameObject, HierarchicalObjectLayerGameObject holgo)
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

        private void PositionGeoReferencedObj(GameObject returnedGameObject, HierarchicalObjectLayerGameObject holgo)
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