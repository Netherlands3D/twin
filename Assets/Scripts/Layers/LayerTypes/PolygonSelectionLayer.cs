using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers
{
    public enum ShapeType
    {
        Undefined = 0,
        Polygon = 1,
        Line = 2
    }

    [Serializable]
    public class PolygonSelectionLayer : ReferencedLayerData, ILayerWithPropertyData//, ILayerWithPropertyPanels
    {
        [JsonProperty] public List<Coordinate> OriginalPolygon { get; private set; }
        [SerializeField, JsonProperty] private ShapeType shapeType;
        [JsonIgnore] private PolygonSelectionLayerPropertyData polygonPropertyData;
        [JsonIgnore] public LayerPropertyData PropertyData => polygonPropertyData;
        [JsonIgnore] public CompoundPolygon Polygon { get; set; }
        [JsonIgnore] public UnityEvent<PolygonSelectionLayer> polygonSelected = new();
        [JsonIgnore] public UnityEvent polygonMoved = new();
        [JsonIgnore] public UnityEvent polygonChanged = new();
        [JsonIgnore] private bool notifyOnPolygonChange = true;

        [JsonIgnore]
        public ShapeType ShapeType
        {
            get => shapeType;
            set => shapeType = value;
        }

        [JsonIgnore]
        public float LineWidth
        {
            get => polygonPropertyData.LineWidth;
            set
            {
                polygonPropertyData.LineWidth = value;
                CreatePolygonFromLine(OriginalPolygon, value);
            }
        }

        [JsonIgnore] public PolygonSelectionVisualisation PolygonVisualisation => Reference as PolygonSelectionVisualisation;

        [JsonConstructor]
        public PolygonSelectionLayer(string name, string prefabId, List<LayerPropertyData> layerProperties, List<Coordinate> originalPolygon, ShapeType shapeType) : base(name, prefabId, layerProperties)
        {
            OriginalPolygon = originalPolygon;
            ShapeType = shapeType;

            LoadProperties(layerProperties);
            PolygonSelectionCalculator.RegisterPolygon(this);

            //Add shifter that manipulates the polygon if the world origin is shifted
            Origin.current.onPostShift.AddListener(ShiftedPolygon);
            LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged);
        }

        public PolygonSelectionLayer(string name, string prefabId, List<Vector3> polygonUnityInput, ShapeType shapeType, float defaultLineWidth = 10f) : base(name, prefabId, new List<LayerPropertyData>())
        {
            polygonPropertyData = new PolygonSelectionLayerPropertyData();
            AddProperty(polygonPropertyData);
            ShapeType = shapeType;

            var coordinates = ConvertToCoordinates(polygonUnityInput);
            SetShape(coordinates);
            PolygonSelectionCalculator.RegisterPolygon(this);

            //Add shifter that manipulates the polygon if the world origin is shifted
            Origin.current.onPostShift.AddListener(ShiftedPolygon);
            LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged);
        }

        private static List<LayerPropertyData> CreateNewProperties()
        {
            var properties = new List<LayerPropertyData>(1);
            properties.Add(new PolygonSelectionLayerPropertyData());
            return properties;
        }

        ~PolygonSelectionLayer()
        {
            LayerActiveInHierarchyChanged.RemoveListener(OnLayerActiveInHierarchyChanged);
            Origin.current.onPostShift.RemoveListener(ShiftedPolygon);
        }

        private void ShiftedPolygon(Coordinate fromOrigin, Coordinate toOrigin)
        {
            //Silent update of the polygon shape, so the visualisation is updated without notifying the listeners
            notifyOnPolygonChange = false;
            SetShape(OriginalPolygon);
            polygonMoved.Invoke();
            notifyOnPolygonChange = true;
        }

        /// <summary>
        /// Sets the contour causing update of Line or Polygon, based on chosen ShapeType
        /// </summary>
        /// <param name="unityShape">Contour</param>
        public void SetShape(List<Coordinate> shape)
        {
            if (shapeType == ShapeType.Line)
                SetLine(shape);
            else
                SetPolygon(shape);
        }

        /// <summary>
        /// Set the polygon of the layer as a solid filled polygon with Coordinates
        /// </summary>
        private void SetPolygon(List<Coordinate> solidPolygon)
        {
            ShapeType = ShapeType.Polygon;
            OriginalPolygon = solidPolygon;

            var unityPolygon = ConvertToUnityPoints(solidPolygon);
            var flatPolygon = PolygonCalculator.FlattenPolygon(unityPolygon, new Plane(Vector3.up, 0));
            Polygon = new CompoundPolygon(flatPolygon);
            PolygonVisualisation.UpdateVisualisation(flatPolygon, polygonPropertyData.ExtrusionHeight);

            if (notifyOnPolygonChange)
            {
                polygonChanged.Invoke();
            }
        }

        /// <summary>
        /// Set the layer as a 'line' with Coordinates. This will create a rectangle polygon from the line with a given width.
        /// </summary>
        private void SetLine(List<Coordinate> line)
        {
            ShapeType = ShapeType.Line;
            OriginalPolygon = line;
            CreatePolygonFromLine(line, polygonPropertyData.LineWidth);
        }

        private void CreatePolygonFromLine(List<Coordinate> line, float width)
        {
            var rectangle = PolygonFromLine(line, width);
            Polygon = new CompoundPolygon(rectangle);
            PolygonVisualisation.UpdateVisualisation(rectangle, polygonPropertyData.ExtrusionHeight);

            if (notifyOnPolygonChange)
            {
                polygonChanged.Invoke();
            }
        }

        private static Vector2[] PolygonFromLine(List<Coordinate> originalLine, float width)
        {
            if (originalLine.Count != 2)
            {
                Debug.LogError("cannot create rectangle because position list contains more than 2 entries");
                return null;
            }

            var worldPlane = new Plane(Vector3.up, 0); //todo: work with terrain height
            var unityLine = ConvertToUnityPoints(originalLine);
            var flatPolygon = PolygonCalculator.FlattenPolygon(unityLine, worldPlane);
            var dir = flatPolygon[1] - flatPolygon[0];
            var normal = new Vector2(-dir.y, dir.x).normalized;

            var dist = normal * width / 2;

            var point1 = flatPolygon[0] + new Vector2(dist.x, dist.y);
            var point4 = flatPolygon[1] + new Vector2(dist.x, dist.y);
            var point3 = flatPolygon[1] - new Vector2(dist.x, dist.y);
            var point2 = flatPolygon[0] - new Vector2(dist.x, dist.y);

            var polygon = new Vector2[]
            {
                point1,
                point2,
                point3,
                point4
            };

            return polygon;
        }

        private void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            PolygonVisualisation.gameObject.SetActive(activeInHierarchy);
        }

        public override void SelectLayer(bool deselectOthers = false)
        {
            base.SelectLayer(deselectOthers);
            polygonSelected.Invoke(this);
        }

        public override void DeselectLayer()
        {
            base.DeselectLayer();
            polygonSelected.Invoke(null);
        }

        public override void DestroyLayer()
        {
            PolygonSelectionCalculator.UnregisterPolygon(this);
            base.DestroyLayer();
        }

        public List<Vector3> GetPolygonAsUnityPoints()
        {
            return ConvertToUnityPoints(OriginalPolygon);
        }

        public static List<Coordinate> ConvertToCoordinates(List<Vector3> unityCoordinates)
        {
            var coordList = new List<Coordinate>(unityCoordinates.Capacity);
            var coord = new Coordinate((int)CoordinateSystem.Unity); //cache variable to minimize garbage collection
            foreach (var point in unityCoordinates)
            {
                coord.Points = new double[] { point.x, point.y, point.z };
                coordList.Add(coord);
            }

            return coordList;
        }

        public static List<Vector3> ConvertToUnityPoints(List<Coordinate> coordinateList)
        {
            var pointList = new List<Vector3>(coordinateList.Capacity);
            foreach (var coord in coordinateList)
            {
                pointList.Add(coord.ToUnity());
            }

            return pointList;
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var polygonProperty = (PolygonSelectionLayerPropertyData)properties.FirstOrDefault(p => p is PolygonSelectionLayerPropertyData);
            if (polygonProperty != null)
            {
                polygonPropertyData = polygonProperty; //take existing property to overwrite the unlinked one of this class
                SetShape(OriginalPolygon); //initialize the shape again with properties (use shape instead of setLine to ensure polygon is also 
            }
        }
    }
}