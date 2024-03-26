using System.Collections.Generic;
using Netherlands3D.SelectionTools;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public class PolygonInputToLayer : MonoBehaviour
    {
        [Header("Polygon settings")] [SerializeField]
        private float polygonExtrusionHeight = 0.1f;

        [SerializeField] private Material polygonMeshMaterial;

        private Dictionary<PolygonVisualisation, PolygonSelectionLayer> layers = new();
        private PolygonSelectionLayer activeLayer;

        [SerializeField] private PolygonInput polygonInput;

        [Header("Line settings")] 
        [SerializeField] private PolygonInput lineInput;
        [SerializeField] private float defaultLineWidth = 10.0f;


        private void OnEnable()
        {
            polygonInput.createdNewPolygonArea.AddListener(CreatePolygonLayer);
            polygonInput.editedPolygonArea.AddListener(UpdatePolygonLayer);

            lineInput.createdNewPolygonArea.AddListener(CreateLineLayer);
            lineInput.editedPolygonArea.AddListener(UpdateLineLayer);
        }

        private void OnDisable()
        {
            polygonInput.createdNewPolygonArea.RemoveListener(CreatePolygonLayer);
            polygonInput.editedPolygonArea.RemoveListener(UpdatePolygonLayer);

            lineInput.createdNewPolygonArea.RemoveListener(CreateLineLayer);
            lineInput.editedPolygonArea.RemoveListener(UpdateLineLayer);
        }

        public void CreatePolygonLayer(List<Vector3> polygon)
        {
            var newObject = new GameObject("Polygon");
            var layerComponent = newObject.AddComponent<PolygonSelectionLayer>();
            layerComponent.Initialize(polygon, polygonExtrusionHeight, polygonMeshMaterial, ShapeType.Polygon);
            layers.Add(layerComponent.PolygonVisualisation, layerComponent);
            layerComponent.polygonSelected.AddListener(ProcessPolygonSelection);

            activeLayer = layerComponent;
        }
        public void UpdatePolygonLayer(List<Vector3> editedPolygon)
        {
            activeLayer.SetPolygon(editedPolygon);
        }

        public void CreateLineLayer(List<Vector3> line)
        {
            var newObject = new GameObject("Line");
            var layerComponent = newObject.AddComponent<PolygonSelectionLayer>();
            layerComponent.Initialize(line, polygonExtrusionHeight, polygonMeshMaterial, ShapeType.Line);
            layers.Add(layerComponent.PolygonVisualisation, layerComponent);
            layerComponent.polygonSelected.AddListener(ProcessPolygonSelection);

            activeLayer = layerComponent;
        }
        public void UpdateLineLayer(List<Vector3> editedLine)
        {
            activeLayer.SetPolygon(editedLine);
        }

        private void ProcessPolygonSelection(PolygonSelectionLayer layer)
        {
            //Do not allow selecting a new polygon if we are still creating one
            if (polygonInput.Mode == PolygonInput.DrawMode.Create || lineInput.Mode == PolygonInput.DrawMode.Create)
                return;

            activeLayer = layer;
            if (layer)
            {
                if(layer.ShapeType == ShapeType.Polygon)
                {
                    polygonInput.ReselectPolygon(layer.Polygon.SolidPolygon.ToVector3List());

                    polygonInput.gameObject.SetActive(true);
                    lineInput.gameObject.SetActive(false);
                }
                else if(layer.ShapeType == ShapeType.Line)
                {
                    lineInput.ReselectPolygon(layer.Polygon.SolidPolygon.ToVector3List());

                    lineInput.gameObject.SetActive(true);
                    polygonInput.gameObject.SetActive(false);
                }
                return;
            }
            
            //Clear inputs if no layer is selected by default
            var emptyList = new List<Vector3>();
            polygonInput.ReselectPolygon(emptyList);
            lineInput.ReselectPolygon(emptyList);
        }

        public void SetPolygonInputModeToCreate(bool isCreateMode)
        {
            if(activeLayer)
                activeLayer.DeselectPolygon(); 
            
            polygonInput.SetDrawMode(isCreateMode ? PolygonInput.DrawMode.Create : PolygonInput.DrawMode.Edit);
        }

        public void SetLineInputModeToCreate(bool isCreateMode)
        {
            if(activeLayer)
                activeLayer.DeselectPolygon(); 
            
            lineInput.SetDrawMode(isCreateMode ? PolygonInput.DrawMode.Create : PolygonInput.DrawMode.Edit);
        }
    }
}