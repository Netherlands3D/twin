using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.OBJImporter
{
    [RequireComponent(typeof(HierarchicalObjectLayerGameObject))]
    public class OBJSpawner : MonoBehaviour, IVisualizationWithPropertyData, IImportedObject
    {
        [Header("Required input")]
        [SerializeField]
        private Material baseMaterial;

        [SerializeField] private Netherlands3D.ObjImporter.ObjImporter importerPrefab;

        [Header("Settings")][SerializeField] private bool createSubMeshes = false;
        [SerializeField] private float cameraDistanceFromGeoReferencedObject = 150f;

        // private OBJPropertyData propertyData = new();

        private Netherlands3D.ObjImporter.ObjImporter importer;
        private GameObject importedObject;

        // private string ObjFilePath => propertyData.ObjFile != null ? AssetUriFactory.GetLocalPath(propertyData.ObjFile) : string.Empty;
        // private string MtlFilePath => propertyData.MtlFile != null ? AssetUriFactory.GetLocalPath(propertyData.MtlFile) : string.Empty;

        // public bool HasMtl => MtlFilePath != string.Empty;
        
        private HierarchicalObjectLayerGameObject layerGameObject;
        private MoveCameraToCoordinate cameraMover;

        private OBJPropertyData propertyData;
        public UnityEvent<GameObject> ObjectVisualized { get; } = new();

        private void Awake()
        {
            cameraMover = Camera.main.GetComponent<MoveCameraToCoordinate>();
            gameObject.transform.position = ObjectPlacementUtility.GetSpawnPoint();
            layerGameObject = GetComponent<HierarchicalObjectLayerGameObject>();
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            propertyData = properties.Get<OBJPropertyData>();
            if (propertyData == null) return;

            propertyData.OnObjUriChanged.AddListener(OnPathChanged);
            propertyData.OnMtlUriChanged.AddListener(OnPathChanged);
            
            StartImport();
            // Property data is set here, and the parsing and loading of the actual data is done
            // in the start method, there a coroutine is started to load the data in a streaming fashion.
            // If we do that here, then this may conflict with the loading of the project file and it would
            // cause duplication when adding a layer manually instead of through the loading mechanism
            // this.propertyData = propertyData;
        }

        private void OnPathChanged(Uri uri)
        {
            StartImport();
        }

        private string GetFilePath(Uri propertyDataUri)
        {
            var filePath = string.Empty;
            if (propertyDataUri != null)
            {
                filePath = AssetUriFactory.GetLocalPath(propertyDataUri);
            }

            return filePath;
        }

        public void StartImport()
        {
            var objFilePath = GetFilePath(propertyData.ObjFile);
            var mtlFilePath = GetFilePath(propertyData.MtlFile);

            if (importedObject)
                Destroy(importedObject);

            DisposeImporter();

            importer = Instantiate(importerPrefab);

            ImportObj(objFilePath, mtlFilePath);
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
                importer.MtlImportSucceeded.AddListener(propertyData.MtlImportSuccess.Invoke);
            }

            importer.imgFilePath = "";

            importer.BaseMaterial = baseMaterial;
            importer.createSubMeshes = createSubMeshes;
            importer.StartImporting(OnObjImported);
        }

        private void OnObjImported(GameObject returnedGameObject)
        {
            PositionImportedGameObject(returnedGameObject);

            ParentImportedGameObject(returnedGameObject);

            importedObject = returnedGameObject;
            returnedGameObject.AddComponent<MeshCollider>();
            DisposeImporter();

            //Object is loaded / replaced - trigger the application of styling
            ObjectVisualized.Invoke(returnedGameObject);
        }

        private void PositionImportedGameObject(GameObject returnedGameObject)
        {
            if (IsGeoReferenced())
            {
                var transformPropertyData = layerGameObject.LayerData.GetProperty<TransformLayerPropertyData>();
                PositionGeoReferencedObj(returnedGameObject, transformPropertyData.Position);
                return;
            }

            // if we have saved transform data, we will use that position, otherwise we will use this object's
            // current position.
            if (!layerGameObject.LayerData.IsNew)
            {
                var transformPropertyData = layerGameObject.LayerData.GetProperty<TransformLayerPropertyData>();
                transform.position = transformPropertyData.Position.ToUnity();
            }
        }

        private bool IsGeoReferenced()
        {
            return !importer.createdGameobjectIsMoveable;
        }

        private void PositionGeoReferencedObj(GameObject returnedGameObject, Coordinate coordinate)
        {
            if (layerGameObject.LayerData.IsNew)
            {
                // georeferenced position as coordinate. todo: there is already precision lost in the importer, this should
                // be preserved while parsing, as there is nothing we can do now anymore.
                coordinate = new Coordinate(returnedGameObject.transform.position);

                // move the camera to the georeferenced position, this also shifts the origin if needed.
                cameraMover.LookAtTarget(coordinate, cameraDistanceFromGeoReferencedObject);
            }

            //set this object to the georeferenced position, since this is the correct position.
            layerGameObject.WorldTransform.MoveToCoordinate(coordinate);
        }

        private void ParentImportedGameObject(GameObject returnedGameObject)
        {
            // we set the parent and reset its localPosition, since the origin might have changed.
            returnedGameObject.transform.SetParent(transform, false);
            returnedGameObject.transform.localPosition = Vector3.zero;
        }

        private void DisposeImporter()
        {
            if (importer == null) return;

            importer.MtlImportSucceeded.RemoveListener(propertyData.MtlImportSuccess.Invoke);
            Destroy(importer.gameObject);
        }

        void OnDestroy()
        {
            propertyData.OnObjUriChanged.RemoveListener(OnPathChanged);
            propertyData.OnMtlUriChanged.RemoveListener(OnPathChanged);
        }
    }
}