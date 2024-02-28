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

        public void Initialize(List<Vector3> solidPolygon, float polygonExtrusionHeight, Material polygonMeshMaterial)
        {
            this.polygonExtrusionHeight = polygonExtrusionHeight;
            this.polygonMeshMaterial = polygonMeshMaterial;
            SetPolygon(solidPolygon);
            PolygonVisualisation.reselectVisualisedPolygon.AddListener(OnPolygonVisualisationSelected);
        }

        private void OnPolygonVisualisationSelected(PolygonVisualisation visualisation)
        {
            if(UI)
                UI.Select(!LayerUI.SequentialSelectionModifierKeyIsPressed() && !LayerUI.AddToSelectionModifierKeyIsPressed()); //if there is no UI, this will do nothing. this is intended as when the layer panel is closed the polygon should not be (accidentally) selectable
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
        }

        public static PolygonVisualisation CreatePolygonMesh(List<Vector3> polygon, float polygonExtrusionHeight, Material polygonMeshMaterial)
        {
            var contours = new List<List<Vector3>> { polygon };
            var polygonVisualisation = PolygonVisualisationUtility.CreateAndReturnPolygonObject(contours, polygonExtrusionHeight, true, false, false, polygonMeshMaterial);
            polygonVisualisation.DrawLine = false; //lines will be drawn per layer, but a single mesh will receive clicks to select
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
            polygonSelected.Invoke(null);
        }
    }
}