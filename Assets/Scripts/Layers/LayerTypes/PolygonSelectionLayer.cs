using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.LayerTypes;
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
    public class PolygonSelectionLayer : LayerData, ILayerWithPropertyPanels
    {
        [SerializeField, JsonProperty] private ShapeType shapeType;
        [JsonIgnore] public CompoundPolygon Polygon { get; set; }
        [JsonIgnore] public PolygonVisualisation PolygonVisualisation { get; private set; }

        [SerializeField, JsonProperty] private float polygonExtrusionHeight;
        [JsonIgnore] private Material polygonMeshMaterial;
        [JsonIgnore] public Material PolygonMeshMaterial => polygonMeshMaterial;

        [JsonIgnore] public UnityEvent<PolygonSelectionLayer> polygonSelected = new();
        [JsonIgnore] public UnityEvent polygonMoved = new();
        [JsonIgnore] public UnityEvent polygonChanged = new();
        [JsonIgnore] private bool notifyOnPolygonChange = true;

        [JsonIgnore] private List<IPropertySectionInstantiator> propertySections = new();

        // [JsonIgnore] private PolygonWorldTransformShifter worldTransformShifter;

        [JsonIgnore]
        public ShapeType ShapeType
        {
            get => shapeType;
            set => shapeType = value;
        }

        [JsonProperty] public List<Coordinate> OriginalPolygon { get; private set; }
        [SerializeField, JsonProperty] private float lineWidth;

        [JsonIgnore]
        public float LineWidth
        {
            get => lineWidth;
            set
            {
                lineWidth = value;
                CreatePolygonFromLine(OriginalPolygon, lineWidth);
            }
        }

        [JsonConstructor]
        public PolygonSelectionLayer(string name, List<Coordinate> originalPolygon, ShapeType shapeType, float polygonExtrusionHeight, float lineWidth): base(name)
        {
            this.ShapeType = shapeType;
            this.polygonExtrusionHeight = polygonExtrusionHeight;
            
            SetShape(originalPolygon);
            PolygonSelectionCalculator.RegisterPolygon(this);
            ProjectData.Current.AddStandardLayer(this);
            
            //Add shifter that manipulates the polygon if the world origin is shifted
            PolygonVisualisation.gameObject.AddComponent<GameObjectWorldTransformShifter>();
            // worldTransformShifter.polygonSelectionLayer = this;
            PolygonVisualisation.gameObject.AddComponent<WorldTransform>();
            // Origin.current.onPreShift.AddListener(PrepareToShift);
            Origin.current.onPostShift.AddListener(ShiftedPolygon);
            // worldTransformShifter.polygonShifted.AddListener(ShiftedPolygon);
            
            LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged);
        }

        public PolygonSelectionLayer(string name, List<Vector3> polygonUnityInput, float polygonExtrusionHeight, Material polygonMeshMaterial, ShapeType shapeType, float defaultLineWidth = 10f) : base(name)
        {

            this.ShapeType = shapeType;
            this.polygonExtrusionHeight = polygonExtrusionHeight;
            this.polygonMeshMaterial = polygonMeshMaterial;
            this.lineWidth = defaultLineWidth;

            var coordinates = ConvertToCoordinates(polygonUnityInput);
            SetShape(coordinates);
            PolygonSelectionCalculator.RegisterPolygon(this);
            ProjectData.Current.AddStandardLayer(this);
            
            //Add shifter that manipulates the polygon if the world origin is shifted
            PolygonVisualisation.gameObject.AddComponent<GameObjectWorldTransformShifter>();
            // worldTransformShifter.polygonSelectionLayer = this;
            PolygonVisualisation.gameObject.AddComponent<WorldTransform>();
            // worldTransformShifter.polygonShifted.AddListener(ShiftedPolygon);
            Origin.current.onPostShift.AddListener(ShiftedPolygon);

            LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged);
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
            if (shapeType == Layers.ShapeType.Line)
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

            UpdateVisualisation(unityPolygon);

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

            CreatePolygonFromLine(line, lineWidth);

            if (propertySections.Count == 0)
            {
                var lineProperties = PolygonVisualisation.gameObject.AddComponent<PolygonPropertySectionInstantiator>();
                lineProperties.PolygonLayer = this;
                propertySections = new List<IPropertySectionInstantiator>() { lineProperties };
            }        
        }

        private void CreatePolygonFromLine(List<Coordinate> line, float width)
        {
            var rectangle = PolygonFromLine(line, width);

            Polygon = new CompoundPolygon(rectangle);

            var rectangle3D = rectangle.ToVector3List();
            UpdateVisualisation(rectangle3D);

            if (notifyOnPolygonChange)
            {
                polygonChanged.Invoke();
            }
        }

        private Vector2[] PolygonFromLine(List<Coordinate> originalLine, float width)
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

        /// <summary>
        /// Create or update PolygonVisualisation
        /// </summary>
        private void UpdateVisualisation(List<Vector3> newPolygon)
        {
            if (!PolygonVisualisation)
                PolygonVisualisation = CreatePolygonMesh(newPolygon, polygonExtrusionHeight, polygonMeshMaterial);
            else
                PolygonVisualisation.UpdateVisualisation(newPolygon);
        }

        public PolygonVisualisation CreatePolygonMesh(List<Vector3> polygon, float polygonExtrusionHeight, Material polygonMeshMaterial)
        {
            var contours = new List<List<Vector3>> { polygon };
            var polygonVisualisation = PolygonVisualisationUtility.CreateAndReturnPolygonObject(contours, polygonExtrusionHeight, false, false, false, polygonMeshMaterial);

            //Add the polygon shifter to the polygon visualisation, so it can move with our origin shifts
            polygonVisualisation.DrawLine = false; //lines will be drawn per layer, but a single mesh will receive clicks to select
            polygonVisualisation.gameObject.layer = LayerMask.NameToLayer("Projected");

            return polygonVisualisation;
        }

        private void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            PolygonVisualisation.gameObject.SetActive(activeInHierarchy);
        }

        public override void SelectLayer(bool deselectOthers = false)
        {
            base.SelectLayer();
            polygonSelected.Invoke(this);
        }

        public override void DeselectLayer()
        {
            base.DeselectLayer();
            polygonSelected.Invoke(null);
        }
        
        public override void DestroyLayer()
        {
            base.DestroyLayer();
            PolygonSelectionCalculator.UnregisterPolygon(this);

            if (PolygonVisualisation)
                GameObject.Destroy(PolygonVisualisation.gameObject);
        }

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return propertySections;
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
                coord.Points= new double[]{point.x, point.y, point.z};
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
    }
}