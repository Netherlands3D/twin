using Netherlands3D.Coordinates;
using Netherlands3D.Tiles3D;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Utility;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Functionalities.CityJSON
{
    public class CityJSONSpawner : MonoBehaviour, ILayerWithPropertyData
    {
        [SerializeField] private float cameraDistanceFromGeoReferencedObject = 150f;
        [SerializeField] private bool addMeshCollidersToCityObjects = true;
        private CityJSONPropertyData propertyData = new();
        public LayerPropertyData PropertyData => propertyData;

        private void Awake()
        {
            gameObject.transform.position = ObjectPlacementUtility.GetSpawnPoint();

            propertyData.OnCRSChanged.AddListener(UpdateCRS);
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
        private void UpdateCRS(int crs)
        {
            CoordinateSystem system = (CoordinateSystem)crs;            

            //var holgo = GetComponent<HierarchicalObjectLayerGameObject>();            
            //if (holgo.WorldTransform.Coordinate.CoordinateSystem == crs) return;

            //Coordinate newCoord = new Coordinate(system);
            //newCoord.easting = holgo.WorldTransform.Coordinate.easting;
            //newCoord.northing = holgo.WorldTransform.Coordinate.northing;
            //newCoord.height = holgo.WorldTransform.Coordinate.height;
            //holgo.WorldTransform.MoveToCoordinate(newCoord);
        }

        private void Start()
        {
            StartImport(); //called after loading properties or after setting the file path through the import adapter
        }

        private void StartImport()
        {
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

            var json = File.ReadAllText(file);
            GetComponent<CityJson.Structure.CityJSON>().ParseCityJSON(json);
            OnObjImported();
        }

        private void OnObjImported()
        {
            var holgo = GetComponent<HierarchicalObjectLayerGameObject>();

            if (holgo.TransformIsSetFromProperty) //use transform property if it is set
            {
                var transformPropterty = (TransformLayerPropertyData)((ILayerWithPropertyData)holgo).PropertyData;
                holgo.WorldTransform.MoveToCoordinate(transformPropterty.Position);
            }
            else //transform property is not set, we need to set it if it is georeferenced, if not, we just keep the position it was at.
            {
                if (transform.childCount > 0)
                {
                    var referencePosition = transform.GetChild(0).localPosition;
                    if (EPSG7415.IsValid(referencePosition.x, referencePosition.y, referencePosition.z, out var origin))
                    {
                        PositionGeoReferencedCityJson(holgo, origin);
                    }
                }
            }

            if (addMeshCollidersToCityObjects)
            {
                foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
                {
                    meshFilter.gameObject.AddComponent<MeshCollider>();
                }
            }
            // Object is loaded / replaced - trigger the application of styling
            holgo.ApplyStyling();
        }
        

        private void PositionGeoReferencedCityJson(HierarchicalObjectLayerGameObject holgo, Coordinate origin)
        {
            if (!holgo.TransformIsSetFromProperty) //move the camera only if this is is a user imported object, not if this is a project import. We know this because a project import has its Transform property set.
            {
                var cameraMover = Camera.main.GetComponent<MoveCameraToCoordinate>();
                cameraMover.LookAtTarget(origin, cameraDistanceFromGeoReferencedObject); //move the camera to the georeferenced position, this also shifts the origin if needed.
            }

            holgo.WorldTransform.MoveToCoordinate(origin); //set this object to the georeferenced position, since this is the correct position.

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

        private void OnDestroy()
        {       
            propertyData.OnCRSChanged.RemoveListener(UpdateCRS);
        }
    }
}