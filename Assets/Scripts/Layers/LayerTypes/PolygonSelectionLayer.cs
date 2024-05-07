using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.PlayerLoop;

namespace Netherlands3D.Twin.Layers
{
    public enum ShapeType
    {
        Undefined = 0,
        Polygon = 1,
        Line = 2
    }

    public class PolygonSelectionLayer : LayerNL3DBase, ILayerWithProperties
    {
        private ShapeType shapeType;
        public CompoundPolygon Polygon { get; set; }
        public PolygonVisualisation PolygonVisualisation { get; private set; }

        private float polygonExtrusionHeight;
        private Material polygonMeshMaterial;
        public Material PolygonMeshMaterial => polygonMeshMaterial;

        public UnityEvent<PolygonSelectionLayer> polygonSelected = new();
        public UnityEvent polygonChanged = new();
        private bool notifyOnPolygonChange = true;
        
        private List<IPropertySectionInstantiator> propertySections = new();
        
        public ShapeType ShapeType
        {
            get => shapeType;
            set => shapeType = value;
        }

        public List<Vector3> OriginalPolygon;
        private float lineWidth;

        public float LineWidth
        {
            get => lineWidth;
            set
            {
                lineWidth = value;
                CreatePolygonFromLine(OriginalPolygon, lineWidth);
            }
        }

        public void Initialize(List<Vector3> polygon, float polygonExtrusionHeight, Material polygonMeshMaterial, ShapeType shapeType, float defaultLineWidth = 10f)
        {
            this.ShapeType = shapeType;
            this.polygonExtrusionHeight = polygonExtrusionHeight;
            this.polygonMeshMaterial = polygonMeshMaterial;
            this.lineWidth = defaultLineWidth;

            SetShape(polygon);

            PolygonSelectionCalculator.RegisterPolygon(this);
        }

        protected override void Start()
        {
            base.Start();
            if (shapeType == ShapeType.Line)
                UI.ToggleProperties(true); //start with the properties section opened. this is done in Start, because we need to wait for the UI to initialize in base.Start()
        }

        
        public void SelectPolygon()
        {
            if (UI)
                UI.Select(!LayerUI.SequentialSelectionModifierKeyIsPressed() && !LayerUI.AddToSelectionModifierKeyIsPressed()); //if there is no UI, this will do nothing. this is intended as when the layer panel is closed the polygon should not be (accidentally) selectable
        }

        public void DeselectPolygon()
        {
            if (UI && UI.IsSelected)
                UI.Deselect(); // processes OnDeselect as well
            else
                OnDeselect(); // only call this if the UI does not exist. This should not happen with the intended behaviour being that polygon selection is only active when the layer panel is open
        }

        public void SetShapesWithoutNotify(List<List<Vector3>> shapes)
        {
            //Hide handles if polygon was shifted
            notifyOnPolygonChange = false;
            DeselectPolygon();
            SetShapes(shapes);
            notifyOnPolygonChange = true;
        }

        /// <summary>
        /// Set the shape of the polygon layer
        /// </summary>
        /// <param name="shapes">Nested Vector3 list where first index [0] is used as contour</param>
        public void SetShapes(List<List<Vector3>> shapes)
        {
            if(shapes.Count == 0)
                return;      
                
            SetShape(shapes[0]);
        }

        /// <summary>
        /// Sets the contour causing update of Line or Polygon, based on chosen ShapeType
        /// </summary>
        /// <param name="shape">Contour</param>
        public void SetShape(List<Vector3> shape)
        {
             if (shapeType == Layers.ShapeType.Line)
                SetLine(shape);
            else
                SetPolygon(shape);
        }

        /// <summary>
        /// Set the polygon of the layer as a solid filled polygon
        /// </summary>
        private void SetPolygon(List<Vector3> solidPolygon)
        {
            ShapeType = ShapeType.Polygon;
            OriginalPolygon = solidPolygon;

            var flatPolygon = PolygonCalculator.FlattenPolygon(solidPolygon.ToArray(), new Plane(Vector3.up, 0));
            Polygon = new CompoundPolygon(flatPolygon);

            if(notifyOnPolygonChange){
                UpdateVisualisation(solidPolygon);
                polygonChanged.Invoke();
            }
        }
       
       /// <summary>
       /// Set the layer as a 'line'. This will create a rectangle polygon from the line with a given width.
       /// </summary>
        private void SetLine(List<Vector3> line)
        {
            ShapeType = ShapeType.Line;
            OriginalPolygon = line;

            CreatePolygonFromLine(line, lineWidth);

            if (propertySections.Count == 0)
            {
                var lineProperties = gameObject.AddComponent<PolygonPropertySectionInstantiator>();
                propertySections = new List<IPropertySectionInstantiator>() { lineProperties };
            }
        }

        private void CreatePolygonFromLine(List<Vector3> line, float width)
        {
            var rectangle = PolygonFromLine(line, width);

            Polygon = new CompoundPolygon(rectangle);

            var rectangle3D = rectangle.ToVector3List();
            
            if(notifyOnPolygonChange)
            {
                UpdateVisualisation(rectangle3D);
                polygonChanged.Invoke();
            }
        }

        private Vector2[] PolygonFromLine(List<Vector3> originalLine, float width)
        {
            if (originalLine.Count != 2)
            {
                Debug.LogError("cannot create rectangle because position list contains more than 2 entries");
                return null;
            }

            var worldPlane = new Plane(Vector3.up, 0); //todo: work with terrain height
            var flatPolygon = PolygonCalculator.FlattenPolygon(originalLine, worldPlane);
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
            var polygonShifter = polygonVisualisation.gameObject.AddComponent<PolygonShifter>();
            polygonVisualisation.gameObject.AddComponent<WorldTransform>(); 
            polygonShifter.polygonShifted.AddListener(SetShapesWithoutNotify);

            polygonVisualisation.DrawLine = false; //lines will be drawn per layer, but a single mesh will receive clicks to select
            polygonVisualisation.gameObject.layer = LayerMask.NameToLayer("ScatterPolygons");

            return polygonVisualisation;
        }

        protected override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            print("setting active: " + activeInHierarchy);
            PolygonVisualisation.gameObject.SetActive(activeInHierarchy);
        }

        public override void OnSelect()
        {
            base.OnSelect();
            polygonSelected.Invoke(this);
        }

        public override void OnDeselect()
        {
            base.OnDeselect();
            polygonSelected.Invoke(null);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            PolygonSelectionCalculator.UnregisterPolygon(this);

            if(PolygonVisualisation)
                Destroy(PolygonVisualisation.gameObject);
        }

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return propertySections;
        }
    }
}