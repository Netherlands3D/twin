using System.Collections;
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

namespace Netherlands3D.Functionalities.CityJSON
{
    public class CityJSONSpawner : MonoBehaviour, ILayerWithPropertyData
    {
        [SerializeField] private float cameraDistanceFromGeoReferencedObject = 150f;
        [SerializeField] private bool addMeshCollidersToCityObjects = true;
        private CityJSONPropertyData propertyData = new();
        public LayerPropertyData PropertyData => propertyData;

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var propertyData = properties.Get<CityJSONPropertyData>();
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
            var cityJson = GetComponent<CityJson.Structure.CityJSON>();
            cityJson.ParseCityJSON(json);
            OnCityJSONImported(cityJson);
        }

        private void OnCityJSONImported(CityJson.Structure.CityJSON cityJson)
        {
            var layerGameObject = GetComponent<HierarchicalObjectLayerGameObject>();

            if (!layerGameObject.LayerData.IsNew) //use transform property if it is set
            {
                var transformProperty = layerGameObject.LayerData.GetProperty<TransformLayerPropertyData>();
                layerGameObject.WorldTransform.MoveToCoordinate(transformProperty.Position);
            }
            else if (transform.childCount > 0)
            {
                // transform property is not set, we need to set it if it is georeferenced, if not, we just keep the position it was at.
                var referencePosition = cityJson.AbsoluteCenter;
                if (EPSG7415.IsValid(referencePosition.x, referencePosition.y, referencePosition.z, out var origin))
                {
                    PositionGeoReferencedCityJson(layerGameObject, origin);
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
            layerGameObject.ApplyStyling();
        }
        

        private void PositionGeoReferencedCityJson(HierarchicalObjectLayerGameObject layerGameObject, Coordinate origin)
        {
            if (layerGameObject.LayerData.IsNew) //move the camera only if this is is a user imported object, not if this is a project import. We know this because a project import has its Transform property set.
            {
                var cameraMover = Camera.main.GetComponent<MoveCameraToCoordinate>();
                cameraMover.LookAtTarget(origin, cameraDistanceFromGeoReferencedObject); //move the camera to the georeferenced position, this also shifts the origin if needed.
            }

            layerGameObject.WorldTransform.MoveToCoordinate(origin); //set this object to the georeferenced position, since this is the correct position.

            // imported object should stay where it is initially, and only then apply any user transformations if present.
            if (!layerGameObject.LayerData.IsNew)
            {
                var transformProperty = layerGameObject.LayerData.GetProperty<TransformLayerPropertyData>();
                layerGameObject.WorldTransform.MoveToCoordinate(transformProperty.Position); //apply saved user changes to position.
            }
        }

        private string GetCityJsonPathFromPropertyData()
        {
            if (propertyData.CityJsonFile == null) return "";

            return AssetUriFactory.GetLocalPath(propertyData.CityJsonFile);
        }
    }
}