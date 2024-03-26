using System;
using System.Collections.Generic;
using Netherlands3D.SelectionTools;
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

    public class PolygonSelectionLayer : LayerNL3DBase
    {
        public CompoundPolygon Polygon { get; set; }
        public PolygonVisualisation PolygonVisualisation { get; private set; }

        private float polygonExtrusionHeight;
        private Material polygonMeshMaterial;
        public Material PolygonMeshMaterial => polygonMeshMaterial;

        public UnityEvent<PolygonSelectionLayer> polygonSelected = new();
        public UnityEvent polygonChanged = new();

        private ShapeType shapeType;
        public ShapeType ShapeType { get => shapeType; }
        
        private List<Vector3> originalPolygon;
        private float lineWidth = 10.0f;

        public void Initialize(List<Vector3> polygon, float polygonExtrusionHeight, Material polygonMeshMaterial, ShapeType shapeType)
        {
            this.shapeType = shapeType;
            this.polygonExtrusionHeight = polygonExtrusionHeight;
            this.polygonMeshMaterial = polygonMeshMaterial;
            originalPolygon = polygon;

            if(shapeType == ShapeType.Line)
                polygon = PolygonFromLine(polygon);

            SetPolygon(polygon);
            PolygonVisualisation.reselectVisualisedPolygon.AddListener(OnPolygonVisualisationSelected);
        }

        private void OnEnable()
        {
            ClickNothingPlane.ClickedOnNothing.AddListener(DeselectPolygon);
        }

        private void OnDisable()
        {
            ClickNothingPlane.ClickedOnNothing.RemoveListener(DeselectPolygon);
        }

        private void OnPolygonVisualisationSelected(PolygonVisualisation visualisation)
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
            var flatPolygon = PolygonCalculator.FlattenPolygon(solidPolygon.ToArray(), new Plane(Vector3.up, 0));
            var polygon = new CompoundPolygon(flatPolygon);
            Polygon = polygon;

            if (PolygonVisualisation)
                PolygonVisualisation.UpdateVisualisation(solidPolygon);
            else
                PolygonVisualisation = CreatePolygonMesh(solidPolygon, polygonExtrusionHeight, polygonMeshMaterial);
            
            polygonChanged.Invoke();
        }

        private List<Vector3> PolygonFromLine(List<Vector3> originalLine)
        {
            var polygon = new List<Vector3>();
            for (int i = 0; i < originalLine.Count; i++)
            {
                var startPoint = originalLine[i];
                var endPoint = originalLine[(i + 1) % originalLine.Count];

                var direction1 = new Vector3(endPoint.y - startPoint.y, startPoint.x - endPoint.x, 0).normalized * lineWidth;
                var direction2 = new Vector3(startPoint.y - endPoint.y, endPoint.x - startPoint.x, 0).normalized * lineWidth;

                var p1 = startPoint + direction1;
                var p2 = endPoint + direction1;
                var p3 = endPoint + direction2;
                var p4 = startPoint + direction2;

                polygon.Add(p1);
                polygon.Add(p2);
                polygon.Add(p3);
                polygon.Add(p4);
            }
            return polygon;
        }

        public static PolygonVisualisation CreatePolygonMesh(List<Vector3> polygon, float polygonExtrusionHeight, Material polygonMeshMaterial)
        {
            var contours = new List<List<Vector3>> { polygon };
            var polygonVisualisation = PolygonVisualisationUtility.CreateAndReturnPolygonObject(contours, polygonExtrusionHeight, true, false, false, polygonMeshMaterial);
            polygonVisualisation.DrawLine = false; //lines will be drawn per layer, but a single mesh will receive clicks to select
            
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
            PolygonVisualisation.reselectVisualisedPolygon.RemoveListener(OnPolygonVisualisationSelected);
            Destroy(PolygonVisualisation.gameObject);
        }
    }
}