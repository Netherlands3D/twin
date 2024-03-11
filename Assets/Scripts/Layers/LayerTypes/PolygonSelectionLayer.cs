using System;
using System.Collections.Generic;
using Netherlands3D.SelectionTools;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.UI.LayerInspector
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

        public UnityEvent<PolygonSelectionLayer> polygonSelected = new();
        public UnityEvent polygonChanged = new();

        public void Initialize(List<Vector3> solidPolygon, float polygonExtrusionHeight, Material polygonMeshMaterial)
        {
            this.polygonExtrusionHeight = polygonExtrusionHeight;
            this.polygonMeshMaterial = polygonMeshMaterial;
            SetPolygon(solidPolygon);
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
//todo: remove this, it is only for debugging
ScatterMap.Instance.GenerateScatterPoints(Polygon, 0.01f, 0, 0);
        }

        public static PolygonVisualisation CreatePolygonMesh(List<Vector3> polygon, float polygonExtrusionHeight, Material polygonMeshMaterial)
        {
            var contours = new List<List<Vector3>> { polygon };
            var polygonVisualisation = PolygonVisualisationUtility.CreateAndReturnPolygonObject(contours, polygonExtrusionHeight, true, false, false, polygonMeshMaterial);
            polygonVisualisation.DrawLine = false; //lines will be drawn per layer, but a single mesh will receive clicks to select
            
            polygonVisualisation.gameObject.layer = LayerMask.NameToLayer("Polygons");
            // PolygonVisualisation.transform.SetParent(transform);
            return polygonVisualisation;
        }

        protected override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            print("setting active: " + activeInHierarchy);
            PolygonVisualisation.gameObject.SetActive(activeInHierarchy);
            // throw new System.NotImplementedException();
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