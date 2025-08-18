using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.T3DPipeline;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.CityJSON
{
    public class CityJSONSpawner : MonoBehaviour, ILayerWithPropertyData
    {
        [SerializeField] private float cameraDistanceFromGeoReferencedObject = 150f;
        private CityJSONPropertyData propertyData = new();
        public LayerPropertyData PropertyData => propertyData;
        private GameObject importedObject;

        private void Awake()
        {
            gameObject.transform.position = ObjectPlacementUtility.GetSpawnPoint();
        }


        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var propertyData = properties.OfType<CityJSONPropertyData>().FirstOrDefault();
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

            var path = GetCityJsonPathFromPropertyData();
            StartCoroutine(LoadCityJson(path));
        }
        
        private IEnumerator LoadCityJson(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                Debug.LogError("Invalid file path: " + file);
                yield break;
            }

            // FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
            var json = File.ReadAllText(file);
            
            var root = new GameObject("CityJSONRoot");
            var cityjson = root.AddComponent<T3DPipeline.CityJSON>();
            cityjson.ParseCityJSON(json);
            foreach (var cityObject in root.GetComponentsInChildren<CityObject>())
            {
                cityObject.gameObject.AddComponent<CityObjectVisualizer>();
            }

            OnObjImported(root);
        }

        private void OnObjImported(GameObject returnedGameObject)
        {
            var holgo = GetComponent<HierarchicalObjectLayerGameObject>();

            var isGeoReferenced = false;
            if (returnedGameObject.transform.childCount > 0)
            {
                //GLB stores coordinates as 32 bit floats, and therefore cannot accurately be georeferenced.
                //However, we will still do a check to ensure at least the model will appear roughly where it should if it is still georeferenced despite this.
                var referencePosition = returnedGameObject.transform.GetChild(0).localPosition;
                if (IsValidRD(referencePosition.x, referencePosition.y, referencePosition.z, out var origin))
                {
                    PositionGeoReferencedCityJson(returnedGameObject, holgo, origin);
                }
            }
            
            if(!isGeoReferenced)
                PositionNonGeoReferencedCityJson(returnedGameObject, holgo);

            importedObject = returnedGameObject;
            foreach (var meshFilter in returnedGameObject.GetComponentsInChildren<MeshFilter>())
            {
                meshFilter.gameObject.AddComponent<MeshCollider>();
            }

            // Object is loaded / replaced - trigger the application of styling
            holgo.ApplyStyling();
        }
        
        //todo: this function should move to the Coordinates package
        private bool IsValidRD(double x, double y, double z, out Coordinate rdOrigin)
        {
            if (EPSG7415.IsValid(new Vector3RD(x, z, y)))
            {
                rdOrigin = new Coordinate(CoordinateSystem.RDNAP, x, z, 0); //don't offset the height
                return true;
            }

            if (EPSG7415.IsValid(new Vector3RD(x, y, z)))
            {
                rdOrigin = new Coordinate(CoordinateSystem.RDNAP, x, y, 0); //don't offset the height
                return true;
            }

            rdOrigin = new Coordinate();
            return false;
        }
        
        private void PositionNonGeoReferencedCityJson(GameObject returnedGameObject, HierarchicalObjectLayerGameObject holgo)
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
        
        private void PositionGeoReferencedCityJson(GameObject returnedGameObject, HierarchicalObjectLayerGameObject holgo, Coordinate origin)
        {
            if (!holgo.TransformIsSetFromProperty) //move the camera only if this is is a user imported object, not if this is a project import. We know this because a project import has its Transform property set.
            {
                var cameraMover = Camera.main.GetComponent<MoveCameraToCoordinate>();
                cameraMover.LookAtTarget(origin, cameraDistanceFromGeoReferencedObject); //move the camera to the georeferenced position, this also shifts the origin if needed.
            }
            
            holgo.WorldTransform.MoveToCoordinate(origin); //set this object to the georeferenced position, since this is the correct position.
            returnedGameObject.transform.SetParent(transform, false); // we set the parent and reset its localPosition, since the origin might have changed.
            returnedGameObject.transform.localPosition = Vector3.zero;
            foreach (Transform t in returnedGameObject.transform)
            {
                t.localPosition -= origin.ToUnity();
            }

            // imported object should stay where it is initially, and only then apply any user transformations if present.
            if (holgo.TransformIsSetFromProperty)
            {
                var transformPropterty = (TransformLayerPropertyData)((ILayerWithPropertyData)holgo).PropertyData;
                holgo.WorldTransform.MoveToCoordinate(transformPropterty.Position); //apply saved user changes to position.
            }
        }
        
        public void SetCityJSONPathInPropertyData(string fullPath)
        {
            var propertyData = PropertyData as CityJSONPropertyData;
            propertyData.CityJsonFile = AssetUriFactory.CreateProjectAssetUri(fullPath);
        }
        
        private string GetCityJsonPathFromPropertyData()
        {
            if (propertyData.CityJsonFile == null)
                return "";

            var localPath = AssetUriFactory.GetLocalPath(propertyData.CityJsonFile);
            return localPath;
        }
    }
}
