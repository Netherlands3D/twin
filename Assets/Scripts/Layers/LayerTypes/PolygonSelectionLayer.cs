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
                RecalculateLineWidth(OriginalPolygon, lineWidth);
            }
        }

        public void Initialize(List<Vector3> polygon, float polygonExtrusionHeight, Material polygonMeshMaterial, ShapeType shapeType, float defaultLineWidth = 10f)
        {
            this.ShapeType = shapeType;
            this.polygonExtrusionHeight = polygonExtrusionHeight;
            this.polygonMeshMaterial = polygonMeshMaterial;
            this.lineWidth = defaultLineWidth;

            if (shapeType == Layers.ShapeType.Line)
                SetLine(polygon);
            else
                SetPolygon(polygon);

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

        public void SetPolygon(List<Vector3> solidPolygon)
        {
            OriginalPolygon = solidPolygon;

            var flatPolygon = PolygonCalculator.FlattenPolygon(solidPolygon.ToArray(), new Plane(Vector3.up, 0));
            Polygon = new CompoundPolygon(flatPolygon);

            if (PolygonVisualisation)
                PolygonVisualisation.UpdateVisualisation(solidPolygon);
            else
                PolygonVisualisation = CreatePolygonMesh(solidPolygon, polygonExtrusionHeight, polygonMeshMaterial);

            polygonChanged.Invoke();
        }

        public void SetLine(List<Vector3> line)
        {
            OriginalPolygon = line;

            if (shapeType != ShapeType.Line)
                Debug.LogError("The polygon layer is not a line layer, this will result in unexpected behaviour");

            RecalculateLineWidth(line, lineWidth);

            if (propertySections.Count == 0)
            {
                var lineProperties = gameObject.AddComponent<PolygonPropertySectionInstantiator>();
                propertySections = new List<IPropertySectionInstantiator>() { lineProperties };
            }
            // Properties.Properties.Instance.Show(this);
        }

        private void RecalculateLineWidth(List<Vector3> line, float width)
        {
            var rectangle = PolygonFromLine(line, width);

            Polygon = new CompoundPolygon(rectangle);

            var rectangle3D = rectangle.ToVector3List();

            if (PolygonVisualisation)
                PolygonVisualisation.UpdateVisualisation(rectangle3D);
            else
                PolygonVisualisation = CreatePolygonMesh(rectangle3D, polygonExtrusionHeight, polygonMeshMaterial);
            polygonChanged.Invoke();
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

        public static PolygonVisualisation CreatePolygonMesh(List<Vector3> polygon, float polygonExtrusionHeight, Material polygonMeshMaterial)
        {
            var contours = new List<List<Vector3>> { polygon };
            var polygonVisualisation = PolygonVisualisationUtility.CreateAndReturnPolygonObject(contours, polygonExtrusionHeight, false, false, false, polygonMeshMaterial);
            polygonVisualisation.gameObject.AddComponent<PolygonShifter>();
            polygonVisualisation.gameObject.AddComponent<WorldTransform>();  
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

            if(PolygonVisualisation.gameObject)
                Destroy(PolygonVisualisation.gameObject);
        }

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return propertySections;
        }
    }
}