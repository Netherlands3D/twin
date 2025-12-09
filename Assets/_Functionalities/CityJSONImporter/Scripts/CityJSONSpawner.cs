using System.Collections;
using System.Collections.Generic;
using System.IO;
using Netherlands3D.CityJson.Visualisation;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Functionalities.CityJSON
{
    [RequireComponent(typeof(CityJSONLayerGameObject))]
    public class CityJSONSpawner : MonoBehaviour, IVisualizationWithPropertyData
    {
        [SerializeField] private float cameraDistanceFromGeoReferencedObject = 150f;
        private CityJSONPropertyData propertyData = new();
        private CityJSONLayerGameObject layerGameObject;

        private TransformLayerPropertyData TransformLayerPropertyData =>
            layerGameObject.LayerData.GetProperty<TransformLayerPropertyData>();

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

        private void Awake()
        {
            layerGameObject = GetComponent<CityJSONLayerGameObject>();
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

            foreach (var co in cityJson.CityObjects)
            {
                var visualizers = co.GetComponents<CityObjectVisualizer>();
                foreach (var visualizer in visualizers)
                {
                    layerGameObject.AddFeature(visualizer);
                }
            }

            SetCityJSONPosition(cityJson);
        }

        private void SetCityJSONPosition(CityJson.Structure.CityJSON cityJson)
        {
            if (!layerGameObject.LayerData.IsNew) //use transform property if it is set
            {
                layerGameObject.WorldTransform.MoveToCoordinate(TransformLayerPropertyData.Position);
            }
            else if(cityJson.CoordinateSystem != CoordinateSystem.Undefined) //transform property is not set, we need to set it if it is georeferenced, if not, we just keep the position it was at.
            {
                var referencePosition = cityJson.AbsoluteCenter;
                var origin = new Coordinate(cityJson.CoordinateSystem, referencePosition.x, referencePosition.y, referencePosition.z);
                if (origin.IsValid())
                {
                    PositionGeoReferencedCityJson(origin);
                }
            }
        }

        private void PositionGeoReferencedCityJson(Coordinate origin)
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
                layerGameObject.WorldTransform.MoveToCoordinate(TransformLayerPropertyData.Position); //apply saved user changes to position.
            }
        }

        private string GetCityJsonPathFromPropertyData()
        {
            if (propertyData.CityJsonFile == null) return "";

            return AssetUriFactory.GetLocalPath(propertyData.CityJsonFile);
        }
    }
}